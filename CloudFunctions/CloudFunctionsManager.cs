
using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using ArenaBrasil.Backend;

namespace ArenaBrasil.CloudServices
{
    public class CloudFunctionsManager : MonoBehaviour
    {
        public static CloudFunctionsManager Instance { get; private set; }
        
        [Header("Cloud Functions Configuration")]
        public string firebaseFunctionsUrl = "https://us-central1-arena-brasil.cloudfunctions.net";
        public bool enableCloudValidation = true;
        public float timeoutSeconds = 30f;
        
        // Events
        public event Action<string, bool> OnValidationResult;
        public event Action<string, Exception> OnValidationError;
        
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
        
        // Purchase validation
        public async Task<bool> ValidatePurchase(string playerId, string itemId, string transactionId, decimal amount)
        {
            if (!enableCloudValidation) return true;
            
            try
            {
                var requestData = new PurchaseValidationRequest
                {
                    playerId = playerId,
                    itemId = itemId,
                    transactionId = transactionId,
                    amount = amount,
                    timestamp = DateTime.UtcNow,
                    platform = Application.platform.ToString()
                };
                
                var response = await CallCloudFunction<PurchaseValidationResponse>("validatePurchase", requestData);
                
                if (response.isValid)
                {
                    // Grant items if validation successful
                    await GrantPurchaseReward(playerId, itemId, response.grantedItems);
                }
                
                OnValidationResult?.Invoke($"purchase_{transactionId}", response.isValid);
                return response.isValid;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Purchase validation failed: {ex.Message}");
                OnValidationError?.Invoke($"purchase_{transactionId}", ex);
                return false;
            }
        }
        
        // Anti-cheat validation
        public async Task<bool> ValidatePlayerAction(string playerId, PlayerActionData actionData)
        {
            if (!enableCloudValidation) return true;
            
            try
            {
                var requestData = new AntiCheatValidationRequest
                {
                    playerId = playerId,
                    actionType = actionData.actionType,
                    actionData = actionData,
                    serverTimestamp = DateTime.UtcNow,
                    clientTimestamp = actionData.timestamp
                };
                
                var response = await CallCloudFunction<AntiCheatValidationResponse>("validatePlayerAction", requestData);
                
                if (!response.isValid && response.severity >= CheatSeverity.High)
                {
                    // Auto-ban for severe violations
                    await BanPlayer(playerId, response.reason, response.banDuration);
                }
                
                OnValidationResult?.Invoke($"action_{playerId}", response.isValid);
                return response.isValid;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Anti-cheat validation failed: {ex.Message}");
                OnValidationError?.Invoke($"action_{playerId}", ex);
                return true; // Allow action on validation failure to prevent false positives
            }
        }
        
        // Leaderboard validation
        public async Task<bool> ValidateLeaderboardScore(string playerId, string leaderboardId, long score)
        {
            try
            {
                var requestData = new LeaderboardValidationRequest
                {
                    playerId = playerId,
                    leaderboardId = leaderboardId,
                    score = score,
                    timestamp = DateTime.UtcNow
                };
                
                var response = await CallCloudFunction<LeaderboardValidationResponse>("validateScore", requestData);
                
                if (response.isValid)
                {
                    await UpdateLeaderboard(playerId, leaderboardId, score);
                }
                
                return response.isValid;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Leaderboard validation failed: {ex.Message}");
                return false;
            }
        }
        
        // Event reward validation
        public async Task<bool> ValidateEventReward(string playerId, string eventId, string rewardId)
        {
            try
            {
                var requestData = new EventRewardValidationRequest
                {
                    playerId = playerId,
                    eventId = eventId,
                    rewardId = rewardId,
                    timestamp = DateTime.UtcNow
                };
                
                var response = await CallCloudFunction<EventRewardValidationResponse>("validateEventReward", requestData);
                
                if (response.isValid)
                {
                    await GrantEventReward(playerId, response.rewardData);
                }
                
                return response.isValid;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Event reward validation failed: {ex.Message}");
                return false;
            }
        }
        
        // Generic cloud function caller
        private async Task<TResponse> CallCloudFunction<TResponse>(string functionName, object requestData) where TResponse : class
        {
            string url = $"{firebaseFunctionsUrl}/{functionName}";
            string jsonData = JsonUtility.ToJson(requestData);
            
            using (var www = new UnityEngine.Networking.UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
                www.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(bodyRaw);
                www.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");
                www.timeout = (int)timeoutSeconds;
                
                var operation = www.SendWebRequest();
                
                // Wait for completion
                while (!operation.isDone)
                {
                    await Task.Yield();
                }
                
                if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    string responseJson = www.downloadHandler.text;
                    return JsonUtility.FromJson<TResponse>(responseJson);
                }
                else
                {
                    throw new Exception($"Cloud function call failed: {www.error}");
                }
            }
        }
        
        // Helper methods for post-validation actions
        private async Task GrantPurchaseReward(string playerId, string itemId, List<RewardItem> grantedItems)
        {
            if (EconomyManager.Instance != null)
            {
                foreach (var item in grantedItems)
                {
                    switch (item.type)
                    {
                        case "currency":
                            await EconomyManager.Instance.AddCurrency(playerId, item.currencyType, item.quantity);
                            break;
                        case "item":
                            if (InventorySystem.Instance != null)
                            {
                                var inventoryItem = CreateInventoryItem(item);
                                InventorySystem.Instance.AddItemServerRpc(ulong.Parse(playerId), inventoryItem);
                            }
                            break;
                    }
                }
            }
        }
        
        private async Task BanPlayer(string playerId, string reason, TimeSpan duration)
        {
            if (FirebaseBackendService.Instance != null)
            {
                var banData = new PlayerBanData
                {
                    playerId = playerId,
                    reason = reason,
                    duration = duration,
                    bannedAt = DateTime.UtcNow,
                    bannedBy = "ANTI_CHEAT_SYSTEM"
                };
                
                await FirebaseBackendService.Instance.SavePlayerBan(playerId, banData);
                
                // Kick player if online
                if (AntiCheatSystem.Instance != null)
                {
                    AntiCheatSystem.Instance.KickPlayer(ulong.Parse(playerId), reason);
                }
            }
        }
        
        private async Task UpdateLeaderboard(string playerId, string leaderboardId, long score)
        {
            if (RankingSystem.Instance != null)
            {
                await RankingSystem.Instance.UpdatePlayerScore(playerId, leaderboardId, score);
            }
        }
        
        private async Task GrantEventReward(string playerId, EventRewardData rewardData)
        {
            if (LiveOpsManager.Instance != null)
            {
                await LiveOpsManager.Instance.GrantEventReward(playerId, rewardData);
            }
        }
        
        private InventoryItem CreateInventoryItem(RewardItem rewardItem)
        {
            return new InventoryItem
            {
                itemId = rewardItem.itemId,
                itemName = rewardItem.name,
                quantity = (int)rewardItem.quantity,
                itemType = ItemType.Special,
                rarity = ItemRarity.Common
            };
        }
    }
    
    // Data classes for cloud function requests/responses
    [System.Serializable]
    public class PurchaseValidationRequest
    {
        public string playerId;
        public string itemId;
        public string transactionId;
        public decimal amount;
        public DateTime timestamp;
        public string platform;
    }
    
    [System.Serializable]
    public class PurchaseValidationResponse
    {
        public bool isValid;
        public string reason;
        public List<RewardItem> grantedItems;
    }
    
    [System.Serializable]
    public class AntiCheatValidationRequest
    {
        public string playerId;
        public string actionType;
        public PlayerActionData actionData;
        public DateTime serverTimestamp;
        public DateTime clientTimestamp;
    }
    
    [System.Serializable]
    public class AntiCheatValidationResponse
    {
        public bool isValid;
        public string reason;
        public CheatSeverity severity;
        public TimeSpan banDuration;
    }
    
    [System.Serializable]
    public class LeaderboardValidationRequest
    {
        public string playerId;
        public string leaderboardId;
        public long score;
        public DateTime timestamp;
    }
    
    [System.Serializable]
    public class LeaderboardValidationResponse
    {
        public bool isValid;
        public string reason;
        public long validatedScore;
    }
    
    [System.Serializable]
    public class EventRewardValidationRequest
    {
        public string playerId;
        public string eventId;
        public string rewardId;
        public DateTime timestamp;
    }
    
    [System.Serializable]
    public class EventRewardValidationResponse
    {
        public bool isValid;
        public string reason;
        public EventRewardData rewardData;
    }
    
    [System.Serializable]
    public class PlayerActionData
    {
        public string actionType;
        public Vector3 position;
        public Vector3 velocity;
        public float deltaTime;
        public DateTime timestamp;
        public Dictionary<string, object> additionalData;
    }
    
    [System.Serializable]
    public class RewardItem
    {
        public string type;
        public string itemId;
        public string name;
        public long quantity;
        public string currencyType;
    }
    
    [System.Serializable]
    public class EventRewardData
    {
        public string eventId;
        public string rewardId;
        public List<RewardItem> rewards;
    }
    
    [System.Serializable]
    public class PlayerBanData
    {
        public string playerId;
        public string reason;
        public TimeSpan duration;
        public DateTime bannedAt;
        public string bannedBy;
    }
    
    public enum CheatSeverity
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
    }
}
