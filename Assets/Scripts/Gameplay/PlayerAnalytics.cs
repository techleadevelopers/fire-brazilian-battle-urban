
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System;
using ArenaBrasil.Backend;

namespace ArenaBrasil.Analytics
{
    public class PlayerAnalytics : MonoBehaviour
    {
        public static PlayerAnalytics Instance { get; private set; }
        
        [Header("Analytics Configuration")]
        public bool enableRealTimeTracking = true;
        public float sessionTrackingInterval = 30f;
        public int maxEventsPerSession = 1000;
        
        private Dictionary<string, PlayerAnalyticsData> playerData = new Dictionary<string, PlayerAnalyticsData>();
        private List<GameEvent> currentSessionEvents = new List<GameEvent>();
        private DateTime sessionStartTime;
        private bool isTrackingSession = false;
        
        // Events
        public event Action<PlayerAnalyticsData> OnPlayerDataUpdated;
        public event Action<GameEvent> OnGameEventTracked;
        
        [System.Serializable]
        public class PlayerAnalyticsData
        {
            public string playerId;
            public DateTime firstPlayDate;
            public DateTime lastPlayDate;
            public int totalSessions;
            public float totalPlayTime;
            public int totalMatches;
            public int totalKills;
            public int totalDeaths;
            public float averageSessionLength;
            public float kdrRatio;
            public int winCount;
            public float winRate;
            public int highestPlacement;
            public float averagePlacement;
            public Dictionary<string, int> weaponUsage = new Dictionary<string, int>();
            public Dictionary<string, int> heroUsage = new Dictionary<string, int>();
            public Dictionary<string, float> mapPerformance = new Dictionary<string, float>();
            public List<string> achievements = new List<string>();
            public PlayerBehaviorData behavior = new PlayerBehaviorData();
            public DeviceInfo deviceInfo = new DeviceInfo();
        }
        
        [System.Serializable]
        public class PlayerBehaviorData
        {
            public float averageMovementSpeed;
            public int chatMessagesPerMatch;
            public float aimAccuracy;
            public int itemsLooted;
            public float survivalRate;
            public Dictionary<string, int> actionCounts = new Dictionary<string, int>();
            public List<string> preferredGameModes = new List<string>();
            public float toxicityScore;
            public int reportsReceived;
            public int reportsGiven;
        }
        
        [System.Serializable]
        public class DeviceInfo
        {
            public string deviceModel;
            public string operatingSystem;
            public int ram;
            public string processor;
            public float averageFPS;
            public string networkType;
            public float averagePing;
            public string graphicsQuality;
        }
        
        [System.Serializable]
        public class GameEvent
        {
            public string eventType;
            public string playerId;
            public DateTime timestamp;
            public Dictionary<string, object> parameters = new Dictionary<string, object>();
            public Vector3 position;
            public string mapId;
            public float sessionTime;
        }
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeAnalytics();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void InitializeAnalytics()
        {
            Debug.Log("Arena Brasil - Initializing Player Analytics");
            LoadPlayerData();
            
            if (enableRealTimeTracking)
            {
                InvokeRepeating(nameof(TrackSessionMetrics), sessionTrackingInterval, sessionTrackingInterval);
            }
        }
        
        public void StartSession(string playerId)
        {
            sessionStartTime = DateTime.Now;
            isTrackingSession = true;
            currentSessionEvents.Clear();
            
            var data = GetOrCreatePlayerData(playerId);
            data.totalSessions++;
            data.lastPlayDate = DateTime.Now;
            
            // Track session start
            TrackEvent("session_start", playerId, new Dictionary<string, object>
            {
                {"session_number", data.totalSessions},
                {"device_model", SystemInfo.deviceModel},
                {"os", SystemInfo.operatingSystem}
            });
            
            // Update device info
            UpdateDeviceInfo(playerId);
            
            Debug.Log($"Analytics session started for player: {playerId}");
        }
        
        public void EndSession(string playerId)
        {
            if (!isTrackingSession) return;
            
            var sessionDuration = (float)(DateTime.Now - sessionStartTime).TotalSeconds;
            var data = GetOrCreatePlayerData(playerId);
            
            data.totalPlayTime += sessionDuration;
            data.averageSessionLength = data.totalPlayTime / data.totalSessions;
            
            TrackEvent("session_end", playerId, new Dictionary<string, object>
            {
                {"session_duration", sessionDuration},
                {"events_tracked", currentSessionEvents.Count}
            });
            
            SaveSessionData(playerId, sessionDuration);
            isTrackingSession = false;
            
            Debug.Log($"Analytics session ended for player: {playerId}, Duration: {sessionDuration}s");
        }
        
        public void TrackEvent(string eventType, string playerId, Dictionary<string, object> parameters = null)
        {
            if (!isTrackingSession || currentSessionEvents.Count >= maxEventsPerSession)
                return;
            
            var gameEvent = new GameEvent
            {
                eventType = eventType,
                playerId = playerId,
                timestamp = DateTime.Now,
                parameters = parameters ?? new Dictionary<string, object>(),
                sessionTime = (float)(DateTime.Now - sessionStartTime).TotalSeconds
            };
            
            // Add position if player exists
            var player = FindPlayerController(playerId);
            if (player != null)
            {
                gameEvent.position = player.transform.position;
                gameEvent.mapId = GetCurrentMapId();
            }
            
            currentSessionEvents.Add(gameEvent);
            OnGameEventTracked?.Invoke(gameEvent);
            
            // Process event for player data updates
            ProcessEventForPlayerData(gameEvent);
        }
        
        void ProcessEventForPlayerData(GameEvent gameEvent)
        {
            var data = GetOrCreatePlayerData(gameEvent.playerId);
            
            switch (gameEvent.eventType)
            {
                case "player_kill":
                    data.totalKills++;
                    data.kdrRatio = data.totalDeaths > 0 ? (float)data.totalKills / data.totalDeaths : data.totalKills;
                    
                    if (gameEvent.parameters.ContainsKey("weapon"))
                    {
                        string weapon = gameEvent.parameters["weapon"].ToString();
                        if (!data.weaponUsage.ContainsKey(weapon))
                            data.weaponUsage[weapon] = 0;
                        data.weaponUsage[weapon]++;
                    }
                    break;
                    
                case "player_death":
                    data.totalDeaths++;
                    data.kdrRatio = data.totalDeaths > 0 ? (float)data.totalKills / data.totalDeaths : data.totalKills;
                    break;
                    
                case "match_end":
                    data.totalMatches++;
                    
                    if (gameEvent.parameters.ContainsKey("placement"))
                    {
                        int placement = (int)gameEvent.parameters["placement"];
                        if (placement == 1)
                        {
                            data.winCount++;
                        }
                        
                        if (placement < data.highestPlacement || data.highestPlacement == 0)
                        {
                            data.highestPlacement = placement;
                        }
                        
                        // Calculate average placement
                        data.averagePlacement = (data.averagePlacement * (data.totalMatches - 1) + placement) / data.totalMatches;
                    }
                    
                    data.winRate = data.totalMatches > 0 ? (float)data.winCount / data.totalMatches : 0f;
                    break;
                    
                case "hero_selected":
                    if (gameEvent.parameters.ContainsKey("hero"))
                    {
                        string hero = gameEvent.parameters["hero"].ToString();
                        if (!data.heroUsage.ContainsKey(hero))
                            data.heroUsage[hero] = 0;
                        data.heroUsage[hero]++;
                    }
                    break;
                    
                case "item_looted":
                    data.behavior.itemsLooted++;
                    break;
                    
                case "chat_message":
                    data.behavior.chatMessagesPerMatch++;
                    break;
            }
            
            OnPlayerDataUpdated?.Invoke(data);
        }
        
        void UpdateDeviceInfo(string playerId)
        {
            var data = GetOrCreatePlayerData(playerId);
            var deviceInfo = data.deviceInfo;
            
            deviceInfo.deviceModel = SystemInfo.deviceModel;
            deviceInfo.operatingSystem = SystemInfo.operatingSystem;
            deviceInfo.ram = SystemInfo.systemMemorySize;
            deviceInfo.processor = SystemInfo.processorType;
            deviceInfo.graphicsQuality = QualitySettings.names[QualitySettings.GetQualityLevel()];
            
            // Track FPS
            deviceInfo.averageFPS = (deviceInfo.averageFPS + (1f / Time.deltaTime)) / 2f;
            
            // Network info
            deviceInfo.networkType = Application.internetReachability.ToString();
        }
        
        void TrackSessionMetrics()
        {
            if (!isTrackingSession) return;
            
            // Track various session metrics
            TrackEvent("session_heartbeat", GetCurrentPlayerId(), new Dictionary<string, object>
            {
                {"fps", 1f / Time.deltaTime},
                {"memory_usage", UnityEngine.Profiling.Profiler.GetTotalAllocatedMemory(false)},
                {"active_scene", UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}
            });
        }
        
        public PlayerAnalyticsData GetPlayerData(string playerId)
        {
            return GetOrCreatePlayerData(playerId);
        }
        
        PlayerAnalyticsData GetOrCreatePlayerData(string playerId)
        {
            if (!playerData.ContainsKey(playerId))
            {
                playerData[playerId] = new PlayerAnalyticsData
                {
                    playerId = playerId,
                    firstPlayDate = DateTime.Now,
                    lastPlayDate = DateTime.Now
                };
            }
            return playerData[playerId];
        }
        
        void SaveSessionData(string playerId, float sessionDuration)
        {
            // Save to Firebase Analytics
            if (FirebaseBackendService.Instance != null)
            {
                // Send analytics data to Firebase
            }
            
            // Save locally
            SavePlayerData();
        }
        
        void LoadPlayerData()
        {
            // Load from PlayerPrefs or Firebase
            string dataJson = PlayerPrefs.GetString("PlayerAnalyticsData", "{}");
            try
            {
                // Parse and load data
                Debug.Log("Player analytics data loaded");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load analytics data: {e.Message}");
            }
        }
        
        void SavePlayerData()
        {
            try
            {
                // Save to PlayerPrefs and Firebase
                string dataJson = JsonUtility.ToJson(playerData);
                PlayerPrefs.SetString("PlayerAnalyticsData", dataJson);
                PlayerPrefs.Save();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save analytics data: {e.Message}");
            }
        }
        
        // Utility methods
        PlayerController FindPlayerController(string playerId)
        {
            // Find player by ID
            return null; // Implement based on your player management system
        }
        
        string GetCurrentMapId()
        {
            return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        }
        
        string GetCurrentPlayerId()
        {
            // Get current player ID from authentication system
            return FirebaseBackendService.Instance?.GetCurrentUserId() ?? "guest";
        }
        
        // Public API for specific tracking
        public void TrackKill(string playerId, string victimId, string weaponId, Vector3 position)
        {
            TrackEvent("player_kill", playerId, new Dictionary<string, object>
            {
                {"victim_id", victimId},
                {"weapon", weaponId},
                {"position_x", position.x},
                {"position_y", position.y},
                {"position_z", position.z}
            });
        }
        
        public void TrackDeath(string playerId, string killerId, string weaponId)
        {
            TrackEvent("player_death", playerId, new Dictionary<string, object>
            {
                {"killer_id", killerId},
                {"weapon", weaponId}
            });
        }
        
        public void TrackMatchResult(string playerId, int placement, int kills, float survivalTime)
        {
            TrackEvent("match_end", playerId, new Dictionary<string, object>
            {
                {"placement", placement},
                {"kills", kills},
                {"survival_time", survivalTime}
            });
        }
        
        public void TrackPurchase(string playerId, string itemId, int price, string currency)
        {
            TrackEvent("item_purchase", playerId, new Dictionary<string, object>
            {
                {"item_id", itemId},
                {"price", price},
                {"currency", currency}
            });
        }
        
        public void TrackAchievement(string playerId, string achievementId)
        {
            var data = GetOrCreatePlayerData(playerId);
            if (!data.achievements.Contains(achievementId))
            {
                data.achievements.Add(achievementId);
                
                TrackEvent("achievement_unlocked", playerId, new Dictionary<string, object>
                {
                    {"achievement_id", achievementId}
                });
            }
        }
        
        // Analytics reporting
        public Dictionary<string, object> GeneratePlayerReport(string playerId)
        {
            var data = GetPlayerData(playerId);
            
            return new Dictionary<string, object>
            {
                {"total_sessions", data.totalSessions},
                {"total_play_time", data.totalPlayTime},
                {"average_session_length", data.averageSessionLength},
                {"total_matches", data.totalMatches},
                {"win_rate", data.winRate},
                {"kdr_ratio", data.kdrRatio},
                {"highest_placement", data.highestPlacement},
                {"average_placement", data.averagePlacement},
                {"favorite_weapon", GetFavoriteWeapon(data)},
                {"favorite_hero", GetFavoriteHero(data)},
                {"device_model", data.deviceInfo.deviceModel},
                {"average_fps", data.deviceInfo.averageFPS}
            };
        }
        
        string GetFavoriteWeapon(PlayerAnalyticsData data)
        {
            string favorite = "";
            int maxUsage = 0;
            
            foreach (var weapon in data.weaponUsage)
            {
                if (weapon.Value > maxUsage)
                {
                    maxUsage = weapon.Value;
                    favorite = weapon.Key;
                }
            }
            
            return favorite;
        }
        
        string GetFavoriteHero(PlayerAnalyticsData data)
        {
            string favorite = "";
            int maxUsage = 0;
            
            foreach (var hero in data.heroUsage)
            {
                if (hero.Value > maxUsage)
                {
                    maxUsage = hero.Value;
                    favorite = hero.Key;
                }
            }
            
            return favorite;
        }
    }
}
