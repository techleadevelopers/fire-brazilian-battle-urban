
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.EconomyModels;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace ArenaBrasil.Economy
{
    public class PlayFabManager : MonoBehaviour
    {
        public static PlayFabManager Instance { get; private set; }
        
        [Header("PlayFab Configuration")]
        public string titleId = "YOUR_PLAYFAB_TITLE_ID";
        public bool autoLogin = true;
        
        [Header("Economy Settings")]
        public List<VirtualCurrency> virtualCurrencies = new List<VirtualCurrency>();
        public List<CatalogItem> catalogItems = new List<CatalogItem>();
        
        // Events
        public event Action<string> OnLoginSuccess;
        public event Action<string> OnLoginError;
        public event Action<List<CatalogItem>> OnCatalogLoaded;
        public event Action<GetPlayerInventoryResult> OnInventoryLoaded;
        
        private bool isInitialized = false;
        private Dictionary<string, uint> playerCurrencies = new Dictionary<string, uint>();
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializePlayFab();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void InitializePlayFab()
        {
            if (!string.IsNullOrEmpty(titleId))
            {
                PlayFabSettings.staticSettings.TitleId = titleId;
            }
            
            SetupVirtualCurrencies();
            SetupCatalogItems();
            
            if (autoLogin)
            {
                LoginWithDeviceId();
            }
            
            Debug.Log("PlayFab Manager inicializado para Arena Brasil");
        }
        
        void SetupVirtualCurrencies()
        {
            virtualCurrencies.Add(new VirtualCurrency
            {
                CurrencyCode = "CO",
                DisplayName = "Moedas Brasil",
                InitialDeposit = 100,
                RechargeRate = 0,
                RechargeMax = 0
            });
            
            virtualCurrencies.Add(new VirtualCurrency
            {
                CurrencyCode = "GE",
                DisplayName = "Gemas Lendárias",
                InitialDeposit = 10,
                RechargeRate = 0,
                RechargeMax = 0
            });
            
            virtualCurrencies.Add(new VirtualCurrency
            {
                CurrencyCode = "PR",
                DisplayName = "Moedas Premium",
                InitialDeposit = 0,
                RechargeRate = 0,
                RechargeMax = 0
            });
        }
        
        void SetupCatalogItems()
        {
            // Skins de Heróis Brasileiros
            catalogItems.Add(new CatalogItem
            {
                ItemId = "skin_saci_gold",
                DisplayName = "Saci Dourado",
                Description = "Skin lendária do Saci com efeitos dourados especiais",
                VirtualCurrencyPrices = new Dictionary<string, uint> { { "GE", 100 } },
                ItemClass = "HeroSkin",
                Tags = new List<string> { "legendary", "hero", "saci", "gold" }
            });
            
            catalogItems.Add(new CatalogItem
            {
                ItemId = "skin_curupira_amazon",
                DisplayName = "Curupira da Amazônia",
                Description = "Guardião supremo da floresta amazônica",
                VirtualCurrencyPrices = new Dictionary<string, uint> { { "GE", 150 } },
                ItemClass = "HeroSkin",
                Tags = new List<string> { "legendary", "hero", "curupira", "amazon" }
            });
            
            // Armas Brasileiras
            catalogItems.Add(new CatalogItem
            {
                ItemId = "weapon_fal_brasil",
                DisplayName = "FAL Brasileiro Elite",
                Description = "Fuzil de assalto FAL com camuflagem da bandeira brasileira",
                VirtualCurrencyPrices = new Dictionary<string, uint> { { "CO", 2500 } },
                ItemClass = "WeaponSkin",
                Tags = new List<string> { "epic", "weapon", "fal", "brasil" }
            });
            
            // Battle Pass
            catalogItems.Add(new CatalogItem
            {
                ItemId = "battlepass_legends_s1",
                DisplayName = "Passe de Batalha - Lendas do Brasil S1",
                Description = "100 níveis de recompensas exclusivas temáticas brasileiras",
                VirtualCurrencyPrices = new Dictionary<string, uint> { { "GE", 200 } },
                ItemClass = "BattlePass",
                Tags = new List<string> { "battlepass", "season1", "exclusive" }
            });
            
            // Emotes
            catalogItems.Add(new CatalogItem
            {
                ItemId = "emote_samba_victory",
                DisplayName = "Dança da Vitória Samba",
                Description = "Celebre suas vitórias com o ritmo do samba brasileiro",
                VirtualCurrencyPrices = new Dictionary<string, uint> { { "CO", 500 } },
                ItemClass = "Emote",
                Tags = new List<string> { "common", "emote", "dance", "samba" }
            });
        }
        
        public void LoginWithDeviceId()
        {
            var request = new LoginWithAndroidDeviceIDRequest
            {
                AndroidDeviceId = SystemInfo.deviceUniqueIdentifier,
                CreateAccount = true,
                InfoRequestParameters = new GetPlayerCombinedInfoRequestParams
                {
                    GetPlayerProfile = true,
                    GetPlayerStatistics = true,
                    GetUserVirtualCurrency = true,
                    GetUserInventory = true
                }
            };
            
            PlayFabClientAPI.LoginWithAndroidDeviceID(request, OnLoginResult, OnLoginFailure);
        }
        
        void OnLoginResult(LoginResult result)
        {
            Debug.Log("Login PlayFab sucesso: " + result.PlayFabId);
            isInitialized = true;
            
            // Armazenar moedas do jogador
            if (result.InfoResultPayload?.UserVirtualCurrency != null)
            {
                playerCurrencies.Clear();
                foreach (var currency in result.InfoResultPayload.UserVirtualCurrency)
                {
                    playerCurrencies[currency.Key] = (uint)currency.Value;
                }
            }
            
            OnLoginSuccess?.Invoke(result.PlayFabId);
            LoadCatalog();
        }
        
        void OnLoginFailure(PlayFabError error)
        {
            Debug.LogError("Erro login PlayFab: " + error.GenerateErrorReport());
            OnLoginError?.Invoke(error.ErrorMessage);
        }
        
        public void LoadCatalog()
        {
            var request = new GetCatalogItemsRequest();
            PlayFabClientAPI.GetCatalogItems(request, OnCatalogResult, OnCatalogError);
        }
        
        void OnCatalogResult(GetCatalogItemsResult result)
        {
            Debug.Log($"Catálogo carregado: {result.Catalog.Count} itens");
            OnCatalogLoaded?.Invoke(result.Catalog);
        }
        
        void OnCatalogError(PlayFabError error)
        {
            Debug.LogError("Erro ao carregar catálogo: " + error.GenerateErrorReport());
        }
        
        public void PurchaseItem(string itemId, string virtualCurrency, uint price)
        {
            var request = new PurchaseItemRequest
            {
                ItemId = itemId,
                VirtualCurrency = virtualCurrency,
                Price = (int)price
            };
            
            PlayFabClientAPI.PurchaseItem(request, OnPurchaseSuccess, OnPurchaseError);
        }
        
        void OnPurchaseSuccess(PurchaseItemResult result)
        {
            Debug.Log($"Compra realizada com sucesso!");
            
            // Atualizar moedas locais
            foreach (var currency in result.RemainingVirtualCurrency)
            {
                playerCurrencies[currency.Key] = (uint)currency.Value;
            }
            
            // Notificar outros sistemas
            if (InventorySystem.Instance != null)
            {
                // Adicionar item ao inventário
                foreach (var item in result.Items)
                {
                    var inventoryItem = new InventoryItem
                    {
                        itemId = item.ItemId,
                        itemName = item.DisplayName,
                        quantity = 1,
                        itemType = GetItemTypeFromClass(item.ItemClass),
                        rarity = GetRarityFromTags(item.CustomData)
                    };
                    
                    InventorySystem.Instance.AddItemToInventory(inventoryItem);
                }
            }
        }
        
        void OnPurchaseError(PlayFabError error)
        {
            Debug.LogError("Erro na compra: " + error.GenerateErrorReport());
        }
        
        public async Task GrantCurrencyToPlayer(string playerId, string currencyCode, int amount)
        {
            var request = new PlayFab.AdminModels.AddUserVirtualCurrencyRequest
            {
                PlayFabId = playerId,
                VirtualCurrency = currencyCode,
                Amount = amount
            };
            
            // Usar Admin API para conceder moedas (servidor autoritário)
            // Em produção, isso seria feito via Cloud Functions
            Debug.Log($"Concedendo {amount} {currencyCode} para jogador {playerId}");
        }
        
        public void UpdatePlayerStatistics(Dictionary<string, int> statistics)
        {
            var statisticUpdates = new List<StatisticUpdate>();
            
            foreach (var stat in statistics)
            {
                statisticUpdates.Add(new StatisticUpdate
                {
                    StatisticName = stat.Key,
                    Value = stat.Value
                });
            }
            
            var request = new UpdatePlayerStatisticsRequest
            {
                Statistics = statisticUpdates
            };
            
            PlayFabClientAPI.UpdatePlayerStatistics(request, OnStatisticsUpdated, OnStatisticsError);
        }
        
        void OnStatisticsUpdated(UpdatePlayerStatisticsResult result)
        {
            Debug.Log("Estatísticas do jogador atualizadas");
        }
        
        void OnStatisticsError(PlayFabError error)
        {
            Debug.LogError("Erro ao atualizar estatísticas: " + error.GenerateErrorReport());
        }
        
        public void SubmitScore(string leaderboardName, int score)
        {
            var request = new UpdatePlayerStatisticsRequest
            {
                Statistics = new List<StatisticUpdate>
                {
                    new StatisticUpdate
                    {
                        StatisticName = leaderboardName,
                        Value = score
                    }
                }
            };
            
            PlayFabClientAPI.UpdatePlayerStatistics(request, OnScoreSubmitted, OnScoreError);
        }
        
        void OnScoreSubmitted(UpdatePlayerStatisticsResult result)
        {
            Debug.Log("Score submetido ao leaderboard");
        }
        
        void OnScoreError(PlayFabError error)
        {
            Debug.LogError("Erro ao submeter score: " + error.GenerateErrorReport());
        }
        
        public void GetLeaderboard(string leaderboardName, int maxResults = 100)
        {
            var request = new GetLeaderboardRequest
            {
                StatisticName = leaderboardName,
                MaxResultsCount = maxResults
            };
            
            PlayFabClientAPI.GetLeaderboard(request, OnLeaderboardResult, OnLeaderboardError);
        }
        
        void OnLeaderboardResult(GetLeaderboardResult result)
        {
            Debug.Log($"Leaderboard carregado: {result.Leaderboard.Count} entradas");
            
            // Enviar para sistema de ranking
            if (RankingSystem.Instance != null)
            {
                RankingSystem.Instance.UpdateLeaderboardDisplay(result.Leaderboard);
            }
        }
        
        void OnLeaderboardError(PlayFabError error)
        {
            Debug.LogError("Erro ao carregar leaderboard: " + error.GenerateErrorReport());
        }
        
        // Utility methods
        ItemType GetItemTypeFromClass(string itemClass)
        {
            return itemClass switch
            {
                "HeroSkin" => ItemType.Skin,
                "WeaponSkin" => ItemType.Weapon,
                "Emote" => ItemType.Consumable,
                "BattlePass" => ItemType.Special,
                _ => ItemType.Common
            };
        }
        
        ItemRarity GetRarityFromTags(Dictionary<string, string> customData)
        {
            if (customData != null && customData.ContainsKey("rarity"))
            {
                return customData["rarity"] switch
                {
                    "legendary" => ItemRarity.Legendary,
                    "epic" => ItemRarity.Epic,
                    "rare" => ItemRarity.Rare,
                    _ => ItemRarity.Common
                };
            }
            return ItemRarity.Common;
        }
        
        // Public getters
        public bool IsInitialized => isInitialized;
        public uint GetCurrency(string currencyCode) => playerCurrencies.ContainsKey(currencyCode) ? playerCurrencies[currencyCode] : 0;
        public Dictionary<string, uint> GetAllCurrencies() => new Dictionary<string, uint>(playerCurrencies);
    }
    
    [Serializable]
    public class VirtualCurrency
    {
        public string CurrencyCode;
        public string DisplayName;
        public int InitialDeposit;
        public int RechargeRate;
        public int RechargeMax;
    }
}
