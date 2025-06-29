
using Unity.Netcode;
using UnityEngine;

namespace ArenaBrasil.Networking.Client
{
    public class NetworkManagerClient : MonoBehaviour
    {
        public static NetworkManagerClient Instance { get; private set; }
        
        [Header("Connection Settings")]
        public string serverIP = "127.0.0.1";
        public ushort serverPort = 7777;
        
        [Header("Game Settings")]
        public int maxPlayersPerMatch = 60;
        public GameObject playerPrefab;
        
        private NetworkManager networkManager;
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                
                networkManager = GetComponent<NetworkManager>();
                if (networkManager == null)
                {
                    networkManager = gameObject.AddComponent<NetworkManager>();
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void Start()
        {
            SetupNetworkManager();
        }
        
        void SetupNetworkManager()
        {
            // Configurar prefabs de rede
            if (playerPrefab != null && networkManager.NetworkConfig != null)
            {
                networkManager.NetworkConfig.PlayerPrefab = playerPrefab;
            }
            
            // Configurar eventos de rede
            networkManager.OnClientConnectedCallback += OnClientConnected;
            networkManager.OnClientDisconnectCallback += OnClientDisconnected;
            networkManager.OnServerStarted += OnServerStarted;
        }
        
        public void StartHost()
        {
            Debug.Log("Arena Brasil - Starting Host");
            
            if (networkManager.StartHost())
            {
                Debug.Log("Host started successfully");
            }
            else
            {
                Debug.LogError("Failed to start host");
            }
        }
        
        public void StartClient()
        {
            Debug.Log($"Arena Brasil - Connecting to server: {serverIP}:{serverPort}");
            
            // Configurar transporte para conectar ao servidor dedicado
            var transport = networkManager.NetworkConfig.NetworkTransport;
            
            if (networkManager.StartClient())
            {
                Debug.Log("Client connection initiated");
            }
            else
            {
                Debug.LogError("Failed to start client");
            }
        }
        
        public void StartServer()
        {
            Debug.Log("Arena Brasil - Starting Dedicated Server");
            
            if (networkManager.StartServer())
            {
                Debug.Log("Dedicated server started successfully");
            }
            else
            {
                Debug.LogError("Failed to start server");
            }
        }
        
        public void Disconnect()
        {
            Debug.Log("Arena Brasil - Disconnecting");
            
            if (networkManager.IsHost)
            {
                networkManager.Shutdown();
            }
            else if (networkManager.IsClient)
            {
                networkManager.Shutdown();
            }
        }
        
        public void ConnectToGameServer(string ip, ushort port)
        {
            serverIP = ip;
            serverPort = port;
            
            StartClient();
        }
        
        // Event Handlers
        void OnClientConnected(ulong clientId)
        {
            Debug.Log($"Client {clientId} connected to Arena Brasil");
            
            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                // Cliente local conectado com sucesso
                OnLocalClientConnected();
            }
        }
        
        void OnClientDisconnected(ulong clientId)
        {
            Debug.Log($"Client {clientId} disconnected from Arena Brasil");
            
            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                // Cliente local desconectado
                OnLocalClientDisconnected();
            }
        }
        
        void OnServerStarted()
        {
            Debug.Log("Arena Brasil - Server started and ready for connections");
        }
        
        void OnLocalClientConnected()
        {
            Debug.Log("Successfully connected to Arena Brasil server");
            
            // Transição para lobby ou estado de jogo
            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.ChangeGameState(GameState.Lobby);
            }
        }
        
        void OnLocalClientDisconnected()
        {
            Debug.Log("Disconnected from Arena Brasil server");
            
            // Retornar ao menu principal
            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.ChangeGameState(GameState.MainMenu);
            }
        }
        
        public bool IsConnected()
        {
            return networkManager != null && (networkManager.IsHost || networkManager.IsClient);
        }
        
        public int GetConnectedPlayersCount()
        {
            if (networkManager != null && networkManager.IsServer)
            {
                return (int)networkManager.ConnectedClients.Count;
            }
            return 0;
        }
        
        void OnDestroy()
        {
            if (networkManager != null)
            {
                networkManager.OnClientConnectedCallback -= OnClientConnected;
                networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
                networkManager.OnServerStarted -= OnServerStarted;
            }
        }
    }
}
