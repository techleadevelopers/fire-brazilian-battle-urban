
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System;
using ArenaBrasil.Backend;
using ArenaBrasil.Economy;

namespace ArenaBrasil.Social
{
    public class BrazilianInfluencerSystem : MonoBehaviour
    {
        public static BrazilianInfluencerSystem Instance { get; private set; }
        
        [Header("Influencer Configuration")]
        public bool enableInfluencerEvents = true;
        public float eventDuration = 24f; // hours
        public int maxSimultaneousEvents = 3;
        
        [Header("Partnership Rewards")]
        public int influencerCodeReward = 200;
        public int followerBonusGems = 50;
        public float viewershipMultiplier = 1.5f;
        
        private Dictionary<string, BrazilianInfluencer> influencers = new Dictionary<string, BrazilianInfluencer>();
        private Dictionary<string, InfluencerEvent> activeEvents = new Dictionary<string, InfluencerEvent>();
        private Dictionary<string, InfluencerCode> activeCodes = new Dictionary<string, InfluencerCode>();
        
        // Events
        public event Action<InfluencerEvent> OnInfluencerEventStarted;
        public event Action<InfluencerEvent> OnInfluencerEventEnded;
        public event Action<string, string> OnInfluencerCodeUsed;
        public event Action<BrazilianInfluencer> OnInfluencerLive;
        
        [System.Serializable]
        public class BrazilianInfluencer
        {
            public string influencerId;
            public string displayName;
            public string realName;
            public InfluencerTier tier;
            public InfluencerPlatform platform;
            public int followers;
            public string profileImage;
            public string bannerImage;
            public bool isVerified;
            public bool isLive;
            public string currentStreamUrl;
            
            [Header("Content")]
            public List<string> specialties = new List<string>();
            public string catchPhrase;
            public List<string> signatures = new List<string>();
            public Color personalColor = Color.white;
            
            [Header("Partnership")]
            public bool isPartner;
            public DateTime partnershipStartDate;
            public int totalEventsHosted;
            public float averageViewership;
            public List<string> exclusiveContent = new List<string>();
            
            [Header("Customization")]
            public InfluencerCosmetics cosmetics = new InfluencerCosmetics();
            public List<string> customEmotes = new List<string>();
            public string customTitle;
        }
        
        [System.Serializable]
        public class InfluencerCosmetics
        {
            public string exclusiveSkin;
            public string weaponSkin;
            public string emoteId;
            public string victoryPose;
            public string lobbyAnimation;
        }
        
        [System.Serializable]
        public class InfluencerEvent
        {
            public string eventId;
            public string influencerId;
            public string title;
            public string description;
            public DateTime startTime;
            public DateTime endTime;
            public InfluencerEventType eventType;
            public bool isActive;
            public int participantCount;
            public List<EventChallenge> challenges = new List<EventChallenge>();
            public EventRewards rewards = new EventRewards();
            public string streamUrl;
            public bool isLiveEvent;
        }
        
        [System.Serializable]
        public class EventChallenge
        {
            public string challengeId;
            public string title;
            public string description;
            public ChallengeType type;
            public int targetValue;
            public int rewardGems;
            public string rewardCosmetic;
        }
        
        [System.Serializable]
        public class EventRewards
        {
            public int participationGems;
            public int completionGems;
            public string exclusiveSkin;
            public string exclusiveEmote;
            public string exclusiveTitle;
        }
        
        [System.Serializable]
        public class InfluencerCode
        {
            public string code;
            public string influencerId;
            public int rewardGems;
            public string rewardItem;
            public DateTime expiryDate;
            public int maxUses;
            public int currentUses;
            public bool isActive;
            public List<string> usedByPlayers = new List<string>();
        }
        
        public enum InfluencerTier
        {
            Micro,      // 1K-10K followers
            Rising,     // 10K-100K followers  
            Established,// 100K-1M followers
            Mega,       // 1M+ followers
            Celebrity   // Major celebrities
        }
        
        public enum InfluencerPlatform
        {
            YouTube,
            Twitch,
            TikTok,
            Instagram,
            Facebook,
            KwaiPlay,
            NimoTV,
            Trovo
        }
        
        public enum InfluencerEventType
        {
            Tournament,
            Challenge,
            Showcase,
            Community,
            Collaboration,
            Charity,
            Launch
        }
        
        public enum ChallengeType
        {
            Kills,
            Wins,
            Survival,
            TeamPlay,
            Creative,
            Social
        }
        
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
            Debug.Log("Arena Brasil - Initializing Brazilian Influencer System");
            
            CreateBrazilianInfluencers();
            LoadInfluencerData();
            
            if (enableInfluencerEvents)
            {
                InvokeRepeating(nameof(CheckInfluencerEvents), 0f, 300f); // Check every 5 minutes
            }
        }
        
        void CreateBrazilianInfluencers()
        {
            // Major Brazilian Gaming Influencers (Fictional/Inspired)
            
            // Mega Tier Influencers
            var nobru = new BrazilianInfluencer
            {
                influencerId = "nobru_br",
                displayName = "Nobru",
                realName = "Bruno Goes",
                tier = InfluencerTier.Mega,
                platform = InfluencerPlatform.YouTube,
                followers = 15000000,
                isVerified = true,
                isPartner = true,
                catchPhrase = "Bora pro fight!",
                specialties = new List<string> { "Battle Royale", "Mobile Gaming", "Competitive" },
                personalColor = new Color(1f, 0.2f, 0.2f), // Red
                customTitle = "Rei do Free Fire BR"
            };
            
            nobru.cosmetics.exclusiveSkin = "Nobru Warrior";
            nobru.signatures.AddRange(new string[] { "Headshot duplo", "Rush agressivo", "Clutch impossível" });
            influencers[nobru.influencerId] = nobru;
            
            var cellbit = new BrazilianInfluencer
            {
                influencerId = "cellbit_br",
                displayName = "Cellbit",
                realName = "Rafael Lange",
                tier = InfluencerTier.Mega,
                platform = InfluencerPlatform.Twitch,
                followers = 8000000,
                isVerified = true,
                isPartner = true,
                catchPhrase = "Isso é muito suspeito...",
                specialties = new List<string> { "Strategy", "Investigation", "Entertainment" },
                personalColor = new Color(0.5f, 0f, 0.8f), // Purple
                customTitle = "Detetive Gamer"
            };
            
            influencers[cellbit.influencerId] = cellbit;
            
            // Established Tier Influencers
            var flakes = new BrazilianInfluencer
            {
                influencerId = "flakes_power",
                displayName = "FlakesPower",
                realName = "João Pedro",
                tier = InfluencerTier.Established,
                platform = InfluencerPlatform.YouTube,
                followers = 2500000,
                isVerified = true,
                isPartner = true,
                catchPhrase = "Foco, força e fé!",
                specialties = new List<string> { "Mobile Gaming", "Tips & Tricks", "Community" },
                personalColor = new Color(0f, 0.8f, 1f), // Cyan
                customTitle = "Professor dos Games"
            };
            
            influencers[flakes.influencerId] = flakes;
            
            var loud = new BrazilianInfluencer
            {
                influencerId = "loud_team",
                displayName = "LOUD",
                realName = "Team LOUD",
                tier = InfluencerTier.Mega,
                platform = InfluencerPlatform.YouTube,
                followers = 5000000,
                isVerified = true,
                isPartner = true,
                catchPhrase = "LOUD até morrer!",
                specialties = new List<string> { "Esports", "Team Play", "Competitive" },
                personalColor = new Color(0f, 0f, 0f), // Black
                customTitle = "Legends of Brasil"
            };
            
            influencers[loud.influencerId] = loud;
            
            // Rising Tier Influencers
            var gemaplys = new BrazilianInfluencer
            {
                influencerId = "gemaplys_br",
                displayName = "GemaPlys",
                realName = "Géssica",
                tier = InfluencerTier.Rising,
                platform = InfluencerPlatform.TikTok,
                followers = 800000,
                isVerified = true,
                isPartner = false,
                catchPhrase = "Gameplay da gema!",
                specialties = new List<string> { "Female Gaming", "Content Creation", "Lifestyle" },
                personalColor = new Color(1f, 0.4f, 0.8f), // Pink
                customTitle = "Gamer Girl BR"
            };
            
            influencers[gemaplys.influencerId] = gemaplys;
            
            Debug.Log($"Created {influencers.Count} Brazilian influencers");
        }
        
        public void StartInfluencerEvent(string influencerId, InfluencerEventType eventType, string title = "")
        {
            if (!influencers.ContainsKey(influencerId))
            {
                Debug.LogWarning($"Influencer not found: {influencerId}");
                return;
            }
            
            if (activeEvents.Count >= maxSimultaneousEvents)
            {
                Debug.LogWarning("Maximum simultaneous events reached");
                return;
            }
            
            var influencer = influencers[influencerId];
            string eventId = Guid.NewGuid().ToString();
            
            var influencerEvent = new InfluencerEvent
            {
                eventId = eventId,
                influencerId = influencerId,
                title = title.IsNullOrEmpty() ? $"Evento do {influencer.displayName}" : title,
                description = GenerateEventDescription(influencer, eventType),
                startTime = DateTime.Now,
                endTime = DateTime.Now.AddHours(eventDuration),
                eventType = eventType,
                isActive = true
            };
            
            // Create event challenges
            influencerEvent.challenges = CreateEventChallenges(influencer, eventType);
            
            // Set rewards
            influencerEvent.rewards = CreateEventRewards(influencer, eventType);
            
            activeEvents[eventId] = influencerEvent;
            OnInfluencerEventStarted?.Invoke(influencerEvent);
            
            // Create promotional code
            CreateInfluencerCode(influencer, eventId);
            
            Debug.Log($"Started influencer event: {influencerEvent.title}");
        }
        
        string GenerateEventDescription(BrazilianInfluencer influencer, InfluencerEventType eventType)
        {
            var descriptions = new Dictionary<InfluencerEventType, string[]>
            {
                [InfluencerEventType.Tournament] = new string[]
                {
                    $"Participe do torneio oficial do {influencer.displayName}!",
                    $"Competição épica com {influencer.displayName} - venha mostrar sua skill!",
                    $"Arena Brasil x {influencer.displayName} - o maior torneio brasileiro!"
                },
                [InfluencerEventType.Challenge] = new string[]
                {
                    $"Desafio exclusivo do {influencer.displayName} - você aceita?",
                    $"Complete os desafios e ganhe recompensas únicas!",
                    $"{influencer.displayName} te desafia - mostre do que é capaz!"
                },
                [InfluencerEventType.Showcase] = new string[]
                {
                    $"Assista {influencer.displayName} jogando ao vivo!",
                    $"Showcase especial com {influencer.displayName}",
                    $"Live exclusiva - {influencer.displayName} mostra as melhores jogadas!"
                }
            };
            
            var options = descriptions.ContainsKey(eventType) ? descriptions[eventType] : new string[] { "Evento especial!" };
            return options[UnityEngine.Random.Range(0, options.Length)];
        }
        
        List<EventChallenge> CreateEventChallenges(BrazilianInfluencer influencer, InfluencerEventType eventType)
        {
            var challenges = new List<EventChallenge>();
            
            // Basic challenges for all events
            challenges.Add(new EventChallenge
            {
                challengeId = "participate",
                title = "Participação",
                description = "Participe do evento jogando 1 partida",
                type = ChallengeType.Social,
                targetValue = 1,
                rewardGems = 50
            });
            
            challenges.Add(new EventChallenge
            {
                challengeId = "kills_event",
                title = $"Elimina como {influencer.displayName}",
                description = "Elimine 10 adversários durante o evento",
                type = ChallengeType.Kills,
                targetValue = 10,
                rewardGems = 100
            });
            
            // Influencer-specific challenges
            if (influencer.specialties.Contains("Battle Royale"))
            {
                challenges.Add(new EventChallenge
                {
                    challengeId = "br_master",
                    title = "Mestre do BR",
                    description = "Termine em Top 3 por 3 vezes seguidas",
                    type = ChallengeType.Survival,
                    targetValue = 3,
                    rewardGems = 200,
                    rewardCosmetic = $"{influencer.displayName}_Victory_Emote"
                });
            }
            
            if (influencer.specialties.Contains("Team Play"))
            {
                challenges.Add(new EventChallenge
                {
                    challengeId = "team_work",
                    title = "Trabalho em Equipe",
                    description = "Vença 5 partidas jogando em dupla/squad",
                    type = ChallengeType.TeamPlay,
                    targetValue = 5,
                    rewardGems = 150
                });
            }
            
            return challenges;
        }
        
        EventRewards CreateEventRewards(BrazilianInfluencer influencer, InfluencerEventType eventType)
        {
            return new EventRewards
            {
                participationGems = 100,
                completionGems = 300,
                exclusiveSkin = $"{influencer.displayName}_Exclusive",
                exclusiveEmote = $"{influencer.displayName}_Signature",
                exclusiveTitle = $"Fã do {influencer.displayName}"
            };
        }
        
        void CreateInfluencerCode(BrazilianInfluencer influencer, string eventId)
        {
            string code = $"{influencer.displayName.ToUpper()}{UnityEngine.Random.Range(100, 999)}";
            
            var influencerCode = new InfluencerCode
            {
                code = code,
                influencerId = influencer.influencerId,
                rewardGems = influencerCodeReward,
                rewardItem = $"{influencer.displayName}_Supporter",
                expiryDate = DateTime.Now.AddDays(7),
                maxUses = 10000,
                isActive = true
            };
            
            activeCodes[code] = influencerCode;
            
            Debug.Log($"Created influencer code: {code} for {influencer.displayName}");
        }
        
        public bool UseInfluencerCode(string code, string playerId)
        {
            if (!activeCodes.ContainsKey(code))
            {
                Debug.LogWarning($"Invalid influencer code: {code}");
                return false;
            }
            
            var influencerCode = activeCodes[code];
            
            // Check if code is still valid
            if (!influencerCode.isActive || DateTime.Now > influencerCode.expiryDate)
            {
                Debug.LogWarning($"Expired influencer code: {code}");
                return false;
            }
            
            // Check if player already used this code
            if (influencerCode.usedByPlayers.Contains(playerId))
            {
                Debug.LogWarning($"Player {playerId} already used code: {code}");
                return false;
            }
            
            // Check max uses
            if (influencerCode.currentUses >= influencerCode.maxUses)
            {
                Debug.LogWarning($"Code usage limit reached: {code}");
                return false;
            }
            
            // Apply code rewards
            influencerCode.usedByPlayers.Add(playerId);
            influencerCode.currentUses++;
            
            // Give rewards
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.AddGems(playerId, influencerCode.rewardGems);
                
                if (!string.IsNullOrEmpty(influencerCode.rewardItem))
                {
                    EconomyManager.Instance.UnlockSkin(playerId, influencerCode.rewardItem);
                }
            }
            
            OnInfluencerCodeUsed?.Invoke(code, playerId);
            
            Debug.Log($"Player {playerId} used influencer code: {code}");
            return true;
        }
        
        public void SetInfluencerLive(string influencerId, bool isLive, string streamUrl = "")
        {
            if (!influencers.ContainsKey(influencerId))
                return;
            
            var influencer = influencers[influencerId];
            influencer.isLive = isLive;
            influencer.currentStreamUrl = streamUrl;
            
            if (isLive)
            {
                OnInfluencerLive?.Invoke(influencer);
                
                // Create live event automatically
                StartInfluencerEvent(influencerId, InfluencerEventType.Showcase, $"Live do {influencer.displayName}");
                
                Debug.Log($"{influencer.displayName} is now live!");
            }
        }
        
        void CheckInfluencerEvents()
        {
            DateTime now = DateTime.Now;
            var eventsToRemove = new List<string>();
            
            foreach (var eventPair in activeEvents)
            {
                var influencerEvent = eventPair.Value;
                
                if (now > influencerEvent.endTime)
                {
                    EndInfluencerEvent(influencerEvent);
                    eventsToRemove.Add(eventPair.Key);
                }
            }
            
            // Remove ended events
            foreach (var eventId in eventsToRemove)
            {
                activeEvents.Remove(eventId);
            }
            
            // Check for expired codes
            CheckExpiredCodes();
        }
        
        void CheckExpiredCodes()
        {
            DateTime now = DateTime.Now;
            var codesToRemove = new List<string>();
            
            foreach (var codePair in activeCodes)
            {
                if (now > codePair.Value.expiryDate)
                {
                    codesToRemove.Add(codePair.Key);
                }
            }
            
            foreach (var code in codesToRemove)
            {
                activeCodes.Remove(code);
                Debug.Log($"Removed expired influencer code: {code}");
            }
        }
        
        void EndInfluencerEvent(InfluencerEvent influencerEvent)
        {
            influencerEvent.isActive = false;
            OnInfluencerEventEnded?.Invoke(influencerEvent);
            
            Debug.Log($"Ended influencer event: {influencerEvent.title}");
        }
        
        void LoadInfluencerData()
        {
            // Load from Firebase/PlayerPrefs
            string data = PlayerPrefs.GetString("InfluencerData", "{}");
            
            try
            {
                // Parse and load data
                Debug.Log("Influencer data loaded");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load influencer data: {e.Message}");
            }
        }
        
        void SaveInfluencerData()
        {
            try
            {
                // Save to Firebase/PlayerPrefs
                string dataJson = JsonUtility.ToJson(influencers);
                PlayerPrefs.SetString("InfluencerData", dataJson);
                PlayerPrefs.Save();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save influencer data: {e.Message}");
            }
        }
        
        // Public API
        public List<BrazilianInfluencer> GetActiveInfluencers()
        {
            var active = new List<BrazilianInfluencer>();
            
            foreach (var influencer in influencers.Values)
            {
                if (influencer.isPartner)
                {
                    active.Add(influencer);
                }
            }
            
            return active;
        }
        
        public List<BrazilianInfluencer> GetLiveInfluencers()
        {
            var live = new List<BrazilianInfluencer>();
            
            foreach (var influencer in influencers.Values)
            {
                if (influencer.isLive)
                {
                    live.Add(influencer);
                }
            }
            
            return live;
        }
        
        public List<InfluencerEvent> GetActiveEvents()
        {
            return new List<InfluencerEvent>(activeEvents.Values);
        }
        
        public BrazilianInfluencer GetInfluencer(string influencerId)
        {
            return influencers.ContainsKey(influencerId) ? influencers[influencerId] : null;
        }
        
        public List<string> GetActiveInfluencerCodes()
        {
            var codes = new List<string>();
            
            foreach (var code in activeCodes.Keys)
            {
                if (activeCodes[code].isActive && DateTime.Now <= activeCodes[code].expiryDate)
                {
                    codes.Add(code);
                }
            }
            
            return codes;
        }
        
        public void CompleteEventChallenge(string eventId, string challengeId, string playerId)
        {
            if (!activeEvents.ContainsKey(eventId))
                return;
                
            var influencerEvent = activeEvents[eventId];
            var challenge = influencerEvent.challenges.Find(c => c.challengeId == challengeId);
            
            if (challenge != null)
            {
                // Give challenge rewards
                if (EconomyManager.Instance != null)
                {
                    EconomyManager.Instance.AddGems(playerId, challenge.rewardGems);
                    
                    if (!string.IsNullOrEmpty(challenge.rewardCosmetic))
                    {
                        EconomyManager.Instance.UnlockSkin(playerId, challenge.rewardCosmetic);
                    }
                }
                
                Debug.Log($"Player {playerId} completed challenge: {challenge.title}");
            }
        }
        
        public void RegisterNewInfluencer(BrazilianInfluencer influencer)
        {
            influencers[influencer.influencerId] = influencer;
            SaveInfluencerData();
            
            Debug.Log($"Registered new influencer: {influencer.displayName}");
        }
    }
    
    // Extension methods
    public static class StringExtensions
    {
        public static bool IsNullOrEmpty(this string str)
        {
            return string.IsNullOrEmpty(str);
        }
    }
}
