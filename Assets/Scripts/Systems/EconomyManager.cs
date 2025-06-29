
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System;
using ArenaBrasil.Backend;

namespace ArenaBrasil.Economy
{
    public class EconomyManager : NetworkBehaviour
    {
        public static EconomyManager Instance { get; private set; }
        
        [Header("Free Fire Style Economy")]
        public int playerCoins = 500;           // Moedas gratuitas
        public int playerDiamonds = 50;         // Diamantes premium
        public int playerGold = 0;              // Ouro (moeda especial)
        public int playerTokens = 0;            // Tokens de evento
        
        [Header("Daily Rewards")]
        public int dailyLoginDay = 1;
        public DateTime lastLoginDate = DateTime.MinValue;
        public List<DailyReward> dailyRewards = new List<DailyReward>();
        
        [Header("Battle Pass")]
        public int battlePassLevel = 1;
        public int battlePassXP = 0;
        public bool hasPremiumBattlePass = false;
        public List<BattlePassReward> battlePassRewards = new List<BattlePassReward>();
        
        [Header("Store Packages")]
        public List<StorePackage> storePackages = new List<StorePackage>();
        public List<IAP_Package> iapPackages = new List<IAP_Package>();
        
        [Header("Free Fire Style Pricing")]
        [SerializeField] private GamePricing pricing = new GamePricing();
        
        // Events
        public event Action<int> OnCoinsChanged;
        public event Action<int> OnDiamondsChanged;
        public event Action<int> OnGoldChanged;
        public event Action<DailyReward> OnDailyRewardClaimed;
        public event Action<BattlePassReward> OnBattlePassRewardClaimed;
        
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
            Debug.Log("Arena Brasil - Inicializando sistema econômico");
            
            InitializePricing();
            InitializeDailyRewards();
            InitializeBattlePass();
            InitializeStore();
            InitializeIAPs();
            
            // Check daily login
            CheckDailyLogin();
        }
        
        void InitializePricing()
        {
            // Preços similares ao Free Fire (adaptados ao Brasil)
            pricing.weaponSkinPrices = new Dictionary<string, int>
            {
                ["common"] = 50,      // Skins comuns
                ["rare"] = 150,       // Skins raras  
                ["epic"] = 400,       // Skins épicas
                ["legendary"] = 800   // Skins lendárias
            };
            
            pricing.characterSkinPrices = new Dictionary<string, int>
            {
                ["common"] = 100,
                ["rare"] = 250,
                ["epic"] = 600,
                ["legendary"] = 1200
            };
            
            pricing.vehiclePrices = new Dictionary<string, int>
            {
                ["car_common"] = 200,
                ["car_rare"] = 500,
                ["car_epic"] = 1000,
                ["motorcycle"] = 300,
                ["special"] = 1500
            };
            
            // Preços de IAPs em diamantes
            pricing.iapPrices = new Dictionary<string, float>
            {
                ["100_diamonds"] = 5.99f,    // R$ 5,99
                ["520_diamonds"] = 24.99f,   // R$ 24,99
                ["1080_diamonds"] = 49.99f,  // R$ 49,99
                ["2200_diamonds"] = 99.99f,  // R$ 99,99
                ["premium_pass"] = 19.99f,   // R$ 19,99
                ["mega_pack"] = 149.99f      // R$ 149,99
            };
        }
        
        void InitializeDailyRewards()
        {
            dailyRewards.Clear();
            
            // 7 dias de recompensas diárias (como Free Fire)
            dailyRewards.Add(new DailyReward { day = 1, coins = 200, diamonds = 0, description = "Bem-vindo de volta!" });
            dailyRewards.Add(new DailyReward { day = 2, coins = 300, diamonds = 5, description = "Continue jogando!" });
            dailyRewards.Add(new DailyReward { day = 3, coins = 400, diamonds = 0, itemId = "weapon_skin_common", description = "Skin de arma grátis!" });
            dailyRewards.Add(new DailyReward { day = 4, coins = 500, diamonds = 10, description = "Meio da semana!" });
            dailyRewards.Add(new DailyReward { day = 5, coins = 600, diamonds = 0, itemId = "character_skin_rare", description = "Roupa especial!" });
            dailyRewards.Add(new DailyReward { day = 6, coins = 800, diamonds = 15, description = "Quase lá!" });
            dailyRewards.Add(new DailyReward { day = 7, coins = 1000, diamonds = 50, itemId = "vehicle_gol", description = "Volkswagen Gol GRÁTIS!" });
        }
        
        void InitializeBattlePass()
        {
            battlePassRewards.Clear();
            
            // 100 níveis de Battle Pass
            for (int i = 1; i <= 100; i++)
            {
                battlePassRewards.Add(new BattlePassReward
                {
                    level = i,
                    freeReward = GenerateFreeReward(i),
                    premiumReward = GeneratePremiumReward(i),
                    requiredXP = i * 1000
                });
            }
        }
        
        BattlePassRewardItem GenerateFreeReward(int level)
        {
            if (level % 10 == 0) // A cada 10 níveis
            {
                return new BattlePassRewardItem
                {
                    type = "diamonds",
                    quantity = 20,
                    itemId = "",
                    description = "Diamantes grátis!"
                };
            }
            else if (level % 5 == 0) // A cada 5 níveis
            {
                return new BattlePassRewardItem
                {
                    type = "weapon_skin",
                    quantity = 1,
                    itemId = "skin_fal_comum",
                    description = "Skin de arma"
                };
            }
            else
            {
                return new BattlePassRewardItem
                {
                    type = "coins",
                    quantity = 100 + (level * 10),
                    itemId = "",
                    description = "Moedas Arena Brasil"
                };
            }
        }
        
        BattlePassRewardItem GeneratePremiumReward(int level)
        {
            if (level == 1)
            {
                return new BattlePassRewardItem
                {
                    type = "character_skin",
                    quantity = 1,
                    itemId = "uniforme_bope",
                    description = "Uniforme BOPE Exclusivo"
                };
            }
            else if (level == 50)
            {
                return new BattlePassRewardItem
                {
                    type = "vehicle",
                    quantity = 1,
                    itemId = "civic_si_turbo",
                    description = "Honda Civic Si Turbo"
                };
            }
            else if (level == 100)
            {
                return new BattlePassRewardItem
                {
                    type = "legendary_set",
                    quantity = 1,
                    itemId = "saci_legend_set",
                    description = "Conjunto Lendário do Saci"
                };
            }
            else if (level % 10 == 0)
            {
                return new BattlePassRewardItem
                {
                    type = "diamonds",
                    quantity = 50,
                    itemId = "",
                    description = "Diamantes Premium"
                };
            }
            else
            {
                return new BattlePassRewardItem
                {
                    type = "epic_item",
                    quantity = 1,
                    itemId = $"epic_item_{level}",
                    description = "Item Épico"
                };
            }
        }
        
        void InitializeStore()
        {
            storePackages.Clear();
            
            // Pacotes da loja (rotação diária/semanal como Free Fire)
            storePackages.Add(new StorePackage
            {
                packageId = "pacote_iniciante",
                packageName = "Pacote do Iniciante",
                price = 100,
                currency = "diamonds",
                items = new List<StoreItem>
                {
                    new StoreItem { itemId = "taurus_pt92_gold", itemName = "Taurus PT92 Dourada", quantity = 1 },
                    new StoreItem { itemId = "coins", itemName = "Moedas", quantity = 1000 },
                    new StoreItem { itemId = "uniform_policia", itemName = "Uniforme Policial", quantity = 1 }
                },
                discount = 50,
                timeLimit = TimeSpan.FromDays(3)
            });
            
            storePackages.Add(new StorePackage
            {
                packageId = "pacote_carnaval",
                packageName = "Pacote Carnaval 2024",
                price = 300,
                currency = "diamonds",
                items = new List<StoreItem>
                {
                    new StoreItem { itemId = "fantasia_carnaval", itemName = "Fantasia de Carnaval", quantity = 1 },
                    new StoreItem { itemId = "mask_carnaval", itemName = "Máscara de Carnaval", quantity = 1 },
                    new StoreItem { itemId = "emote_samba", itemName = "Emote Samba", quantity = 1 }
                },
                isLimited = true,
                timeLimit = TimeSpan.FromDays(7)
            });
        }
        
        void InitializeIAPs()
        {
            iapPackages.Clear();
            
            // Pacotes de compra com dinheiro real (como Free Fire)
            iapPackages.Add(new IAP_Package
            {
                packageId = "diamonds_small",
                packageName = "100 Diamantes",
                priceReal = 5.99f,
                currency = "BRL",
                diamonds = 100,
                bonusDiamonds = 10,
                description = "Pacote básico de diamantes"
            });
            
            iapPackages.Add(new IAP_Package
            {
                packageId = "diamonds_medium",
                packageName = "520 Diamantes",
                priceReal = 24.99f,
                currency = "BRL",
                diamonds = 520,
                bonusDiamonds = 80,
                description = "Melhor custo-benefício!"
            });
            
            iapPackages.Add(new IAP_Package
            {
                packageId = "diamonds_large",
                packageName = "1080 Diamantes",
                priceReal = 49.99f,
                currency = "BRL",
                diamonds = 1080,
                bonusDiamonds = 220,
                description = "Pacote popular"
            });
            
            iapPackages.Add(new IAP_Package
            {
                packageId = "diamonds_mega",
                packageName = "2200 Diamantes",
                priceReal = 99.99f,
                currency = "BRL",
                diamonds = 2200,
                bonusDiamonds = 550,
                description = "Máximo valor!"
            });
            
            iapPackages.Add(new IAP_Package
            {
                packageId = "premium_battlepass",
                packageName = "Battle Pass Premium",
                priceReal = 19.99f,
                currency = "BRL",
                diamonds = 0,
                bonusDiamonds = 0,
                description = "Desbloqueie recompensas premium",
                specialReward = "premium_pass_access"
            });
        }
        
        void CheckDailyLogin()
        {
            DateTime today = DateTime.Today;
            
            if (lastLoginDate.Date != today)
            {
                if (lastLoginDate.Date == today.AddDays(-1))
                {
                    // Consecutive day
                    dailyLoginDay++;
                    if (dailyLoginDay > 7) dailyLoginDay = 1; // Reset cycle
                }
                else
                {
                    // Reset streak
                    dailyLoginDay = 1;
                }
                
                lastLoginDate = today;
                ShowDailyReward();
            }
        }
        
        void ShowDailyReward()
        {
            var reward = dailyRewards.Find(r => r.day == dailyLoginDay);
            if (reward != null)
            {
                ClaimDailyReward(reward);
            }
        }
        
        public void ClaimDailyReward(DailyReward reward)
        {
            AddCoins(reward.coins);
            AddDiamonds(reward.diamonds);
            
            if (!string.IsNullOrEmpty(reward.itemId))
            {
                // Give item to inventory
                if (ArenaBrasil.Inventory.InventorySystem.Instance != null)
                {
                    ArenaBrasil.Inventory.InventorySystem.Instance.AddItem(reward.itemId, 1);
                }
            }
            
            OnDailyRewardClaimed?.Invoke(reward);
            Debug.Log($"Recompensa diária dia {reward.day} coletada!");
        }
        
        // === CURRENCY MANAGEMENT ===
        
        public void AddCoins(int amount)
        {
            playerCoins += amount;
            OnCoinsChanged?.Invoke(playerCoins);
            SaveEconomyData();
        }
        
        public void AddDiamonds(int amount)
        {
            playerDiamonds += amount;
            OnDiamondsChanged?.Invoke(playerDiamonds);
            SaveEconomyData();
        }
        
        public void AddGold(int amount)
        {
            playerGold += amount;
            OnGoldChanged?.Invoke(playerGold);
            SaveEconomyData();
        }
        
        public bool SpendCoins(int amount)
        {
            if (playerCoins >= amount)
            {
                playerCoins -= amount;
                OnCoinsChanged?.Invoke(playerCoins);
                SaveEconomyData();
                return true;
            }
            return false;
        }
        
        public bool SpendDiamonds(int amount)
        {
            if (playerDiamonds >= amount)
            {
                playerDiamonds -= amount;
                OnDiamondsChanged?.Invoke(playerDiamonds);
                SaveEconomyData();
                return true;
            }
            return false;
        }
        
        public bool SpendGold(int amount)
        {
            if (playerGold >= amount)
            {
                playerGold -= amount;
                OnGoldChanged?.Invoke(playerGold);
                SaveEconomyData();
                return true;
            }
            return false;
        }
        
        // === BATTLE PASS ===
        
        public void AddBattlePassXP(int xp)
        {
            battlePassXP += xp;
            
            // Check for level up
            while (CanLevelUpBattlePass())
            {
                LevelUpBattlePass();
            }
        }
        
        bool CanLevelUpBattlePass()
        {
            if (battlePassLevel >= 100) return false;
            
            var nextLevelReward = battlePassRewards.Find(r => r.level == battlePassLevel + 1);
            return nextLevelReward != null && battlePassXP >= nextLevelReward.requiredXP;
        }
        
        void LevelUpBattlePass()
        {
            battlePassLevel++;
            
            var reward = battlePassRewards.Find(r => r.level == battlePassLevel);
            if (reward != null)
            {
                // Give free reward
                GiveBattlePassReward(reward.freeReward);
                
                // Give premium reward if has premium pass
                if (hasPremiumBattlePass)
                {
                    GiveBattlePassReward(reward.premiumReward);
                }
                
                OnBattlePassRewardClaimed?.Invoke(reward);
            }
        }
        
        void GiveBattlePassReward(BattlePassRewardItem reward)
        {
            switch (reward.type)
            {
                case "coins":
                    AddCoins(reward.quantity);
                    break;
                case "diamonds":
                    AddDiamonds(reward.quantity);
                    break;
                case "gold":
                    AddGold(reward.quantity);
                    break;
                default:
                    // Give item to inventory
                    if (ArenaBrasil.Inventory.InventorySystem.Instance != null)
                    {
                        ArenaBrasil.Inventory.InventorySystem.Instance.AddItem(reward.itemId, reward.quantity);
                    }
                    break;
            }
        }
        
        public bool PurchasePremiumBattlePass()
        {
            if (SpendDiamonds(500)) // Preço do passe premium
            {
                hasPremiumBattlePass = true;
                
                // Give all previous premium rewards
                for (int i = 1; i <= battlePassLevel; i++)
                {
                    var reward = battlePassRewards.Find(r => r.level == i);
                    if (reward != null)
                    {
                        GiveBattlePassReward(reward.premiumReward);
                    }
                }
                
                SaveEconomyData();
                return true;
            }
            return false;
        }
        
        // === STORE PURCHASES ===
        
        public bool PurchaseStorePackage(string packageId)
        {
            var package = storePackages.Find(p => p.packageId == packageId);
            if (package == null) return false;
            
            bool canAfford = false;
            
            switch (package.currency)
            {
                case "coins":
                    canAfford = SpendCoins(package.price);
                    break;
                case "diamonds":
                    canAfford = SpendDiamonds(package.price);
                    break;
                case "gold":
                    canAfford = SpendGold(package.price);
                    break;
            }
            
            if (canAfford)
            {
                // Give package items
                foreach (var item in package.items)
                {
                    if (item.itemId == "coins")
                        AddCoins(item.quantity);
                    else if (item.itemId == "diamonds")
                        AddDiamonds(item.quantity);
                    else if (item.itemId == "gold")
                        AddGold(item.quantity);
                    else
                    {
                        // Give to inventory
                        if (ArenaBrasil.Inventory.InventorySystem.Instance != null)
                        {
                            ArenaBrasil.Inventory.InventorySystem.Instance.AddItem(item.itemId, item.quantity);
                        }
                    }
                }
                
                return true;
            }
            
            return false;
        }
        
        // === IAP PROCESSING ===
        
        public void ProcessIAP(string packageId)
        {
            var package = iapPackages.Find(p => p.packageId == packageId);
            if (package == null) return;
            
            // In real implementation, this would go through platform IAP
            // For now, simulate successful purchase
            
            AddDiamonds(package.diamonds + package.bonusDiamonds);
            
            if (package.specialReward == "premium_pass_access")
            {
                hasPremiumBattlePass = true;
            }
            
            Debug.Log($"IAP processado: {package.packageName} - {package.diamonds + package.bonusDiamonds} diamantes");
        }
        
        // === MATCH REWARDS ===
        
        public void GiveMatchReward(bool isWinner, int kills, int placement, float matchDuration)
        {
            int baseCoins = 50;
            int baseXP = 100;
            
            // Winner bonus
            if (isWinner)
            {
                baseCoins += 200;
                baseXP += 500;
                AddGold(10); // Gold for winners
            }
            
            // Placement bonus
            if (placement <= 3)
            {
                baseCoins += (4 - placement) * 50;
                baseXP += (4 - placement) * 100;
            }
            
            // Kill bonus
            baseCoins += kills * 10;
            baseXP += kills * 25;
            
            // Time bonus
            float timeMultiplier = Mathf.Clamp(matchDuration / 1800f, 0.5f, 1.5f); // 30 min reference
            baseCoins = Mathf.RoundToInt(baseCoins * timeMultiplier);
            baseXP = Mathf.RoundToInt(baseXP * timeMultiplier);
            
            AddCoins(baseCoins);
            AddBattlePassXP(baseXP);
            
            Debug.Log($"Recompensa da partida: {baseCoins} moedas, {baseXP} XP");
        }
        
        // === SAVE/LOAD ===
        
        void SaveEconomyData()
        {
            if (ArenaBrasil.Backend.FirebaseBackendService.Instance != null)
            {
                var economyData = new EconomyData
                {
                    coins = playerCoins,
                    diamonds = playerDiamonds,
                    gold = playerGold,
                    tokens = playerTokens,
                    battlePassLevel = battlePassLevel,
                    battlePassXP = battlePassXP,
                    hasPremiumBattlePass = hasPremiumBattlePass,
                    dailyLoginDay = dailyLoginDay,
                    lastLoginDate = lastLoginDate
                };
                
                // Save to Firebase
                ArenaBrasil.Backend.FirebaseBackendService.Instance.SaveEconomyData(economyData);
            }
        }
        
        // === GETTERS ===
        
        public int GetCoins() => playerCoins;
        public int GetDiamonds() => playerDiamonds;
        public int GetGold() => playerGold;
        public int GetTokens() => playerTokens;
        public int GetBattlePassLevel() => battlePassLevel;
        public bool HasPremiumBattlePass() => hasPremiumBattlePass;
        public List<DailyReward> GetDailyRewards() => dailyRewards;
        public List<StorePackage> GetStorePackages() => storePackages;
        public List<IAP_Package> GetIAPPackages() => iapPackages;
    }
    
    // === DATA CLASSES ===
    
    [System.Serializable]
    public class GamePricing
    {
        public Dictionary<string, int> weaponSkinPrices = new Dictionary<string, int>();
        public Dictionary<string, int> characterSkinPrices = new Dictionary<string, int>();
        public Dictionary<string, int> vehiclePrices = new Dictionary<string, int>();
        public Dictionary<string, float> iapPrices = new Dictionary<string, float>();
    }
    
    [System.Serializable]
    public class DailyReward
    {
        public int day;
        public int coins;
        public int diamonds;
        public string itemId;
        public string description;
    }
    
    [System.Serializable]
    public class BattlePassReward
    {
        public int level;
        public int requiredXP;
        public BattlePassRewardItem freeReward;
        public BattlePassRewardItem premiumReward;
    }
    
    [System.Serializable]
    public class BattlePassRewardItem
    {
        public string type;
        public int quantity;
        public string itemId;
        public string description;
    }
    
    [System.Serializable]
    public class StorePackage
    {
        public string packageId;
        public string packageName;
        public int price;
        public string currency;
        public List<StoreItem> items = new List<StoreItem>();
        public int discount;
        public bool isLimited;
        public TimeSpan timeLimit;
    }
    
    [System.Serializable]
    public class StoreItem
    {
        public string itemId;
        public string itemName;
        public int quantity;
    }
    
    [System.Serializable]
    public class IAP_Package
    {
        public string packageId;
        public string packageName;
        public float priceReal;
        public string currency;
        public int diamonds;
        public int bonusDiamonds;
        public string description;
        public string specialReward;
    }
    
    [System.Serializable]
    public class EconomyData
    {
        public int coins;
        public int diamonds;
        public int gold;
        public int tokens;
        public int battlePassLevel;
        public int battlePassXP;
        public bool hasPremiumBattlePass;
        public int dailyLoginDay;
        public DateTime lastLoginDate;
    }
}
