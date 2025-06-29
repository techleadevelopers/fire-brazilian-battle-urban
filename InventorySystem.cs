
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;

namespace ArenaBrasil.Inventory
{
    public class InventorySystem : NetworkBehaviour
    {
        public static InventorySystem Instance { get; private set; }
        
        [Header("Inventory Configuration")]
        public int maxInventorySlots = 100;
        public int maxWeaponSlots = 10;
        public int maxCosmeticSlots = 50;
        
        [Header("Player Cosmetics - Free Fire Style")]
        public PlayerCosmetics equippedCosmetics = new PlayerCosmetics();
        public List<CosmeticItem> ownedCosmetics = new List<CosmeticItem>();
        public List<VehicleItem> ownedVehicles = new List<VehicleItem>();
        
        [Header("Brazilian Fashion Items")]
        public List<CosmeticItem> availableCosmetics = new List<CosmeticItem>();
        public List<VehicleItem> availableVehicles = new List<VehicleItem>();
        
        // Inventory data
        private Dictionary<string, int> items = new Dictionary<string, int>();
        private List<WeaponItem> weapons = new List<WeaponItem>();
        
        // Events
        public event System.Action<CosmeticItem> OnCosmeticEquipped;
        public event System.Action<VehicleItem> OnVehicleEquipped;
        public event System.Action OnInventoryUpdated;
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeBrazilianCosmetics();
                InitializeBrazilianVehicles();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void InitializeBrazilianCosmetics()
        {
            // === ROUPAS MASCULINAS BRASILEIRAS ===
            
            // Camisetas
            availableCosmetics.Add(new CosmeticItem
            {
                itemId = "camisa_flamengo",
                itemName = "Camisa do Flamengo",
                itemType = CosmeticType.Shirt,
                rarity = ItemRarity.Common,
                price = 50,
                gender = Gender.Male,
                description = "Camisa oficial do Mengão",
                culturalTheme = "Futebol Brasileiro"
            });
            
            availableCosmetics.Add(new CosmeticItem
            {
                itemId = "camisa_brasil",
                itemName = "Camisa da Seleção",
                itemType = CosmeticType.Shirt,
                rarity = ItemRarity.Epic,
                price = 200,
                gender = Gender.Unisex,
                description = "Camisa oficial da Seleção Brasileira",
                culturalTheme = "Copa do Mundo"
            });
            
            availableCosmetics.Add(new CosmeticItem
            {
                itemId = "regata_copacabana",
                itemName = "Regata de Copacabana",
                itemType = CosmeticType.Shirt,
                rarity = ItemRarity.Rare,
                price = 75,
                gender = Gender.Male,
                description = "Regata estilosa do Rio",
                culturalTheme = "Carioca"
            });
            
            // Calças e Shorts
            availableCosmetics.Add(new CosmeticItem
            {
                itemId = "bermuda_surf",
                itemName = "Bermuda de Surf",
                itemType = CosmeticType.Pants,
                rarity = ItemRarity.Common,
                price = 40,
                gender = Gender.Male,
                description = "Bermuda perfeita para o verão brasileiro"
            });
            
            availableCosmetics.Add(new CosmeticItem
            {
                itemId = "jeans_osasco",
                itemName = "Jeans de Osasco",
                itemType = CosmeticType.Pants,
                rarity = ItemRarity.Common,
                price = 60,
                gender = Gender.Unisex,
                description = "Jeans resistente da periferia"
            });
            
            // === TÊNIS BRASILEIROS ===
            
            availableCosmetics.Add(new CosmeticItem
            {
                itemId = "tenis_olimpikus",
                itemName = "Tênis Olympikus",
                itemType = CosmeticType.Shoes,
                rarity = ItemRarity.Common,
                price = 80,
                gender = Gender.Unisex,
                description = "Tênis brasileiro de qualidade",
                specialEffect = "+5% Velocidade de Movimento"
            });
            
            availableCosmetics.Add(new CosmeticItem
            {
                itemId = "tenis_mizuno_br",
                itemName = "Mizuno Wave Brasil",
                itemType = CosmeticType.Shoes,
                rarity = ItemRarity.Rare,
                price = 150,
                gender = Gender.Unisex,
                description = "Tênis de corrida premium",
                specialEffect = "+8% Velocidade de Movimento"
            });
            
            availableCosmetics.Add(new CosmeticItem
            {
                itemId = "chuteira_falcao",
                itemName = "Chuteira do Falcão",
                itemType = CosmeticType.Shoes,
                rarity = ItemRarity.Legendary,
                price = 500,
                gender = Gender.Unisex,
                description = "Chuteira lendária do Rei do Futsal",
                specialEffect = "+15% Velocidade, +10% Agilidade"
            });
            
            availableCosmetics.Add(new CosmeticItem
            {
                itemId = "havaianas_dourada",
                itemName = "Havaianas Dourada",
                itemType = CosmeticType.Shoes,
                rarity = ItemRarity.Epic,
                price = 300,
                gender = Gender.Unisex,
                description = "Chinelo dourado exclusivo",
                specialEffect = "+20% Resistência a Dano de Fogo"
            });
            
            // === ROUPAS FEMININAS ===
            
            availableCosmetics.Add(new CosmeticItem
            {
                itemId = "top_carnaval",
                itemName = "Top de Carnaval",
                itemType = CosmeticType.Shirt,
                rarity = ItemRarity.Epic,
                price = 250,
                gender = Gender.Female,
                description = "Top brilhante de carnaval",
                culturalTheme = "Carnaval Brasileiro"
            });
            
            availableCosmetics.Add(new CosmeticItem
            {
                itemId = "saia_baiana",
                itemName = "Saia Baiana",
                itemType = CosmeticType.Pants,
                rarity = ItemRarity.Rare,
                price = 120,
                gender = Gender.Female,
                description = "Saia tradicional da Bahia",
                culturalTheme = "Folclore Bahiano"
            });
            
            // === ACESSÓRIOS ===
            
            availableCosmetics.Add(new CosmeticItem
            {
                itemId = "bone_sp",
                itemName = "Boné de São Paulo",
                itemType = CosmeticType.Hat,
                rarity = ItemRarity.Common,
                price = 35,
                gender = Gender.Unisex,
                description = "Boné clássico paulista"
            });
            
            availableCosmetics.Add(new CosmeticItem
            {
                itemId = "oculos_juliet",
                itemName = "Óculos Juliet",
                itemType = CosmeticType.Glasses,
                rarity = ItemRarity.Rare,
                price = 180,
                gender = Gender.Unisex,
                description = "Óculos icônico dos anos 2000",
                specialEffect = "+10% Precisão de Mira"
            });
            
            availableCosmetics.Add(new CosmeticItem
            {
                itemId = "corrente_ouro",
                itemName = "Corrente de Ouro",
                itemType = CosmeticType.Necklace,
                rarity = ItemRarity.Epic,
                price = 400,
                gender = Gender.Unisex,
                description = "Corrente de ouro ostentação",
                specialEffect = "+5% XP Ganho"
            });
            
            // === UNIFORMES ESPECIAIS ===
            
            availableCosmetics.Add(new CosmeticItem
            {
                itemId = "uniforme_bope",
                itemName = "Uniforme BOPE",
                itemType = CosmeticType.FullOutfit,
                rarity = ItemRarity.Legendary,
                price = 800,
                gender = Gender.Unisex,
                description = "Uniforme das forças especiais",
                specialEffect = "+15% Resistência a Dano, +10% Velocidade de Recarga"
            });
            
            availableCosmetics.Add(new CosmeticItem
            {
                itemId = "fantasia_saci",
                itemName = "Fantasia do Saci",
                itemType = CosmeticType.FullOutfit,
                rarity = ItemRarity.Legendary,
                price = 600,
                gender = Gender.Unisex,
                description = "Fantasia do Saci Pererê",
                specialEffect = "+25% Velocidade, Teletransporte Limitado"
            });
        }
        
        void InitializeBrazilianVehicles()
        {
            // === CARROS BRASILEIROS ===
            
            availableVehicles.Add(new VehicleItem
            {
                vehicleId = "gol_g4",
                vehicleName = "Volkswagen Gol G4",
                vehicleType = VehicleType.Car,
                rarity = ItemRarity.Common,
                price = 200,
                maxSpeed = 80f,
                acceleration = 6f,
                handling = 7f,
                durability = 100f,
                fuelCapacity = 50f,
                seats = 4,
                description = "O carro do povo brasileiro",
                isIconic = true
            });
            
            availableVehicles.Add(new VehicleItem
            {
                vehicleId = "uno_mille",
                vehicleName = "Fiat Uno Mille",
                vehicleType = VehicleType.Car,
                rarity = ItemRarity.Common,
                price = 150,
                maxSpeed = 75f,
                acceleration = 5f,
                handling = 8f,
                durability = 90f,
                fuelCapacity = 45f,
                seats = 4,
                description = "Uno com escada em cima",
                isIconic = true
            });
            
            availableVehicles.Add(new VehicleItem
            {
                vehicleId = "corsa_wind",
                vehicleName = "Chevrolet Corsa Wind",
                vehicleType = VehicleType.Car,
                rarity = ItemRarity.Common,
                price = 180,
                maxSpeed = 85f,
                acceleration = 7f,
                handling = 8f,
                durability = 95f,
                fuelCapacity = 48f,
                seats = 4,
                description = "Corsinha nervosinho"
            });
            
            availableVehicles.Add(new VehicleItem
            {
                vehicleId = "civic_si",
                vehicleName = "Honda Civic Si Turbo",
                vehicleType = VehicleType.Car,
                rarity = ItemRarity.Epic,
                price = 800,
                maxSpeed = 120f,
                acceleration = 9f,
                handling = 9f,
                durability = 120f,
                fuelCapacity = 60f,
                seats = 4,
                description = "Civic do ano rebaixado",
                specialEffect = "Turbo Boost +50% Velocidade (10s)"
            });
            
            // === MOTOS BRASILEIRAS ===
            
            availableVehicles.Add(new VehicleItem
            {
                vehicleId = "biz_125",
                vehicleName = "Honda Biz 125",
                vehicleType = VehicleType.Motorcycle,
                rarity = ItemRarity.Common,
                price = 100,
                maxSpeed = 60f,
                acceleration = 8f,
                handling = 9f,
                durability = 70f,
                fuelCapacity = 20f,
                seats = 2,
                description = "Moto do entregador brasileiro"
            });
            
            availableVehicles.Add(new VehicleItem
            {
                vehicleId = "cb600_hornet",
                vehicleName = "Honda CB 600F Hornet",
                vehicleType = VehicleType.Motorcycle,
                rarity = ItemRarity.Rare,
                price = 400,
                maxSpeed = 140f,
                acceleration = 10f,
                handling = 8f,
                durability = 80f,
                fuelCapacity = 25f,
                seats = 2,
                description = "Hornet das pistas brasileiras"
            });
            
            availableVehicles.Add(new VehicleItem
            {
                vehicleId = "ninja_zx10r",
                vehicleName = "Kawasaki Ninja ZX-10R",
                vehicleType = VehicleType.Motorcycle,
                rarity = ItemRarity.Legendary,
                price = 1000,
                maxSpeed = 180f,
                acceleration = 10f,
                handling = 7f,
                durability = 90f,
                fuelCapacity = 30f,
                seats = 2,
                description = "Ninja dos sonhos brasileiros",
                specialEffect = "Modo Fantasma - Invisível no radar (5s)"
            });
            
            // === VEÍCULOS ESPECIAIS ===
            
            availableVehicles.Add(new VehicleItem
            {
                vehicleId = "kombi_surf",
                vehicleName = "Kombi do Surf",
                vehicleType = VehicleType.Van,
                rarity = ItemRarity.Epic,
                price = 500,
                maxSpeed = 70f,
                acceleration = 4f,
                handling = 6f,
                durability = 150f,
                fuelCapacity = 80f,
                seats = 8,
                description = "Kombi clássica das praias brasileiras",
                specialEffect = "Ponto de Respawn Móvel"
            });
            
            availableVehicles.Add(new VehicleItem
            {
                vehicleId = "jeep_willys",
                vehicleName = "Jeep Willys Rural",
                vehicleType = VehicleType.OffRoad,
                rarity = ItemRarity.Rare,
                price = 350,
                maxSpeed = 90f,
                acceleration = 6f,
                handling = 8f,
                durability = 140f,
                fuelCapacity = 70f,
                seats = 4,
                description = "Jeep das fazendas brasileiras",
                specialEffect = "Resistente a Terreno Difícil"
            });
            
            availableVehicles.Add(new VehicleItem
            {
                vehicleId = "saveiro_cross",
                vehicleName = "VW Saveiro Cross",
                vehicleType = VehicleType.Truck,
                rarity = ItemRarity.Rare,
                price = 300,
                maxSpeed = 95f,
                acceleration = 7f,
                handling = 7f,
                durability = 130f,
                fuelCapacity = 65f,
                seats = 2,
                description = "Saveiro preparada para off-road"
            });
        }
        
        // === SISTEMA DE EQUIPAR COSMÉTICOS ===
        
        public void EquipCosmetic(string itemId)
        {
            var cosmetic = ownedCosmetics.Find(c => c.itemId == itemId);
            if (cosmetic == null) return;
            
            // Remove previous item of same type
            switch (cosmetic.itemType)
            {
                case CosmeticType.Shirt:
                    equippedCosmetics.shirt = cosmetic;
                    break;
                case CosmeticType.Pants:
                    equippedCosmetics.pants = cosmetic;
                    break;
                case CosmeticType.Shoes:
                    equippedCosmetics.shoes = cosmetic;
                    break;
                case CosmeticType.Hat:
                    equippedCosmetics.hat = cosmetic;
                    break;
                case CosmeticType.Glasses:
                    equippedCosmetics.glasses = cosmetic;
                    break;
                case CosmeticType.Necklace:
                    equippedCosmetics.necklace = cosmetic;
                    break;
                case CosmeticType.FullOutfit:
                    EquipFullOutfit(cosmetic);
                    break;
            }
            
            OnCosmeticEquipped?.Invoke(cosmetic);
            ApplyCosmeticEffects(cosmetic);
        }
        
        void EquipFullOutfit(CosmeticItem outfit)
        {
            equippedCosmetics.shirt = outfit;
            equippedCosmetics.pants = outfit;
            equippedCosmetics.shoes = outfit;
            equippedCosmetics.hat = outfit;
        }
        
        public void EquipVehicle(string vehicleId)
        {
            var vehicle = ownedVehicles.Find(v => v.vehicleId == vehicleId);
            if (vehicle == null) return;
            
            equippedCosmetics.equippedVehicle = vehicle;
            OnVehicleEquipped?.Invoke(vehicle);
        }
        
        void ApplyCosmeticEffects(CosmeticItem cosmetic)
        {
            if (string.IsNullOrEmpty(cosmetic.specialEffect)) return;
            
            var playerController = GetComponent<PlayerController>();
            if (playerController == null) return;
            
            // Apply special effects based on cosmetic
            if (cosmetic.specialEffect.Contains("Velocidade"))
            {
                float speedBonus = ExtractPercentage(cosmetic.specialEffect);
                playerController.ApplySpeedBonus(speedBonus);
            }
            
            if (cosmetic.specialEffect.Contains("Precisão"))
            {
                float accuracyBonus = ExtractPercentage(cosmetic.specialEffect);
                var weaponController = GetComponent<ArenaBrasil.Gameplay.Weapons.WeaponController>();
                if (weaponController != null)
                {
                    weaponController.ApplyAccuracyBonus(accuracyBonus);
                }
            }
        }
        
        float ExtractPercentage(string text)
        {
            var parts = text.Split('%');
            if (parts.Length > 0)
            {
                var numbers = parts[0].Where(char.IsDigit).ToArray();
                if (numbers.Length > 0)
                {
                    string numberStr = new string(numbers);
                    if (float.TryParse(numberStr, out float result))
                    {
                        return result / 100f;
                    }
                }
            }
            return 0f;
        }
        
        // === SISTEMA DE COMPRA ===
        
        public bool PurchaseCosmetic(string itemId, int price)
        {
            var cosmetic = availableCosmetics.Find(c => c.itemId == itemId);
            if (cosmetic == null) return false;
            
            if (ArenaBrasil.Economy.EconomyManager.Instance.SpendCoins(price))
            {
                ownedCosmetics.Add(cosmetic);
                OnInventoryUpdated?.Invoke();
                return true;
            }
            
            return false;
        }
        
        public bool PurchaseVehicle(string vehicleId, int price)
        {
            var vehicle = availableVehicles.Find(v => v.vehicleId == vehicleId);
            if (vehicle == null) return false;
            
            if (ArenaBrasil.Economy.EconomyManager.Instance.SpendCoins(price))
            {
                ownedVehicles.Add(vehicle);
                OnInventoryUpdated?.Invoke();
                return true;
            }
            
            return false;
        }
        
        // === SISTEMA DE ITENS BÁSICOS ===
        
        public void AddItem(string itemId, int quantity = 1)
        {
            if (items.ContainsKey(itemId))
            {
                items[itemId] += quantity;
            }
            else
            {
                items[itemId] = quantity;
            }
            
            OnInventoryUpdated?.Invoke();
        }
        
        public bool UseItem(string itemId, int quantity = 1)
        {
            if (!items.ContainsKey(itemId) || items[itemId] < quantity)
                return false;
            
            items[itemId] -= quantity;
            if (items[itemId] <= 0)
            {
                items.Remove(itemId);
            }
            
            OnInventoryUpdated?.Invoke();
            return true;
        }
        
        public int GetItemCount(string itemId)
        {
            return items.ContainsKey(itemId) ? items[itemId] : 0;
        }
        
        public bool HasItem(string itemId)
        {
            return items.ContainsKey(itemId) && items[itemId] > 0;
        }
        
        // === GETTERS ===
        
        public List<CosmeticItem> GetOwnedCosmetics() => ownedCosmetics;
        public List<VehicleItem> GetOwnedVehicles() => ownedVehicles;
        public PlayerCosmetics GetEquippedCosmetics() => equippedCosmetics;
        public List<CosmeticItem> GetAvailableCosmetics() => availableCosmetics;
        public List<VehicleItem> GetAvailableVehicles() => availableVehicles;
        
        public List<CosmeticItem> GetCosmeticsByType(CosmeticType type)
        {
            return availableCosmetics.Where(c => c.itemType == type).ToList();
        }
        
        public List<VehicleItem> GetVehiclesByType(VehicleType type)
        {
            return availableVehicles.Where(v => v.vehicleType == type).ToList();
        }
    }
    
    // === CLASSES DE DADOS ===
    
    [System.Serializable]
    public class PlayerCosmetics
    {
        public CosmeticItem shirt;
        public CosmeticItem pants;
        public CosmeticItem shoes;
        public CosmeticItem hat;
        public CosmeticItem glasses;
        public CosmeticItem necklace;
        public VehicleItem equippedVehicle;
    }
    
    [System.Serializable]
    public class CosmeticItem
    {
        public string itemId;
        public string itemName;
        public CosmeticType itemType;
        public ItemRarity rarity;
        public int price;
        public Gender gender;
        public string description;
        public string specialEffect;
        public string culturalTheme;
        public Sprite itemIcon;
        public GameObject itemModel;
        public bool isLimited;
        public bool isEvent;
    }
    
    [System.Serializable]
    public class VehicleItem
    {
        public string vehicleId;
        public string vehicleName;
        public VehicleType vehicleType;
        public ItemRarity rarity;
        public int price;
        public float maxSpeed;
        public float acceleration;
        public float handling;
        public float durability;
        public float fuelCapacity;
        public int seats;
        public string description;
        public string specialEffect;
        public Sprite vehicleIcon;
        public GameObject vehicleModel;
        public bool isIconic;
    }
    
    [System.Serializable]
    public class WeaponItem
    {
        public string weaponId;
        public string weaponName;
        public int quantity;
        public List<string> attachments = new List<string>();
    }
    
    public enum CosmeticType
    {
        Shirt,
        Pants,
        Shoes,
        Hat,
        Glasses,
        Necklace,
        FullOutfit
    }
    
    public enum VehicleType
    {
        Car,
        Motorcycle,
        Truck,
        Van,
        OffRoad,
        Boat,
        Helicopter
    }
    
    public enum Gender
    {
        Male,
        Female,
        Unisex
    }
    
    public enum ItemRarity
    {
        Common,
        Rare,
        Epic,
        Legendary
    }
}
