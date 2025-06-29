
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;
using ArenaBrasil.UI;
using ArenaBrasil.Inventory;

namespace ArenaBrasil.PlayerCreation
{
    public class PlayerCreationSystem : NetworkBehaviour
    {
        public static PlayerCreationSystem Instance { get; private set; }
        
        [Header("Character Creation")]
        public Transform characterPreview;
        public Camera previewCamera;
        public GameObject[] characterModels;
        
        [Header("Customization Options")]
        public List<HairStyle> hairStyles = new List<HairStyle>();
        public List<FaceOption> faceOptions = new List<FaceOption>();
        public List<TattooOption> tattoos = new List<TattooOption>();
        public List<ClothingItem> clothing = new List<ClothingItem>();
        public List<AccessoryItem> accessories = new List<AccessoryItem>();
        
        [Header("Brazilian Urban Style")]
        public List<BrazilianHaircut> brazilianHaircuts = new List<BrazilianHaircut>();
        public List<UrbanAccessory> urbanAccessories = new List<UrbanAccessory>();
        public List<BrazilianTattoo> brazilianTattoos = new List<BrazilianTattoo>();
        
        private PlayerProfile currentPlayerProfile = new PlayerProfile();
        private GameObject previewCharacter;
        
        public event System.Action<PlayerProfile> OnPlayerCreated;
        public event System.Action<string> OnCustomizationChanged;
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeCharacterCreation();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void InitializeCharacterCreation()
        {
            Debug.Log("🎨 Arena Brasil - Sistema de Criação de Personagens Iniciado");
            
            LoadBrazilianCustomizations();
            LoadUrbanStyleOptions();
            LoadAccessoriesAndClothing();
            SetupPreviewSystem();
        }
        
        void LoadBrazilianCustomizations()
        {
            // === CORTES DE CABELO BRASILEIROS ===
            
            brazilianHaircuts.Add(new BrazilianHaircut
            {
                id = "corte_degradê",
                name = "Degradê Moderno",
                description = "Corte clássico das quebradas",
                rarity = ItemRarity.Common,
                price = 50,
                culturalOrigin = "Periferia Brasileira",
                isUnlocked = true
            });
            
            brazilianHaircuts.Add(new BrazilianHaircut
            {
                id = "topete_pompadour",
                name = "Topete Pompadour",
                description = "Estilo anos 50 brasileiro",
                rarity = ItemRarity.Rare,
                price = 120,
                culturalOrigin = "Rock Brasileiro"
            });
            
            brazilianHaircuts.Add(new BrazilianHaircut
            {
                id = "moicano_punk",
                name = "Moicano Punk BR",
                description = "Rebeldia das ruas brasileiras",
                rarity = ItemRarity.Epic,
                price = 200,
                culturalOrigin = "Movimento Punk Nacional"
            });
            
            brazilianHaircuts.Add(new BrazilianHaircut
            {
                id = "dreadlock_reggae",
                name = "Dreadlocks Reggae",
                description = "Estilo rastafári brasileiro",
                rarity = ItemRarity.Legendary,
                price = 400,
                culturalOrigin = "Cultura Reggae BR"
            });
            
            brazilianHaircuts.Add(new BrazilianHaircut
            {
                id = "careca_ostentacao",
                name = "Careca Ostentação",
                description = "Estilo ostentação paulista",
                rarity = ItemRarity.Epic,
                price = 80,
                culturalOrigin = "Funk Ostentação"
            });
        }
        
        void LoadUrbanStyleOptions()
        {
            // === TATUAGENS BRASILEIRAS ===
            
            brazilianTattoos.Add(new BrazilianTattoo
            {
                id = "tattoo_cristo_redentor",
                name = "Cristo Redentor",
                description = "Tatuagem icônica do RJ",
                bodyPart = BodyPart.Back,
                rarity = ItemRarity.Epic,
                price = 300,
                culturalMeaning = "Fé e proteção divina"
            });
            
            brazilianTattoos.Add(new BrazilianTattoo
            {
                id = "tattoo_favela",
                name = "Vida da Favela",
                description = "Arte urbana das comunidades",
                bodyPart = BodyPart.Arm,
                rarity = ItemRarity.Rare,
                price = 180,
                culturalMeaning = "Orgulho da origem"
            });
            
            brazilianTattoos.Add(new BrazilianTattoo
            {
                id = "tattoo_saci",
                name = "Saci Pererê",
                description = "Folclore brasileiro na pele",
                bodyPart = BodyPart.Leg,
                rarity = ItemRarity.Legendary,
                price = 500,
                culturalMeaning = "Conexão com as raízes",
                specialEffect = "+10% Velocidade"
            });
            
            brazilianTattoos.Add(new BrazilianTattoo
            {
                id = "tattoo_flamengo",
                name = "Escudo do Mengão",
                description = "Paixão rubro-negra",
                bodyPart = BodyPart.Chest,
                rarity = ItemRarity.Common,
                price = 100,
                culturalMeaning = "Amor clubístico"
            });
        }
        
        void LoadAccessoriesAndClothing()
        {
            // === ACESSÓRIOS URBANOS ===
            
            urbanAccessories.Add(new UrbanAccessory
            {
                id = "corrente_ouro_18k",
                name = "Corrente de Ouro 18k",
                description = "Ostentação das quebradas",
                type = AccessoryType.Necklace,
                rarity = ItemRarity.Epic,
                price = 400,
                specialEffect = "+5% XP Ganho",
                brandName = "Made in Brás"
            });
            
            urbanAccessories.Add(new UrbanAccessory
            {
                id = "bone_new_era_br",
                name = "Boné New Era Brasil",
                description = "Estilo americano com toque brasileiro",
                type = AccessoryType.Hat,
                rarity = ItemRarity.Rare,
                price = 150,
                brandName = "New Era"
            });
            
            urbanAccessories.Add(new UrbanAccessory
            {
                id = "oculos_oakley_juliet",
                name = "Oakley Juliet",
                description = "Óculos dos sonhos dos anos 2000",
                type = AccessoryType.Glasses,
                rarity = ItemRarity.Legendary,
                price = 800,
                specialEffect = "+15% Precisão de Mira",
                brandName = "Oakley"
            });
            
            // === TÊNIS DE MARCA ===
            
            urbanAccessories.Add(new UrbanAccessory
            {
                id = "nike_air_jordan_br",
                name = "Nike Air Jordan Brasil",
                description = "Jordan exclusivo verde e amarelo",
                type = AccessoryType.Shoes,
                rarity = ItemRarity.Legendary,
                price = 1200,
                specialEffect = "+20% Velocidade de Movimento",
                brandName = "Nike"
            });
            
            urbanAccessories.Add(new UrbanAccessory
            {
                id = "adidas_superstar_br",
                name = "Adidas Superstar Brasil",
                description = "Clássico com as cores nacionais",
                type = AccessoryType.Shoes,
                rarity = ItemRarity.Epic,
                price = 600,
                specialEffect = "+10% Velocidade, +5% Agilidade",
                brandName = "Adidas"
            });
            
            urbanAccessories.Add(new UrbanAccessory
            {
                id = "mizuno_wave_creation",
                name = "Mizuno Wave Creation",
                description = "Tênis brasileiro premium",
                type = AccessoryType.Shoes,
                rarity = ItemRarity.Rare,
                price = 300,
                specialEffect = "+8% Velocidade",
                brandName = "Mizuno"
            });
        }
        
        void SetupPreviewSystem()
        {
            if (characterModels.Length > 0)
            {
                previewCharacter = Instantiate(characterModels[0], characterPreview);
                UpdateCharacterPreview();
            }
        }
        
        // === SISTEMA DE PERSONALIZAÇÃO ===
        
        public void ChangeGender(Gender gender)
        {
            currentPlayerProfile.gender = gender;
            
            // Trocar modelo 3D
            if (previewCharacter != null)
            {
                Destroy(previewCharacter);
            }
            
            int modelIndex = gender == Gender.Male ? 0 : 1;
            previewCharacter = Instantiate(characterModels[modelIndex], characterPreview);
            
            UpdateCharacterPreview();
            OnCustomizationChanged?.Invoke("Gender");
        }
        
        public void ChangeHaircut(string haircutId)
        {
            var haircut = brazilianHaircuts.Find(h => h.id == haircutId);
            if (haircut != null && (haircut.isUnlocked || CanPurchase(haircut.price)))
            {
                currentPlayerProfile.haircutId = haircutId;
                ApplyHaircut(haircut);
                OnCustomizationChanged?.Invoke("Haircut");
            }
        }
        
        public void AddTattoo(string tattooId)
        {
            var tattoo = brazilianTattoos.Find(t => t.id == tattooId);
            if (tattoo != null && CanPurchase(tattoo.price))
            {
                if (!currentPlayerProfile.tattooIds.Contains(tattooId))
                {
                    currentPlayerProfile.tattooIds.Add(tattooId);
                    ApplyTattoo(tattoo);
                    OnCustomizationChanged?.Invoke("Tattoo");
                }
            }
        }
        
        public void EquipAccessory(string accessoryId)
        {
            var accessory = urbanAccessories.Find(a => a.id == accessoryId);
            if (accessory != null && CanPurchase(accessory.price))
            {
                // Remove acessório anterior do mesmo tipo
                currentPlayerProfile.accessories.RemoveAll(a => GetAccessoryType(a) == accessory.type);
                
                currentPlayerProfile.accessories.Add(accessoryId);
                ApplyAccessory(accessory);
                OnCustomizationChanged?.Invoke("Accessory");
            }
        }
        
        public void ChangeClothing(string clothingId, ClothingType type)
        {
            switch (type)
            {
                case ClothingType.Shirt:
                    currentPlayerProfile.shirtId = clothingId;
                    break;
                case ClothingType.Pants:
                    currentPlayerProfile.pantsId = clothingId;
                    break;
            }
            
            ApplyClothing(clothingId, type);
            OnCustomizationChanged?.Invoke("Clothing");
        }
        
        void ApplyHaircut(BrazilianHaircut haircut)
        {
            if (previewCharacter == null) return;
            
            // Encontrar e trocar o mesh do cabelo
            var hairRenderer = previewCharacter.transform.Find("Hair")?.GetComponent<SkinnedMeshRenderer>();
            if (hairRenderer != null)
            {
                // Aplicar novo mesh e material do corte
                // hairRenderer.sharedMesh = haircut.hairMesh;
                // hairRenderer.material = haircut.hairMaterial;
            }
        }
        
        void ApplyTattoo(BrazilianTattoo tattoo)
        {
            if (previewCharacter == null) return;
            
            // Aplicar tatuagem na parte do corpo correspondente
            var bodyPart = GetBodyPartRenderer(tattoo.bodyPart);
            if (bodyPart != null)
            {
                // Adicionar textura da tatuagem
                ApplyTattooTexture(bodyPart, tattoo);
            }
        }
        
        void ApplyAccessory(UrbanAccessory accessory)
        {
            if (previewCharacter == null) return;
            
            // Instanciar modelo do acessório no local correto
            Transform attachPoint = GetAccessoryAttachPoint(accessory.type);
            if (attachPoint != null)
            {
                // GameObject accessoryObj = Instantiate(accessory.model, attachPoint);
                // Configurar posição e rotação específicas
            }
        }
        
        void ApplyClothing(string clothingId, ClothingType type)
        {
            if (previewCharacter == null) return;
            
            // Trocar mesh e textura da roupa
            var clothingRenderer = GetClothingRenderer(type);
            if (clothingRenderer != null)
            {
                // Aplicar nova roupa
                // clothingRenderer.material = GetClothingMaterial(clothingId);
            }
        }
        
        // === SISTEMA DE COMPRAS E DESBLOQUEIOS ===
        
        bool CanPurchase(int price)
        {
            return EconomyManager.Instance?.GetCoins() >= price;
        }
        
        public bool PurchaseCustomization(string itemId, int price, CustomizationType type)
        {
            if (!CanPurchase(price)) return false;
            
            if (EconomyManager.Instance.SpendCoins(price))
            {
                UnlockCustomization(itemId, type);
                return true;
            }
            
            return false;
        }
        
        void UnlockCustomization(string itemId, CustomizationType type)
        {
            switch (type)
            {
                case CustomizationType.Haircut:
                    var haircut = brazilianHaircuts.Find(h => h.id == itemId);
                    if (haircut != null) haircut.isUnlocked = true;
                    break;
                    
                case CustomizationType.Tattoo:
                    var tattoo = brazilianTattoos.Find(t => t.id == itemId);
                    if (tattoo != null) tattoo.isUnlocked = true;
                    break;
                    
                case CustomizationType.Accessory:
                    var accessory = urbanAccessories.Find(a => a.id == itemId);
                    if (accessory != null) accessory.isUnlocked = true;
                    break;
            }
        }
        
        // === SISTEMA DE PRESETS ===
        
        public void ApplyPreset(PlayerPreset preset)
        {
            currentPlayerProfile = preset.playerProfile.Clone();
            UpdateCharacterPreview();
        }
        
        public void SaveCurrentAsPreset(string presetName)
        {
            var preset = new PlayerPreset
            {
                presetName = presetName,
                playerProfile = currentPlayerProfile.Clone()
            };
            
            SavePreset(preset);
        }
        
        // === FINALIZAÇÃO E CRIAÇÃO ===
        
        public void ConfirmPlayerCreation()
        {
            // Validar se tem todas as opções básicas
            if (ValidatePlayerProfile())
            {
                CreatePlayerServerRpc(currentPlayerProfile);
                OnPlayerCreated?.Invoke(currentPlayerProfile);
            }
        }
        
        bool ValidatePlayerProfile()
        {
            return !string.IsNullOrEmpty(currentPlayerProfile.playerName) &&
                   !string.IsNullOrEmpty(currentPlayerProfile.haircutId);
        }
        
        [ServerRpc(RequireOwnership = false)]
        void CreatePlayerServerRpc(PlayerProfile profile, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            
            // Salvar perfil no servidor
            SavePlayerProfile(clientId, profile);
            
            // Confirmar criação para o cliente
            ConfirmPlayerCreationClientRpc(profile, new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { clientId }
                }
            });
        }
        
        [ClientRpc]
        void ConfirmPlayerCreationClientRpc(PlayerProfile profile, ClientRpcParams clientRpcParams = default)
        {
            Debug.Log($"✅ Personagem criado: {profile.playerName}");
            
            // Aplicar perfil ao jogador local
            ApplyProfileToPlayer(profile);
            
            // Ir para o lobby
            TransitionToLobby();
        }
        
        void ApplyProfileToPlayer(PlayerProfile profile)
        {
            var playerController = FindObjectOfType<PlayerController>();
            if (playerController != null)
            {
                playerController.ApplyCustomization(profile);
            }
        }
        
        void TransitionToLobby()
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowLobbyUI();
            }
        }
        
        void UpdateCharacterPreview()
        {
            if (previewCharacter == null) return;
            
            // Aplicar todas as customizações no preview
            ApplyAllCustomizations();
        }
        
        void ApplyAllCustomizations()
        {
            // Aplicar corte de cabelo
            if (!string.IsNullOrEmpty(currentPlayerProfile.haircutId))
            {
                var haircut = brazilianHaircuts.Find(h => h.id == currentPlayerProfile.haircutId);
                if (haircut != null) ApplyHaircut(haircut);
            }
            
            // Aplicar tatuagens
            foreach (string tattooId in currentPlayerProfile.tattooIds)
            {
                var tattoo = brazilianTattoos.Find(t => t.id == tattooId);
                if (tattoo != null) ApplyTattoo(tattoo);
            }
            
            // Aplicar acessórios
            foreach (string accessoryId in currentPlayerProfile.accessories)
            {
                var accessory = urbanAccessories.Find(a => a.id == accessoryId);
                if (accessory != null) ApplyAccessory(accessory);
            }
        }
        
        // === HELPERS ===
        
        SkinnedMeshRenderer GetBodyPartRenderer(BodyPart bodyPart)
        {
            string partName = bodyPart.ToString();
            return previewCharacter.transform.Find(partName)?.GetComponent<SkinnedMeshRenderer>();
        }
        
        Transform GetAccessoryAttachPoint(AccessoryType type)
        {
            switch (type)
            {
                case AccessoryType.Hat: return previewCharacter.transform.Find("Head/HatAttach");
                case AccessoryType.Glasses: return previewCharacter.transform.Find("Head/GlassesAttach");
                case AccessoryType.Necklace: return previewCharacter.transform.Find("Neck/NecklaceAttach");
                case AccessoryType.Shoes: return previewCharacter.transform.Find("Feet");
                default: return null;
            }
        }
        
        SkinnedMeshRenderer GetClothingRenderer(ClothingType type)
        {
            switch (type)
            {
                case ClothingType.Shirt: return previewCharacter.transform.Find("Torso")?.GetComponent<SkinnedMeshRenderer>();
                case ClothingType.Pants: return previewCharacter.transform.Find("Legs")?.GetComponent<SkinnedMeshRenderer>();
                default: return null;
            }
        }
        
        AccessoryType GetAccessoryType(string accessoryId)
        {
            var accessory = urbanAccessories.Find(a => a.id == accessoryId);
            return accessory?.type ?? AccessoryType.Hat;
        }
        
        void ApplyTattooTexture(SkinnedMeshRenderer renderer, BrazilianTattoo tattoo)
        {
            // Implementar sistema de blending de texturas para tatuagens
        }
        
        void SavePlayerProfile(ulong clientId, PlayerProfile profile)
        {
            // Salvar no Firebase/PlayFab
        }
        
        void SavePreset(PlayerPreset preset)
        {
            // Salvar preset localmente
        }
        
        // === GETTERS PÚBLICOS ===
        
        public List<BrazilianHaircut> GetAvailableHaircuts() => brazilianHaircuts;
        public List<BrazilianTattoo> GetAvailableTattoos() => brazilianTattoos;
        public List<UrbanAccessory> GetAvailableAccessories() => urbanAccessories;
        public PlayerProfile GetCurrentProfile() => currentPlayerProfile;
    }
    
    // === CLASSES DE DADOS ===
    
    [System.Serializable]
    public class PlayerProfile
    {
        public string playerName;
        public Gender gender;
        public string haircutId;
        public List<string> tattooIds = new List<string>();
        public List<string> accessories = new List<string>();
        public string shirtId;
        public string pantsId;
        public string faceId;
        public Color skinColor = Color.white;
        
        public PlayerProfile Clone()
        {
            return new PlayerProfile
            {
                playerName = this.playerName,
                gender = this.gender,
                haircutId = this.haircutId,
                tattooIds = new List<string>(this.tattooIds),
                accessories = new List<string>(this.accessories),
                shirtId = this.shirtId,
                pantsId = this.pantsId,
                faceId = this.faceId,
                skinColor = this.skinColor
            };
        }
    }
    
    [System.Serializable]
    public class BrazilianHaircut
    {
        public string id;
        public string name;
        public string description;
        public ItemRarity rarity;
        public int price;
        public string culturalOrigin;
        public bool isUnlocked;
        public Mesh hairMesh;
        public Material hairMaterial;
    }
    
    [System.Serializable]
    public class BrazilianTattoo
    {
        public string id;
        public string name;
        public string description;
        public BodyPart bodyPart;
        public ItemRarity rarity;
        public int price;
        public string culturalMeaning;
        public string specialEffect;
        public bool isUnlocked;
        public Texture2D tattooTexture;
    }
    
    [System.Serializable]
    public class UrbanAccessory
    {
        public string id;
        public string name;
        public string description;
        public AccessoryType type;
        public ItemRarity rarity;
        public int price;
        public string specialEffect;
        public string brandName;
        public bool isUnlocked;
        public GameObject model;
    }
    
    [System.Serializable]
    public class PlayerPreset
    {
        public string presetName;
        public PlayerProfile playerProfile;
    }
    
    public enum Gender
    {
        Male,
        Female
    }
    
    public enum BodyPart
    {
        Head,
        Neck,
        Chest,
        Back,
        Arm,
        Leg,
        Hand
    }
    
    public enum AccessoryType
    {
        Hat,
        Glasses,
        Necklace,
        Shoes,
        Watch,
        Ring
    }
    
    public enum ClothingType
    {
        Shirt,
        Pants,
        Shoes,
        Hat
    }
    
    public enum CustomizationType
    {
        Haircut,
        Tattoo,
        Accessory,
        Clothing
    }
    
    public enum ItemRarity
    {
        Common,
        Rare,
        Epic,
        Legendary
    }
}
