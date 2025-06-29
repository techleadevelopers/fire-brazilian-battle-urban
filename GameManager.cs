
using System;
using UnityEngine;
using ArenaBrasil.Core.Managers;
using ArenaBrasil.Backend;
using ArenaBrasil.Networking.Client;

namespace ArenaBrasil.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        
        [Header("Game Configuration")]
        public bool initializeOnStart = true;
        public bool debugMode = false;
        
        [Header("Game Settings")]
        public int targetFrameRate = 60;
        public float matchDuration = 1800f; // 30 minutes
        public int maxPlayersPerMatch = 60;
        
        // Core Systems
        private GameFlowManager gameFlowManager;
        private FirebaseBackendService backendService;
        private NetworkManagerClient networkManager;
        
        // Game State
        public bool IsGameInitialized { get; private set; }
        public bool IsInMatch { get; private set; }
        public float MatchTimeRemaining { get; private set; }
        
        // Events
        public event Action OnGameInitialized;
        public event Action OnMatchStarted;
        public event Action OnMatchEnded;
        public event Action<float> OnMatchTimeUpdated;
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeGameManager();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void Start()
        {
            if (initializeOnStart)
            {
                StartGameInitialization();
            }
        }
        
        void Update()
        {
            if (IsInMatch)
            {
                UpdateMatchTimer();
            }
        }
        
        void InitializeGameManager()
        {
            Debug.Log("Arena Brasil - Initializing Game Manager");
            
            // Set target frame rate for mobile optimization
            Application.targetFrameRate = targetFrameRate;
            
            // Don't destroy on load for persistent systems
            DontDestroyOnLoad(gameObject);
            
            // Initialize core systems
            InitializeCoreServices();
        }
        
        void InitializeCoreServices()
        {
            // Get or create GameFlowManager
            gameFlowManager = GameFlowManager.Instance;
            if (gameFlowManager == null)
            {
                var flowManagerGO = new GameObject("GameFlowManager");
                gameFlowManager = flowManagerGO.AddComponent<GameFlowManager>();
                DontDestroyOnLoad(flowManagerGO);
            }
            
            // Get or create FirebaseBackendService
            backendService = FirebaseBackendService.Instance;
            if (backendService == null)
            {
                var backendGO = new GameObject("FirebaseBackendService");
                backendService = backendGO.AddComponent<FirebaseBackendService>();
                DontDestroyOnLoad(backendGO);
            }
            
            // Get or create NetworkManagerClient
            networkManager = NetworkManagerClient.Instance;
            if (networkManager == null)
            {
                var networkGO = new GameObject("NetworkManagerClient");
                networkManager = networkGO.AddComponent<NetworkManagerClient>();
                DontDestroyOnLoad(networkGO);
            }
            
            // Subscribe to events
            SubscribeToEvents();
        }
        
        void SubscribeToEvents()
        {
            if (backendService != null)
            {
                backendService.OnFirebaseInitialized += OnFirebaseInitialized;
                backendService.OnUserSignedIn += OnUserSignedIn;
                backendService.OnUserSignedOut += OnUserSignedOut;
            }
            
            if (gameFlowManager != null)
            {
                gameFlowManager.OnGameStateChanged += OnGameStateChanged;
            }
        }
        
        public void StartGameInitialization()
        {
            Debug.Log("Arena Brasil - Starting game initialization sequence");
            
            // Initialize Firebase first
            if (backendService != null)
            {
                backendService.InitializeFirebase();
            }
            else
            {
                Debug.LogError("Backend service not found! Cannot initialize Firebase.");
            }
        }
        
        void OnFirebaseInitialized()
        {
            Debug.Log("Arena Brasil - Firebase initialized, game ready");
            IsGameInitialized = true;
            OnGameInitialized?.Invoke();
        }
        
        void OnUserSignedIn(Firebase.Auth.FirebaseUser user)
        {
            Debug.Log($"Arena Brasil - User signed in: {user.DisplayName}");
            
            // Load player data after sign in
            LoadPlayerData(user.UserId);
        }
        
        void OnUserSignedOut()
        {
            Debug.Log("Arena Brasil - User signed out");
            
            // Return to main menu
            if (gameFlowManager != null)
            {
                gameFlowManager.ChangeGameState(GameState.MainMenu);
            }
        }
        
        async void LoadPlayerData(string userId)
        {
            if (backendService != null)
            {
                var playerProfile = await backendService.GetPlayerProfile(userId);
                if (playerProfile != null)
                {
                    Debug.Log($"Player profile loaded: Level {playerProfile.Level}, XP {playerProfile.XP}");
                }
            }
        }
        
        void OnGameStateChanged(GameState newState)
        {
            Debug.Log($"Arena Brasil - Game state changed to: {newState}");
            
            switch (newState)
            {
                case GameState.InGame:
                    StartMatch();
                    break;
                case GameState.Results:
                    EndMatch();
                    break;
            }
        }
        
        public void StartMatch()
        {
            Debug.Log("Arena Brasil - Starting match");
            
            IsInMatch = true;
            MatchTimeRemaining = matchDuration;
            
            OnMatchStarted?.Invoke();
        }
        
        public void EndMatch()
        {
            Debug.Log("Arena Brasil - Ending match");
            
            IsInMatch = false;
            MatchTimeRemaining = 0f;
            
            OnMatchEnded?.Invoke();
        }
        
        void UpdateMatchTimer()
        {
            if (MatchTimeRemaining > 0)
            {
                MatchTimeRemaining -= Time.deltaTime;
                OnMatchTimeUpdated?.Invoke(MatchTimeRemaining);
                
                // End match when time runs out
                if (MatchTimeRemaining <= 0)
                {
                    EndMatch();
                }
            }
        }
        
        public void QuitGame()
        {
            Debug.Log("Arena Brasil - Quitting game");
            
            // Sign out user
            if (backendService != null)
            {
                backendService.SignOut();
            }
            
            // Disconnect from network
            if (networkManager != null)
            {
                networkManager.Disconnect();
            }
            
            // Quit application
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
        
        public void RestartGame()
        {
            Debug.Log("Arena Brasil - Restarting game");
            
            // Reset game state
            IsInMatch = false;
            MatchTimeRemaining = 0f;
            
            // Return to main menu
            if (gameFlowManager != null)
            {
                gameFlowManager.ChangeGameState(GameState.MainMenu);
            }
        }
        
        // Utility methods
        public string FormatTime(float timeInSeconds)
        {
            int minutes = Mathf.FloorToInt(timeInSeconds / 60);
            int seconds = Mathf.FloorToInt(timeInSeconds % 60);
            return $"{minutes:00}:{seconds:00}";
        }
        
        public void SetTargetFrameRate(int frameRate)
        {
            targetFrameRate = frameRate;
            Application.targetFrameRate = targetFrameRate;
            Debug.Log($"Target frame rate set to: {targetFrameRate}");
        }
        
        void OnDestroy()
        {
            // Unsubscribe from events
            if (backendService != null)
            {
                backendService.OnFirebaseInitialized -= OnFirebaseInitialized;
                backendService.OnUserSignedIn -= OnUserSignedIn;
                backendService.OnUserSignedOut -= OnUserSignedOut;
            }
            
            if (gameFlowManager != null)
            {
                gameFlowManager.OnGameStateChanged -= OnGameStateChanged;
            }
        }
    }
}
