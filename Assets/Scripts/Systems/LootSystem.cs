
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using ArenaBrasil.Gameplay.Weapons;

namespace ArenaBrasil.Loot
{
    public class LootSystem : NetworkBehaviour
    {
        public static LootSystem Instance { get; private set; }
        
        [Header("Loot Configuration")]
        public List<LootTable> lootTables = new List<LootTable>();
        public List<LootSpawnPoint> spawnPoints = new List<LootSpawnPoint>();
        public GameObject lootBoxPrefab;
        
        [Header("Spawn Settings")]
        public int maxLootItems = 500;
        public float respawnDelay = 30f;
        public float lootDespawnTime = 300f; // 5 minutes
        
        // Active loot tracking
        private Dictionary<int, GameObject> activeLoot = new Dictionary<int, GameObject>();
        private int nextLootId = 0;
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeLootTables();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void InitializeLootTables()
        {
            if (lootTables.Count == 0)
            {
                // Create default Brazilian-themed loot tables
                lootTables.Add(new LootTable
                {
                    tableName = "Common Urban",
                    items = new List<LootItem>
                    {
                        new LootItem { itemId = "fal_br", itemName = "FAL Brasileiro", rarity = ItemRarity.Common, dropChance = 25f },
                        new LootItem { itemId = "taurus_pt92", itemName = "Taurus PT92", rarity = ItemRarity.Common, dropChance = 30f },
                        new LootItem { itemId = "bandage", itemName = "Bandagem", rarity = ItemRarity.Common, dropChance = 40f },
                        new LootItem { itemId = "energy_drink", itemName = "Energético", rarity = ItemRarity.Common, dropChance = 35f }
                    }
                });
                
                lootTables.Add(new LootTable
                {
                    tableName = "Military Rare",
                    items = new List<LootItem>
                    {
                        new LootItem { itemId = "imbel_ia2", itemName = "IMBEL IA2", rarity = ItemRarity.Rare, dropChance = 15f },
                        new LootItem { itemId = "armor_vest", itemName = "Colete Balístico", rarity = ItemRarity.Rare, dropChance = 20f },
                        new LootItem { itemId = "medkit", itemName = "Kit Médico", rarity = ItemRarity.Rare, dropChance = 18f }
                    }
                });
                
                lootTables.Add(new LootTable
                {
                    tableName = "Legendary Cache",
                    items = new List<LootItem>
                    {
                        new LootItem { itemId = "saci_staff", itemName = "Cajado do Saci", rarity = ItemRarity.Legendary, dropChance = 5f },
                        new LootItem { itemId = "curupira_boots", itemName = "Botas do Curupira", rarity = ItemRarity.Legendary, dropChance = 3f },
                        new LootItem { itemId = "iara_charm", itemName = "Amuleto da Iara", rarity = ItemRarity.Legendary, dropChance = 2f }
                    }
                });
            }
        }
        
        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                SpawnInitialLoot();
            }
        }
        
        void SpawnInitialLoot()
        {
            if (spawnPoints.Count == 0)
            {
                GenerateSpawnPoints();
            }
            
            foreach (var spawnPoint in spawnPoints)
            {
                SpawnLootAtPoint(spawnPoint);
            }
        }
        
        void GenerateSpawnPoints()
        {
            // Generate spawn points based on map areas
            for (int i = 0; i < 100; i++)
            {
                Vector3 randomPos = new Vector3(
                    Random.Range(-500f, 500f),
                    50f,
                    Random.Range(-500f, 500f)
                );
                
                // Raycast to find ground
                if (Physics.Raycast(randomPos, Vector3.down, out RaycastHit hit, 100f))
                {
                    spawnPoints.Add(new LootSpawnPoint
                    {
                        position = hit.point + Vector3.up * 0.5f,
                        lootTableName = DetermineLootTable(hit.point),
                        isActive = true
                    });
                }
            }
        }
        
        string DetermineLootTable(Vector3 position)
        {
            // Determine loot table based on position/area type
            float distanceFromCenter = Vector3.Distance(position, Vector3.zero);
            
            if (distanceFromCenter < 100f)
                return "Legendary Cache";
            else if (distanceFromCenter < 300f)
                return "Military Rare";
            else
                return "Common Urban";
        }
        
        void SpawnLootAtPoint(LootSpawnPoint spawnPoint)
        {
            if (!spawnPoint.isActive) return;
            
            var lootTable = GetLootTable(spawnPoint.lootTableName);
            if (lootTable == null) return;
            
            var selectedItem = SelectRandomItem(lootTable);
            if (selectedItem != null)
            {
                SpawnLootItem(selectedItem, spawnPoint.position);
            }
        }
        
        LootTable GetLootTable(string tableName)
        {
            return lootTables.Find(table => table.tableName == tableName);
        }
        
        LootItem SelectRandomItem(LootTable table)
        {
            float totalWeight = 0f;
            foreach (var item in table.items)
            {
                totalWeight += item.dropChance;
            }
            
            float randomValue = Random.Range(0f, totalWeight);
            float currentWeight = 0f;
            
            foreach (var item in table.items)
            {
                currentWeight += item.dropChance;
                if (randomValue <= currentWeight)
                {
                    return item;
                }
            }
            
            return table.items[0]; // Fallback
        }
        
        void SpawnLootItem(LootItem item, Vector3 position)
        {
            if (activeLoot.Count >= maxLootItems) return;
            
            int lootId = nextLootId++;
            SpawnLootItemClientRpc(lootId, item.itemId, item.itemName, position, (int)item.rarity);
        }
        
        [ClientRpc]
        void SpawnLootItemClientRpc(int lootId, string itemId, string itemName, Vector3 position, int rarity)
        {
            if (lootBoxPrefab != null)
            {
                GameObject lootObject = Instantiate(lootBoxPrefab, position, Quaternion.identity);
                var lootPickup = lootObject.GetComponent<LootPickup>();
                
                if (lootPickup == null)
                {
                    lootPickup = lootObject.AddComponent<LootPickup>();
                }
                
                lootPickup.Initialize(lootId, itemId, itemName, (ItemRarity)rarity);
                activeLoot[lootId] = lootObject;
                
                // Schedule despawn
                Invoke(nameof(DespawnLoot), lootDespawnTime);
            }
        }
        
        [ServerRpc(RequireOwnership = false)]
        public void PickupLootServerRpc(int lootId, ulong playerId)
        {
            if (activeLoot.ContainsKey(lootId))
            {
                // Validate pickup and give item to player
                var player = GetPlayerById(playerId);
                if (player != null)
                {
                    // Add item to player inventory
                    NotifyLootPickupClientRpc(lootId, playerId);
                    RemoveLootItem(lootId);
                }
            }
        }
        
        [ClientRpc]
        void NotifyLootPickupClientRpc(int lootId, ulong playerId)
        {
            if (activeLoot.ContainsKey(lootId))
            {
                Destroy(activeLoot[lootId]);
                activeLoot.Remove(lootId);
            }
        }
        
        void RemoveLootItem(int lootId)
        {
            if (activeLoot.ContainsKey(lootId))
            {
                activeLoot.Remove(lootId);
            }
        }
        
        void DespawnLoot()
        {
            // Clean up old loot items
            List<int> toRemove = new List<int>();
            
            foreach (var kvp in activeLoot)
            {
                if (kvp.Value == null)
                {
                    toRemove.Add(kvp.Key);
                }
            }
            
            foreach (int id in toRemove)
            {
                activeLoot.Remove(id);
            }
        }
        
        PlayerController GetPlayerById(ulong clientId)
        {
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
            {
                return client.PlayerObject?.GetComponent<PlayerController>();
            }
            return null;
        }
        
        public void SpawnCarePackage(Vector3 position)
        {
            var legendaryTable = GetLootTable("Legendary Cache");
            if (legendaryTable != null)
            {
                var item = SelectRandomItem(legendaryTable);
                SpawnLootItem(item, position);
            }
        }
    }
    
    [System.Serializable]
    public class LootTable
    {
        public string tableName;
        public List<LootItem> items = new List<LootItem>();
    }
    
    [System.Serializable]
    public class LootItem
    {
        public string itemId;
        public string itemName;
        public ItemRarity rarity;
        public float dropChance;
        public Sprite icon;
    }
    
    [System.Serializable]
    public class LootSpawnPoint
    {
        public Vector3 position;
        public string lootTableName;
        public bool isActive;
        public float lastSpawnTime;
    }
    
    public enum ItemRarity
    {
        Common,
        Rare,
        Epic,
        Legendary
    }
}
