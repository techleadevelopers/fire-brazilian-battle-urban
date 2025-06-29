
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System;
using ArenaBrasil.Economy;

namespace ArenaBrasil.Influencer
{
    public class InfluencerSystem : NetworkBehaviour
    {
        public static InfluencerSystem Instance { get; private set; }
        
        [Header("Influencer Configuration")]
        public List<BrazilianInfluencer> partneredInfluencers = new List<BrazilianInfluencer>();
        public List<CelebrityCollaboration> activeCelebCollabs = new List<CelebrityCollaboration>();
        
        [Header("Creator Program")]
        public CreatorProgramConfig creatorProgram;
        
        private Dictionary<string, InfluencerMetrics> influencerMetrics = new Dictionary<string, InfluencerMetrics>();
        
        public event Action<BrazilianInfluencer> OnInfluencerEventStarted;
        public event Action<string> OnCreatorCodeUsed;
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeInfluencerSystem();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void InitializeInfluencerSystem()
        {
            Debug.Log("Arena Brasil - Inicializando sistema de influenciadores");
            
            SetupBrazilianInfluencers();
            SetupCelebrityCollaborations();
            InitializeCreatorProgram();
        }
        
        void SetupBrazilianInfluencers()
        {
            partneredInfluencers.Add(new BrazilianInfluencer
            {
                influencerId = "nobru_official",
                realName = "Bruno Goes",
                stageName = "Nobru",
                platform = "Twitch/YouTube",
                followers = 15000000,
                region = "São Paulo",
                niche = "Gaming",
                partnershipLevel = PartnershipLevel.Tier1,
                exclusiveContent = new string[] {
                    "Skin Nobru Exclusive",
                    "Emote Signature Nobru",
                    "Nobru's Arena Map"
                },
                creatorCode = "NOBRU",
                commissionRate = 0.05f
            });
            
            partneredInfluencers.Add(new BrazilianInfluencer
            {
                influencerId = "loud_team",
                realName = "LOUD",
                stageName = "LOUD",
                platform = "Multi-platform",
                followers = 20000000,
                region = "Rio de Janeiro",
                niche = "Esports",
                partnershipLevel = PartnershipLevel.Tier1,
                exclusiveContent = new string[] {
                    "LOUD Team Uniform",
                    "LOUD Arena Stadium",
                    "LOUD Victory Dance"
                },
                creatorCode = "LOUD",
                commissionRate = 0.07f
            });
            
            partneredInfluencers.Add(new BrazilianInfluencer
            {
                influencerId = "gaules_cs",
                realName = "Alexandre Borba",
                stageName = "Gaules",
                platform = "Twitch",
                followers = 8000000,
                region = "Porto Alegre",
                niche = "FPS Gaming",
                partnershipLevel = PartnershipLevel.Tier2,
                exclusiveContent = new string[] {
                    "Gaules Commentator Pack",
                    "Southern Brazil Map",
                    "Gaules Catchphrase Emotes"
                },
                creatorCode = "GAULES",
                commissionRate = 0.04f
            });
        }
        
        void SetupCelebrityCollaborations()
        {
            activeCelebCollabs.Add(new CelebrityCollaboration
            {
                collaborationId = "anitta_summer_2024",
                celebrityName = "Anitta",
                collaborationType = CollaborationType.MusicIntegration,
                duration = TimeSpan.FromDays(30),
                exclusiveContent = new string[] {
                    "Anitta Victory Song",
                    "Anitta Dance Emote",
                    "Rio Carnival Arena Map"
                },
                isActive = true,
                startDate = DateTime.Now,
                endDate = DateTime.Now.AddDays(30)
            });
            
            activeCelebCollabs.Add(new CelebrityCollaboration
            {
                collaborationId = "neymar_football_event",
                celebrityName = "Neymar Jr.",
                collaborationType = CollaborationType.SportsIntegration,
                duration = TimeSpan.FromDays(14),
                exclusiveContent = new string[] {
                    "Neymar Football Skills Emote",
                    "PSG x Arena Brasil Skin",
                    "Football Arena Map"
                },
                isActive = true,
                startDate = DateTime.Now,
                endDate = DateTime.Now.AddDays(14)
            });
        }
        
        void InitializeCreatorProgram()
        {
            creatorProgram = new CreatorProgramConfig
            {
                minFollowersRequired = 1000,
                minViewsRequired = 10000,
                applicationReviewTime = TimeSpan.FromDays(7),
                defaultCommissionRate = 0.03f,
                bonusThresholds = new Dictionary<int, float>
                {
                    { 10000, 0.04f },
                    { 100000, 0.05f },
                    { 1000000, 0.06f }
                }
            };
        }
        
        [ServerRpc(RequireOwnership = false)]
        public void UseCreatorCodeServerRpc(ulong playerId, string creatorCode)
        {
            var influencer = partneredInfluencers.Find(inf => inf.creatorCode == creatorCode);
            if (influencer == null) return;
            
            // Apply creator code benefits
            ApplyCreatorCodeBenefits(playerId, influencer);
            
            // Track metrics
            TrackCreatorCodeUsage(creatorCode, playerId);
            
            OnCreatorCodeUsed?.Invoke(creatorCode);
            CreatorCodeUsedClientRpc(playerId, creatorCode, influencer.stageName);
        }
        
        void ApplyCreatorCodeBenefits(ulong playerId, BrazilianInfluencer influencer)
        {
            // Give bonus rewards based on influencer tier
            int bonusCoins = influencer.partnershipLevel switch
            {
                PartnershipLevel.Tier1 => 500,
                PartnershipLevel.Tier2 => 300,
                PartnershipLevel.Tier3 => 200,
                _ => 100
            };
            
            int bonusGems = influencer.partnershipLevel switch
            {
                PartnershipLevel.Tier1 => 50,
                PartnershipLevel.Tier2 => 30,
                PartnershipLevel.Tier3 => 20,
                _ => 10
            };
            
            // Apply rewards via Economy Manager
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.AddCurrency(CurrencyType.Coins, bonusCoins);
                EconomyManager.Instance.AddCurrency(CurrencyType.Gems, bonusGems);
            }
            
            // Grant exclusive content access
            GrantExclusiveContent(playerId, influencer.exclusiveContent);
        }
        
        void GrantExclusiveContent(ulong playerId, string[] exclusiveContent)
        {
            foreach (var content in exclusiveContent)
            {
                Debug.Log($"Concedendo conteúdo exclusivo: {content} para jogador {playerId}");
                // Integration with inventory system would go here
            }
        }
        
        void TrackCreatorCodeUsage(string creatorCode, ulong playerId)
        {
            if (!influencerMetrics.ContainsKey(creatorCode))
            {
                influencerMetrics[creatorCode] = new InfluencerMetrics { creatorCode = creatorCode };
            }
            
            var metrics = influencerMetrics[creatorCode];
            metrics.totalUses++;
            metrics.uniqueUsers.Add(playerId);
            metrics.lastUsed = DateTime.Now;
            
            // Calculate commission for influencer
            CalculateInfluencerCommission(creatorCode);
        }
        
        void CalculateInfluencerCommission(string creatorCode)
        {
            var influencer = partneredInfluencers.Find(inf => inf.creatorCode == creatorCode);
            if (influencer == null) return;
            
            var metrics = influencerMetrics[creatorCode];
            float commission = metrics.totalUses * 0.10f * influencer.commissionRate; // $0.10 per use
            
            Debug.Log($"Comissão calculada para {influencer.stageName}: ${commission:F2}");
        }
        
        [ClientRpc]
        void CreatorCodeUsedClientRpc(ulong playerId, string creatorCode, string influencerName)
        {
            if (NetworkManager.Singleton.LocalClientId == playerId)
            {
                ShowCreatorCodeReward(creatorCode, influencerName);
            }
        }
        
        void ShowCreatorCodeReward(string creatorCode, string influencerName)
        {
            Debug.Log($"✨ Código {creatorCode} de {influencerName} aplicado! Bônus recebidos!");
            
            if (UIManager.Instance != null)
            {
                // Show reward notification
                // UIManager.Instance.ShowNotification($"Código {creatorCode} aplicado!");
            }
        }
        
        public void StartInfluencerEvent(string influencerId)
        {
            var influencer = partneredInfluencers.Find(inf => inf.influencerId == influencerId);
            if (influencer == null) return;
            
            // Create special event
            var eventData = new InfluencerEvent
            {
                eventId = $"{influencerId}_event_{DateTime.Now:yyyyMMdd}",
                influencer = influencer,
                eventType = "Partida Especial",
                description = $"Jogue com {influencer.stageName} ao vivo!",
                startTime = DateTime.Now,
                duration = TimeSpan.FromHours(2),
                specialRewards = influencer.exclusiveContent
            };
            
            OnInfluencerEventStarted?.Invoke(influencer);
            StartInfluencerEventClientRpc(influencerId, eventData.description);
        }
        
        [ClientRpc]
        void StartInfluencerEventClientRpc(string influencerId, string description)
        {
            Debug.Log($"🎉 EVENTO ESPECIAL: {description}");
            
            if (UIManager.Instance != null)
            {
                // Show event notification
                ShowInfluencerEventNotification(description);
            }
        }
        
        void ShowInfluencerEventNotification(string description)
        {
            Debug.Log($"Notificação de evento influenciador: {description}");
        }
        
        public bool ValidateCreatorCode(string code)
        {
            return partneredInfluencers.Exists(inf => inf.creatorCode.Equals(code, StringComparison.OrdinalIgnoreCase));
        }
        
        public List<BrazilianInfluencer> GetTopInfluencers()
        {
            var sorted = new List<BrazilianInfluencer>(partneredInfluencers);
            sorted.Sort((a, b) => b.followers.CompareTo(a.followers));
            return sorted.GetRange(0, Math.Min(10, sorted.Count));
        }
        
        public InfluencerMetrics GetInfluencerMetrics(string creatorCode)
        {
            return influencerMetrics.ContainsKey(creatorCode) ? influencerMetrics[creatorCode] : null;
        }
    }
    
    [Serializable]
    public class BrazilianInfluencer
    {
        public string influencerId;
        public string realName;
        public string stageName;
        public string platform;
        public int followers;
        public string region;
        public string niche;
        public PartnershipLevel partnershipLevel;
        public string[] exclusiveContent;
        public string creatorCode;
        public float commissionRate;
        public bool isVerified;
    }
    
    [Serializable]
    public class CelebrityCollaboration
    {
        public string collaborationId;
        public string celebrityName;
        public CollaborationType collaborationType;
        public TimeSpan duration;
        public string[] exclusiveContent;
        public bool isActive;
        public DateTime startDate;
        public DateTime endDate;
    }
    
    [Serializable]
    public class CreatorProgramConfig
    {
        public int minFollowersRequired;
        public int minViewsRequired;
        public TimeSpan applicationReviewTime;
        public float defaultCommissionRate;
        public Dictionary<int, float> bonusThresholds;
    }
    
    [Serializable]
    public class InfluencerMetrics
    {
        public string creatorCode;
        public int totalUses;
        public HashSet<ulong> uniqueUsers = new HashSet<ulong>();
        public DateTime lastUsed;
        public float totalCommission;
    }
    
    [Serializable]
    public class InfluencerEvent
    {
        public string eventId;
        public BrazilianInfluencer influencer;
        public string eventType;
        public string description;
        public DateTime startTime;
        public TimeSpan duration;
        public string[] specialRewards;
    }
    
    public enum PartnershipLevel
    {
        Tier1, // Top tier
        Tier2, // Mid tier
        Tier3  // Entry tier
    }
    
    public enum CollaborationType
    {
        MusicIntegration,
        SportsIntegration,
        ActingCollaboration,
        BrandAmbassador
    }
}
