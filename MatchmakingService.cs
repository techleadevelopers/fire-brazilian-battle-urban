
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using PlayFab;
using PlayFab.MultiplayerModels;
using ArenaBrasil.Networking.Client;

namespace ArenaBrasil.Services
{
    public class MatchmakingService : MonoBehaviour
    {
        public static MatchmakingService Instance { get; private set; }
        
        [Header("Matchmaking Configuration")]
        public string matchmakingQueue = "arena_brasil_queue";
        public int maxPlayersPerMatch = 60;
        public float matchmakingTimeout = 120f; // 2 minutes
        
        // Matchmaking state
        private bool isSearching = false;
        private float searchStartTime;
        private string ticketId;
        
        // Events
        public event Action OnMatchmakingStarted;
        public event Action<string> OnMatchFound;
        public event Action<string> OnMatchmakingFailed;
        public event Action OnMatchmakingCancelled;
        public event Action<float> OnMatchmakingProgress;
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void Update()
        {
            if (isSearching)
            {
                UpdateMatchmakingProgress();
                CheckMatchmakingTimeout();
            }
        }
        
        public async void StartMatchmaking(MatchmakingPlayerAttributes playerAttributes = null)
        {
            if (isSearching)
            {
                Debug.LogWarning("Matchmaking already in progress");
                return;
            }
            
            Debug.Log("Arena Brasil - Starting matchmaking");
            
            isSearching = true;
            searchStartTime = Time.time;
            OnMatchmakingStarted?.Invoke();
            
            try
            {
                var request = new CreateMatchmakingTicketRequest
                {
                    Creator = new MatchmakingPlayer
                    {
                        Entity = new PlayFab.MultiplayerModels.EntityKey
                        {
                            Id = PlayFabSettings.staticPlayer.EntityId,
                            Type = PlayFabSettings.staticPlayer.EntityType
                        },
                        Attributes = playerAttributes ?? GetDefaultPlayerAttributes()
                    },
                    QueueName = matchmakingQueue,
                    GiveUpAfterSeconds = (int)matchmakingTimeout
                };
                
                var result = await ExecutePlayFabRequest<CreateMatchmakingTicketRequest, CreateMatchmakingTicketResult>(
                    request, PlayFabMultiplayerAPI.CreateMatchmakingTicketAsync);
                
                if (result != null)
                {
                    ticketId = result.TicketId;
                    Debug.Log($"Matchmaking ticket created: {ticketId}");
                    PollMatchmakingStatus();
                }
                else
                {
                    FailMatchmaking("Failed to create matchmaking ticket");
                }
            }
            catch (Exception e)
            {
                FailMatchmaking($"Matchmaking error: {e.Message}");
            }
        }
        
        async void PollMatchmakingStatus()
        {
            while (isSearching && !string.IsNullOrEmpty(ticketId))
            {
                try
                {
                    var request = new GetMatchmakingTicketRequest
                    {
                        TicketId = ticketId,
                        QueueName = matchmakingQueue
                    };
                    
                    var result = await ExecutePlayFabRequest<GetMatchmakingTicketRequest, GetMatchmakingTicketResult>(
                        request, PlayFabMultiplayerAPI.GetMatchmakingTicketAsync);
                    
                    if (result != null)
                    {
                        switch (result.Status)
                        {
                            case "Matched":
                                OnMatchmakingSuccess(result.MatchId);
                                return;
                            case "Failed":
                            case "Cancelled":
                                FailMatchmaking($"Matchmaking {result.Status.ToLower()}");
                                return;
                            case "WaitingForPlayers":
                                Debug.Log("Waiting for more players...");
                                break;
                        }
                    }
                    
                    await Task.Delay(2000); // Poll every 2 seconds
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error polling matchmaking status: {e.Message}");
                    await Task.Delay(5000); // Wait longer on error
                }
            }
        }
        
        void OnMatchmakingSuccess(string matchId)
        {
            Debug.Log($"Arena Brasil - Match found: {matchId}");
            
            isSearching = false;
            ticketId = null;
            
            OnMatchFound?.Invoke(matchId);
            
            // Connect to match server
            ConnectToMatch(matchId);
        }
        
        async void ConnectToMatch(string matchId)
        {
            try
            {
                var request = new GetMatchRequest
                {
                    MatchId = matchId,
                    QueueName = matchmakingQueue
                };
                
                var result = await ExecutePlayFabRequest<GetMatchRequest, GetMatchResult>(
                    request, PlayFabMultiplayerAPI.GetMatchAsync);
                
                if (result != null && result.ServerDetails != null)
                {
                    string serverIP = result.ServerDetails.IPV4Address;
                    int serverPort = result.ServerDetails.Ports[0].Num;
                    
                    Debug.Log($"Connecting to server: {serverIP}:{serverPort}");
                    
                    // Connect via NetworkManagerClient
                    if (NetworkManagerClient.Instance != null)
                    {
                        NetworkManagerClient.Instance.ConnectToGameServer(serverIP, (ushort)serverPort);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to connect to match: {e.Message}");
                FailMatchmaking("Failed to connect to game server");
            }
        }
        
        public void CancelMatchmaking()
        {
            if (!isSearching)
            {
                Debug.LogWarning("No matchmaking in progress to cancel");
                return;
            }
            
            Debug.Log("Arena Brasil - Cancelling matchmaking");
            
            if (!string.IsNullOrEmpty(ticketId))
            {
                CancelMatchmakingTicket();
            }
            
            isSearching = false;
            ticketId = null;
            OnMatchmakingCancelled?.Invoke();
        }
        
        async void CancelMatchmakingTicket()
        {
            try
            {
                var request = new CancelMatchmakingTicketRequest
                {
                    TicketId = ticketId,
                    QueueName = matchmakingQueue
                };
                
                await ExecutePlayFabRequest<CancelMatchmakingTicketRequest, CancelMatchmakingTicketResult>(
                    request, PlayFabMultiplayerAPI.CancelMatchmakingTicketAsync);
                
                Debug.Log("Matchmaking ticket cancelled successfully");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to cancel matchmaking ticket: {e.Message}");
            }
        }
        
        void FailMatchmaking(string reason)
        {
            Debug.LogError($"Arena Brasil - Matchmaking failed: {reason}");
            
            isSearching = false;
            ticketId = null;
            OnMatchmakingFailed?.Invoke(reason);
        }
        
        void UpdateMatchmakingProgress()
        {
            float elapsedTime = Time.time - searchStartTime;
            float progress = Mathf.Clamp01(elapsedTime / matchmakingTimeout);
            OnMatchmakingProgress?.Invoke(progress);
        }
        
        void CheckMatchmakingTimeout()
        {
            float elapsedTime = Time.time - searchStartTime;
            if (elapsedTime >= matchmakingTimeout)
            {
                FailMatchmaking("Matchmaking timeout reached");
            }
        }
        
        MatchmakingPlayerAttributes GetDefaultPlayerAttributes()
        {
            // Get player level and skill rating for matchmaking
            var playerProfile = GetPlayerProfile();
            
            return new MatchmakingPlayerAttributes
            {
                DataObject = new Dictionary<string, object>
                {
                    { "Level", playerProfile?.Level ?? 1 },
                    { "SkillRating", CalculateSkillRating(playerProfile) },
                    { "Region", GetPlayerRegion() },
                    { "Platform", "Mobile" }
                }
            };
        }
        
        ArenaBrasil.Backend.PlayerProfile GetPlayerProfile()
        {
            // Get from FirebaseBackendService or local cache
            if (ArenaBrasil.Backend.FirebaseBackendService.Instance != null && 
                ArenaBrasil.Backend.FirebaseBackendService.Instance.IsSignedIn)
            {
                // Would typically load from cache or make async call
                return null; // Placeholder for actual implementation
            }
            return null;
        }
        
        int CalculateSkillRating(ArenaBrasil.Backend.PlayerProfile profile)
        {
            if (profile == null) return 1000; // Default rating
            
            // Simple skill rating calculation based on wins/matches ratio
            float winRate = profile.Matches > 0 ? (float)profile.Wins / profile.Matches : 0f;
            int baseRating = 1000;
            int levelBonus = profile.Level * 10;
            int winBonus = Mathf.RoundToInt(winRate * 500);
            
            return baseRating + levelBonus + winBonus;
        }
        
        string GetPlayerRegion()
        {
            // For Brazilian game, default to Brazil region
            return "Brazil";
        }
        
        async Task<TResult> ExecutePlayFabRequest<TRequest, TResult>(
            TRequest request, 
            Func<TRequest, Task<PlayFabResult<TResult>>> apiMethod)
            where TResult : class
        {
            var result = await apiMethod(request);
            
            if (result.Error != null)
            {
                Debug.LogError($"PlayFab Error: {result.Error.GenerateErrorReport()}");
                return null;
            }
            
            return result.Result;
        }
        
        public bool IsSearching => isSearching;
        public float SearchProgress => isSearching ? 
            Mathf.Clamp01((Time.time - searchStartTime) / matchmakingTimeout) : 0f;
    }
}
