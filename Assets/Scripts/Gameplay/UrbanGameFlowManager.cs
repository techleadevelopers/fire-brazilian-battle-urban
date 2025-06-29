
using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;
using ArenaBrasil.PlayerCreation;
using ArenaBrasil.UI;

namespace ArenaBrasil.GameFlow
{
    public class UrbanGameFlowManager : NetworkBehaviour
    {
        public static UrbanGameFlowManager Instance { get; private set; }
        
        [Header("Game Flow Configuration")]
        public GameState currentState = GameState.MainMenu;
        public float lobbyWaitTime = 60f;
        public float matchStartCountdown = 10f;
        public int maxPlayersPerMatch = 60;
        public int minPlayersToStart = 40;
        
        [Header("Map Selection")]
        public List<BrazilianMap> availableMaps = new List<BrazilianMap>();
        public BrazilianMap currentMap;
        
        [Header("Urban Battle Royale Settings")]
        public float matchDuration = 1800f; // 30 minutos
        public int safeZonePhases = 8;
        public float phaseTransitionTime = 120f;
        
        private NetworkVariable<GameState> networkGameState = new NetworkVariable<GameState>();
        private NetworkVariable<float> matchTimer = new NetworkVariable<float>();
        private NetworkVariable<int> playersAlive = new NetworkVariable<int>();
        
        private List<ulong> connectedPlayers = new List<ulong>();
        private Dictionary<ulong, PlayerMatchData> playerData = new Dictionary<ulong, PlayerMatchData>();
        
        public event System.Action<GameState> OnGameStateChanged;
        public event System.Action<BrazilianMap> OnMapSelected;
        public event System.Action<float> OnMatchTimerUpdate;
        public event System.Action<int> OnPlayersAliveChanged;
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeGameFlow();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void InitializeGameFlow()
        {
            Debug.Log("🎮 Arena Brasil - Fluxo de Jogo Urbano Inicializado");
            
            LoadBrazilianMaps();
            SetupNetworkCallbacks();
            
            // Começar no menu principal
            ChangeGameState(GameState.MainMenu);
        }
        
        void LoadBrazilianMaps()
        {
            // === MAPAS URBANOS BRASILEIROS ===
            
            availableMaps.Add(new BrazilianMap
            {
                mapId = "sp_centro_expandido",
                mapName = "São Paulo: Centro Expandido",
                description = "Batalha urbana no coração de SP com arranha-céus, metrô e favelas",
                maxPlayers = 60,
                mapSize = MapSize.Large,
                biome = BrazilianBiome.Urban,
                landmarks = new string[] 
                {
                    "Estação da Sé", "Vale do Anhangabaú", "Edifício Copan", 
                    "Cracolândia", "25 de Março", "Liberdade"
                },
                culturalElements = new string[]
                {
                    "Grafites de rua", "Camelôs", "Metrô", "Prédios antigos",
                    "Food trucks", "Skatistas"
                }
            });
            
            availableMaps.Add(new BrazilianMap
            {
                mapId = "rj_zona_sul",
                mapName = "Rio: Zona Sul Completa",
                description = "De Copacabana ao Leblon, com morros e praias icônicas",
                maxPlayers = 60,
                mapSize = MapSize.Large,
                biome = BrazilianBiome.Coastal,
                landmarks = new string[]
                {
                    "Copacabana", "Ipanema", "Pão de Açúcar", "Favela do Vidigal",
                    "Lagoa Rodrigo de Freitas", "Cristo Redentor"
                },
                culturalElements = new string[]
                {
                    "Posto de gasolina", "Caipirinha beach", "Funk carioca",
                    "Bicicletas da orla", "Futevôlei", "Vendedores ambulantes"
                }
            });
            
            availableMaps.Add(new BrazilianMap
            {
                mapId = "salvador_pelourinho",
                name = "Salvador: Pelourinho + Subúrbio",
                mapName = "Salvador: Centro Histórico",
                description = "Casarões coloniais misturados com periferia baiana",
                maxPlayers = 50,
                mapSize = MapSize.Medium,
                biome = BrazilianBiome.Historic,
                landmarks = new string[]
                {
                    "Pelourinho", "Elevador Lacerda", "Mercado Modelo",
                    "Subúrbio Ferroviário", "Porto da Barra"
                },
                culturalElements = new string[]
                {
                    "Música ao vivo", "Acarajé", "Capoeira roda",
                    "Igreja barroca", "Blocos de carnaval"
                }
            });
            
            availableMaps.Add(new BrazilianMap
            {
                mapId = "brasilia_plano_piloto",
                mapName = "Brasília: Plano Piloto",
                description = "Arquitetura moderna com satélites urbanas",
                maxPlayers = 45,
                mapSize = MapSize.Large,
                biome = BrazilianBiome.Planned,
                landmarks = new string[]
                {
                    "Congresso Nacional", "Palácio da Alvorada", "Esplanada",
                    "Asa Norte", "Asa Sul", "Ceilândia"
                },
                culturalElements = new string[]
                {
                    "Arquitetura Niemeyer", "Eixões", "Quadras residenciais",
                    "Cerrado", "Política"
                }
            });
            
            availableMaps.Add(new BrazilianMap
            {
                mapId = "manaus_amazonia_urbana",
                mapName = "Manaus: Amazônia Urbana",
                description = "Cidade na floresta com rios e igarapés",
                maxPlayers = 55,
                mapSize = MapSize.Large,
                biome = BrazilianBiome.Rainforest,
                landmarks = new string[]
                {
                    "Teatro Amazonas", "Porto de Manaus", "Mercado Adolpho Lisboa",
                    "Encontro das Águas", "Zona Franca", "Floating Houses"
                },
                culturalElements = new string[]
                {
                    "Barcos regionais", "Peixe assado", "Mercado de peixes",
                    "Casas flutuantes", "Indígenas urbanos"
                }
            });
        }
        
        void SetupNetworkCallbacks()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            }
        }
        
        // === FLUXO PRINCIPAL DO JOGO ===
        
        public void StartGameFlow()
        {
            ChangeGameState(GameState.CharacterCreation);
        }
        
        public void OnCharacterCreationComplete()
        {
            ChangeGameState(GameState.MainLobby);
        }
        
        public void StartMatchmaking()
        {
            ChangeGameState(GameState.Matchmaking);
            
            if (IsServer)
            {
                StartCoroutine(MatchmakingProcess());
            }
        }
        
        IEnumerator MatchmakingProcess()
        {
            Debug.Log("🔍 Iniciando matchmaking...");
            
            float waitTime = 0f;
            
            while (connectedPlayers.Count < minPlayersToStart && waitTime < lobbyWaitTime)
            {
                yield return new WaitForSeconds(1f);
                waitTime += 1f;
                
                UpdateMatchmakingUI(connectedPlayers.Count, minPlayersToStart, waitTime);
            }
            
            if (connectedPlayers.Count >= minPlayersToStart)
            {
                SelectMapForMatch();
                ChangeGameState(GameState.PreMatch);
                StartCoroutine(PreMatchCountdown());
            }
            else
            {
                // Voltar ao lobby se não encontrar jogadores suficientes
                ChangeGameState(GameState.MainLobby);
            }
        }
        
        void SelectMapForMatch()
        {
            // Selecionar mapa baseado no horário brasileiro ou votação
            BrazilianMap selectedMap = SelectMapBasedOnTime();
            currentMap = selectedMap;
            
            OnMapSelected?.Invoke(currentMap);
            NotifyMapSelectionClientRpc(currentMap.mapId);
        }
        
        BrazilianMap SelectMapBasedOnTime()
        {
            System.DateTime now = System.DateTime.Now;
            
            // Mapas baseados no horário brasileiro
            if (now.Hour >= 6 && now.Hour < 12)
            {
                // Manhã: Mapas urbanos movimentados
                return availableMaps.Find(m => m.mapId == "sp_centro_expandido");
            }
            else if (now.Hour >= 12 && now.Hour < 18)
            {
                // Tarde: Mapas costeiros
                return availableMaps.Find(m => m.mapId == "rj_zona_sul");
            }
            else if (now.Hour >= 18 && now.Hour < 24)
            {
                // Noite: Mapas históricos com clima noturno
                return availableMaps.Find(m => m.mapId == "salvador_pelourinho");
            }
            else
            {
                // Madrugada: Mapas amazônicos misteriosos
                return availableMaps.Find(m => m.mapId == "manaus_amazonia_urbana");
            }
        }
        
        IEnumerator PreMatchCountdown()
        {
            float countdown = matchStartCountdown;
            
            while (countdown > 0)
            {
                UpdatePreMatchUIClientRpc(countdown);
                yield return new WaitForSeconds(1f);
                countdown -= 1f;
            }
            
            StartMatch();
        }
        
        void StartMatch()
        {
            Debug.Log($"🚀 Iniciando partida no mapa: {currentMap.mapName}");
            
            ChangeGameState(GameState.InMatch);
            matchTimer.Value = matchDuration;
            playersAlive.Value = connectedPlayers.Count;
            
            // Spawnar jogadores no mapa
            SpawnPlayersInMap();
            
            // Iniciar zona segura
            StartSafeZoneManager();
            
            // Iniciar contador de partida
            StartCoroutine(MatchTimer());
        }
        
        void SpawnPlayersInMap()
        {
            if (!IsServer) return;
            
            List<Vector3> spawnPoints = GetMapSpawnPoints(currentMap);
            
            for (int i = 0; i < connectedPlayers.Count; i++)
            {
                ulong clientId = connectedPlayers[i];
                Vector3 spawnPosition = spawnPoints[i % spawnPoints.Count];
                
                SpawnPlayerAtPositionClientRpc(clientId, spawnPosition);
            }
        }
        
        List<Vector3> GetMapSpawnPoints(BrazilianMap map)
        {
            // Retornar pontos de spawn baseados no mapa
            List<Vector3> points = new List<Vector3>();
            
            // Distribuir jogadores em diferentes landmarks
            foreach (string landmark in map.landmarks)
            {
                Vector3 landmarkPosition = GetLandmarkPosition(landmark);
                
                // Adicionar múltiplos spawns ao redor de cada landmark
                for (int i = 0; i < 10; i++)
                {
                    Vector3 offset = new Vector3(
                        Random.Range(-50f, 50f),
                        0f,
                        Random.Range(-50f, 50f)
                    );
                    
                    points.Add(landmarkPosition + offset);
                }
            }
            
            return points;
        }
        
        Vector3 GetLandmarkPosition(string landmark)
        {
            // Retornar posições fixas dos landmarks
            switch (landmark)
            {
                case "Estação da Sé": return new Vector3(0, 0, 0);
                case "Copacabana": return new Vector3(100, 0, 200);
                case "Pelourinho": return new Vector3(-200, 0, 100);
                default: return Vector3.zero;
            }
        }
        
        void StartSafeZoneManager()
        {
            if (SafeZoneController.Instance != null)
            {
                SafeZoneController.Instance.StartSafeZoneSequence(safeZonePhases, phaseTransitionTime);
            }
        }
        
        IEnumerator MatchTimer()
        {
            while (matchTimer.Value > 0 && currentState == GameState.InMatch)
            {
                yield return new WaitForSeconds(1f);
                matchTimer.Value -= 1f;
                OnMatchTimerUpdate?.Invoke(matchTimer.Value);
            }
            
            // Tempo esgotado
            if (matchTimer.Value <= 0)
            {
                EndMatchByTime();
            }
        }
        
        public void OnPlayerEliminated(ulong playerId)
        {
            if (!IsServer) return;
            
            playersAlive.Value--;
            OnPlayersAliveChanged?.Invoke(playersAlive.Value);
            
            // Verificar condição de vitória
            if (playersAlive.Value <= 1)
            {
                EndMatchByElimination();
            }
        }
        
        void EndMatchByElimination()
        {
            Debug.Log("🏆 Partida finalizada por eliminação!");
            EndMatch();
        }
        
        void EndMatchByTime()
        {
            Debug.Log("⏰ Partida finalizada por tempo!");
            EndMatch();
        }
        
        void EndMatch()
        {
            ChangeGameState(GameState.MatchResults);
            
            // Mostrar resultados
            ShowMatchResults();
            
            // Voltar ao lobby após alguns segundos
            StartCoroutine(ReturnToLobbyAfterResults());
        }
        
        void ShowMatchResults()
        {
            // Calcular estatísticas da partida
            MatchResults results = CalculateMatchResults();
            
            ShowMatchResultsClientRpc(results);
        }
        
        MatchResults CalculateMatchResults()
        {
            var results = new MatchResults
            {
                mapPlayed = currentMap.mapName,
                totalPlayers = connectedPlayers.Count,
                matchDuration = matchDuration - matchTimer.Value,
                playerStats = new List<PlayerMatchStats>()
            };
            
            foreach (var playerKvp in playerData)
            {
                ulong playerId = playerKvp.Key;
                PlayerMatchData data = playerKvp.Value;
                
                var stats = new PlayerMatchStats
                {
                    playerId = playerId,
                    kills = data.kills,
                    damage = data.damage,
                    survivalTime = data.survivalTime,
                    placement = data.placement
                };
                
                results.playerStats.Add(stats);
            }
            
            return results;
        }
        
        IEnumerator ReturnToLobbyAfterResults()
        {
            yield return new WaitForSeconds(10f);
            ChangeGameState(GameState.MainLobby);
        }
        
        // === NETWORK RPCS ===
        
        [ClientRpc]
        void NotifyMapSelectionClientRpc(string mapId)
        {
            var selectedMap = availableMaps.Find(m => m.mapId == mapId);
            if (selectedMap != null)
            {
                currentMap = selectedMap;
                OnMapSelected?.Invoke(currentMap);
            }
        }
        
        [ClientRpc]
        void UpdatePreMatchUIClientRpc(float countdown)
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdatePreMatchCountdown(countdown);
            }
        }
        
        [ClientRpc]
        void SpawnPlayerAtPositionClientRpc(ulong targetClientId, Vector3 position)
        {
            if (NetworkManager.Singleton.LocalClientId == targetClientId)
            {
                // Spawnar o jogador local na posição
                var player = FindObjectOfType<PlayerController>();
                if (player != null)
                {
                    player.transform.position = position;
                }
            }
        }
        
        [ClientRpc]
        void ShowMatchResultsClientRpc(MatchResults results)
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowMatchResults(results);
            }
        }
        
        // === NETWORK CALLBACKS ===
        
        void OnClientConnected(ulong clientId)
        {
            if (IsServer)
            {
                connectedPlayers.Add(clientId);
                playerData[clientId] = new PlayerMatchData { playerId = clientId };
                
                Debug.Log($"Jogador {clientId} conectou. Total: {connectedPlayers.Count}");
            }
        }
        
        void OnClientDisconnected(ulong clientId)
        {
            if (IsServer)
            {
                connectedPlayers.Remove(clientId);
                playerData.Remove(clientId);
                
                if (currentState == GameState.InMatch)
                {
                    OnPlayerEliminated(clientId);
                }
                
                Debug.Log($"Jogador {clientId} desconectou. Total: {connectedPlayers.Count}");
            }
        }
        
        // === GAME STATE MANAGEMENT ===
        
        void ChangeGameState(GameState newState)
        {
            GameState previousState = currentState;
            currentState = newState;
            
            if (IsServer)
            {
                networkGameState.Value = newState;
            }
            
            OnGameStateChanged?.Invoke(newState);
            HandleGameStateTransition(previousState, newState);
            
            Debug.Log($"🎮 Game State: {previousState} → {newState}");
        }
        
        void HandleGameStateTransition(GameState from, GameState to)
        {
            switch (to)
            {
                case GameState.MainMenu:
                    if (UIManager.Instance != null)
                        UIManager.Instance.ShowMainMenu();
                    break;
                    
                case GameState.CharacterCreation:
                    if (UIManager.Instance != null)
                        UIManager.Instance.ShowCharacterCreation();
                    break;
                    
                case GameState.MainLobby:
                    if (UIManager.Instance != null)
                        UIManager.Instance.ShowLobby();
                    break;
                    
                case GameState.Matchmaking:
                    if (UIManager.Instance != null)
                        UIManager.Instance.ShowMatchmaking();
                    break;
                    
                case GameState.PreMatch:
                    if (UIManager.Instance != null)
                        UIManager.Instance.ShowPreMatch(currentMap);
                    break;
                    
                case GameState.InMatch:
                    if (UIManager.Instance != null)
                        UIManager.Instance.ShowGameHUD();
                    break;
                    
                case GameState.MatchResults:
                    // Handled in ShowMatchResults
                    break;
            }
        }
        
        void UpdateMatchmakingUI(int current, int required, float waitTime)
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateMatchmakingStatus(current, required, waitTime);
            }
        }
        
        // === PUBLIC GETTERS ===
        
        public GameState GetCurrentState() => currentState;
        public BrazilianMap GetCurrentMap() => currentMap;
        public List<BrazilianMap> GetAvailableMaps() => availableMaps;
        public float GetMatchTimeRemaining() => matchTimer.Value;
        public int GetPlayersAlive() => playersAlive.Value;
    }
    
    // === ENUMS E CLASSES ===
    
    public enum GameState
    {
        MainMenu,
        CharacterCreation,
        MainLobby,
        Matchmaking,
        PreMatch,
        InMatch,
        MatchResults,
        Settings
    }
    
    public enum MapSize
    {
        Small,
        Medium,
        Large,
        Massive
    }
    
    public enum BrazilianBiome
    {
        Urban,
        Coastal,
        Historic,
        Planned,
        Rainforest,
        Countryside
    }
    
    [System.Serializable]
    public class BrazilianMap
    {
        public string mapId;
        public string mapName;
        public string name; // For compatibility
        public string description;
        public int maxPlayers;
        public MapSize mapSize;
        public BrazilianBiome biome;
        public string[] landmarks;
        public string[] culturalElements;
        public Sprite mapIcon;
        public Texture2D mapPreview;
    }
    
    [System.Serializable]
    public class PlayerMatchData
    {
        public ulong playerId;
        public int kills;
        public float damage;
        public float survivalTime;
        public int placement;
    }
    
    [System.Serializable]
    public class MatchResults
    {
        public string mapPlayed;
        public int totalPlayers;
        public float matchDuration;
        public List<PlayerMatchStats> playerStats;
    }
    
    [System.Serializable]
    public class PlayerMatchStats
    {
        public ulong playerId;
        public int kills;
        public float damage;
        public float survivalTime;
        public int placement;
    }
}
