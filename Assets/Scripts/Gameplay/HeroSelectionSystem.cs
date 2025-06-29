
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;
using System;

namespace ArenaBrasil.Heroes
{
    public class HeroSelectionSystem : NetworkBehaviour
    {
        public static HeroSelectionSystem Instance { get; private set; }
        
        [Header("Hero Selection Configuration")]
        public float selectionTimeLimit = 30f;
        public bool allowDuplicateHeroes = false;
        public List<HeroData> availableHeroes = new List<HeroData>();
        
        [Header("UI References")]
        public GameObject heroSelectionUI;
        public Transform heroDisplayGrid;
        public GameObject heroPreviewPanel;
        
        // Network Variables
        private NetworkVariable<float> selectionTimer = new NetworkVariable<float>();
        private NetworkVariable<bool> selectionPhaseActive = new NetworkVariable<bool>();
        
        // Selections tracking
        private Dictionary<ulong, HeroType> playerSelections = new Dictionary<ulong, HeroType>();
        private Dictionary<ulong, bool> playerReady = new Dictionary<ulong, bool>();
        
        // Events
        public event Action<ulong, HeroType> OnPlayerHeroSelected;
        public event Action<ulong, bool> OnPlayerReadyChanged;
        public event Action OnSelectionPhaseComplete;
        public event Action<float> OnSelectionTimerUpdate;
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        public override void OnNetworkSpawn()
        {
            InitializeHeroData();
            
            if (IsServer)
            {
                selectionTimer.Value = selectionTimeLimit;
                selectionPhaseActive.Value = false;
            }
            
            // Subscribe to network variable changes
            selectionTimer.OnValueChanged += OnSelectionTimerChanged;
            selectionPhaseActive.OnValueChanged += OnSelectionPhaseChanged;
            
            Debug.Log("🦸 Sistema de Seleção de Heróis inicializado");
        }
        
        void InitializeHeroData()
        {
            // Heróis do Folclore Brasileiro
            availableHeroes.Add(new HeroData
            {
                heroType = HeroType.Saci,
                displayName = "Saci-Pererê",
                description = "O travesso guardião das florestas, mestre da velocidade e teleporte",
                rarity = HeroRarity.Common,
                region = "Sudeste",
                primaryRole = HeroRole.Assassin,
                secondaryRole = HeroRole.Support,
                abilities = new List<HeroAbility>
                {
                    new HeroAbility
                    {
                        name = "Redemoinho Mágico",
                        description = "Teleporta curta distância, causando dano em área",
                        type = AbilityType.Movement,
                        cooldown = 8f,
                        damage = 120,
                        range = 15f,
                        effect = "teleport_damage"
                    },
                    new HeroAbility
                    {
                        name = "Travessuras",
                        description = "Torna-se invisível por alguns segundos",
                        type = AbilityType.Utility,
                        cooldown = 15f,
                        duration = 4f,
                        effect = "invisibility"
                    },
                    new HeroAbility
                    {
                        name = "Furacão Destruidor",
                        description = "Ultimate: Cria tornado gigante que persegue inimigos",
                        type = AbilityType.Ultimate,
                        cooldown = 60f,
                        damage = 300,
                        range = 25f,
                        duration = 8f,
                        effect = "tornado_ultimate"
                    }
                },
                baseStats = new HeroStats
                {
                    health = 180,
                    armor = 25,
                    speed = 420, // Muito rápido
                    damage = 85,
                    criticalChance = 0.15f,
                    abilityPower = 110
                },
                unlockRequirement = new UnlockRequirement
                {
                    type = UnlockType.Default,
                    isUnlocked = true
                }
            });
            
            availableHeroes.Add(new HeroData
            {
                heroType = HeroType.Curupira,
                displayName = "Curupira",
                description = "O protetor ancestral da Amazônia, guerreiro tanque implacável",
                rarity = HeroRarity.Epic,
                region = "Norte",
                primaryRole = HeroRole.Tank,
                secondaryRole = HeroRole.Support,
                abilities = new List<HeroAbility>
                {
                    new HeroAbility
                    {
                        name = "Pegadas Invertidas",
                        description = "Confunde inimigos próximos, invertendo seus controles",
                        type = AbilityType.Debuff,
                        cooldown = 12f,
                        range = 10f,
                        duration = 3f,
                        effect = "control_inversion"
                    },
                    new HeroAbility
                    {
                        name = "Escudo da Floresta",
                        description = "Cria barreira natural que absorve dano",
                        type = AbilityType.Defense,
                        cooldown = 18f,
                        shieldAmount = 250,
                        duration = 6f,
                        effect = "nature_shield"
                    },
                    new HeroAbility
                    {
                        name = "Fúria da Natureza",
                        description = "Ultimate: Transforma-se em gigante, ganha vida e força",
                        type = AbilityType.Ultimate,
                        cooldown = 80f,
                        healthBonus = 300,
                        damageBonus = 100,
                        duration = 12f,
                        effect = "giant_transformation"
                    }
                },
                baseStats = new HeroStats
                {
                    health = 280,
                    armor = 65,
                    speed = 320,
                    damage = 70,
                    criticalChance = 0.05f,
                    abilityPower = 90
                },
                unlockRequirement = new UnlockRequirement
                {
                    type = UnlockType.Level,
                    levelRequired = 5
                }
            });
            
            availableHeroes.Add(new HeroData
            {
                heroType = HeroType.Iara,
                displayName = "Iara",
                description = "A sedutora sereia dos rios, mestre dos elementos aquáticos",
                rarity = HeroRarity.Legendary,
                region = "Amazônia",
                primaryRole = HeroRole.Mage,
                secondaryRole = HeroRole.Support,
                abilities = new List<HeroAbility>
                {
                    new HeroAbility
                    {
                        name = "Canto Hipnótico",
                        description = "Charma inimigos fazendo-os atacar aliados",
                        type = AbilityType.Debuff,
                        cooldown = 15f,
                        range = 20f,
                        duration = 4f,
                        effect = "mind_control"
                    },
                    new HeroAbility
                    {
                        name = "Ondas Curadoras",
                        description = "Cura aliados em área e causa dano a inimigos",
                        type = AbilityType.Healing,
                        cooldown = 10f,
                        healing = 150,
                        damage = 100,
                        range = 15f,
                        effect = "water_waves"
                    },
                    new HeroAbility
                    {
                        name = "Tsunami Devastador",
                        description = "Ultimate: Invoca tsunami massivo em linha reta",
                        type = AbilityType.Ultimate,
                        cooldown = 90f,
                        damage = 450,
                        range = 40f,
                        knockback = 20f,
                        effect = "tsunami_ultimate"
                    }
                },
                baseStats = new HeroStats
                {
                    health = 200,
                    armor = 35,
                    speed = 360,
                    damage = 60,
                    criticalChance = 0.10f,
                    abilityPower = 140
                },
                unlockRequirement = new UnlockRequirement
                {
                    type = UnlockType.Achievement,
                    achievementId = "amazon_explorer"
                }
            });
            
            availableHeroes.Add(new HeroData
            {
                heroType = HeroType.Boitata,
                displayName = "Boitatá",
                description = "A serpente de fogo ancestral, destruidor implacável",
                rarity = HeroRarity.Legendary,
                region = "Pantanal",
                primaryRole = HeroRole.Damage,
                secondaryRole = HeroRole.Assassin,
                abilities = new List<HeroAbility>
                {
                    new HeroAbility
                    {
                        name = "Jato de Fogo",
                        description = "Dispara chamas em cone, queimando inimigos",
                        type = AbilityType.Damage,
                        cooldown = 6f,
                        damage = 140,
                        range = 12f,
                        burnDuration = 3f,
                        effect = "fire_cone"
                    },
                    new HeroAbility
                    {
                        name = "Escamas Ígneas",
                        description = "Reflete dano recebido como fogo por alguns segundos",
                        type = AbilityType.Defense,
                        cooldown = 20f,
                        reflectPercentage = 0.5f,
                        duration = 5f,
                        effect = "fire_reflect"
                    },
                    new HeroAbility
                    {
                        name = "Serpente Infernal",
                        description = "Ultimate: Transforma-se em serpente gigante de fogo",
                        type = AbilityType.Ultimate,
                        cooldown = 100f,
                        damage = 500,
                        speed = 600,
                        duration = 10f,
                        effect = "fire_serpent_form"
                    }
                },
                baseStats = new HeroStats
                {
                    health = 220,
                    armor = 40,
                    speed = 380,
                    damage = 110,
                    criticalChance = 0.20f,
                    abilityPower = 130
                },
                unlockRequirement = new UnlockRequirement
                {
                    type = UnlockType.Purchase,
                    cost = 2000,
                    currency = "gems"
                }
            });
            
            // Heróis Urbanos Brasileiros
            availableHeroes.Add(new HeroData
            {
                heroType = HeroType.Capoeirista,
                displayName = "Mestre Capoeira",
                description = "Lutador urbano com movimentos fluidos e acrobáticos",
                rarity = HeroRarity.Rare,
                region = "Bahia",
                primaryRole = HeroRole.Fighter,
                secondaryRole = HeroRole.Assassin,
                abilities = new List<HeroAbility>
                {
                    new HeroAbility
                    {
                        name = "Ginga Mortal",
                        description = "Esquiva e contra-ataca com movimento fluido",
                        type = AbilityType.Movement,
                        cooldown = 8f,
                        damage = 100,
                        dodgeChance = 0.8f,
                        effect = "capoeira_dodge"
                    },
                    new HeroAbility
                    {
                        name = "Roda de Capoeira",
                        description = "Ataque giratório que atinge múltiplos inimigos",
                        type = AbilityType.Damage,
                        cooldown = 12f,
                        damage = 80,
                        range = 8f,
                        effect = "spinning_attack"
                    },
                    new HeroAbility
                    {
                        name = "Berimbau Ancestral",
                        description = "Ultimate: Invoca energia dos ancestrais, aumentando todos atributos",
                        type = AbilityType.Ultimate,
                        cooldown = 70f,
                        duration = 15f,
                        allStatsBonus = 50,
                        effect = "ancestor_blessing"
                    }
                },
                baseStats = new HeroStats
                {
                    health = 210,
                    armor = 30,
                    speed = 400,
                    damage = 90,
                    criticalChance = 0.18f,
                    abilityPower = 75
                },
                unlockRequirement = new UnlockRequirement
                {
                    type = UnlockType.Level,
                    levelRequired = 10
                }
            });
            
            Debug.Log($"🦸 {availableHeroes.Count} heróis brasileiros carregados");
        }
        
        [ServerRpc(RequireOwnership = false)]
        public void StartSelectionPhaseServerRpc()
        {
            if (!IsServer) return;
            
            selectionPhaseActive.Value = true;
            selectionTimer.Value = selectionTimeLimit;
            
            playerSelections.Clear();
            playerReady.Clear();
            
            Debug.Log("🎯 Fase de seleção de heróis iniciada");
            
            // Iniciar countdown
            StartCoroutine(SelectionCountdown());
        }
        
        System.Collections.IEnumerator SelectionCountdown()
        {
            while (selectionTimer.Value > 0 && selectionPhaseActive.Value)
            {
                yield return new WaitForSeconds(1f);
                selectionTimer.Value = Mathf.Max(0, selectionTimer.Value - 1f);
            }
            
            // Tempo esgotado ou todos prontos
            CompleteSelectionPhase();
        }
        
        [ServerRpc(RequireOwnership = false)]
        public void SelectHeroServerRpc(HeroType heroType, ServerRpcParams rpcParams = default)
        {
            if (!IsServer || !selectionPhaseActive.Value) return;
            
            ulong playerId = rpcParams.Receive.SenderClientId;
            
            // Verificar se herói está disponível
            if (!IsHeroAvailable(heroType, playerId))
            {
                SendHeroUnavailableClientRpc(heroType, new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new ulong[] { playerId }
                    }
                });
                return;
            }
            
            // Registrar seleção
            playerSelections[playerId] = heroType;
            playerReady[playerId] = false; // Reset ready status
            
            Debug.Log($"🦸 Jogador {playerId} selecionou {heroType}");
            
            // Notificar todos os clientes
            OnPlayerHeroSelectedClientRpc(playerId, heroType);
        }
        
        [ServerRpc(RequireOwnership = false)]
        public void ConfirmHeroSelectionServerRpc(ServerRpcParams rpcParams = default)
        {
            if (!IsServer || !selectionPhaseActive.Value) return;
            
            ulong playerId = rpcParams.Receive.SenderClientId;
            
            if (!playerSelections.ContainsKey(playerId))
            {
                Debug.LogWarning($"Jogador {playerId} tentou confirmar sem selecionar herói");
                return;
            }
            
            playerReady[playerId] = true;
            
            Debug.Log($"✅ Jogador {playerId} confirmou seleção: {playerSelections[playerId]}");
            
            // Notificar mudança de status
            OnPlayerReadyChangedClientRpc(playerId, true);
            
            // Verificar se todos estão prontos
            CheckAllPlayersReady();
        }
        
        void CheckAllPlayersReady()
        {
            if (!IsServer) return;
            
            var connectedClients = NetworkManager.Singleton.ConnectedClientsIds;
            bool allReady = connectedClients.All(id => playerReady.ContainsKey(id) && playerReady[id]);
            
            if (allReady && connectedClients.Count > 0)
            {
                Debug.Log("🎉 Todos os jogadores prontos! Finalizando seleção...");
                CompleteSelectionPhase();
            }
        }
        
        void CompleteSelectionPhase()
        {
            if (!IsServer) return;
            
            selectionPhaseActive.Value = false;
            
            // Atribuir heróis aleatórios para jogadores que não selecionaram
            var connectedClients = NetworkManager.Singleton.ConnectedClientsIds;
            foreach (ulong playerId in connectedClients)
            {
                if (!playerSelections.ContainsKey(playerId))
                {
                    var availableHero = GetRandomAvailableHero(playerId);
                    playerSelections[playerId] = availableHero.heroType;
                    
                    Debug.Log($"🎲 Herói aleatório atribuído ao jogador {playerId}: {availableHero.heroType}");
                }
            }
            
            // Aplicar seleções aos jogadores
            ApplyHeroSelectionsToPlayers();
            
            // Notificar conclusão
            OnSelectionPhaseCompleteClientRpc();
            OnSelectionPhaseComplete?.Invoke();
        }
        
        void ApplyHeroSelectionsToPlayers()
        {
            foreach (var selection in playerSelections)
            {
                ulong playerId = selection.Key;
                HeroType heroType = selection.Value;
                
                // Encontrar PlayerController do jogador
                if (NetworkManager.Singleton.ConnectedClients.TryGetValue(playerId, out var clientData))
                {
                    var playerObject = clientData.PlayerObject;
                    if (playerObject != null)
                    {
                        var playerController = playerObject.GetComponent<PlayerController>();
                        var heroLenda = playerObject.GetComponent<HeroLenda>();
                        
                        if (playerController != null && heroLenda != null)
                        {
                            // Aplicar herói selecionado
                            heroLenda.SetHeroType(heroType);
                            
                            // Configurar habilidades
                            SetupHeroAbilities(heroLenda, heroType);
                            
                            Debug.Log($"🦸 Herói {heroType} aplicado ao jogador {playerId}");
                        }
                    }
                }
            }
        }
        
        void SetupHeroAbilities(HeroLenda heroLenda, HeroType heroType)
        {
            var heroData = availableHeroes.FirstOrDefault(h => h.heroType == heroType);
            if (heroData != null)
            {
                // Aplicar estatísticas base
                heroLenda.ApplyBaseStats(heroData.baseStats);
                
                // Configurar habilidades
                heroLenda.SetAbilities(heroData.abilities);
                
                // Aplicar modificadores visuais
                heroLenda.ApplyHeroAppearance(heroData);
            }
        }
        
        bool IsHeroAvailable(HeroType heroType, ulong playerId)
        {
            // Verificar se herói existe
            var heroData = availableHeroes.FirstOrDefault(h => h.heroType == heroType);
            if (heroData == null) return false;
            
            // Verificar se está desbloqueado para o jogador
            if (!IsHeroUnlockedForPlayer(heroData, playerId)) return false;
            
            // Verificar duplicatas se não permitidas
            if (!allowDuplicateHeroes && playerSelections.ContainsValue(heroType))
                return false;
            
            return true;
        }
        
        bool IsHeroUnlockedForPlayer(HeroData heroData, ulong playerId)
        {
            // Por enquanto, assumir que todos os heróis estão desbloqueados
            // Em implementação completa, verificar com sistema de progressão
            return heroData.unlockRequirement.isUnlocked;
        }
        
        HeroData GetRandomAvailableHero(ulong playerId)
        {
            var availableForPlayer = availableHeroes.Where(h => 
                IsHeroUnlockedForPlayer(h, playerId) &&
                (!allowDuplicateHeroes ? !playerSelections.ContainsValue(h.heroType) : true)
            ).ToList();
            
            if (availableForPlayer.Count == 0)
                return availableHeroes.First(); // Fallback para primeiro herói
            
            return availableForPlayer[UnityEngine.Random.Range(0, availableForPlayer.Count)];
        }
        
        // Client RPCs
        [ClientRpc]
        void OnPlayerHeroSelectedClientRpc(ulong playerId, HeroType heroType)
        {
            OnPlayerHeroSelected?.Invoke(playerId, heroType);
            
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateHeroSelection(playerId, heroType);
            }
        }
        
        [ClientRpc]
        void OnPlayerReadyChangedClientRpc(ulong playerId, bool isReady)
        {
            OnPlayerReadyChanged?.Invoke(playerId, isReady);
            
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdatePlayerReadyStatus(playerId, isReady);
            }
        }
        
        [ClientRpc]
        void OnSelectionPhaseCompleteClientRpc()
        {
            Debug.Log("🎯 Fase de seleção concluída!");
            
            if (heroSelectionUI != null)
            {
                heroSelectionUI.SetActive(false);
            }
            
            OnSelectionPhaseComplete?.Invoke();
        }
        
        [ClientRpc]
        void SendHeroUnavailableClientRpc(HeroType heroType, ClientRpcParams clientRpcParams = default)
        {
            Debug.LogWarning($"❌ Herói {heroType} não disponível para seleção");
            
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowNotification($"Herói {heroType} não disponível!", NotificationType.Warning);
            }
        }
        
        // Network variable change handlers
        void OnSelectionTimerChanged(float oldValue, float newValue)
        {
            OnSelectionTimerUpdate?.Invoke(newValue);
            
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateSelectionTimer(newValue);
            }
        }
        
        void OnSelectionPhaseChanged(bool oldValue, bool newValue)
        {
            if (heroSelectionUI != null)
            {
                heroSelectionUI.SetActive(newValue);
            }
            
            Debug.Log($"🎯 Fase de seleção: {(newValue ? "ATIVA" : "INATIVA")}");
        }
        
        // Public getters
        public List<HeroData> GetAvailableHeroes() => availableHeroes;
        public Dictionary<ulong, HeroType> GetPlayerSelections() => new Dictionary<ulong, HeroType>(playerSelections);
        public float GetSelectionTimeRemaining() => selectionTimer.Value;
        public bool IsSelectionPhaseActive() => selectionPhaseActive.Value;
        
        public HeroData GetHeroData(HeroType heroType)
        {
            return availableHeroes.FirstOrDefault(h => h.heroType == heroType);
        }
    }
    
    // === CLASSES DE DADOS ===
    [Serializable]
    public class HeroData
    {
        public HeroType heroType;
        public string displayName;
        public string description;
        public HeroRarity rarity;
        public string region;
        public HeroRole primaryRole;
        public HeroRole secondaryRole;
        public List<HeroAbility> abilities;
        public HeroStats baseStats;
        public UnlockRequirement unlockRequirement;
    }
    
    [Serializable]
    public class HeroAbility
    {
        public string name;
        public string description;
        public AbilityType type;
        public float cooldown;
        public float damage;
        public float healing;
        public float range;
        public float duration;
        public float shieldAmount;
        public float speed;
        public float knockback;
        public float burnDuration;
        public float dodgeChance;
        public float reflectPercentage;
        public int healthBonus;
        public int damageBonus;
        public int allStatsBonus;
        public string effect;
    }
    
    [Serializable]
    public class HeroStats
    {
        public int health;
        public int armor;
        public int speed;
        public int damage;
        public float criticalChance;
        public int abilityPower;
    }
    
    [Serializable]
    public class UnlockRequirement
    {
        public UnlockType type;
        public bool isUnlocked;
        public int levelRequired;
        public string achievementId;
        public int cost;
        public string currency;
    }
    
    public enum HeroType
    {
        Saci,
        Curupira,
        Iara,
        Boitata,
        Capoeirista,
        Cangaceiro,
        Pescador,
        Vaqueiro
    }
    
    public enum HeroRarity
    {
        Common,
        Rare,
        Epic,
        Legendary,
        Mythic
    }
    
    public enum HeroRole
    {
        Tank,
        Damage,
        Assassin,
        Mage,
        Support,
        Fighter
    }
    
    public enum AbilityType
    {
        Damage,
        Healing,
        Movement,
        Defense,
        Utility,
        Debuff,
        Ultimate
    }
    
    public enum UnlockType
    {
        Default,
        Level,
        Achievement,
        Purchase,
        Event
    }
}
