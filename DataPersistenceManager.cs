
using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;

namespace ArenaBrasil.Data
{
    public class DataPersistenceManager : MonoBehaviour
    {
        public static DataPersistenceManager Instance { get; private set; }
        
        [Header("Persistence Configuration")]
        public bool enableOfflineMode = true;
        public float autoSaveInterval = 30f;
        public int maxOfflineDataAge = 7; // dias
        
        // Cache local para dados offline
        private Dictionary<string, object> localDataCache = new Dictionary<string, object>();
        private Dictionary<string, DateTime> dataCacheTimestamps = new Dictionary<string, DateTime>();
        
        // Events
        public event Action<string, object> OnDataSaved;
        public event Action<string, object> OnDataLoaded;
        public event Action<string> OnDataError;
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializePersistence();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void Start()
        {
            if (autoSaveInterval > 0)
            {
                InvokeRepeating(nameof(AutoSave), autoSaveInterval, autoSaveInterval);
            }
        }
        
        void InitializePersistence()
        {
            Debug.Log("🗄️ Inicializando sistema de persistência Arena Brasil");
            
            // Carregar dados locais ao inicializar
            LoadLocalCache();
            
            // Limpar dados antigos
            CleanupOldData();
        }
        
        // === DADOS DE PROGRESSO DE JOGADORES ===
        public async Task<PlayerProgressData> LoadPlayerProgress(string playerId)
        {
            try
            {
                string cacheKey = $"player_progress_{playerId}";
                
                // Tentar cache local primeiro
                if (localDataCache.ContainsKey(cacheKey))
                {
                    var cachedData = (PlayerProgressData)localDataCache[cacheKey];
                    if (!IsDataStale(cacheKey))
                    {
                        Debug.Log($"📂 Carregando progresso do cache: {playerId}");
                        OnDataLoaded?.Invoke(cacheKey, cachedData);
                        return cachedData;
                    }
                }
                
                // Carregar do Firebase
                if (FirebaseBackendService.Instance?.IsSignedIn == true)
                {
                    var playerProfile = await FirebaseBackendService.Instance.GetPlayerProfile(playerId);
                    
                    if (playerProfile != null)
                    {
                        var progressData = new PlayerProgressData
                        {
                            playerId = playerId,
                            level = playerProfile.Level,
                            xp = playerProfile.XP,
                            totalMatches = playerProfile.Matches,
                            totalWins = playerProfile.Wins,
                            totalKills = playerProfile.Kills,
                            lastLogin = playerProfile.LastLogin,
                            achievements = new List<string>(),
                            questProgress = new Dictionary<string, QuestProgress>(),
                            statistics = new PlayerStatistics
                            {
                                playtime = 0,
                                favoriteHero = playerProfile.SelectedHero,
                                bestPlacement = 1,
                                longestWinStreak = 0
                            }
                        };
                        
                        // Cache localmente
                        CacheData(cacheKey, progressData);
                        OnDataLoaded?.Invoke(cacheKey, progressData);
                        return progressData;
                    }
                }
                
                // Retornar dados padrão se nada for encontrado
                return CreateDefaultPlayerProgress(playerId);
                
            }
            catch (Exception e)
            {
                Debug.LogError($"Erro ao carregar progresso do jogador: {e.Message}");
                OnDataError?.Invoke($"Falha ao carregar progresso: {e.Message}");
                return CreateDefaultPlayerProgress(playerId);
            }
        }
        
        public async Task SavePlayerProgress(PlayerProgressData progressData)
        {
            try
            {
                string cacheKey = $"player_progress_{progressData.playerId}";
                
                // Salvar localmente primeiro
                CacheData(cacheKey, progressData);
                
                // Salvar no Firebase se conectado
                if (FirebaseBackendService.Instance?.IsSignedIn == true)
                {
                    await FirebaseBackendService.Instance.UpdatePlayerStats(
                        progressData.playerId,
                        progressData.xp,
                        0, // coins não estão em progressData
                        progressData.totalWins > 0,
                        progressData.totalKills
                    );
                    
                    Debug.Log($"💾 Progresso salvo: {progressData.playerId}");
                }
                
                // Salvar localmente para backup
                await SaveToLocalStorage(cacheKey, progressData);
                OnDataSaved?.Invoke(cacheKey, progressData);
                
            }
            catch (Exception e)
            {
                Debug.LogError($"Erro ao salvar progresso: {e.Message}");
                OnDataError?.Invoke($"Falha ao salvar progresso: {e.Message}");
            }
        }
        
        // === DADOS DE GACHA E PITY ===
        public async Task<GachaData> LoadGachaData(string playerId)
        {
            try
            {
                string cacheKey = $"gacha_data_{playerId}";
                
                if (localDataCache.ContainsKey(cacheKey) && !IsDataStale(cacheKey))
                {
                    return (GachaData)localDataCache[cacheKey];
                }
                
                // Carregar do Firebase via Cloud Functions
                if (CloudFunctionsManager.Instance != null)
                {
                    // Implementar chamada para cloud function que retorna dados de gacha
                    // Por enquanto, criar dados padrão
                    var gachaData = new GachaData
                    {
                        playerId = playerId,
                        pityCounters = new Dictionary<string, int>
                        {
                            ["hero_banner"] = 0,
                            ["weapon_banner"] = 0,
                            ["event_banner"] = 0
                        },
                        pullHistory = new Dictionary<string, List<GachaPull>>(),
                        guaranteedCounters = new Dictionary<string, int>
                        {
                            ["hero_banner"] = 0,
                            ["weapon_banner"] = 0,
                            ["event_banner"] = 0
                        },
                        totalPulls = 0,
                        lastPullTime = DateTime.MinValue
                    };
                    
                    CacheData(cacheKey, gachaData);
                    return gachaData;
                }
                
                return CreateDefaultGachaData(playerId);
                
            }
            catch (Exception e)
            {
                Debug.LogError($"Erro ao carregar dados de gacha: {e.Message}");
                return CreateDefaultGachaData(playerId);
            }
        }
        
        public async Task SaveGachaData(GachaData gachaData)
        {
            try
            {
                string cacheKey = $"gacha_data_{gachaData.playerId}";
                CacheData(cacheKey, gachaData);
                await SaveToLocalStorage(cacheKey, gachaData);
                
                Debug.Log($"🎰 Dados de gacha salvos: {gachaData.playerId}");
                OnDataSaved?.Invoke(cacheKey, gachaData);
                
            }
            catch (Exception e)
            {
                Debug.LogError($"Erro ao salvar dados de gacha: {e.Message}");
                OnDataError?.Invoke($"Falha ao salvar gacha: {e.Message}");
            }
        }
        
        // === DADOS DE CLÃS ===
        public async Task<ClanData> LoadClanData(string clanId)
        {
            try
            {
                string cacheKey = $"clan_data_{clanId}";
                
                if (localDataCache.ContainsKey(cacheKey) && !IsDataStale(cacheKey))
                {
                    return (ClanData)localDataCache[cacheKey];
                }
                
                // Carregar via Firebase/Cloud Functions
                var clanData = new ClanData
                {
                    clanId = clanId,
                    name = "Arena Brasil Clan",
                    tag = "[AB]",
                    description = "Clã brasileiro competitivo",
                    leaderId = "",
                    members = new List<ClanMember>(),
                    level = 1,
                    xp = 0,
                    createdAt = DateTime.UtcNow,
                    settings = new ClanSettings
                    {
                        isPublic = true,
                        autoAccept = false,
                        language = "pt-BR",
                        region = "Brazil"
                    },
                    statistics = new ClanStatistics
                    {
                        totalMatches = 0,
                        totalWins = 0,
                        totalKills = 0,
                        averageRank = "Bronze"
                    }
                };
                
                CacheData(cacheKey, clanData);
                return clanData;
                
            }
            catch (Exception e)
            {
                Debug.LogError($"Erro ao carregar dados do clã: {e.Message}");
                return new ClanData { clanId = clanId };
            }
        }
        
        public async Task SaveClanData(ClanData clanData)
        {
            try
            {
                string cacheKey = $"clan_data_{clanData.clanId}";
                CacheData(cacheKey, clanData);
                await SaveToLocalStorage(cacheKey, clanData);
                
                Debug.Log($"👥 Dados do clã salvos: {clanData.clanId}");
                OnDataSaved?.Invoke(cacheKey, clanData);
                
            }
            catch (Exception e)
            {
                Debug.LogError($"Erro ao salvar dados do clã: {e.Message}");
            }
        }
        
        // === DADOS DE EVENTOS AO VIVO ===
        public async Task<List<LiveEventData>> LoadActiveEvents()
        {
            try
            {
                string cacheKey = "active_events";
                
                if (localDataCache.ContainsKey(cacheKey) && !IsDataStale(cacheKey, 300)) // 5 min cache
                {
                    return (List<LiveEventData>)localDataCache[cacheKey];
                }
                
                var activeEvents = new List<LiveEventData>
                {
                    new LiveEventData
                    {
                        eventId = "brasil_celebration_2024",
                        name = "Celebração Brasil 2024",
                        description = "Evento especial comemorativo brasileiro",
                        type = "celebration",
                        startTime = DateTime.UtcNow.AddHours(-1),
                        endTime = DateTime.UtcNow.AddDays(7),
                        isActive = true,
                        rewards = new List<EventReward>
                        {
                            new EventReward { type = "coins", amount = 1000 },
                            new EventReward { type = "gems", amount = 50 },
                            new EventReward { type = "skin", itemId = "brasil_flag_emote" }
                        },
                        challenges = new List<EventChallenge>
                        {
                            new EventChallenge
                            {
                                challengeId = "play_brazilian_hero",
                                name = "Jogar com Herói Brasileiro",
                                description = "Vença 3 partidas usando Saci, Curupira ou Iara",
                                requirement = 3,
                                progress = 0,
                                completed = false
                            }
                        }
                    }
                };
                
                CacheData(cacheKey, activeEvents);
                return activeEvents;
                
            }
            catch (Exception e)
            {
                Debug.LogError($"Erro ao carregar eventos ativos: {e.Message}");
                return new List<LiveEventData>();
            }
        }
        
        public async Task<EventProgressData> LoadEventProgress(string playerId, string eventId)
        {
            try
            {
                string cacheKey = $"event_progress_{eventId}_{playerId}";
                
                if (localDataCache.ContainsKey(cacheKey))
                {
                    return (EventProgressData)localDataCache[cacheKey];
                }
                
                var progressData = new EventProgressData
                {
                    playerId = playerId,
                    eventId = eventId,
                    challengeProgress = new Dictionary<string, int>(),
                    completedChallenges = new List<string>(),
                    collectedRewards = new List<string>(),
                    totalProgress = 0,
                    startedAt = DateTime.UtcNow
                };
                
                CacheData(cacheKey, progressData);
                return progressData;
                
            }
            catch (Exception e)
            {
                Debug.LogError($"Erro ao carregar progresso do evento: {e.Message}");
                return new EventProgressData { playerId = playerId, eventId = eventId };
            }
        }
        
        // === DADOS DE RANKING ===
        public async Task<PlayerRankData> LoadPlayerRank(string playerId, string seasonId)
        {
            try
            {
                string cacheKey = $"player_rank_{seasonId}_{playerId}";
                
                if (localDataCache.ContainsKey(cacheKey) && !IsDataStale(cacheKey))
                {
                    return (PlayerRankData)localDataCache[cacheKey];
                }
                
                var rankData = new PlayerRankData
                {
                    playerId = playerId,
                    seasonId = seasonId,
                    currentRank = "bronze_i",
                    rankPoints = 1000,
                    matches = 0,
                    wins = 0,
                    topPlacements = 0,
                    bestRank = "bronze_i",
                    rankHistory = new List<RankChange>()
                };
                
                CacheData(cacheKey, rankData);
                return rankData;
                
            }
            catch (Exception e)
            {
                Debug.LogError($"Erro ao carregar rank do jogador: {e.Message}");
                return new PlayerRankData { playerId = playerId, seasonId = seasonId };
            }
        }
        
        public async Task SavePlayerRank(PlayerRankData rankData)
        {
            try
            {
                string cacheKey = $"player_rank_{rankData.seasonId}_{rankData.playerId}";
                CacheData(cacheKey, rankData);
                await SaveToLocalStorage(cacheKey, rankData);
                
                Debug.Log($"🏆 Rank salvo: {rankData.playerId} - {rankData.currentRank}");
                OnDataSaved?.Invoke(cacheKey, rankData);
                
            }
            catch (Exception e)
            {
                Debug.LogError($"Erro ao salvar rank: {e.Message}");
            }
        }
        
        // === DADOS SOCIAIS ===
        public async Task<SocialData> LoadSocialData(string playerId)
        {
            try
            {
                string cacheKey = $"social_data_{playerId}";
                
                if (localDataCache.ContainsKey(cacheKey))
                {
                    return (SocialData)localDataCache[cacheKey];
                }
                
                var socialData = new SocialData
                {
                    playerId = playerId,
                    connectedAccounts = new Dictionary<string, SocialAccount>(),
                    shareHistory = new List<ShareRecord>(),
                    friendsList = new List<string>(),
                    blockedPlayers = new List<string>(),
                    privacy = new PrivacySettings
                    {
                        showOnlineStatus = true,
                        allowFriendRequests = true,
                        showStatistics = true
                    }
                };
                
                CacheData(cacheKey, socialData);
                return socialData;
                
            }
            catch (Exception e)
            {
                Debug.LogError($"Erro ao carregar dados sociais: {e.Message}");
                return new SocialData { playerId = playerId };
            }
        }
        
        // === MÉTODOS AUXILIARES ===
        void CacheData(string key, object data)
        {
            localDataCache[key] = data;
            dataCacheTimestamps[key] = DateTime.UtcNow;
        }
        
        bool IsDataStale(string key, int maxAgeSeconds = 3600) // 1 hora padrão
        {
            if (!dataCacheTimestamps.ContainsKey(key))
                return true;
                
            return (DateTime.UtcNow - dataCacheTimestamps[key]).TotalSeconds > maxAgeSeconds;
        }
        
        async Task SaveToLocalStorage(string key, object data)
        {
            try
            {
                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                string path = Application.persistentDataPath + $"/arena_brasil_{key}.json";
                
                await System.IO.File.WriteAllTextAsync(path, json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Falha ao salvar localmente: {e.Message}");
            }
        }
        
        void LoadLocalCache()
        {
            try
            {
                string dataPath = Application.persistentDataPath;
                string[] files = System.IO.Directory.GetFiles(dataPath, "arena_brasil_*.json");
                
                foreach (string file in files)
                {
                    try
                    {
                        string json = System.IO.File.ReadAllText(file);
                        string fileName = System.IO.Path.GetFileNameWithoutExtension(file);
                        string key = fileName.Replace("arena_brasil_", "");
                        
                        // Determinar tipo baseado na chave
                        object data = DeserializeByKey(key, json);
                        if (data != null)
                        {
                            localDataCache[key] = data;
                            dataCacheTimestamps[key] = System.IO.File.GetLastWriteTime(file);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"Erro ao carregar arquivo {file}: {e.Message}");
                    }
                }
                
                Debug.Log($"📂 Cache local carregado: {localDataCache.Count} entradas");
            }
            catch (Exception e)
            {
                Debug.LogError($"Erro ao carregar cache local: {e.Message}");
            }
        }
        
        object DeserializeByKey(string key, string json)
        {
            try
            {
                if (key.StartsWith("player_progress_"))
                    return JsonConvert.DeserializeObject<PlayerProgressData>(json);
                else if (key.StartsWith("gacha_data_"))
                    return JsonConvert.DeserializeObject<GachaData>(json);
                else if (key.StartsWith("clan_data_"))
                    return JsonConvert.DeserializeObject<ClanData>(json);
                else if (key.StartsWith("player_rank_"))
                    return JsonConvert.DeserializeObject<PlayerRankData>(json);
                else if (key.StartsWith("social_data_"))
                    return JsonConvert.DeserializeObject<SocialData>(json);
                else if (key == "active_events")
                    return JsonConvert.DeserializeObject<List<LiveEventData>>(json);
                    
                return null;
            }
            catch
            {
                return null;
            }
        }
        
        void CleanupOldData()
        {
            try
            {
                string dataPath = Application.persistentDataPath;
                string[] files = System.IO.Directory.GetFiles(dataPath, "arena_brasil_*.json");
                DateTime cutoffDate = DateTime.UtcNow.AddDays(-maxOfflineDataAge);
                
                int deletedCount = 0;
                foreach (string file in files)
                {
                    if (System.IO.File.GetLastWriteTime(file) < cutoffDate)
                    {
                        System.IO.File.Delete(file);
                        deletedCount++;
                    }
                }
                
                if (deletedCount > 0)
                {
                    Debug.Log($"🗑️ Dados antigos limpos: {deletedCount} arquivos removidos");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Erro na limpeza de dados: {e.Message}");
            }
        }
        
        void AutoSave()
        {
            if (localDataCache.Count == 0) return;
            
            Debug.Log("💾 Auto-save executado");
            
            // Salvar dados modificados recentemente
            foreach (var kvp in dataCacheTimestamps)
            {
                if ((DateTime.UtcNow - kvp.Value).TotalMinutes < autoSaveInterval / 60f)
                {
                    if (localDataCache.ContainsKey(kvp.Key))
                    {
                        _ = SaveToLocalStorage(kvp.Key, localDataCache[kvp.Key]);
                    }
                }
            }
        }
        
        // Métodos de criação de dados padrão
        PlayerProgressData CreateDefaultPlayerProgress(string playerId)
        {
            return new PlayerProgressData
            {
                playerId = playerId,
                level = 1,
                xp = 0,
                totalMatches = 0,
                totalWins = 0,
                totalKills = 0,
                lastLogin = DateTime.UtcNow,
                achievements = new List<string>(),
                questProgress = new Dictionary<string, QuestProgress>(),
                statistics = new PlayerStatistics()
            };
        }
        
        GachaData CreateDefaultGachaData(string playerId)
        {
            return new GachaData
            {
                playerId = playerId,
                pityCounters = new Dictionary<string, int>(),
                pullHistory = new Dictionary<string, List<GachaPull>>(),
                guaranteedCounters = new Dictionary<string, int>(),
                totalPulls = 0,
                lastPullTime = DateTime.MinValue
            };
        }
        
        void OnDestroy()
        {
            CancelInvoke();
        }
    }
    
    // === CLASSES DE DADOS ===
    [Serializable]
    public class PlayerProgressData
    {
        public string playerId;
        public int level;
        public int xp;
        public int totalMatches;
        public int totalWins;
        public int totalKills;
        public DateTime lastLogin;
        public List<string> achievements;
        public Dictionary<string, QuestProgress> questProgress;
        public PlayerStatistics statistics;
    }
    
    [Serializable]
    public class QuestProgress
    {
        public string questId;
        public int progress;
        public int requirement;
        public bool completed;
        public DateTime startedAt;
        public DateTime completedAt;
    }
    
    [Serializable]
    public class PlayerStatistics
    {
        public float playtime;
        public string favoriteHero;
        public int bestPlacement;
        public int longestWinStreak;
        public Dictionary<string, int> weaponKills;
        public Dictionary<string, float> heroPlaytime;
    }
    
    [Serializable]
    public class GachaData
    {
        public string playerId;
        public Dictionary<string, int> pityCounters;
        public Dictionary<string, List<GachaPull>> pullHistory;
        public Dictionary<string, int> guaranteedCounters;
        public int totalPulls;
        public DateTime lastPullTime;
    }
    
    [Serializable]
    public class GachaPull
    {
        public string itemId;
        public string rarity;
        public DateTime pulledAt;
        public int pullNumber;
        public bool wasGuaranteed;
    }
    
    [Serializable]
    public class ClanData
    {
        public string clanId;
        public string name;
        public string tag;
        public string description;
        public string leaderId;
        public List<ClanMember> members;
        public int level;
        public int xp;
        public DateTime createdAt;
        public ClanSettings settings;
        public ClanStatistics statistics;
    }
    
    [Serializable]
    public class ClanMember
    {
        public string playerId;
        public string displayName;
        public string role;
        public DateTime joinedAt;
        public int contributedXP;
        public DateTime lastActive;
    }
    
    [Serializable]
    public class ClanSettings
    {
        public bool isPublic;
        public bool autoAccept;
        public string language;
        public string region;
        public int minLevel;
    }
    
    [Serializable]
    public class ClanStatistics
    {
        public int totalMatches;
        public int totalWins;
        public int totalKills;
        public string averageRank;
        public float winRate;
    }
    
    [Serializable]
    public class LiveEventData
    {
        public string eventId;
        public string name;
        public string description;
        public string type;
        public DateTime startTime;
        public DateTime endTime;
        public bool isActive;
        public List<EventReward> rewards;
        public List<EventChallenge> challenges;
    }
    
    [Serializable]
    public class EventReward
    {
        public string type;
        public int amount;
        public string itemId;
    }
    
    [Serializable]
    public class EventChallenge
    {
        public string challengeId;
        public string name;
        public string description;
        public int requirement;
        public int progress;
        public bool completed;
    }
    
    [Serializable]
    public class EventProgressData
    {
        public string playerId;
        public string eventId;
        public Dictionary<string, int> challengeProgress;
        public List<string> completedChallenges;
        public List<string> collectedRewards;
        public int totalProgress;
        public DateTime startedAt;
    }
    
    [Serializable]
    public class PlayerRankData
    {
        public string playerId;
        public string seasonId;
        public string currentRank;
        public int rankPoints;
        public int matches;
        public int wins;
        public int topPlacements;
        public string bestRank;
        public List<RankChange> rankHistory;
    }
    
    [Serializable]
    public class RankChange
    {
        public string fromRank;
        public string toRank;
        public int pointsChange;
        public DateTime timestamp;
        public string reason;
    }
    
    [Serializable]
    public class SocialData
    {
        public string playerId;
        public Dictionary<string, SocialAccount> connectedAccounts;
        public List<ShareRecord> shareHistory;
        public List<string> friendsList;
        public List<string> blockedPlayers;
        public PrivacySettings privacy;
    }
    
    [Serializable]
    public class SocialAccount
    {
        public string platform;
        public string accountId;
        public string username;
        public DateTime connectedAt;
        public bool isActive;
    }
    
    [Serializable]
    public class ShareRecord
    {
        public string platform;
        public string contentType;
        public string contentId;
        public DateTime sharedAt;
        public Dictionary<string, object> metadata;
    }
    
    [Serializable]
    public class PrivacySettings
    {
        public bool showOnlineStatus;
        public bool allowFriendRequests;
        public bool showStatistics;
        public bool allowClanInvites;
    }
}
