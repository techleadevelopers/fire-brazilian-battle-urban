
using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Unity.Netcode;

namespace ArenaBrasil.Cloud
{
    public class AWSGameLiftManager : MonoBehaviour
    {
        public static AWSGameLiftManager Instance { get; private set; }
        
        [Header("AWS GameLift Configuration")]
        public string fleetId = "fleet-arena-brasil-prod";
        public string aliasId = "alias-arena-brasil";
        public string queueName = "arena-brasil-queue";
        public int maxPlayersPerSession = 60;
        
        [Header("Regional Settings")]
        public List<GameLiftRegion> supportedRegions = new List<GameLiftRegion>();
        public string preferredRegion = "sa-east-1"; // São Paulo
        
        [Header("Matchmaking")]
        public string matchmakingConfigurationName = "arena-brasil-matchmaking";
        public float matchmakingTimeout = 120f;
        
        private GameSession currentGameSession;
        private string currentTicketId;
        private bool isSearchingForMatch = false;
        
        public event Action<GameSession> OnGameSessionCreated;
        public event Action<string> OnMatchmakingSuccess;
        public event Action<string> OnMatchmakingFailed;
        public event Action<PlayerSession> OnPlayerSessionCreated;
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeGameLift();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void InitializeGameLift()
        {
            Debug.Log("🚀 Inicializando AWS GameLift para Arena Brasil");
            
            SetupRegionalConfiguration();
            ConfigureMatchmaking();
        }
        
        void SetupRegionalConfiguration()
        {
            supportedRegions.Add(new GameLiftRegion
            {
                regionCode = "sa-east-1",
                regionName = "South America (São Paulo)",
                endpoint = "gamelift.sa-east-1.amazonaws.com",
                priority = 1,
                maxLatency = 50
            });
            
            supportedRegions.Add(new GameLiftRegion
            {
                regionCode = "us-east-1",
                regionName = "US East (N. Virginia)",
                endpoint = "gamelift.us-east-1.amazonaws.com",
                priority = 2,
                maxLatency = 100
            });
            
            supportedRegions.Add(new GameLiftRegion
            {
                regionCode = "us-west-2",
                regionName = "US West (Oregon)",
                endpoint = "gamelift.us-west-2.amazonaws.com",
                priority = 3,
                maxLatency = 150
            });
        }
        
        void ConfigureMatchmaking()
        {
            var matchmakingConfig = new MatchmakingConfiguration
            {
                name = matchmakingConfigurationName,
                description = "Arena Brasil Battle Royale Matchmaking",
                gameSessionQueueNames = new string[] { queueName },
                requestTimeoutSeconds = (int)matchmakingTimeout,
                acceptanceTimeoutSeconds = 30,
                acceptanceRequired = true,
                customEventData = "arena_brasil_matchmaking",
                additionalPlayerCount = 0,
                backfillMode = "AUTOMATIC",
                flexMatchMode = "STANDALONE"
            };
            
            Debug.Log($"Configuração de matchmaking: {matchmakingConfig.name}");
        }
        
        public async Task<string> StartMatchmaking(PlayerAttributes playerAttributes)
        {
            if (isSearchingForMatch)
            {
                Debug.LogWarning("Matchmaking já em andamento");
                return null;
            }
            
            try
            {
                isSearchingForMatch = true;
                
                var matchmakingRequest = new StartMatchmakingRequest
                {
                    ticketId = GenerateTicketId(),
                    configurationName = matchmakingConfigurationName,
                    players = new MatchmakingPlayer[]
                    {
                        new MatchmakingPlayer
                        {
                            playerId = playerAttributes.playerId,
                            playerAttributes = playerAttributes.attributes,
                            team = playerAttributes.team,
                            latencyInMs = await GetPlayerLatency()
                        }
                    }
                };
                
                currentTicketId = await SendMatchmakingRequest(matchmakingRequest);
                
                if (!string.IsNullOrEmpty(currentTicketId))
                {
                    Debug.Log($"🎯 Matchmaking iniciado: {currentTicketId}");
                    StartMatchmakingPolling();
                    return currentTicketId;
                }
                else
                {
                    isSearchingForMatch = false;
                    OnMatchmakingFailed?.Invoke("Falha ao iniciar matchmaking");
                    return null;
                }
            }
            catch (Exception e)
            {
                isSearchingForMatch = false;
                Debug.LogError($"Erro no matchmaking: {e.Message}");
                OnMatchmakingFailed?.Invoke(e.Message);
                return null;
            }
        }
        
        async Task<string> SendMatchmakingRequest(StartMatchmakingRequest request)
        {
            // Simulação da chamada AWS GameLift API
            // Em produção, usar AWS SDK for .NET
            await Task.Delay(100);
            
            Debug.Log($"📡 Enviando request de matchmaking para região {preferredRegion}");
            return $"ticket_{DateTime.Now.Ticks}";
        }
        
        async void StartMatchmakingPolling()
        {
            while (isSearchingForMatch && !string.IsNullOrEmpty(currentTicketId))
            {
                try
                {
                    var status = await CheckMatchmakingStatus(currentTicketId);
                    
                    switch (status.status)
                    {
                        case MatchmakingStatus.COMPLETED:
                            await HandleMatchmakingSuccess(status);
                            return;
                            
                        case MatchmakingStatus.FAILED:
                        case MatchmakingStatus.CANCELLED:
                        case MatchmakingStatus.TIMED_OUT:
                            HandleMatchmakingFailure(status);
                            return;
                            
                        case MatchmakingStatus.SEARCHING:
                        case MatchmakingStatus.REQUIRES_ACCEPTANCE:
                            Debug.Log($"🔍 Status matchmaking: {status.status}");
                            break;
                    }
                    
                    await Task.Delay(2000); // Poll a cada 2 segundos
                }
                catch (Exception e)
                {
                    Debug.LogError($"Erro no polling de matchmaking: {e.Message}");
                    await Task.Delay(5000); // Aguardar mais tempo em caso de erro
                }
            }
        }
        
        async Task<MatchmakingTicketStatus> CheckMatchmakingStatus(string ticketId)
        {
            // Simulação da verificação de status
            await Task.Delay(50);
            
            // Simular diferentes estados baseado no tempo
            float elapsed = Time.time % 60f;
            
            if (elapsed < 10f)
            {
                return new MatchmakingTicketStatus
                {
                    ticketId = ticketId,
                    status = MatchmakingStatus.SEARCHING,
                    estimatedWaitTime = 30
                };
            }
            else if (elapsed < 50f)
            {
                return new MatchmakingTicketStatus
                {
                    ticketId = ticketId,
                    status = MatchmakingStatus.COMPLETED,
                    gameSessionInfo = new GameSessionInfo
                    {
                        gameSessionArn = $"arn:aws:gamelift:sa-east-1:123456789:gamesession/{fleetId}/gsess-{Guid.NewGuid()}",
                        ipAddress = "18.228.xxx.xxx", // IP São Paulo
                        port = 7777,
                        playerSessionId = $"psess-{Guid.NewGuid()}"
                    }
                };
            }
            else
            {
                return new MatchmakingTicketStatus
                {
                    ticketId = ticketId,
                    status = MatchmakingStatus.TIMED_OUT,
                    statusReason = "Timeout atingido"
                };
            }
        }
        
        async Task HandleMatchmakingSuccess(MatchmakingTicketStatus status)
        {
            isSearchingForMatch = false;
            
            Debug.Log($"🎉 Match encontrado! Conectando ao servidor...");
            
            // Criar sessão do jogador
            var playerSession = await CreatePlayerSession(status.gameSessionInfo);
            
            if (playerSession != null)
            {
                // Conectar ao servidor de jogo
                await ConnectToGameServer(status.gameSessionInfo, playerSession);
                OnMatchmakingSuccess?.Invoke(status.gameSessionInfo.gameSessionArn);
            }
            else
            {
                OnMatchmakingFailed?.Invoke("Falha ao criar sessão do jogador");
            }
        }
        
        void HandleMatchmakingFailure(MatchmakingTicketStatus status)
        {
            isSearchingForMatch = false;
            currentTicketId = null;
            
            Debug.LogError($"❌ Matchmaking falhou: {status.status} - {status.statusReason}");
            OnMatchmakingFailed?.Invoke($"{status.status}: {status.statusReason}");
        }
        
        async Task<PlayerSession> CreatePlayerSession(GameSessionInfo gameSessionInfo)
        {
            try
            {
                var request = new CreatePlayerSessionRequest
                {
                    gameSessionId = ExtractGameSessionId(gameSessionInfo.gameSessionArn),
                    playerId = GetCurrentPlayerId(),
                    playerData = GetPlayerSessionData()
                };
                
                var playerSession = await SendCreatePlayerSessionRequest(request);
                
                if (playerSession != null)
                {
                    Debug.Log($"✅ Sessão do jogador criada: {playerSession.playerSessionId}");
                    OnPlayerSessionCreated?.Invoke(playerSession);
                }
                
                return playerSession;
            }
            catch (Exception e)
            {
                Debug.LogError($"Erro ao criar sessão do jogador: {e.Message}");
                return null;
            }
        }
        
        async Task<PlayerSession> SendCreatePlayerSessionRequest(CreatePlayerSessionRequest request)
        {
            // Simulação da criação de sessão do jogador
            await Task.Delay(100);
            
            return new PlayerSession
            {
                playerSessionId = $"psess-{Guid.NewGuid()}",
                playerId = request.playerId,
                gameSessionId = request.gameSessionId,
                fleetId = fleetId,
                creationTime = DateTime.UtcNow,
                status = PlayerSessionStatus.ACTIVE,
                ipAddress = "18.228.xxx.xxx",
                port = 7777
            };
        }
        
        async Task ConnectToGameServer(GameSessionInfo gameSessionInfo, PlayerSession playerSession)
        {
            try
            {
                Debug.Log($"🔌 Conectando ao servidor: {gameSessionInfo.ipAddress}:{gameSessionInfo.port}");
                
                // Usar NetworkManager para conectar ao servidor dedicado
                if (NetworkManager.Singleton != null)
                {
                    var transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport;
                    
                    // Configurar conexão para servidor dedicado
                    await Task.Delay(100); // Simular configuração
                    
                    // Em produção, isso conectaria ao servidor real
                    Debug.Log($"🎮 Conectado ao servidor Arena Brasil!");
                    
                    // Notificar sistemas do jogo
                    if (ArenaBrasilGameManager.Instance != null)
                    {
                        ArenaBrasilGameManager.Instance.OnConnectedToGameServer(playerSession);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Erro ao conectar ao servidor: {e.Message}");
                OnMatchmakingFailed?.Invoke("Falha na conexão com o servidor");
            }
        }
        
        public void StopMatchmaking()
        {
            if (isSearchingForMatch && !string.IsNullOrEmpty(currentTicketId))
            {
                StopMatchmakingTicket(currentTicketId);
                isSearchingForMatch = false;
                currentTicketId = null;
                Debug.Log("🛑 Matchmaking cancelado");
            }
        }
        
        async void StopMatchmakingTicket(string ticketId)
        {
            try
            {
                // Simulação do cancelamento
                await Task.Delay(50);
                Debug.Log($"Ticket {ticketId} cancelado");
            }
            catch (Exception e)
            {
                Debug.LogError($"Erro ao cancelar ticket: {e.Message}");
            }
        }
        
        async Task<Dictionary<string, int>> GetPlayerLatency()
        {
            var latencies = new Dictionary<string, int>();
            
            // Medir latência para cada região
            foreach (var region in supportedRegions)
            {
                int latency = await MeasureLatencyToRegion(region);
                latencies[region.regionCode] = latency;
            }
            
            return latencies;
        }
        
        async Task<int> MeasureLatencyToRegion(GameLiftRegion region)
        {
            try
            {
                var startTime = DateTime.UtcNow;
                
                // Simular ping para a região
                await Task.Delay(UnityEngine.Random.Range(30, 200));
                
                var latency = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
                Debug.Log($"📡 Latência {region.regionName}: {latency}ms");
                
                return latency;
            }
            catch
            {
                return 999; // Latência alta em caso de erro
            }
        }
        
        // Utility Methods
        string GenerateTicketId()
        {
            return $"arena-brasil-{DateTime.UtcNow.Ticks}-{UnityEngine.Random.Range(1000, 9999)}";
        }
        
        string ExtractGameSessionId(string gameSessionArn)
        {
            // Extrair ID da sessão do ARN
            return gameSessionArn.Split('/').Last();
        }
        
        string GetCurrentPlayerId()
        {
            if (FirebaseBackendService.Instance?.CurrentUser != null)
            {
                return FirebaseBackendService.Instance.CurrentUser.UserId;
            }
            return $"player-{SystemInfo.deviceUniqueIdentifier}";
        }
        
        string GetPlayerSessionData()
        {
            var sessionData = new Dictionary<string, object>
            {
                ["playerName"] = "BrazilianGamer",
                ["selectedHero"] = "Saci",
                ["rank"] = "Bronze",
                ["region"] = "BR"
            };
            
            return JsonUtility.ToJson(sessionData);
        }
        
        public bool IsSearchingForMatch => isSearchingForMatch;
        public string CurrentTicketId => currentTicketId;
        public GameSession CurrentGameSession => currentGameSession;
    }
    
    // Data Classes
    [Serializable]
    public class GameLiftRegion
    {
        public string regionCode;
        public string regionName;
        public string endpoint;
        public int priority;
        public int maxLatency;
    }
    
    [Serializable]
    public class MatchmakingConfiguration
    {
        public string name;
        public string description;
        public string[] gameSessionQueueNames;
        public int requestTimeoutSeconds;
        public int acceptanceTimeoutSeconds;
        public bool acceptanceRequired;
        public string customEventData;
        public int additionalPlayerCount;
        public string backfillMode;
        public string flexMatchMode;
    }
    
    [Serializable]
    public class PlayerAttributes
    {
        public string playerId;
        public Dictionary<string, AttributeValue> attributes;
        public string team;
    }
    
    [Serializable]
    public class AttributeValue
    {
        public double? N; // Number
        public string S; // String
        public List<string> SL; // String List
        public Dictionary<string, double> SDM; // String-Double Map
    }
    
    [Serializable]
    public class StartMatchmakingRequest
    {
        public string ticketId;
        public string configurationName;
        public MatchmakingPlayer[] players;
    }
    
    [Serializable]
    public class MatchmakingPlayer
    {
        public string playerId;
        public Dictionary<string, AttributeValue> playerAttributes;
        public string team;
        public Dictionary<string, int> latencyInMs;
    }
    
    [Serializable]
    public class MatchmakingTicketStatus
    {
        public string ticketId;
        public MatchmakingStatus status;
        public string statusReason;
        public int estimatedWaitTime;
        public GameSessionInfo gameSessionInfo;
    }
    
    [Serializable]
    public class GameSessionInfo
    {
        public string gameSessionArn;
        public string ipAddress;
        public int port;
        public string playerSessionId;
    }
    
    [Serializable]
    public class CreatePlayerSessionRequest
    {
        public string gameSessionId;
        public string playerId;
        public string playerData;
    }
    
    [Serializable]
    public class PlayerSession
    {
        public string playerSessionId;
        public string playerId;
        public string gameSessionId;
        public string fleetId;
        public DateTime creationTime;
        public PlayerSessionStatus status;
        public string ipAddress;
        public int port;
    }
    
    [Serializable]
    public class GameSession
    {
        public string gameSessionId;
        public string name;
        public string fleetId;
        public int maximumPlayerSessionCount;
        public PlayerSessionCreationPolicy playerSessionCreationPolicy;
        public GameSessionStatus status;
        public string statusReason;
        public DateTime creationTime;
    }
    
    public enum MatchmakingStatus
    {
        SEARCHING,
        REQUIRES_ACCEPTANCE,
        PLACING,
        COMPLETED,
        FAILED,
        CANCELLED,
        TIMED_OUT
    }
    
    public enum PlayerSessionStatus
    {
        RESERVED,
        ACTIVE,
        COMPLETED,
        TIMEDOUT
    }
    
    public enum PlayerSessionCreationPolicy
    {
        ACCEPT_ALL,
        DENY_ALL
    }
    
    public enum GameSessionStatus
    {
        ACTIVE,
        ACTIVATING,
        TERMINATED,
        TERMINATING,
        ERROR
    }
}
