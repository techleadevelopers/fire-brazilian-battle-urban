
using System;
using UnityEngine;

namespace ArenaBrasil.Core.Managers
{
    public class GameFlowManager : MonoBehaviour
    {
        public static GameFlowManager Instance { get; private set; }
        
        [Header("Game States")]
        public GameState currentState = GameState.MainMenu;
        
        [Header("Scene References")]
        public string mainMenuScene = "MainMenuScene";
        public string lobbyScene = "LobbyScene";
        public string gameplayScene = "Map_Favela";
        public string loadingScene = "LoadingScene";
        
        public event Action<GameState> OnGameStateChanged;
        
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
        
        void Start()
        {
            InitializeGame();
        }
        
        void InitializeGame()
        {
            Debug.Log("Arena Brasil - Initializing Game Flow Manager");
            ChangeGameState(GameState.MainMenu);
        }
        
        public void ChangeGameState(GameState newState)
        {
            if (currentState == newState) return;
            
            Debug.Log($"Game State Changed: {currentState} -> {newState}");
            
            GameState previousState = currentState;
            currentState = newState;
            
            OnGameStateChanged?.Invoke(newState);
            HandleGameStateTransition(previousState, newState);
        }
        
        void HandleGameStateTransition(GameState from, GameState to)
        {
            switch (to)
            {
                case GameState.MainMenu:
                    LoadMainMenu();
                    break;
                case GameState.Lobby:
                    LoadLobby();
                    break;
                case GameState.Loading:
                    LoadGameplay();
                    break;
                case GameState.InGame:
                    StartGameplay();
                    break;
                case GameState.Results:
                    ShowResults();
                    break;
            }
        }
        
        void LoadMainMenu()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(mainMenuScene);
        }
        
        void LoadLobby()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(lobbyScene);
        }
        
        void LoadGameplay()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(loadingScene);
        }
        
        void StartGameplay()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(gameplayScene);
        }
        
        void ShowResults()
        {
            // Implementar tela de resultados
            Debug.Log("Showing match results");
        }
        
        public void StartMatch()
        {
            ChangeGameState(GameState.Loading);
        }
        
        public void EndMatch()
        {
            ChangeGameState(GameState.Results);
        }
        
        public void ReturnToMainMenu()
        {
            ChangeGameState(GameState.MainMenu);
        }
    }
    
    public enum GameState
    {
        MainMenu,
        Lobby,
        Loading,
        InGame,
        Results
    }
}
using UnityEngine;
using Unity.Netcode;
using ArenaBrasil.Backend;
using ArenaBrasil.Services;
using ArenaBrasil.UI;

namespace ArenaBrasil.Core
{
    public class GameFlowManager : MonoBehaviour
    {
        public static GameFlowManager Instance { get; private set; }
        
        [Header("Game Flow Settings")]
        public GameState currentState = GameState.MainMenu;
        
        // Events
        public event System.Action<GameState> OnGameStateChanged;
        
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
        
        void Start()
        {
            // Initialize game state
            ChangeGameState(GameState.MainMenu);
        }
        
        public void ChangeGameState(GameState newState)
        {
            if (currentState == newState) return;
            
            Debug.Log($"Arena Brasil - Changing game state from {currentState} to {newState}");
            
            // Exit current state
            ExitState(currentState);
            
            // Change state
            GameState previousState = currentState;
            currentState = newState;
            
            // Enter new state
            EnterState(newState, previousState);
            
            // Notify listeners
            OnGameStateChanged?.Invoke(newState);
        }
        
        void ExitState(GameState state)
        {
            switch (state)
            {
                case GameState.MainMenu:
                    break;
                case GameState.Lobby:
                    break;
                case GameState.Matchmaking:
                    // Cancel matchmaking if leaving
                    if (MatchmakingService.Instance != null && MatchmakingService.Instance.IsSearching)
                    {
                        MatchmakingService.Instance.CancelMatchmaking();
                    }
                    break;
                case GameState.InMatch:
                    break;
                case GameState.Results:
                    break;
            }
        }
        
        void EnterState(GameState state, GameState previousState)
        {
            switch (state)
            {
                case GameState.MainMenu:
                    EnterMainMenu();
                    break;
                case GameState.Lobby:
                    EnterLobby();
                    break;
                case GameState.Matchmaking:
                    EnterMatchmaking();
                    break;
                case GameState.InMatch:
                    EnterMatch();
                    break;
                case GameState.Results:
                    EnterResults();
                    break;
            }
        }
        
        void EnterMainMenu()
        {
            // Show main menu UI
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowScreen(UIScreen.MainMenu);
            }
            
            // Play menu music
            if (ArenaBrasil.Audio.AudioManager.Instance != null)
            {
                ArenaBrasil.Audio.AudioManager.Instance.PlayMenuMusic();
            }
            
            // Ensure network is disconnected
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
            }
        }
        
        void EnterLobby()
        {
            // Show lobby UI
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowScreen(UIScreen.Lobby);
            }
            
            // Initialize Firebase if not already done
            if (FirebaseBackendService.Instance != null && !FirebaseBackendService.Instance.IsSignedIn)
            {
                // Try to sign in or show login screen
                Debug.Log("Player needs to sign in");
            }
        }
        
        void EnterMatchmaking()
        {
            Debug.Log("Arena Brasil - Starting matchmaking");
            
            // Start matchmaking process
            if (MatchmakingService.Instance != null)
            {
                MatchmakingService.Instance.StartMatchmaking();
            }
            else
            {
                Debug.LogError("MatchmakingService not found!");
                ChangeGameState(GameState.Lobby);
            }
        }
        
        void EnterMatch()
        {
            // Show in-game UI
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowScreen(UIScreen.InGame);
            }
            
            // Start combat music
            if (ArenaBrasil.Audio.AudioManager.Instance != null)
            {
                ArenaBrasil.Audio.AudioManager.Instance.PlayCombatMusic(false);
            }
            
            Debug.Log("Arena Brasil - Match started");
        }
        
        void EnterResults()
        {
            // Show results UI
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowScreen(UIScreen.Results);
            }
            
            // Play victory music
            if (ArenaBrasil.Audio.AudioManager.Instance != null)
            {
                ArenaBrasil.Audio.AudioManager.Instance.PlayVictoryMusic();
            }
            
            // Auto return to lobby after 10 seconds
            Invoke(nameof(ReturnToLobby), 10f);
        }
        
        void ReturnToLobby()
        {
            ChangeGameState(GameState.Lobby);
        }
        
        // Public API methods
        public void StartMatchmaking()
        {
            if (currentState == GameState.Lobby)
            {
                ChangeGameState(GameState.Matchmaking);
            }
        }
        
        public void OnMatchFound()
        {
            if (currentState == GameState.Matchmaking)
            {
                ChangeGameState(GameState.InMatch);
            }
        }
        
        public void OnMatchEnded()
        {
            if (currentState == GameState.InMatch)
            {
                ChangeGameState(GameState.Results);
            }
        }
        
        public void ExitToMainMenu()
        {
            ChangeGameState(GameState.MainMenu);
        }
        
        public void QuitGame()
        {
            Application.Quit();
        }
    }
    
    public enum GameState
    {
        MainMenu,
        Lobby,
        Matchmaking,
        InMatch,
        Results
    }
}
