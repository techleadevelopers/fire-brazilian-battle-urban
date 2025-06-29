
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;

namespace ArenaBrasil.Economy
{
    public class EconomyManager : MonoBehaviour
    {
        public static EconomyManager Instance { get; private set; }
        
        [Header("Currency Configuration")]
        public int startingCoins = 100;
        public int startingGems = 10;
        
        [Header("Reward Configuration")]
        public int baseXpPerMatch = 50;
        public int baseCoinsPerMatch = 25;
        public int winBonusXp = 100;
        public int winBonusCoins = 50;
        public int killBonusXp = 10;
        public int killBonusCoins = 5;
        
        [Header("Shop Configuration")]
        public List<ShopItem> shopItems = new List<ShopItem>();
        
        // Current player currencies
        private int currentCoins = 0;
        private int currentGems = 0;
        private int currentXP = 0;
        private int currentLevel = 1;
        
        // Events
        public event Action<int, int> OnCurrencyUpdated; // coins, gems
        public event Action<int, int> OnXPUpdated; // xp, level
        public event Action<ShopItem> OnItemPurchased;
        public event Action<string> OnPurchaseFailed;
        public event Action<RewardData> OnRewardGranted;
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeEconomy();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void InitializeEconomy()
        {
            Debug.Log("Arena Brasil - Initializing Economy Manager");
            
            // Initialize shop items
            InitializeShopItems();
            
            // Load player currencies from PlayFab
            LoadPlayerCurrencies();
        }
        
        void InitializeShopItems()
        {
            if (shopItems.Count == 0)
            {
                // Brazilian-themed cosmetics
                shopItems.Add(new ShopItem
                {
                    itemId = "skin_saci_classic",
                    itemName = "Saci Clássico",
                    description = "Visual clássico do Saci Pererê com gorro vermelho",
                    price = 500,
                    currency = CurrencyType.Coins,
                    itemType = ItemType.Skin,
                    rarity = Rarity.Common
                });
                
                shopItems.Add(new ShopItem
                {
                    itemId = "skin_curupira_forest",
                    itemName = "Curupira da Floresta",
                    description = "Guardião da floresta com poderes especiais",
                    price = 1000,
                    currency = CurrencyType.Coins,
                    itemType = ItemType.Skin,
                    rarity = Rarity.Rare
                });
                
                shopItems.Add(new ShopItem
                {
                    itemId = "weapon_fal_brazil",
                    itemName = "FAL Brasileiro",
                    description = "Fuzil de assalto FAL com camuflagem da bandeira brasileira",
                    price = 50,
                    currency = CurrencyType.Gems,
                    itemType = ItemType.WeaponSkin,
                    rarity = Rarity.Epic
                });
                
                shopItems.Add(new ShopItem
                {
                    itemId = "emote_samba_dance",
                    itemName = "Dança do Samba",
                    description = "Emote de dança inspirado no samba brasileiro",
                    price = 200,
                    currency = CurrencyType.Coins,
                    itemType = ItemType.Emote,
                    rarity = Rarity.Common
                });
                
                shopItems.Add(new ShopItem
                {
                    itemId = "battlepass_season1",
                    itemName = "Passe de Batalha - Lendas do Brasil",
                    description = "Passe de batalha com 100 níveis de recompensas exclusivas",
                    price = 100,
                    currency = CurrencyType.Gems,
                    itemType = ItemType.BattlePass,
                    rarity = Rarity.Legendary
                });
            }
        }
        
        async void LoadPlayerCurrencies()
        {
            try
            {
                var request = new GetPlayerStatisticsRequest
                {
                    StatisticNames = new List<string> { "Coins", "Gems", "XP", "Level" }
                };
                
                var result = await ExecutePlayFabRequest<GetPlayerStatisticsRequest, GetPlayerStatisticsResult>(
                    request, PlayFabClientAPI.GetPlayerStatisticsAsync);
                
                if (result != null && result.Statistics != null)
                {
                    foreach (var stat in result.Statistics)
                    {
                        switch (stat.StatisticName)
                        {
                            case "Coins":
                                currentCoins = stat.Value;
                                break;
                            case "Gems":
                                currentGems = stat.Value;
                                break;
                            case "XP":
                                currentXP = stat.Value;
                                break;
                            case "Level":
                                currentLevel = stat.Value;
                                break;
                        }
                    }
                }
                else
                {
                    // First time player - set starting values
                    currentCoins = startingCoins;
                    currentGems = startingGems;
                    currentXP = 0;
                    currentLevel = 1;
                    
                    await SavePlayerCurrencies();
                }
                
                OnCurrencyUpdated?.Invoke(currentCoins, currentGems);
                OnXPUpdated?.Invoke(currentXP, currentLevel);
                
                Debug.Log($"Player currencies loaded - Coins: {currentCoins}, Gems: {currentGems}, XP: {currentXP}, Level: {currentLevel}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load player currencies: {e.Message}");
            }
        }
        
        async Task SavePlayerCurrencies()
        {
            try
            {
                var request = new UpdatePlayerStatisticsRequest
                {
                    Statistics = new List<StatisticUpdate>
                    {
                        new StatisticUpdate { StatisticName = "Coins", Value = currentCoins },
                        new StatisticUpdate { StatisticName = "Gems", Value = currentGems },
                        new StatisticUpdate { StatisticName = "XP", Value = currentXP },
                        new StatisticUpdate { StatisticName = "Level", Value = currentLevel }
                    }
                };
                
                await ExecutePlayFabRequest<UpdatePlayerStatisticsRequest, UpdatePlayerStatisticsResult>(
                    request, PlayFabClientAPI.UpdatePlayerStatisticsAsync);
                
                Debug.Log("Player currencies saved successfully");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save player currencies: {e.Message}");
            }
        }
        
        public async Task<bool> PurchaseItem(string itemId)
        {
            var item = shopItems.Find(i => i.itemId == itemId);
            if (item == null)
            {
                OnPurchaseFailed?.Invoke("Item not found");
                return false;
            }
            
            // Check if player has enough currency
            int currentCurrency = item.currency == CurrencyType.Coins ? currentCoins : currentGems;
            if (currentCurrency < item.price)
            {
                OnPurchaseFailed?.Invoke("Insufficient currency");
                return false;
            }
            
            try
            {
                // Use PlayFab purchase system
                var request = new PurchaseItemRequest
                {
                    ItemId = itemId,
                    VirtualCurrency = item.currency == CurrencyType.Coins ? "CO" : "GE",
                    Price = item.price
                };
                
                var result = await ExecutePlayFabRequest<PurchaseItemRequest, PurchaseItemResult>(
                    request, PlayFabClientAPI.PurchaseItemAsync);
                
                if (result != null)
                {
                    // Update local currency
                    if (item.currency == CurrencyType.Coins)
                    {
                        currentCoins -= item.price;
                    }
                    else
                    {
                        currentGems -= item.price;
                    }
                    
                    await SavePlayerCurrencies();
                    OnCurrencyUpdated?.Invoke(currentCoins, currentGems);
                    OnItemPurchased?.Invoke(item);
                    
                    Debug.Log($"Successfully purchased {item.itemName}");
                    return true;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Purchase failed: {e.Message}");
                OnPurchaseFailed?.Invoke($"Purchase failed: {e.Message}");
            }
            
            return false;
        }
        
        public async void GrantMatchRewards(MatchResult matchResult)
        {
            int xpGained = baseXpPerMatch;
            int coinsGained = baseCoinsPerMatch;
            
            // Win bonus
            if (matchResult.won)
            {
                xpGained += winBonusXp;
                coinsGained += winBonusCoins;
            }
            
            // Kill bonus
            xpGained += matchResult.kills * killBonusXp;
            coinsGained += matchResult.kills * killBonusCoins;
            
            // Survival time bonus
            int survivalBonus = Mathf.RoundToInt(matchResult.survivalTime / 60f) * 5; // 5 XP per minute
            xpGained += survivalBonus;
            
            // Apply rewards
            currentXP += xpGained;
            currentCoins += coinsGained;
            
            // Check for level up
            int newLevel = CalculateLevel(currentXP);
            bool leveledUp = newLevel > currentLevel;
            
            if (leveledUp)
            {
                currentLevel = newLevel;
                int levelUpReward = currentLevel * 10; // 10 coins per level
                currentCoins += levelUpReward;
                
                Debug.Log($"Level up! New level: {currentLevel}, Bonus coins: {levelUpReward}");
            }
            
            // Save to PlayFab
            await SavePlayerCurrencies();
            
            // Notify UI
            OnXPUpdated?.Invoke(currentXP, currentLevel);
            OnCurrencyUpdated?.Invoke(currentCoins, currentGems);
            
            var rewardData = new RewardData
            {
                xpGained = xpGained,
                coinsGained = coinsGained,
                leveledUp = leveledUp,
                newLevel = currentLevel
            };
            
            OnRewardGranted?.Invoke(rewardData);
            
            Debug.Log($"Match rewards granted - XP: +{xpGained}, Coins: +{coinsGained}");
        }
        
        int CalculateLevel(int totalXP)
        {
            // Progressive XP requirement: Level 1 = 0 XP, Level 2 = 1000 XP, Level 3 = 3000 XP, etc.
            int level = 1;
            int xpRequired = 0;
            
            while (totalXP >= xpRequired)
            {
                level++;
                xpRequired += level * 1000; // Each level requires more XP
            }
            
            return level - 1;
        }
        
        public int GetXPRequiredForNextLevel()
        {
            int nextLevelXP = 0;
            for (int i = 1; i <= currentLevel + 1; i++)
            {
                nextLevelXP += i * 1000;
            }
            return nextLevelXP;
        }
        
        public float GetLevelProgress()
        {
            int currentLevelXP = 0;
            for (int i = 1; i <= currentLevel; i++)
            {
                currentLevelXP += i * 1000;
            }
            
            int nextLevelXP = GetXPRequiredForNextLevel();
            int xpInCurrentLevel = currentXP - currentLevelXP;
            int xpNeededForLevel = nextLevelXP - currentLevelXP;
            
            return (float)xpInCurrentLevel / xpNeededForLevel;
        }
        
        public void AddCurrency(CurrencyType type, int amount)
        {
            if (type == CurrencyType.Coins)
            {
                currentCoins += amount;
            }
            else
            {
                currentGems += amount;
            }
            
            OnCurrencyUpdated?.Invoke(currentCoins, currentGems);
            SavePlayerCurrencies();
        }
        
        public bool HasEnoughCurrency(CurrencyType type, int amount)
        {
            return type == CurrencyType.Coins ? currentCoins >= amount : currentGems >= amount;
        }
        
        async Task<TResult> ExecutePlayFabRequest<TRequest, TResult>(
            TRequest request,
            Func<TRequest, Task<PlayFabResult<TResult>>> apiMethod)
            where TResult : class
        {
            var result = await apiMethod(request);
            
            if (result.Error != null)
            {
                Debug.LogError($"PlayFab Error: {result.Error.GenerateErrorReport()}");
                return null;
            }
            
            return result.Result;
        }
        
        // Public getters
        public int CurrentCoins => currentCoins;
        public int CurrentGems => currentGems;
        public int CurrentXP => currentXP;
        public int CurrentLevel => currentLevel;
        public List<ShopItem> GetShopItems() => shopItems;
    }
    
    [System.Serializable]
    public class ShopItem
    {
        public string itemId;
        public string itemName;
        public string description;
        public int price;
        public CurrencyType currency;
        public ItemType itemType;
        public Rarity rarity;
        public Sprite icon;
        public bool isLimitedTime;
        public DateTime? expirationDate;
    }
    
    [System.Serializable]
    public class MatchResult
    {
        public bool won;
        public int kills;
        public int placement;
        public float survivalTime;
        public int damageDealt;
        public string heroUsed;
    }
    
    [System.Serializable]
    public class RewardData
    {
        public int xpGained;
        public int coinsGained;
        public int gemsGained;
        public bool leveledUp;
        public int newLevel;
        public List<string> itemsUnlocked = new List<string>();
    }
    
    public enum CurrencyType
    {
        Coins,
        Gems
    }
    
    public enum ItemType
    {
        Skin,
        WeaponSkin,
        Emote,
        BattlePass,
        Consumable,
        Boost
    }
    
    public enum Rarity
    {
        Common,
        Rare,
        Epic,
        Legendary,
        Mythic
    }
}
