
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System;
using ArenaBrasil.Core;
using ArenaBrasil.Analytics;

namespace ArenaBrasil.Cultural
{
    public class BrazilianEasterEggs : NetworkBehaviour
    {
        public static BrazilianEasterEggs Instance { get; private set; }
        
        [Header("Easter Eggs Configuration")]
        public bool enableEasterEggs = true;
        public float easterEggSpawnChance = 0.15f;
        public int maxEasterEggsPerMatch = 5;
        
        // Easter Egg Collections
        private List<CulturalEasterEgg> availableEasterEggs = new List<CulturalEasterEgg>();
        private List<CulturalEasterEgg> activeEasterEggs = new List<CulturalEasterEgg>();
        private Dictionary<ulong, List<string>> playerDiscoveredEggs = new Dictionary<ulong, List<string>>();
        
        // Events
        public event Action<ulong, CulturalEasterEgg> OnEasterEggDiscovered;
        public event Action<CulturalEasterEgg> OnEasterEggActivated;
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeEasterEggs();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void InitializeEasterEggs()
        {
            Debug.Log("🥚 Arena Brasil - Inicializando Easter Eggs culturais brasileiros");
            
            CreateFolkloreEasterEggs();
            CreateHistoricalEasterEggs();
            CreatePopCultureEasterEggs();
            CreateRegionalEasterEggs();
            CreateFoodEasterEggs();
            CreateMusicEasterEggs();
            CreateSportsEasterEggs();
        }
        
        void CreateFolkloreEasterEggs()
        {
            // Saci-Pererê
            availableEasterEggs.Add(new CulturalEasterEgg
            {
                eggId = "saci_redemoinho",
                name = "Redemoinho do Saci",
                description = "Um pequeno redemoinho aparece misteriosamente",
                category = EasterEggCategory.Folklore,
                triggerType = TriggerType.ProximityBased,
                rarityLevel = EasterEggRarity.Common,
                rewardXP = 50,
                rewardCoins = 25,
                audioClip = "saci_laugh.wav",
                visualEffect = "WindVortexEffect",
                activationMessage = "O Saci-Pererê passou por aqui! 🌪️",
                culturalInfo = "O Saci é uma figura do folclore brasileiro, conhecido por seus redemoinhos e travessuras."
            });
            
            // Curupira
            availableEasterEggs.Add(new CulturalEasterEgg
            {
                eggId = "curupira_pegadas",
                name = "Pegadas Invertidas",
                description = "Pegadas misteriosas que apontam para trás",
                category = EasterEggCategory.Folklore,
                triggerType = TriggerType.InteractionBased,
                rarityLevel = EasterEggRarity.Uncommon,
                rewardXP = 75,
                rewardCoins = 50,
                audioClip = "forest_sounds.wav",
                visualEffect = "InvertedFootprints",
                activationMessage = "As pegadas do Curupira! Cuidado para não se perder! 👣",
                culturalInfo = "O Curupira é o protetor das florestas, conhecido por suas pegadas invertidas que confundem os caçadores."
            });
            
            // Iara
            availableEasterEggs.Add(new CulturalEasterEgg
            {
                eggId = "iara_canto",
                name = "Canto da Iara",
                description = "Um canto melodioso ecoa próximo à água",
                category = EasterEggCategory.Folklore,
                triggerType = TriggerType.ProximityBased,
                rarityLevel = EasterEggRarity.Rare,
                rewardXP = 100,
                rewardCoins = 75,
                audioClip = "mermaid_song.wav",
                visualEffect = "WaterRipples",
                activationMessage = "O canto hipnotizante da Iara... 🧜‍♀️",
                culturalInfo = "A Iara é a sereia dos rios brasileiros, que atrai os homens com seu canto."
            });
            
            // Boto-cor-de-rosa
            availableEasterEggs.Add(new CulturalEasterEgg
            {
                eggId = "boto_rosa",
                name = "Boto Encantado",
                description = "Um boto cor-de-rosa nada graciosamente",
                category = EasterEggCategory.Folklore,
                triggerType = TriggerType.TimeBased,
                rarityLevel = EasterEggRarity.Epic,
                rewardXP = 200,
                rewardCoins = 150,
                audioClip = "dolphin_sounds.wav",
                visualEffect = "PinkDolphin",
                activationMessage = "O Boto-cor-de-rosa apareceu! Que sorte! 🐬",
                culturalInfo = "Lenda amazônica do boto que se transforma em homem sedutor nas festas."
            });
        }
        
        void CreateHistoricalEasterEggs()
        {
            // Descobrimento do Brasil
            availableEasterEggs.Add(new CulturalEasterEgg
            {
                eggId = "caravela_cabral",
                name = "Caravelas de Cabral",
                description = "Silhuetas de caravelas no horizonte",
                category = EasterEggCategory.Historical,
                triggerType = TriggerType.ViewBased,
                rarityLevel = EasterEggRarity.Uncommon,
                rewardXP = 100,
                rewardCoins = 50,
                audioClip = "ocean_waves.wav",
                visualEffect = "CaravelSilhouettes",
                activationMessage = "As caravelas de Pedro Álvares Cabral! ⛵",
                culturalInfo = "Em 1500, Cabral chegou ao Brasil com sua frota portuguesa."
            });
            
            // Inconfidência Mineira
            availableEasterEggs.Add(new CulturalEasterEgg
            {
                eggId = "bandeira_inconfidencia",
                name = "Bandeira da Inconfidência",
                description = "Uma bandeira com a frase 'Libertas Quae Sera Tamen'",
                category = EasterEggCategory.Historical,
                triggerType = TriggerType.InteractionBased,
                rarityLevel = EasterEggRarity.Rare,
                rewardXP = 150,
                rewardCoins = 100,
                audioClip = "liberation_hymn.wav",
                visualEffect = "InconfidenciaFlag",
                activationMessage = "Liberdade ainda que tardia! ⚔️",
                culturalInfo = "Movimento de independência em Minas Gerais liderado por Tiradentes."
            });
        }
        
        void CreatePopCultureEasterEggs()
        {
            // Turma da Mônica
            availableEasterEggs.Add(new CulturalEasterEgg
            {
                eggId = "coelho_sansao",
                name = "Coelho Sansão",
                description = "Um coelho azul aparece rapidamente",
                category = EasterEggCategory.PopCulture,
                triggerType = TriggerType.RandomEvent,
                rarityLevel = EasterEggRarity.Common,
                rewardXP = 25,
                rewardCoins = 15,
                audioClip = "cartoon_sound.wav",
                visualEffect = "BlueRabbit",
                activationMessage = "O Sansão da Mônica passou correndo! 🐰",
                culturalInfo = "Personagem icônico dos quadrinhos brasileiros de Mauricio de Sousa."
            });
            
            // Chaves
            availableEasterEggs.Add(new CulturalEasterEgg
            {
                eggId = "barril_chaves",
                name = "Barril do Chaves",
                description = "Um barril misterioso onde alguém pode estar escondido",
                category = EasterEggCategory.PopCulture,
                triggerType = TriggerType.InteractionBased,
                rarityLevel = EasterEggRarity.Uncommon,
                rewardXP = 75,
                rewardCoins = 40,
                audioClip = "chaves_sound.wav",
                visualEffect = "ChavesBarrel",
                activationMessage = "Foi sem querer querendo! 🛢️",
                culturalInfo = "Referência ao seriado mexicano muito popular no Brasil."
            });
        }
        
        void CreateRegionalEasterEggs()
        {
            // Açaí do Pará
            availableEasterEggs.Add(new CulturalEasterEgg
            {
                eggId = "acai_para",
                name = "Açaí Paraense",
                description = "Uma tigela de açaí roxinho aparece",
                category = EasterEggCategory.Regional,
                triggerType = TriggerType.ProximityBased,
                rarityLevel = EasterEggRarity.Common,
                rewardXP = 30,
                rewardCoins = 20,
                audioClip = "amazon_sounds.wav",
                visualEffect = "AcaiBowl",
                activationMessage = "Açaí paraense da boa! 🍇",
                culturalInfo = "O açaí é um fruto tradicional da Amazônia, especialmente consumido no Pará."
            });
            
            // Arrastão de Copacabana
            availableEasterEggs.Add(new CulturalEasterEgg
            {
                eggId = "copacabana_beach",
                name = "Praia de Copacabana",
                description = "Ondas e som de praia carioca",
                category = EasterEggCategory.Regional,
                triggerType = TriggerType.ViewBased,
                rarityLevel = EasterEggRarity.Uncommon,
                rewardXP = 80,
                rewardCoins = 60,
                audioClip = "copacabana_waves.wav",
                visualEffect = "BeachWaves",
                activationMessage = "Copacabana, princesinha do mar! 🏖️",
                culturalInfo = "A famosa praia do Rio de Janeiro, cartão postal do Brasil."
            });
        }
        
        void CreateFoodEasterEggs()
        {
            // Brigadeiro
            availableEasterEggs.Add(new CulturalEasterEgg
            {
                eggId = "brigadeiro_doce",
                name = "Brigadeiro Gourmet",
                description = "Um docinho brasileiro irresistível",
                category = EasterEggCategory.Food,
                triggerType = TriggerType.InteractionBased,
                rarityLevel = EasterEggRarity.Common,
                rewardXP = 20,
                rewardCoins = 10,
                audioClip = "eating_sound.wav",
                visualEffect = "BrigadeiroSweet",
                activationMessage = "Um brigadeirinho! 🍫",
                culturalInfo = "Doce brasileiro feito com chocolate, leite condensado e manteiga."
            });
            
            // Feijoada
            availableEasterEggs.Add(new CulturalEasterEgg
            {
                eggId = "feijoada_completa",
                name = "Feijoada Completa",
                description = "O prato mais brasileiro de todos",
                category = EasterEggCategory.Food,
                triggerType = TriggerType.TimeBased,
                rarityLevel = EasterEggRarity.Rare,
                rewardXP = 150,
                rewardCoins = 100,
                audioClip = "cooking_sounds.wav",
                visualEffect = "FeijoadaPlate",
                activationMessage = "Feijoada completa! Hora do almoço! 🍲",
                culturalInfo = "Prato tradicional brasileiro com feijão preto e carnes variadas."
            });
        }
        
        void CreateMusicEasterEggs()
        {
            // Bossa Nova
            availableEasterEggs.Add(new CulturalEasterEgg
            {
                eggId = "bossa_nova",
                name = "Melodia Bossa Nova",
                description = "Uma suave melodia de bossa nova toca",
                category = EasterEggCategory.Music,
                triggerType = TriggerType.ProximityBased,
                rarityLevel = EasterEggRarity.Uncommon,
                rewardXP = 60,
                rewardCoins = 40,
                audioClip = "bossa_nova.wav",
                visualEffect = "MusicalNotes",
                activationMessage = "Garota de Ipanema... 🎵",
                culturalInfo = "Gênero musical brasileiro que surgiu no final dos anos 1950."
            });
            
            // Frevo
            availableEasterEggs.Add(new CulturalEasterEgg
            {
                eggId = "frevo_pernambuco",
                name = "Frevo Pernambucano",
                description = "Som animado do frevo pernambucano",
                category = EasterEggCategory.Music,
                triggerType = TriggerType.RandomEvent,
                rarityLevel = EasterEggRarity.Rare,
                rewardXP = 120,
                rewardCoins = 80,
                audioClip = "frevo_music.wav",
                visualEffect = "FrevoUmbrella",
                activationMessage = "É frevo, é carnaval! 🎭",
                culturalInfo = "Gênero musical e dança típica de Pernambuco."
            });
        }
        
        void CreateSportsEasterEggs()
        {
            // Pelé
            availableEasterEggs.Add(new CulturalEasterEgg
            {
                eggId = "pele_rei",
                name = "O Rei do Futebol",
                description = "Uma bola de futebol dourada aparece",
                category = EasterEggCategory.Sports,
                triggerType = TriggerType.InteractionBased,
                rarityLevel = EasterEggRarity.Legendary,
                rewardXP = 500,
                rewardCoins = 300,
                audioClip = "stadium_cheer.wav",
                visualEffect = "GoldenSoccerBall",
                activationMessage = "O Rei Pelé abençoou este lugar! ⚽",
                culturalInfo = "Edson Arantes do Nascimento, o maior jogador de futebol de todos os tempos."
            });
            
            // Ayrton Senna
            availableEasterEggs.Add(new CulturalEasterEgg
            {
                eggId = "senna_f1",
                name = "Capacete do Senna",
                description = "Um capacete amarelo e verde brilha",
                category = EasterEggCategory.Sports,
                triggerType = TriggerType.ViewBased,
                rarityLevel = EasterEggRarity.Epic,
                rewardXP = 300,
                rewardCoins = 200,
                audioClip = "f1_engine.wav",
                visualEffect = "SennaHelmet",
                activationMessage = "Senna para sempre! 🏎️",
                culturalInfo = "Ayrton Senna, o maior piloto de Fórmula 1 brasileiro."
            });
        }
        
        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                StartEasterEggSystem();
            }
        }
        
        void StartEasterEggSystem()
        {
            if (enableEasterEggs)
            {
                InvokeRepeating(nameof(SpawnRandomEasterEgg), 30f, 60f);
            }
        }
        
        void SpawnRandomEasterEgg()
        {
            if (activeEasterEggs.Count >= maxEasterEggsPerMatch) return;
            
            if (UnityEngine.Random.value <= easterEggSpawnChance)
            {
                var randomEgg = availableEasterEggs[UnityEngine.Random.Range(0, availableEasterEggs.Count)];
                SpawnEasterEgg(randomEgg);
            }
        }
        
        void SpawnEasterEgg(CulturalEasterEgg easterEgg)
        {
            Vector3 spawnPosition = GetRandomSpawnPosition();
            
            var eggInstance = new CulturalEasterEgg
            {
                eggId = easterEgg.eggId,
                name = easterEgg.name,
                description = easterEgg.description,
                category = easterEgg.category,
                triggerType = easterEgg.triggerType,
                rarityLevel = easterEgg.rarityLevel,
                position = spawnPosition,
                isActive = true,
                rewardXP = easterEgg.rewardXP,
                rewardCoins = easterEgg.rewardCoins,
                audioClip = easterEgg.audioClip,
                visualEffect = easterEgg.visualEffect,
                activationMessage = easterEgg.activationMessage,
                culturalInfo = easterEgg.culturalInfo
            };
            
            activeEasterEggs.Add(eggInstance);
            OnEasterEggActivated?.Invoke(eggInstance);
            
            SpawnEasterEggClientRpc(eggInstance);
        }
        
        [ClientRpc]
        void SpawnEasterEggClientRpc(CulturalEasterEgg easterEgg)
        {
            CreateEasterEggVisual(easterEgg);
        }
        
        void CreateEasterEggVisual(CulturalEasterEgg easterEgg)
        {
            GameObject eggObject = new GameObject($"EasterEgg_{easterEgg.eggId}");
            eggObject.transform.position = easterEgg.position;
            
            // Add appropriate components based on trigger type
            switch (easterEgg.triggerType)
            {
                case TriggerType.ProximityBased:
                    var proximityTrigger = eggObject.AddComponent<SphereCollider>();
                    proximityTrigger.isTrigger = true;
                    proximityTrigger.radius = 5f;
                    break;
                    
                case TriggerType.InteractionBased:
                    var interactionCollider = eggObject.AddComponent<BoxCollider>();
                    interactionCollider.isTrigger = true;
                    break;
                    
                case TriggerType.ViewBased:
                    // Trigger when player looks in direction
                    break;
                    
                case TriggerType.TimeBased:
                    // Auto-trigger after time
                    Invoke(nameof(AutoTriggerEasterEgg), 10f);
                    break;
            }
            
            // Add visual effects
            if (!string.IsNullOrEmpty(easterEgg.visualEffect))
            {
                CreateVisualEffect(eggObject, easterEgg.visualEffect);
            }
        }
        
        void CreateVisualEffect(GameObject target, string effectName)
        {
            // Create particle systems or other visual effects based on effectName
            switch (effectName)
            {
                case "WindVortexEffect":
                    CreateWindVortex(target);
                    break;
                case "WaterRipples":
                    CreateWaterRipples(target);
                    break;
                case "MusicalNotes":
                    CreateMusicalNotes(target);
                    break;
                // Add more effects as needed
            }
        }
        
        void CreateWindVortex(GameObject target)
        {
            var particles = target.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startLifetime = 3f;
            main.startSpeed = 5f;
            main.startSize = 0.1f;
            main.startColor = Color.gray;
            
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 2f;
            
            var velocityOverLifetime = particles.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
            velocityOverLifetime.radial = new ParticleSystem.MinMaxCurve(5f);
        }
        
        void CreateWaterRipples(GameObject target)
        {
            var particles = target.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startLifetime = 2f;
            main.startSpeed = 1f;
            main.startSize = 0.5f;
            main.startColor = Color.blue;
            
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 1f;
        }
        
        void CreateMusicalNotes(GameObject target)
        {
            var particles = target.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startLifetime = 4f;
            main.startSpeed = 2f;
            main.startSize = 0.3f;
            main.startColor = Color.yellow;
            
            var velocityOverLifetime = particles.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(2f);
        }
        
        Vector3 GetRandomSpawnPosition()
        {
            // Get random position within map bounds
            float x = UnityEngine.Random.Range(-100f, 100f);
            float z = UnityEngine.Random.Range(-100f, 100f);
            return new Vector3(x, 0f, z);
        }
        
        public void TriggerEasterEgg(ulong playerId, string eggId)
        {
            var easterEgg = activeEasterEggs.Find(x => x.eggId == eggId && x.isActive);
            if (easterEgg != null)
            {
                DiscoverEasterEgg(playerId, easterEgg);
            }
        }
        
        void DiscoverEasterEgg(ulong playerId, CulturalEasterEgg easterEgg)
        {
            // Check if player already discovered this egg
            if (!playerDiscoveredEggs.ContainsKey(playerId))
            {
                playerDiscoveredEggs[playerId] = new List<string>();
            }
            
            if (playerDiscoveredEggs[playerId].Contains(easterEgg.eggId))
            {
                return; // Already discovered
            }
            
            playerDiscoveredEggs[playerId].Add(easterEgg.eggId);
            
            // Grant rewards
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.AddExperience(playerId.ToString(), easterEgg.rewardXP);
                EconomyManager.Instance.AddCoins(playerId.ToString(), easterEgg.rewardCoins);
            }
            
            // Track discovery in analytics
            if (PlayerAnalytics.Instance != null)
            {
                PlayerAnalytics.Instance.TrackEasterEggDiscovery(playerId.ToString(), easterEgg.eggId, easterEgg.category.ToString());
            }
            
            OnEasterEggDiscovered?.Invoke(playerId, easterEgg);
            
            // Notify client
            NotifyEasterEggDiscoveryClientRpc(playerId, easterEgg);
            
            // Deactivate the easter egg
            easterEgg.isActive = false;
        }
        
        [ClientRpc]
        void NotifyEasterEggDiscoveryClientRpc(ulong playerId, CulturalEasterEgg easterEgg)
        {
            if (playerId == NetworkManager.Singleton.LocalClientId)
            {
                ShowEasterEggDiscoveryUI(easterEgg);
                PlayEasterEggAudio(easterEgg.audioClip);
            }
        }
        
        void ShowEasterEggDiscoveryUI(CulturalEasterEgg easterEgg)
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowEasterEggNotification(easterEgg);
            }
        }
        
        void PlayEasterEggAudio(string audioClip)
        {
            if (AudioManager.Instance != null && !string.IsNullOrEmpty(audioClip))
            {
                AudioManager.Instance.PlaySound(audioClip);
            }
        }
        
        public List<string> GetDiscoveredEasterEggs(ulong playerId)
        {
            return playerDiscoveredEggs.TryGetValue(playerId, out var eggs) ? eggs : new List<string>();
        }
        
        public int GetTotalEasterEggsCount()
        {
            return availableEasterEggs.Count;
        }
        
        public float GetDiscoveryProgress(ulong playerId)
        {
            var discovered = GetDiscoveredEasterEggs(playerId);
            return (float)discovered.Count / availableEasterEggs.Count;
        }
    }
    
    [System.Serializable]
    public class CulturalEasterEgg
    {
        public string eggId;
        public string name;
        public string description;
        public EasterEggCategory category;
        public TriggerType triggerType;
        public EasterEggRarity rarityLevel;
        public Vector3 position;
        public bool isActive;
        public int rewardXP;
        public int rewardCoins;
        public string audioClip;
        public string visualEffect;
        public string activationMessage;
        public string culturalInfo;
    }
    
    public enum EasterEggCategory
    {
        Folklore,
        Historical,
        PopCulture,
        Regional,
        Food,
        Music,
        Sports,
        Nature,
        Architecture
    }
    
    public enum TriggerType
    {
        ProximityBased,
        InteractionBased,
        ViewBased,
        TimeBased,
        RandomEvent,
        SequenceBased
    }
    
    public enum EasterEggRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }
}
