
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System;
using ArenaBrasil.Economy;

namespace ArenaBrasil.Monetization
{
    public class GachaSystem : NetworkBehaviour
    {
        public static GachaSystem Instance { get; private set; }
        
        [Header("Gacha Configuration")]
        public List<GachaBox> gachaBoxes = new List<GachaBox>();
        public List<GachaItem> allItems = new List<GachaItem>();
        
        [Header("Rates Configuration")]
        public float legendaryRate = 0.5f; // 0.5%
        public float epicRate = 2.5f; // 2.5%
        public float rareRate = 15f; // 15%
        public float commonRate = 82f; // 82%
        
        [Header("Pity System")]
        public int guaranteedLegendaryPity = 100;
        public int guaranteedEpicPity = 10;
        
        private Dictionary<ulong, PlayerGachaData> playerGachaData = new Dictionary<ulong, PlayerGachaData>();
        
        public event Action<ulong, GachaResult> OnGachaOpened;
        public event Action<ulong, GachaItem> OnLegendaryObtained;
        
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
            Debug.Log("Arena Brasil - Inicializando sistema Gacha");
            
            CreateGachaBoxes();
            SetupBrazilianItems();
        }
        
        void CreateGachaBoxes()
        {
            gachaBoxes.Add(new GachaBox
            {
                boxId = "lenda_box",
                name = "Caixa das Lendas",
                description = "Itens exclusivos dos heróis folclóricos brasileiros",
                cost = 100,
                currency = CurrencyType.Gems,
                guaranteedRarity = Rarity.Rare,
                specialItems = new string[] { "skin_saci_legendary", "weapon_curupira_bow" }
            });
            
            gachaBoxes.Add(new GachaBox
            {
                boxId = "brasil_box",
                name = "Caixa Brasil",
                description = "Celebre a cultura brasileira com itens temáticos",
                cost = 50,
                currency = CurrencyType.Gems,
                guaranteedRarity = Rarity.Common,
                specialItems = new string[] { "flag_brazil_cape", "samba_emote" }
            });
            
            gachaBoxes.Add(new GachaBox
            {
                boxId = "premium_box",
                name = "Caixa Premium",
                description = "Os itens mais raros e exclusivos do Arena Brasil",
                cost = 200,
                currency = CurrencyType.Gems,
                guaranteedRarity = Rarity.Epic,
                specialItems = new string[] { "mythic_iara_skin", "legendary_weapon_set" }
            });
        }
        
        void SetupBrazilianItems()
        {
            // Legendary Items
            allItems.Add(new GachaItem
            {
                itemId = "skin_saci_legendary",
                name = "Saci Dourado",
                description = "Skin lendária do Saci com efeitos especiais de ouro",
                rarity = Rarity.Legendary,
                type = ItemType.Skin,
                culturalValue = "Representa a lenda mais famosa do folclore brasileiro"
            });
            
            allItems.Add(new GachaItem
            {
                itemId = "mythic_iara_skin",
                name = "Iara Mística",
                description = "Skin mítica da Iara com animações aquáticas únicas",
                rarity = Rarity.Mythic,
                type = ItemType.Skin,
                culturalValue = "Sereia dos rios brasileiros"
            });
            
            // Epic Items
            allItems.Add(new GachaItem
            {
                itemId = "weapon_curupira_bow",
                name = "Arco do Curupira",
                description = "Arco épico com flechas de fogo verde",
                rarity = Rarity.Epic,
                type = ItemType.Weapon,
                culturalValue = "Arma do protetor da floresta"
            });
            
            // Rare Items
            allItems.Add(new GachaItem
            {
                itemId = "flag_brazil_cape",
                name = "Capa Bandeira do Brasil",
                description = "Capa com as cores da bandeira brasileira",
                rarity = Rarity.Rare,
                type = ItemType.Cosmetic,
                culturalValue = "Orgulho nacional brasileiro"
            });
            
            // Common Items
            allItems.Add(new GachaItem
            {
                itemId = "samba_emote",
                name = "Emote Samba",
                description = "Dance samba para celebrar suas vitórias",
                rarity = Rarity.Common,
                type = ItemType.Emote,
                culturalValue = "Dança tradicional brasileira"
            });
        }
        
        [ServerRpc(RequireOwnership = false)]
        public void OpenGachaBoxServerRpc(ulong playerId, string boxId, int quantity = 1)
        {
            if (!gachaBoxes.Exists(box => box.boxId == boxId)) return;
            
            var gachaBox = gachaBoxes.Find(box => box.boxId == boxId);
            var results = new List<GachaItem>();
            
            // Check if player has enough currency
            if (!CanAffordGacha(playerId, gachaBox, quantity)) return;
            
            // Initialize player gacha data if not exists
            if (!playerGachaData.ContainsKey(playerId))
            {
                playerGachaData[playerId] = new PlayerGachaData { playerId = playerId };
            }
            
            var playerData = playerGachaData[playerId];
            
            for (int i = 0; i < quantity; i++)
            {
                var item = RollGachaItem(gachaBox, playerData);
                results.Add(item);
                
                // Update pity counters
                UpdatePityCounters(playerData, item.rarity);
                
                // Check for legendary
                if (item.rarity == Rarity.Legendary || item.rarity == Rarity.Mythic)
                {
                    OnLegendaryObtained?.Invoke(playerId, item);
                    NotifyLegendaryDropClientRpc(playerId, item.name);
                }
            }
            
            // Deduct currency
            DeductGachaCurrency(playerId, gachaBox, quantity);
            
            var gachaResult = new GachaResult
            {
                boxId = boxId,
                items = results,
                timestamp = DateTime.Now
            };
            
            OnGachaOpened?.Invoke(playerId, gachaResult);
            GachaResultClientRpc(playerId, gachaResult);
        }
        
        GachaItem RollGachaItem(GachaBox box, PlayerGachaData playerData)
        {
            // Check pity system first
            if (playerData.pullsSinceLegendary >= guaranteedLegendaryPity)
            {
                return GetRandomItemByRarity(Rarity.Legendary);
            }
            
            if (playerData.pullsSinceEpic >= guaranteedEpicPity)
            {
                return GetRandomItemByRarity(Rarity.Epic);
            }
            
            // Normal probability roll
            float roll = UnityEngine.Random.Range(0f, 100f);
            
            if (roll < legendaryRate)
            {
                return GetRandomItemByRarity(Rarity.Legendary);
            }
            else if (roll < legendaryRate + epicRate)
            {
                return GetRandomItemByRarity(Rarity.Epic);
            }
            else if (roll < legendaryRate + epicRate + rareRate)
            {
                return GetRandomItemByRarity(Rarity.Rare);
            }
            else
            {
                return GetRandomItemByRarity(Rarity.Common);
            }
        }
        
        GachaItem GetRandomItemByRarity(Rarity rarity)
        {
            var itemsOfRarity = allItems.FindAll(item => item.rarity == rarity);
            if (itemsOfRarity.Count == 0) return allItems[0]; // Fallback
            
            return itemsOfRarity[UnityEngine.Random.Range(0, itemsOfRarity.Count)];
        }
        
        void UpdatePityCounters(PlayerGachaData playerData, Rarity obtainedRarity)
        {
            playerData.totalPulls++;
            
            if (obtainedRarity == Rarity.Legendary || obtainedRarity == Rarity.Mythic)
            {
                playerData.pullsSinceLegendary = 0;
                playerData.legendaryCount++;
            }
            else
            {
                playerData.pullsSinceLegendary++;
            }
            
            if (obtainedRarity == Rarity.Epic || obtainedRarity == Rarity.Legendary || obtainedRarity == Rarity.Mythic)
            {
                playerData.pullsSinceEpic = 0;
            }
            else
            {
                playerData.pullsSinceEpic++;
            }
        }
        
        bool CanAffordGacha(ulong playerId, GachaBox box, int quantity)
        {
            int totalCost = box.cost * quantity;
            
            if (EconomyManager.Instance != null)
            {
                return EconomyManager.Instance.HasEnoughCurrency(box.currency, totalCost);
            }
            
            return false;
        }
        
        void DeductGachaCurrency(ulong playerId, GachaBox box, int quantity)
        {
            int totalCost = box.cost * quantity;
            
            if (EconomyManager.Instance != null)
            {
                if (box.currency == CurrencyType.Gems)
                {
                    EconomyManager.Instance.AddCurrency(CurrencyType.Gems, -totalCost);
                }
                else
                {
                    EconomyManager.Instance.AddCurrency(CurrencyType.Coins, -totalCost);
                }
            }
        }
        
        [ClientRpc]
        void NotifyLegendaryDropClientRpc(ulong playerId, string itemName)
        {
            // Show global notification
            Debug.Log($"🎉 LENDÁRIO! Jogador {playerId} obteve {itemName}!");
            
            if (UIManager.Instance != null)
            {
                // Show legendary animation
                ShowLegendaryAnimation(itemName);
            }
        }
        
        [ClientRpc]
        void GachaResultClientRpc(ulong playerId, GachaResult result)
        {
            // Show gacha results to player
            ShowGachaResults(result);
        }
        
        void ShowLegendaryAnimation(string itemName)
        {
            // Legendary drop animation
            Debug.Log($"Mostrando animação lendária para {itemName}");
        }
        
        void ShowGachaResults(GachaResult result)
        {
            // Display gacha results UI
            Debug.Log($"Resultados da gacha: {result.items.Count} itens obtidos");
        }
        
        public PlayerGachaData GetPlayerGachaData(ulong playerId)
        {
            return playerGachaData.ContainsKey(playerId) ? playerGachaData[playerId] : null;
        }
        
        public List<GachaBox> GetAvailableBoxes() => gachaBoxes;
    }
    
    [Serializable]
    public class GachaBox
    {
        public string boxId;
        public string name;
        public string description;
        public int cost;
        public CurrencyType currency;
        public Rarity guaranteedRarity;
        public string[] specialItems;
        public bool isLimitedTime;
        public DateTime expirationDate;
    }
    
    [Serializable]
    public class GachaItem
    {
        public string itemId;
        public string name;
        public string description;
        public Rarity rarity;
        public ItemType type;
        public string culturalValue;
        public Sprite icon;
    }
    
    [Serializable]
    public class GachaResult
    {
        public string boxId;
        public List<GachaItem> items;
        public DateTime timestamp;
    }
    
    [Serializable]
    public class PlayerGachaData
    {
        public ulong playerId;
        public int totalPulls;
        public int pullsSinceLegendary;
        public int pullsSinceEpic;
        public int legendaryCount;
        public Dictionary<string, int> itemCounts = new Dictionary<string, int>();
    }
}
