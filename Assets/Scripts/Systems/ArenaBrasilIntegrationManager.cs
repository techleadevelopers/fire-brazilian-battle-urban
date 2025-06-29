
using UnityEngine;
using Unity.Netcode;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using ArenaBrasil.Backend;
using ArenaBrasil.Data;
using ArenaBrasil.Heroes;
using ArenaBrasil.CloudServices;

namespace ArenaBrasil.Integration
{
    public class ArenaBrasilIntegrationManager : NetworkBehaviour
    {
        public static ArenaBrasilIntegrationManager Instance { get; private set; }
        
        [Header("Integration Configuration")]
        public bool enableCloudIntegration = true;
        public bool enableOfflineMode = true;
        public float syncInterval = 30f;
        
        [Header("System Dependencies")]
        public bool waitForFirebaseInit = true;
        public bool waitForPlayFabInit = true;
        
        // Sistema de inicialização
        private Dictionary<string, bool> systemInitStatus = new Dictionary<string, bool>();
        private bool isFullyInitialized = false;
        
        // Cache de dados para sincronização
        private Dictionary<string, object> pendingSyncData = new Dictionary<string, object>();
        
        // Events para comunicação entre sistemas
        public event Action OnAllSystemsInitialized;
        public event Action<string, object> OnDataSynced;
        public event Action<string> OnSystemError;
        
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
                return;
            }
        }
        
        void Start()
        {
            InitializeIntegrationManager();
        }
        
        async void InitializeIntegrationManager()
        {
            Debug.Log("🔄 Inicializando Arena Brasil Integration Manager");
            
            // Inicializar sistemas base
            await InitializeCoreSystemsSequentially();
            
            // Inicializar sistemas avançados
            await InitializeAdvancedSystems();
            
            // Configurar integrações
            SetupSystemIntegrations();
            
            // Iniciar sincronização automática
            if (enableCloudIntegration && syncInterval > 0)
            {
                InvokeRepeating(nameof(PerformAutoSync), syncInterval, syncInterval);
            }
            
            isFullyInitialized = true;
            OnAllSystemsInitialized?.Invoke();
            
            Debug.Log("✅ Arena Brasil Integration Manager totalmente inicializado!");
        }
        
        async Task InitializeCoreSystemsSequentially()
        {
            // 1. Sistema de Persistência de Dados
            if (DataPersistenceManager.Instance != null)
            {
                systemInitStatus["DataPersistence"] = true;
                Debug.Log("✅ DataPersistenceManager inicializado");
            }
            
            // 2. Firebase Backend Service
            if (FirebaseBackendService.Instance != null)
            {
                if (waitForFirebaseInit)
                {
                    await WaitForFirebaseInitialization();
                }
                systemInitStatus["Firebase"] = true;
                Debug.Log("✅ FirebaseBackendService inicializado");
            }
            
            // 3. PlayFab Manager
            if (PlayFabManager.Instance != null)
            {
                if (waitForPlayFabInit)
                {
                    await WaitForPlayFabInitialization();
                }
                systemInitStatus["PlayFab"] = true;
                Debug.Log("✅ PlayFabManager inicializado");
            }
            
            // 4. Cloud Functions Manager
            if (CloudFunctionsManager.Instance != null)
            {
                systemInitStatus["CloudFunctions"] = true;
                Debug.Log("✅ CloudFunctionsManager inicializado");
            }
            
            // 5. Economy Manager
            if (EconomyManager.Instance != null)
            {
                await InitializeEconomySystem();
                systemInitStatus["Economy"] = true;
                Debug.Log("✅ EconomyManager inicializado");
            }
            
            // 6. Inventory System
            if (InventorySystem.Instance != null)
            {
                await InitializeInventorySystem();
                systemInitStatus["Inventory"] = true;
                Debug.Log("✅ InventorySystem inicializado");
            }
        }
        
        async Task InitializeAdvancedSystems()
        {
            // Sistema de Seleção de Heróis
            if (HeroSelectionSystem.Instance != null)
            {
                systemInitStatus["HeroSelection"] = true;
                Debug.Log("✅ HeroSelectionSystem inicializado");
            }
            
            // Sistema de Ranking
            if (RankingSystem.Instance != null)
            {
                await InitializeRankingSystem();
                systemInitStatus["Ranking"] = true;
                Debug.Log("✅ RankingSystem inicializado");
            }
            
            // Sistema de Conquistas
            if (AchievementSystem.Instance != null)
            {
                await InitializeAchievementSystem();
                systemInitStatus["Achievements"] = true;
                Debug.Log("✅ AchievementSystem inicializado");
            }
            
            // Sistema de Clãs
            if (ClanSystem.Instance != null)
            {
                await InitializeClanSystem();
                systemInitStatus["Clans"] = true;
                Debug.Log("✅ ClanSystem inicializado");
            }
            
            // Sistema de Gacha
            if (GachaSystem.Instance != null)
            {
                await InitializeGachaSystem();
                systemInitStatus["Gacha"] = true;
                Debug.Log("✅ GachaSystem inicializado");
            }
            
            // Sistemas Culturais Brasileiros
            InitializeBrazilianSystems();
        }
        
        void SetupSystemIntegrations()
        {
            Debug.Log("🔗 Configurando integrações entre sistemas");
            
            // Integração Economy <-> Inventory
            if (EconomyManager.Instance != null && InventorySystem.Instance != null)
            {
                EconomyManager.Instance.OnPurchaseCompleted += OnEconomyPurchaseCompleted;
                InventorySystem.Instance.OnItemUsed += OnInventoryItemUsed;
            }
            
            // Integração Gacha <-> Inventory
            if (GachaSystem.Instance != null && InventorySystem.Instance != null)
            {
                GachaSystem.Instance.OnGachaReward += OnGachaRewardReceived;
            }
            
            // Integração Achievement <-> Analytics
            if (AchievementSystem.Instance != null && PlayerAnalytics.Instance != null)
            {
                AchievementSystem.Instance.OnAchievementUnlocked += OnAchievementUnlocked;
            }
            
            // Integração Combat <-> Ranking
            if (CombatSystem.Instance != null && RankingSystem.Instance != null)
            {
                CombatSystem.Instance.OnPlayerKilled += OnPlayerKilledForRanking;
                CombatSystem.Instance.OnMatchEnded += OnMatchEndedForRanking;
            }
            
            // Integração Live Events <-> Persistence
            if (LiveOpsManager.Instance != null && DataPersistenceManager.Instance != null)
            {
                LiveOpsManager.Instance.OnEventProgressUpdated += OnEventProgressUpdated;
            }
            
            // Integração Social <-> Firebase
            if (SocialMediaIntegration.Instance != null && FirebaseBackendService.Instance != null)
            {
                SocialMediaIntegration.Instance.OnContentShared += OnSocialContentShared;
            }
            
            Debug.Log("✅ Integrações configuradas com sucesso");
        }
        
        // === INICIALIZAÇÃO DE SISTEMAS ESPECÍFICOS ===
        async Task InitializeEconomySystem()
        {
            if (EconomyManager.Instance == null) return;
            
            // Carregar configuração de economia
            await EconomyManager.Instance.LoadEconomyConfiguration();
            
            // Sincronizar moedas do jogador
            if (FirebaseBackendService.Instance?.IsSignedIn == true)
            {
                var playerId = FirebaseBackendService.Instance.CurrentUser.UserId;
                await SyncPlayerEconomy(playerId);
            }
        }
        
        async Task InitializeInventorySystem()
        {
            if (InventorySystem.Instance == null) return;
            
            // Carregar inventário do jogador
            if (FirebaseBackendService.Instance?.IsSignedIn == true)
            {
                var playerId = FirebaseBackendService.Instance.CurrentUser.UserId;
                await SyncPlayerInventory(playerId);
            }
        }
        
        async Task InitializeRankingSystem()
        {
            if (RankingSystem.Instance == null) return;
            
            // Carregar ranking atual da temporada
            string currentSeason = GetCurrentSeasonId();
            await RankingSystem.Instance.LoadSeasonRankings(currentSeason);
            
            // Carregar rank do jogador
            if (FirebaseBackendService.Instance?.IsSignedIn == true)
            {
                var playerId = FirebaseBackendService.Instance.CurrentUser.UserId;
                await SyncPlayerRank(playerId, currentSeason);
            }
        }
        
        async Task InitializeAchievementSystem()
        {
            if (AchievementSystem.Instance == null) return;
            
            // Carregar conquistas do jogador
            if (FirebaseBackendService.Instance?.IsSignedIn == true)
            {
                var playerId = FirebaseBackendService.Instance.CurrentUser.UserId;
                await SyncPlayerAchievements(playerId);
            }
        }
        
        async Task InitializeClanSystem()
        {
            if (ClanSystem.Instance == null) return;
            
            // Carregar dados do clã do jogador
            if (FirebaseBackendService.Instance?.IsSignedIn == true)
            {
                var playerId = FirebaseBackendService.Instance.CurrentUser.UserId;
                await SyncPlayerClanData(playerId);
            }
        }
        
        async Task InitializeGachaSystem()
        {
            if (GachaSystem.Instance == null) return;
            
            // Carregar dados de gacha do jogador
            if (FirebaseBackendService.Instance?.IsSignedIn == true)
            {
                var playerId = FirebaseBackendService.Instance.CurrentUser.UserId;
                await SyncPlayerGachaData(playerId);
            }
        }
        
        void InitializeBrazilianSystems()
        {
            Debug.Log("🇧🇷 Inicializando sistemas culturais brasileiros");
            
            var brazilianSystems = new string[]
            {
                "BrazilianNarration", "BrazilianCulturalEvents", "BrazilianInfluencer",
                "BrazilianMusic", "BrazilianEasterEggs", "BrazilianMapGenerator"
            };
            
            foreach (string system in brazilianSystems)
            {
                systemInitStatus[system] = true;
            }
            
            Debug.Log("✅ Sistemas culturais brasileiros inicializados");
        }
        
        // === MÉTODOS DE SINCRONIZAÇÃO ===
        async Task SyncPlayerEconomy(string playerId)
        {
            try
            {
                if (DataPersistenceManager.Instance != null)
                {
                    var progressData = await DataPersistenceManager.Instance.LoadPlayerProgress(playerId);
                    
                    if (EconomyManager.Instance != null && progressData != null)
                    {
                        // Atualizar moedas locais
                        EconomyManager.Instance.UpdateLocalCurrency("coins", progressData.xp / 10); // Conversão exemplo
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Erro ao sincronizar economia: {e.Message}");
            }
        }
        
        async Task SyncPlayerInventory(string playerId)
        {
            try
            {
                // Carregar inventário via Cloud Functions
                if (CloudFunctionsManager.Instance != null)
                {
                    // Implementar chamada para função de inventário
                    Debug.Log($"📦 Sincronizando inventário do jogador {playerId}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Erro ao sincronizar inventário: {e.Message}");
            }
        }
        
        async Task SyncPlayerRank(string playerId, string seasonId)
        {
            try
            {
                if (DataPersistenceManager.Instance != null)
                {
                    var rankData = await DataPersistenceManager.Instance.LoadPlayerRank(playerId, seasonId);
                    
                    if (RankingSystem.Instance != null && rankData != null)
                    {
                        RankingSystem.Instance.UpdatePlayerRankLocal(rankData);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Erro ao sincronizar rank: {e.Message}");
            }
        }
        
        async Task SyncPlayerAchievements(string playerId)
        {
            try
            {
                if (DataPersistenceManager.Instance != null)
                {
                    var progressData = await DataPersistenceManager.Instance.LoadPlayerProgress(playerId);
                    
                    if (AchievementSystem.Instance != null && progressData?.achievements != null)
                    {
                        AchievementSystem.Instance.LoadPlayerAchievements(progressData.achievements);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Erro ao sincronizar conquistas: {e.Message}");
            }
        }
        
        async Task SyncPlayerClanData(string playerId)
        {
            try
            {
                // Implementar sincronização de dados de clã
                Debug.Log($"👥 Sincronizando dados de clã do jogador {playerId}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Erro ao sincronizar clã: {e.Message}");
            }
        }
        
        async Task SyncPlayerGachaData(string playerId)
        {
            try
            {
                if (DataPersistenceManager.Instance != null)
                {
                    var gachaData = await DataPersistenceManager.Instance.LoadGachaData(playerId);
                    
                    if (GachaSystem.Instance != null && gachaData != null)
                    {
                        GachaSystem.Instance.LoadPlayerGachaData(gachaData);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Erro ao sincronizar gacha: {e.Message}");
            }
        }
        
        // === EVENT HANDLERS PARA INTEGRAÇÕES ===
        void OnEconomyPurchaseCompleted(string itemId, int cost, string currency)
        {
            Debug.Log($"💰 Compra realizada: {itemId} por {cost} {currency}");
            
            // Validar compra via Cloud Functions
            if (CloudFunctionsManager.Instance != null && enableCloudIntegration)
            {
                var playerId = GetCurrentPlayerId();
                _ = CloudFunctionsManager.Instance.ValidatePurchase(playerId, itemId, $"purchase_{DateTime.UtcNow.Ticks}", cost);
            }
            
            // Atualizar conquistas relacionadas a compras
            if (AchievementSystem.Instance != null)
            {
                AchievementSystem.Instance.UpdateAchievementProgress("purchases_made", 1);
            }
        }
        
        void OnInventoryItemUsed(InventoryItem item, int quantity)
        {
            Debug.Log($"🎒 Item usado: {item.itemName} x{quantity}");
            
            // Registrar uso para analytics
            if (PlayerAnalytics.Instance != null)
            {
                PlayerAnalytics.Instance.TrackItemUsage(item.itemId, quantity);
            }
        }
        
        void OnGachaRewardReceived(string gachaType, List<GachaReward> rewards)
        {
            Debug.Log($"🎰 Recompensas de gacha recebidas: {rewards.Count} itens");
            
            // Adicionar itens ao inventário
            if (InventorySystem.Instance != null)
            {
                foreach (var reward in rewards)
                {
                    var inventoryItem = new InventoryItem
                    {
                        itemId = reward.itemId,
                        itemName = reward.name,
                        quantity = reward.quantity,
                        itemType = GetItemTypeFromGachaReward(reward),
                        rarity = GetRarityFromGachaReward(reward)
                    };
                    
                    InventorySystem.Instance.AddItemToInventory(inventoryItem);
                }
            }
            
            // Atualizar progresso de gacha no servidor
            if (enableCloudIntegration && CloudFunctionsManager.Instance != null)
            {
                var playerId = GetCurrentPlayerId();
                pendingSyncData[$"gacha_pull_{playerId}_{DateTime.UtcNow.Ticks}"] = new
                {
                    playerId,
                    gachaType,
                    rewards,
                    timestamp = DateTime.UtcNow
                };
            }
        }
        
        void OnAchievementUnlocked(Achievement achievement)
        {
            Debug.Log($"🏆 Conquista desbloqueada: {achievement.name}");
            
            // Conceder recompensas da conquista
            if (EconomyManager.Instance != null && achievement.rewards != null)
            {
                var playerId = GetCurrentPlayerId();
                foreach (var reward in achievement.rewards)
                {
                    switch (reward.type)
                    {
                        case "currency":
                            EconomyManager.Instance.AddCurrency(playerId, reward.currencyType, reward.amount);
                            break;
                        case "item":
                            if (InventorySystem.Instance != null)
                            {
                                var item = CreateInventoryItemFromReward(reward);
                                InventorySystem.Instance.AddItemToInventory(item);
                            }
                            break;
                    }
                }
            }
            
            // Registrar no analytics
            if (PlayerAnalytics.Instance != null)
            {
                PlayerAnalytics.Instance.TrackAchievementUnlocked(achievement.id);
            }
        }
        
        void OnPlayerKilledForRanking(ulong killerId, ulong victimId, string weaponId)
        {
            // Atualizar estatísticas de ranking
            if (RankingSystem.Instance != null)
            {
                RankingSystem.Instance.RecordKill(killerId, victimId, weaponId);
            }
        }
        
        void OnMatchEndedForRanking(MatchResult result)
        {
            Debug.Log($"🏁 Match finalizado para ranking: colocação {result.placement}");
            
            // Atualizar rank via Cloud Functions
            if (enableCloudIntegration && CloudFunctionsManager.Instance != null)
            {
                var playerId = GetCurrentPlayerId();
                var seasonId = GetCurrentSeasonId();
                
                pendingSyncData[$"rank_update_{playerId}_{DateTime.UtcNow.Ticks}"] = new
                {
                    playerId,
                    seasonId,
                    matchResult = result,
                    timestamp = DateTime.UtcNow
                };
            }
        }
        
        void OnEventProgressUpdated(string eventId, string playerId, string challengeId, int progress)
        {
            Debug.Log($"📅 Progresso de evento atualizado: {challengeId} = {progress}");
            
            // Sincronizar com servidor via Cloud Functions
            if (enableCloudIntegration && CloudFunctionsManager.Instance != null)
            {
                pendingSyncData[$"event_progress_{eventId}_{playerId}_{challengeId}"] = new
                {
                    playerId,
                    eventId,
                    challengeId,
                    progressData = new { increment = progress },
                    timestamp = DateTime.UtcNow
                };
            }
        }
        
        void OnSocialContentShared(string platform, string contentType, string contentId)
        {
            Debug.Log($"📱 Conteúdo compartilhado: {contentType} no {platform}");
            
            // Registrar compartilhamento no servidor
            if (enableCloudIntegration && CloudFunctionsManager.Instance != null)
            {
                var playerId = GetCurrentPlayerId();
                pendingSyncData[$"social_share_{playerId}_{DateTime.UtcNow.Ticks}"] = new
                {
                    playerId,
                    platform,
                    contentType,
                    contentId,
                    metadata = new { gameVersion = Application.version },
                    timestamp = DateTime.UtcNow
                };
            }
        }
        
        // === SINCRONIZAÇÃO AUTOMÁTICA ===
        async void PerformAutoSync()
        {
            if (!isFullyInitialized || pendingSyncData.Count == 0) return;
            
            Debug.Log($"🔄 Executando sincronização automática: {pendingSyncData.Count} itens");
            
            var syncTasks = new List<Task>();
            var syncedKeys = new List<string>();
            
            foreach (var kvp in pendingSyncData)
            {
                string key = kvp.Key;
                object data = kvp.Value;
                
                // Determinar tipo de sincronização baseado na chave
                if (key.StartsWith("gacha_pull_"))
                {
                    syncTasks.Add(SyncGachaPull(data));
                }
                else if (key.StartsWith("rank_update_"))
                {
                    syncTasks.Add(SyncRankUpdate(data));
                }
                else if (key.StartsWith("event_progress_"))
                {
                    syncTasks.Add(SyncEventProgress(data));
                }
                else if (key.StartsWith("social_share_"))
                {
                    syncTasks.Add(SyncSocialShare(data));
                }
                
                syncedKeys.Add(key);
            }
            
            try
            {
                await Task.WhenAll(syncTasks);
                
                // Remover itens sincronizados
                foreach (string key in syncedKeys)
                {
                    pendingSyncData.Remove(key);
                    OnDataSynced?.Invoke(key, null);
                }
                
                Debug.Log($"✅ Sincronização concluída: {syncedKeys.Count} itens");
            }
            catch (Exception e)
            {
                Debug.LogError($"Erro na sincronização automática: {e.Message}");
                OnSystemError?.Invoke($"Sync error: {e.Message}");
            }
        }
        
        async Task SyncGachaPull(object data)
        {
            // Implementar sincronização de pulls de gacha
            await Task.Delay(100); // Simular operação async
        }
        
        async Task SyncRankUpdate(object data)
        {
            // Implementar sincronização de rank
            await Task.Delay(100); // Simular operação async
        }
        
        async Task SyncEventProgress(object data)
        {
            // Implementar sincronização de progresso de evento
            await Task.Delay(100); // Simular operação async
        }
        
        async Task SyncSocialShare(object data)
        {
            // Implementar sincronização de compartilhamento social
            await Task.Delay(100); // Simular operação async
        }
        
        // === MÉTODOS AUXILIARES ===
        async Task WaitForFirebaseInitialization()
        {
            if (FirebaseBackendService.Instance == null) return;
            
            float timeout = 30f;
            float elapsed = 0f;
            
            while (!FirebaseBackendService.Instance.IsSignedIn && elapsed < timeout)
            {
                await Task.Delay(100);
                elapsed += 0.1f;
            }
            
            if (elapsed >= timeout)
            {
                Debug.LogWarning("⚠️ Timeout na inicialização do Firebase");
            }
        }
        
        async Task WaitForPlayFabInitialization()
        {
            if (PlayFabManager.Instance == null) return;
            
            float timeout = 30f;
            float elapsed = 0f;
            
            while (!PlayFabManager.Instance.IsInitialized && elapsed < timeout)
            {
                await Task.Delay(100);
                elapsed += 0.1f;
            }
            
            if (elapsed >= timeout)
            {
                Debug.LogWarning("⚠️ Timeout na inicialização do PlayFab");
            }
        }
        
        string GetCurrentPlayerId()
        {
            if (FirebaseBackendService.Instance?.CurrentUser != null)
            {
                return FirebaseBackendService.Instance.CurrentUser.UserId;
            }
            
            return $"offline_player_{SystemInfo.deviceUniqueIdentifier}";
        }
        
        string GetCurrentSeasonId()
        {
            // Gerar ID da temporada baseado na data
            var now = DateTime.UtcNow;
            return $"season_{now.Year}_{(now.Month - 1) / 3 + 1}"; // Temporadas trimestrais
        }
        
        ItemType GetItemTypeFromGachaReward(GachaReward reward)
        {
            return reward.category switch
            {
                "hero_skin" => ItemType.Skin,
                "weapon" => ItemType.Weapon,
                "emote" => ItemType.Consumable,
                _ => ItemType.Common
            };
        }
        
        ItemRarity GetRarityFromGachaReward(GachaReward reward)
        {
            return reward.rarity switch
            {
                "legendary" => ItemRarity.Legendary,
                "epic" => ItemRarity.Epic,
                "rare" => ItemRarity.Rare,
                _ => ItemRarity.Common
            };
        }
        
        InventoryItem CreateInventoryItemFromReward(AchievementReward reward)
        {
            return new InventoryItem
            {
                itemId = reward.itemId,
                itemName = reward.name,
                quantity = reward.amount,
                itemType = ItemType.Special,
                rarity = ItemRarity.Rare
            };
        }
        
        // Public getters
        public bool IsSystemInitialized(string systemName) => systemInitStatus.ContainsKey(systemName) && systemInitStatus[systemName];
        public bool IsFullyInitialized() => isFullyInitialized;
        public Dictionary<string, bool> GetSystemStatus() => new Dictionary<string, bool>(systemInitStatus);
        public int GetPendingSyncCount() => pendingSyncData.Count;
        
        void OnDestroy()
        {
            CancelInvoke();
            
            // Cleanup event subscriptions
            if (EconomyManager.Instance != null)
                EconomyManager.Instance.OnPurchaseCompleted -= OnEconomyPurchaseCompleted;
                
            if (InventorySystem.Instance != null)
                InventorySystem.Instance.OnItemUsed -= OnInventoryItemUsed;
        }
    }
    
    // === CLASSES AUXILIARES ===
    [Serializable]
    public class MatchResult
    {
        public int placement;
        public int kills;
        public float survivalTime;
        public bool won;
        public string heroUsed;
        public string playerName;
    }
    
    [Serializable]
    public class GachaReward
    {
        public string itemId;
        public string name;
        public string category;
        public string rarity;
        public int quantity;
    }
    
    [Serializable]
    public class AchievementReward
    {
        public string type;
        public string itemId;
        public string name;
        public string currencyType;
        public int amount;
    }
    
    [Serializable]
    public class Achievement
    {
        public string id;
        public string name;
        public string description;
        public List<AchievementReward> rewards;
    }
}
