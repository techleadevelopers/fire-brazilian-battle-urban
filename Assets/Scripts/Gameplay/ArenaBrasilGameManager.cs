
using UnityEngine;
using Unity.Netcode;
using ArenaBrasil.Combat;
using ArenaBrasil.Loot;
using ArenaBrasil.Gameplay.SafeZone;
using ArenaBrasil.Environment;
using ArenaBrasil.Backend;
using ArenaBrasil.Economy;

namespace ArenaBrasil.Core
{
    public class ArenaBrasilGameManager : NetworkBehaviour
    {
        public static ArenaBrasilGameManager Instance { get; private set; }
        
        [Header("Game Configuration")]
        public bool autoStartMatch = true;
        public float matchStartDelay = 10f;
        public int minPlayersToStart = 10;
        public int maxPlayersPerMatch = 60;
        
        [Header("Brazilian Theme")]
        public AudioClip[] brazilianMusic;
        public string[] motivationalPhrases = {
            "Vamos nessa, guerreiro!",
            "Mostra a garra brasileira!",
            "É Brasil na veia!",
            "Vamo que vamo!",
            "Joga bonito!"
        };
        
        // Game state
        private MatchState currentMatchState = MatchState.Lobby;
        private float matchTimer = 0f;
        private int playersInMatch = 0;
        
        // Events
        public event System.Action<MatchState> OnMatchStateChanged;
        public event System.Action<int> OnPlayerCountChanged;
        public event System.Action<string> OnMatchEvent;
        
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
        
        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                InitializeMatch();
            }
            
            // Subscribe to combat events
            if (CombatSystem.Instance != null)
            {
                CombatSystem.Instance.OnPlayerEliminated += OnPlayerEliminated;
                CombatSystem.Instance.OnPlayersAliveChanged += OnPlayersAliveChanged;
            }
        }
        
        void InitializeMatch()
        {
            playersInMatch = NetworkManager.Singleton.ConnectedClients.Count;
            ChangeMatchState(MatchState.Lobby);
            
            if (autoStartMatch && playersInMatch >= minPlayersToStart)
            {
                Invoke(nameof(StartMatchSequence), matchStartDelay);
            }
        }
        
        void StartMatchSequence()
        {
            if (currentMatchState != MatchState.Lobby) return;
            
            ChangeMatchState(MatchState.Starting);
            
            // Load random Brazilian map
            if (MapManager.Instance != null)
            {
                MapManager.Instance.LoadRandomMap();
            }
            
            // Initialize systems
            InitializeGameSystems();
            
            // Start countdown
            StartCoroutine(StartCountdown());
        }
        
        void InitializeGameSystems()
        {
            // Initialize safe zone
            if (SafeZoneController.Instance != null)
            {
                SafeZoneController.Instance.StartSafeZone();
            }
            
            // Spawn initial loot
            if (LootSystem.Instance != null)
            {
                // Loot spawning handled in LootSystem.OnNetworkSpawn
            }
            
            // Setup combat system
            if (CombatSystem.Instance != null)
            {
                // Combat system ready
            }
        }
        
        System.Collections.IEnumerator StartCountdown()
        {
            for (int i = 5; i > 0; i--)
            {
                AnnounceToAllClientsRpc($"Partida iniciando em {i}...");
                yield return new WaitForSeconds(1f);
            }
            
            AnnounceToAllClientsRpc(GetRandomMotivationalPhrase());
            ChangeMatchState(MatchState.InProgress);
            
            // Start match timer
            matchTimer = 0f;
        }
        
        string GetRandomMotivationalPhrase()
        {
            return motivationalPhrases[Random.Range(0, motivationalPhrases.Length)];
        }
        
        void ChangeMatchState(MatchState newState)
        {
            currentMatchState = newState;
            OnMatchStateChanged?.Invoke(newState);
            
            MatchStateChangedClientRpc(newState);
            
            switch (newState)
            {
                case MatchState.Starting:
                    Debug.Log("Arena Brasil - Match starting...");
                    break;
                case MatchState.InProgress:
                    Debug.Log("Arena Brasil - Match in progress!");
                    PlayBrazilianMusic();
                    break;
                case MatchState.Ending:
                    Debug.Log("Arena Brasil - Match ending...");
                    break;
                case MatchState.Results:
                    Debug.Log("Arena Brasil - Showing results...");
                    ShowMatchResults();
                    break;
            }
        }
        
        [ClientRpc]
        void MatchStateChangedClientRpc(MatchState newState)
        {
            if (!IsServer)
            {
                currentMatchState = newState;
                OnMatchStateChanged?.Invoke(newState);
            }
        }
        
        [ClientRpc]
        void AnnounceToAllClientsRpc(string message)
        {
            Debug.Log($"Arena Brasil: {message}");
            OnMatchEvent?.Invoke(message);
        }
        
        void PlayBrazilianMusic()
        {
            if (brazilianMusic.Length > 0)
            {
                var audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
                
                audioSource.clip = brazilianMusic[Random.Range(0, brazilianMusic.Length)];
                audioSource.loop = true;
                audioSource.volume = 0.3f;
                audioSource.Play();
            }
        }
        
        void OnPlayerEliminated(ulong killerId, ulong victimId, string weaponId)
        {
            string killerName = GetPlayerName(killerId);
            string victimName = GetPlayerName(victimId);
            
            AnnounceToAllClientsRpc($"{killerName} eliminou {victimName} com {weaponId}!");
        }
        
        void OnPlayersAliveChanged(int playersAlive)
        {
            OnPlayerCountChanged?.Invoke(playersAlive);
            
            if (playersAlive <= 1 && currentMatchState == MatchState.InProgress)
            {
                EndMatch();
            }
            else if (playersAlive <= 10)
            {
                AnnounceToAllClientsRpc($"Só restam {playersAlive} guerreiros na arena!");
            }
        }
        
        void EndMatch()
        {
            ChangeMatchState(MatchState.Ending);
            
            // Find winner
            ulong winnerId = FindWinner();
            string winnerName = GetPlayerName(winnerId);
            
            AnnounceToAllClientsRpc($"🏆 {winnerName} é o novo campeão do Arena Brasil! 🇧🇷");
            
            // Process match results
            ProcessMatchResults(winnerId);
            
            // Transition to results
            Invoke(nameof(ShowResults), 3f);
        }
        
        ulong FindWinner()
        {
            foreach (var client in NetworkManager.Singleton.ConnectedClients)
            {
                var player = client.Value.PlayerObject?.GetComponent<PlayerController>();
                if (player != null && player.GetCurrentHealth() > 0)
                {
                    return client.Key;
                }
            }
            return 0;
        }
        
        void ProcessMatchResults(ulong winnerId)
        {
            if (!IsServer) return;
            
            // Calculate rewards for all players
            foreach (var client in NetworkManager.Singleton.ConnectedClients)
            {
                var player = client.Value.PlayerObject?.GetComponent<PlayerController>();
                if (player != null)
                {
                    var matchResult = new MatchResult
                    {
                        won = client.Key == winnerId,
                        kills = player.GetKills(),
                        placement = CalculatePlacement(client.Key),
                        survivalTime = matchTimer,
                        damageDealt = player.GetDamageDealt(),
                        heroUsed = player.currentHero?.heroName ?? "Saci"
                    };
                    
                    // Grant rewards via Economy Manager
                    if (EconomyManager.Instance != null)
                    {
                        EconomyManager.Instance.GrantMatchRewards(matchResult);
                    }
                }
            }
        }
        
        int CalculatePlacement(ulong clientId)
        {
            // Calculate player placement based on elimination order
            // For now, return random placement
            return Random.Range(1, playersInMatch + 1);
        }
        
        void ShowResults()
        {
            ChangeMatchState(MatchState.Results);
        }
        
        void ShowMatchResults()
        {
            // Implement match results UI
            Debug.Log("Arena Brasil - Showing match results");
        }
        
        string GetPlayerName(ulong clientId)
        {
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
            {
                var player = client.PlayerObject?.GetComponent<PlayerController>();
                return player?.GetPlayerName() ?? $"Jogador {clientId}";
            }
            return $"Jogador {clientId}";
        }
        
        void Update()
        {
            if (IsServer && currentMatchState == MatchState.InProgress)
            {
                matchTimer += Time.deltaTime;
            }
        }
        
        // Public getters
        public MatchState GetCurrentMatchState() => currentMatchState;
        public float GetMatchTimer() => matchTimer;
        public int GetPlayersInMatch() => playersInMatch;
        
        public void ForceEndMatch()
        {
            if (IsServer && currentMatchState == MatchState.InProgress)
            {
                EndMatch();
            }
        }
    }
    
    public enum MatchState
    {
        Lobby,
        Starting,
        InProgress,
        Ending,
        Results
    }
}
