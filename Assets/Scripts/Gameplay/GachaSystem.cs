
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;
using ArenaBrasil.Economy;
using ArenaBrasil.Inventory;

namespace ArenaBrasil.Gacha
{
    public class GachaSystem : NetworkBehaviour
    {
        public static GachaSystem Instance { get; private set; }
        
        [Header("Gacha Configuration")]
        public List<GachaBanner> availableBanners = new List<GachaBanner>();
        public Dictionary<string, int> playerPityCounter = new Dictionary<string, int>();
        
        [Header("Free Fire Style Rates")]
        [Range(0f, 100f)] public float commonRate = 70f;
        [Range(0f, 100f)] public float rareRate = 25f;
        [Range(0f, 100f)] public float epicRate = 4.5f;
        [Range(0f, 100f)] public float legendaryRate = 0.5f;
        
        [Header("Pity System")]
        public int legendaryPity = 200;  // Garantido lendário em 200 pulls
        public int epicPity = 50;        // Garantido épico em 50 pulls
        
        [Header("Special Events")]
        public List<GachaEvent> activeEvents = new List<GachaEvent>();
        
        // Events
        public event System.Action<GachaResult> OnGachaResult;
        public event System.Action<List<GachaResult>> OnMultiGachaResult;
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeGachaSystem();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void InitializeGachaSystem()
        {
            Debug.Log("Arena Brasil - Inicializando sistema de gacha");
            
            CreateGachaBanners();
            CreateGachaEvents();
        }
        
        void CreateGachaBanners()
        {
            availableBanners.Clear();
            
            // === BANNER PRINCIPAL - ARMAS ===
            availableBanners.Add(new GachaBanner
            {
                bannerId = "weapon_banner_main",
                bannerName = "Arsenal Brasileiro",
                description = "Armas lendárias do Brasil!",
                bannerType = GachaBannerType.Weapon,
                cost = 20, // 20 diamantes por pull
                currency = "diamonds",
                featured = new List<GachaItem>
                {
                    new GachaItem
                    {
                        itemId = "fal_dourado",
                        itemName = "FAL Dourado",
                        rarity = ItemRarity.Legendary,
                        dropRate = 0.5f,
                        itemType = "weapon_skin",
                        description = "FAL banhado a ouro brasileiro"
                    },
                    new GachaItem
                    {
                        itemId = "ak47_carnaval",
                        itemName = "AK-47 Carnaval",
                        rarity = ItemRarity.Epic,
                        dropRate = 2f,
                        itemType = "weapon_skin",
                        description = "AK-47 temática de carnaval"
                    }
                },
                items = CreateWeaponGachaPool()
            });
            
            // === BANNER DE ROUPAS ===
            availableBanners.Add(new GachaBanner
            {
                bannerId = "clothing_banner",
                bannerName = "Moda Brasileira",
                description = "Vista-se como um verdadeiro brasileiro!",
                bannerType = GachaBannerType.Cosmetic,
                cost = 15,
                currency = "diamonds",
                featured = new List<GachaItem>
                {
                    new GachaItem
                    {
                        itemId = "uniforme_cbf",
                        itemName = "Uniforme CBF",
                        rarity = ItemRarity.Legendary,
                        dropRate = 0.3f,
                        itemType = "outfit",
                        description = "Uniforme oficial da CBF"
                    }
                },
                items = CreateCosmeticGachaPool()
            });
            
            // === BANNER DE VEÍCULOS ===
            availableBanners.Add(new GachaBanner
            {
                bannerId = "vehicle_banner",
                bannerName = "Garagem Nacional",
                description = "Carros e motos do Brasil!",
                bannerType = GachaBannerType.Vehicle,
                cost = 50,
                currency = "diamonds",
                featured = new List<GachaItem>
                {
                    new GachaItem
                    {
                        itemId = "ferrari_senna",
                        itemName = "Ferrari Ayrton Senna",
                        rarity = ItemRarity.Legendary,
                        dropRate = 0.1f,
                        itemType = "vehicle",
                        description = "Ferrari do eterno campeão brasileiro"
                    }
                },
                items = CreateVehicleGachaPool()
            });
            
            // === BANNER GRATUITO ===
            availableBanners.Add(new GachaBanner
            {
                bannerId = "free_banner",
                bannerName = "Roleta Gratuita",
                description = "1 pull gratuito a cada 24h!",
                bannerType = GachaBannerType.Free,
                cost = 0,
                currency = "free",
                cooldownHours = 24,
                items = CreateFreeGachaPool()
            });
        }
        
        List<GachaItem> CreateWeaponGachaPool()
        {
            var pool = new List<GachaItem>();
            
            // Legendary weapons (0.5%)
            pool.AddRange(new[]
            {
                new GachaItem { itemId = "fal_dourado", itemName = "FAL Dourado", rarity = ItemRarity.Legendary, dropRate = 0.1f, itemType = "weapon_skin" },
                new GachaItem { itemId = "ak47_dragao", itemName = "AK-47 Dragão", rarity = ItemRarity.Legendary, dropRate = 0.1f, itemType = "weapon_skin" },
                new GachaItem { itemId = "awm_onca", itemName = "AWM Onça Pintada", rarity = ItemRarity.Legendary, dropRate = 0.1f, itemType = "weapon_skin" },
                new GachaItem { itemId = "m4a1_saci", itemName = "M4A1 Saci Pererê", rarity = ItemRarity.Legendary, dropRate = 0.1f, itemType = "weapon_skin" },
                new GachaItem { itemId = "shotgun_curupira", itemName = "Shotgun Curupira", rarity = ItemRarity.Legendary, dropRate = 0.1f, itemType = "weapon_skin" }
            });
            
            // Epic weapons (4.5%)
            pool.AddRange(new[]
            {
                new GachaItem { itemId = "ak47_carnaval", itemName = "AK-47 Carnaval", rarity = ItemRarity.Epic, dropRate = 0.9f, itemType = "weapon_skin" },
                new GachaItem { itemId = "m16_brasil", itemName = "M16 Brasil", rarity = ItemRarity.Epic, dropRate = 0.9f, itemType = "weapon_skin" },
                new GachaItem { itemId = "pistol_ipanema", itemName = "Pistola Ipanema", rarity = ItemRarity.Epic, dropRate = 0.9f, itemType = "weapon_skin" },
                new GachaItem { itemId = "sniper_pantanal", itemName = "Sniper Pantanal", rarity = ItemRarity.Epic, dropRate = 0.9f, itemType = "weapon_skin" },
                new GachaItem { itemId = "smg_favela", itemName = "SMG Favela", rarity = ItemRarity.Epic, dropRate = 0.9f, itemType = "weapon_skin" }
            });
            
            // Rare weapons (25%)
            pool.AddRange(new[]
            {
                new GachaItem { itemId = "ak47_verde", itemName = "AK-47 Verde", rarity = ItemRarity.Rare, dropRate = 5f, itemType = "weapon_skin" },
                new GachaItem { itemId = "m4a1_azul", itemName = "M4A1 Azul", rarity = ItemRarity.Rare, dropRate = 5f, itemType = "weapon_skin" },
                new GachaItem { itemId = "shotgun_amarelo", itemName = "Shotgun Amarelo", rarity = ItemRarity.Rare, dropRate = 5f, itemType = "weapon_skin" },
                new GachaItem { itemId = "pistol_prata", itemName = "Pistola Prata", rarity = ItemRarity.Rare, dropRate = 5f, itemType = "weapon_skin" },
                new GachaItem { itemId = "sniper_bronze", itemName = "Sniper Bronze", rarity = ItemRarity.Rare, dropRate = 5f, itemType = "weapon_skin" }
            });
            
            // Common weapons (70%)
            pool.AddRange(new[]
            {
                new GachaItem { itemId = "ak47_comum", itemName = "AK-47 Comum", rarity = ItemRarity.Common, dropRate = 14f, itemType = "weapon_skin" },
                new GachaItem { itemId = "m4a1_comum", itemName = "M4A1 Comum", rarity = ItemRarity.Common, dropRate = 14f, itemType = "weapon_skin" },
                new GachaItem { itemId = "shotgun_comum", itemName = "Shotgun Comum", rarity = ItemRarity.Common, dropRate = 14f, itemType = "weapon_skin" },
                new GachaItem { itemId = "pistol_comum", itemName = "Pistola Comum", rarity = ItemRarity.Common, dropRate = 14f, itemType = "weapon_skin" },
                new GachaItem { itemId = "sniper_comum", itemName = "Sniper Comum", rarity = ItemRarity.Common, dropRate = 14f, itemType = "weapon_skin" }
            });
            
            return pool;
        }
        
        List<GachaItem> CreateCosmeticGachaPool()
        {
            var pool = new List<GachaItem>();
            
            // Legendary outfits
            pool.AddRange(new[]
            {
                new GachaItem { itemId = "uniforme_cbf", itemName = "Uniforme CBF", rarity = ItemRarity.Legendary, dropRate = 0.1f, itemType = "outfit" },
                new GachaItem { itemId = "fantasia_rei_momo", itemName = "Fantasia Rei Momo", rarity = ItemRarity.Legendary, dropRate = 0.1f, itemType = "outfit" },
                new GachaItem { itemId = "roupa_ayrton", itemName = "Macacão Ayrton Senna", rarity = ItemRarity.Legendary, dropRate = 0.1f, itemType = "outfit" },
                new GachaItem { itemId = "vestido_carmen", itemName = "Vestido Carmen Miranda", rarity = ItemRarity.Legendary, dropRate = 0.1f, itemType = "outfit" },
                new GachaItem { itemId = "uniforme_ronaldinho", itemName = "Uniforme Ronaldinho", rarity = ItemRarity.Legendary, dropRate = 0.1f, itemType = "outfit" }
            });
            
            // Epic cosmetics
            pool.AddRange(new[]
            {
                new GachaItem { itemId = "camisa_flamengo_ouro", itemName = "Camisa Flamengo Ouro", rarity = ItemRarity.Epic, dropRate = 0.9f, itemType = "shirt" },
                new GachaItem { itemId = "short_copa", itemName = "Short Copa do Mundo", rarity = ItemRarity.Epic, dropRate = 0.9f, itemType = "pants" },
                new GachaItem { itemId = "tenis_olimpikus_pro", itemName = "Tênis Olympikus Pro", rarity = ItemRarity.Epic, dropRate = 0.9f, itemType = "shoes" },
                new GachaItem { itemId = "bone_cbn", itemName = "Boné CBN", rarity = ItemRarity.Epic, dropRate = 0.9f, itemType = "hat" },
                new GachaItem { itemId = "oculos_oakley", itemName = "Óculos Oakley", rarity = ItemRarity.Epic, dropRate = 0.9f, itemType = "glasses" }
            });
            
            return pool;
        }
        
        List<GachaItem> CreateVehicleGachaPool()
        {
            var pool = new List<GachaItem>();
            
            // Legendary vehicles
            pool.AddRange(new[]
            {
                new GachaItem { itemId = "ferrari_senna", itemName = "Ferrari Ayrton Senna", rarity = ItemRarity.Legendary, dropRate = 0.05f, itemType = "vehicle" },
                new GachaItem { itemId = "lamborghini_br", itemName = "Lamborghini Brasil", rarity = ItemRarity.Legendary, dropRate = 0.05f, itemType = "vehicle" },
                new GachaItem { itemId = "mclaren_massa", itemName = "McLaren Felipe Massa", rarity = ItemRarity.Legendary, dropRate = 0.05f, itemType = "vehicle" },
                new GachaItem { itemId = "bugatti_ronaldo", itemName = "Bugatti Ronaldo", rarity = ItemRarity.Legendary, dropRate = 0.05f, itemType = "vehicle" }
            });
            
            // Epic vehicles
            pool.AddRange(new[]
            {
                new GachaItem { itemId = "civic_turbo", itemName = "Civic Turbo", rarity = ItemRarity.Epic, dropRate = 1f, itemType = "vehicle" },
                new GachaItem { itemId = "golf_gti", itemName = "Golf GTI", rarity = ItemRarity.Epic, dropRate = 1f, itemType = "vehicle" },
                new GachaItem { itemId = "ninja_h2", itemName = "Kawasaki Ninja H2", rarity = ItemRarity.Epic, dropRate = 1f, itemType = "vehicle" },
                new GachaItem { itemId = "ducati_br", itemName = "Ducati Brasil", rarity = ItemRarity.Epic, dropRate = 1f, itemType = "vehicle" }
            });
            
            return pool;
        }
        
        List<GachaItem> CreateFreeGachaPool()
        {
            var pool = new List<GachaItem>();
            
            // Free gacha has lower rates for good items
            pool.AddRange(new[]
            {
                new GachaItem { itemId = "coins_100", itemName = "100 Moedas", rarity = ItemRarity.Common, dropRate = 40f, itemType = "currency" },
                new GachaItem { itemId = "coins_200", itemName = "200 Moedas", rarity = ItemRarity.Common, dropRate = 30f, itemType = "currency" },
                new GachaItem { itemId = "diamonds_5", itemName = "5 Diamantes", rarity = ItemRarity.Rare, dropRate = 15f, itemType = "currency" },
                new GachaItem { itemId = "diamonds_10", itemName = "10 Diamantes", rarity = ItemRarity.Rare, dropRate = 10f, itemType = "currency" },
                new GachaItem { itemId = "weapon_comum", itemName = "Skin Comum", rarity = ItemRarity.Common, dropRate = 4f, itemType = "weapon_skin" },
                new GachaItem { itemId = "diamonds_50", itemName = "50 Diamantes", rarity = ItemRarity.Epic, dropRate = 1f, itemType = "currency" }
            });
            
            return pool;
        }
        
        void CreateGachaEvents()
        {
            activeEvents.Clear();
            
            // Evento de rate up para lendários
            activeEvents.Add(new GachaEvent
            {
                eventId = "legendary_rate_up",
                eventName = "Rate Up Lendário!",
                description = "Chance dobrada de itens lendários!",
                startDate = System.DateTime.Now,
                endDate = System.DateTime.Now.AddDays(7),
                rateMultipliers = new Dictionary<ItemRarity, float>
                {
                    [ItemRarity.Legendary] = 2f,
                    [ItemRarity.Epic] = 1.5f
                },
                bannersAffected = new List<string> { "weapon_banner_main", "clothing_banner" }
            });
        }
        
        // === GACHA PULLING ===
        
        public GachaResult SinglePull(string bannerId)
        {
            var banner = availableBanners.Find(b => b.bannerId == bannerId);
            if (banner == null) return null;
            
            // Check if can afford
            if (!CanAffordPull(banner)) return null;
            
            // Check cooldown for free banner
            if (banner.bannerType == GachaBannerType.Free && !CanUseFreeGacha(bannerId))
            {
                return null;
            }
            
            // Spend currency
            SpendGachaCurrency(banner);
            
            // Perform pull
            var result = PerformPull(banner);
            
            // Update pity counter
            UpdatePityCounter(bannerId, result.rarity);
            
            // Give item to player
            GiveItemToPlayer(result);
            
            OnGachaResult?.Invoke(result);
            return result;
        }
        
        public List<GachaResult> MultiPull(string bannerId, int count = 10)
        {
            var banner = availableBanners.Find(b => b.bannerId == bannerId);
            if (banner == null) return null;
            
            var results = new List<GachaResult>();
            
            // Check if can afford all pulls
            if (!CanAffordMultiPull(banner, count)) return null;
            
            // Spend currency for all pulls
            for (int i = 0; i < count; i++)
            {
                SpendGachaCurrency(banner);
            }
            
            // Guarantee at least one rare+ in 10-pull
            bool hasRareOrBetter = false;
            
            for (int i = 0; i < count; i++)
            {
                var result = PerformPull(banner);
                
                if (result.rarity >= ItemRarity.Rare)
                {
                    hasRareOrBetter = true;
                }
                
                UpdatePityCounter(bannerId, result.rarity);
                GiveItemToPlayer(result);
                results.Add(result);
            }
            
            // If no rare+ items, replace last with guaranteed rare
            if (!hasRareOrBetter)
            {
                var lastResult = results[results.Count - 1];
                var guaranteedRare = GetGuaranteedRareItem(banner);
                results[results.Count - 1] = guaranteedRare;
                GiveItemToPlayer(guaranteedRare);
            }
            
            OnMultiGachaResult?.Invoke(results);
            return results;
        }
        
        GachaResult PerformPull(GachaBanner banner)
        {
            // Check pity system first
            var pityResult = CheckPitySystem(banner);
            if (pityResult != null) return pityResult;
            
            // Apply event multipliers
            var modifiedRates = ApplyEventMultipliers(banner);
            
            // Generate random number
            float roll = Random.Range(0f, 100f);
            float cumulative = 0f;
            
            // Check featured items first
            foreach (var featured in banner.featured)
            {
                cumulative += featured.dropRate * GetEventMultiplier(featured.rarity);
                if (roll <= cumulative)
                {
                    return CreateGachaResult(featured);
                }
            }
            
            // Check regular items by rarity
            cumulative = 0f;
            
            // Legendary
            cumulative += modifiedRates[ItemRarity.Legendary];
            if (roll <= cumulative)
            {
                var legendaryItems = banner.items.Where(i => i.rarity == ItemRarity.Legendary).ToList();
                if (legendaryItems.Count > 0)
                {
                    var selected = legendaryItems[Random.Range(0, legendaryItems.Count)];
                    return CreateGachaResult(selected);
                }
            }
            
            // Epic
            cumulative += modifiedRates[ItemRarity.Epic];
            if (roll <= cumulative)
            {
                var epicItems = banner.items.Where(i => i.rarity == ItemRarity.Epic).ToList();
                if (epicItems.Count > 0)
                {
                    var selected = epicItems[Random.Range(0, epicItems.Count)];
                    return CreateGachaResult(selected);
                }
            }
            
            // Rare
            cumulative += modifiedRates[ItemRarity.Rare];
            if (roll <= cumulative)
            {
                var rareItems = banner.items.Where(i => i.rarity == ItemRarity.Rare).ToList();
                if (rareItems.Count > 0)
                {
                    var selected = rareItems[Random.Range(0, rareItems.Count)];
                    return CreateGachaResult(selected);
                }
            }
            
            // Common (fallback)
            var commonItems = banner.items.Where(i => i.rarity == ItemRarity.Common).ToList();
            if (commonItems.Count > 0)
            {
                var selected = commonItems[Random.Range(0, commonItems.Count)];
                return CreateGachaResult(selected);
            }
            
            // Should never reach here
            return null;
        }
        
        GachaResult CheckPitySystem(GachaBanner banner)
        {
            string pityKey = banner.bannerId;
            
            if (!playerPityCounter.ContainsKey(pityKey))
            {
                playerPityCounter[pityKey] = 0;
            }
            
            int currentPity = playerPityCounter[pityKey];
            
            // Legendary pity
            if (currentPity >= legendaryPity)
            {
                var legendaryItems = banner.items.Where(i => i.rarity == ItemRarity.Legendary).ToList();
                if (legendaryItems.Count > 0)
                {
                    var selected = legendaryItems[Random.Range(0, legendaryItems.Count)];
                    playerPityCounter[pityKey] = 0; // Reset pity
                    return CreateGachaResult(selected, true);
                }
            }
            
            // Epic pity
            if (currentPity >= epicPity && currentPity % epicPity == 0)
            {
                var epicItems = banner.items.Where(i => i.rarity == ItemRarity.Epic).ToList();
                if (epicItems.Count > 0)
                {
                    var selected = epicItems[Random.Range(0, epicItems.Count)];
                    return CreateGachaResult(selected, true);
                }
            }
            
            return null;
        }
        
        Dictionary<ItemRarity, float> ApplyEventMultipliers(GachaBanner banner)
        {
            var rates = new Dictionary<ItemRarity, float>
            {
                [ItemRarity.Common] = commonRate,
                [ItemRarity.Rare] = rareRate,
                [ItemRarity.Epic] = epicRate,
                [ItemRarity.Legendary] = legendaryRate
            };
            
            foreach (var eventData in activeEvents)
            {
                if (eventData.bannersAffected.Contains(banner.bannerId) && eventData.IsActive())
                {
                    foreach (var multiplier in eventData.rateMultipliers)
                    {
                        rates[multiplier.Key] *= multiplier.Value;
                    }
                }
            }
            
            return rates;
        }
        
        float GetEventMultiplier(ItemRarity rarity)
        {
            float multiplier = 1f;
            
            foreach (var eventData in activeEvents)
            {
                if (eventData.IsActive() && eventData.rateMultipliers.ContainsKey(rarity))
                {
                    multiplier *= eventData.rateMultipliers[rarity];
                }
            }
            
            return multiplier;
        }
        
        GachaResult CreateGachaResult(GachaItem item, bool isPity = false)
        {
            return new GachaResult
            {
                itemId = item.itemId,
                itemName = item.itemName,
                rarity = item.rarity,
                itemType = item.itemType,
                description = item.description,
                isPityBreak = isPity,
                pullTime = System.DateTime.Now
            };
        }
        
        GachaResult GetGuaranteedRareItem(GachaBanner banner)
        {
            var rareItems = banner.items.Where(i => i.rarity >= ItemRarity.Rare).ToList();
            if (rareItems.Count > 0)
            {
                var selected = rareItems[Random.Range(0, rareItems.Count)];
                return CreateGachaResult(selected);
            }
            
            return null;
        }
        
        void UpdatePityCounter(string bannerId, ItemRarity rarity)
        {
            if (!playerPityCounter.ContainsKey(bannerId))
            {
                playerPityCounter[bannerId] = 0;
            }
            
            if (rarity == ItemRarity.Legendary)
            {
                playerPityCounter[bannerId] = 0; // Reset on legendary
            }
            else
            {
                playerPityCounter[bannerId]++;
            }
        }
        
        void GiveItemToPlayer(GachaResult result)
        {
            if (result.itemType == "currency")
            {
                if (result.itemId.Contains("coins"))
                {
                    int amount = ExtractAmount(result.itemId);
                    EconomyManager.Instance.AddCoins(amount);
                }
                else if (result.itemId.Contains("diamonds"))
                {
                    int amount = ExtractAmount(result.itemId);
                    EconomyManager.Instance.AddDiamonds(amount);
                }
            }
            else
            {
                // Give to inventory
                if (InventorySystem.Instance != null)
                {
                    InventorySystem.Instance.AddItem(result.itemId, 1);
                }
            }
        }
        
        int ExtractAmount(string itemId)
        {
            var parts = itemId.Split('_');
            if (parts.Length > 1 && int.TryParse(parts[1], out int amount))
            {
                return amount;
            }
            return 0;
        }
        
        // === CURRENCY CHECKS ===
        
        bool CanAffordPull(GachaBanner banner)
        {
            if (banner.cost == 0) return true;
            
            switch (banner.currency)
            {
                case "diamonds":
                    return EconomyManager.Instance.GetDiamonds() >= banner.cost;
                case "coins":
                    return EconomyManager.Instance.GetCoins() >= banner.cost;
                case "gold":
                    return EconomyManager.Instance.GetGold() >= banner.cost;
                default:
                    return true;
            }
        }
        
        bool CanAffordMultiPull(GachaBanner banner, int count)
        {
            if (banner.cost == 0) return true;
            
            int totalCost = banner.cost * count;
            
            switch (banner.currency)
            {
                case "diamonds":
                    return EconomyManager.Instance.GetDiamonds() >= totalCost;
                case "coins":
                    return EconomyManager.Instance.GetCoins() >= totalCost;
                case "gold":
                    return EconomyManager.Instance.GetGold() >= totalCost;
                default:
                    return true;
            }
        }
        
        void SpendGachaCurrency(GachaBanner banner)
        {
            if (banner.cost == 0) return;
            
            switch (banner.currency)
            {
                case "diamonds":
                    EconomyManager.Instance.SpendDiamonds(banner.cost);
                    break;
                case "coins":
                    EconomyManager.Instance.SpendCoins(banner.cost);
                    break;
                case "gold":
                    EconomyManager.Instance.SpendGold(banner.cost);
                    break;
            }
        }
        
        bool CanUseFreeGacha(string bannerId)
        {
            // Check if player used free gacha in last 24h
            string key = $"free_gacha_{bannerId}";
            if (PlayerPrefs.HasKey(key))
            {
                string lastUseStr = PlayerPrefs.GetString(key);
                if (System.DateTime.TryParse(lastUseStr, out System.DateTime lastUse))
                {
                    return System.DateTime.Now >= lastUse.AddHours(24);
                }
            }
            
            return true;
        }
        
        // === PUBLIC GETTERS ===
        
        public List<GachaBanner> GetAvailableBanners() => availableBanners;
        public List<GachaEvent> GetActiveEvents() => activeEvents;
        public int GetPityCounter(string bannerId) => playerPityCounter.ContainsKey(bannerId) ? playerPityCounter[bannerId] : 0;
        public int GetRemainingPity(string bannerId) => legendaryPity - GetPityCounter(bannerId);
    }
    
    // === DATA CLASSES ===
    
    [System.Serializable]
    public class GachaBanner
    {
        public string bannerId;
        public string bannerName;
        public string description;
        public GachaBannerType bannerType;
        public int cost;
        public string currency;
        public int cooldownHours;
        public List<GachaItem> featured = new List<GachaItem>();
        public List<GachaItem> items = new List<GachaItem>();
        public Sprite bannerImage;
    }
    
    [System.Serializable]
    public class GachaItem
    {
        public string itemId;
        public string itemName;
        public ItemRarity rarity;
        public float dropRate;
        public string itemType;
        public string description;
        public Sprite itemIcon;
    }
    
    [System.Serializable]
    public class GachaResult
    {
        public string itemId;
        public string itemName;
        public ItemRarity rarity;
        public string itemType;
        public string description;
        public bool isPityBreak;
        public System.DateTime pullTime;
    }
    
    [System.Serializable]
    public class GachaEvent
    {
        public string eventId;
        public string eventName;
        public string description;
        public System.DateTime startDate;
        public System.DateTime endDate;
        public Dictionary<ItemRarity, float> rateMultipliers = new Dictionary<ItemRarity, float>();
        public List<string> bannersAffected = new List<string>();
        
        public bool IsActive()
        {
            var now = System.DateTime.Now;
            return now >= startDate && now <= endDate;
        }
    }
    
    public enum GachaBannerType
    {
        Weapon,
        Cosmetic,
        Vehicle,
        Free,
        Limited
    }
}
