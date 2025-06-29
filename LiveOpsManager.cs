
using UnityEngine;
using System.Collections.Generic;
using System;
using ArenaBrasil.Backend;
using ArenaBrasil.Economy;

namespace ArenaBrasil.LiveOps
{
    public class LiveOpsManager : MonoBehaviour
    {
        public static LiveOpsManager Instance { get; private set; }
        
        [Header("Event Configuration")]
        public List<LiveEvent> activeEvents = new List<LiveEvent>();
        public List<DailyChallenge> dailyChallenges = new List<DailyChallenge>();
        public List<WeeklyChallenge> weeklyChallenges = new List<WeeklyChallenge>();
        
        [Header("Brazilian Seasonal Events")]
        public BrazilianEventData[] brazilianEvents;
        
        [Header("Content Updates")]
        public float contentCheckInterval = 300f; // 5 minutes
        public string contentVersionUrl;
        
        // Events
        public event Action<LiveEvent> OnEventStarted;
        public event Action<LiveEvent> OnEventEnded;
        public event Action<DailyChallenge> OnDailyChallengeCompleted;
        public event Action<WeeklyChallenge> OnWeeklyChallengeCompleted;
        public event Action<string> OnContentUpdateAvailable;
        
        private float lastContentCheck;
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void Start()
        {
            InitializeLiveOps();
            SetupBrazilianEvents();
        }
        
        void Update()
        {
            UpdateActiveEvents();
            CheckForContentUpdates();
            UpdateChallenges();
        }
        
        void InitializeLiveOps()
        {
            Debug.Log("Arena Brasil - Inicializando Live Operations");
            
            LoadActiveEvents();
            GenerateDailyChallenges();
            GenerateWeeklyChallenges();
            
            InvokeRepeating(nameof(CheckEventSchedule), 0f, 3600f); // Check every hour
        }
        
        void SetupBrazilianEvents()
        {
            // Carnaval Event (February)
            var carnavalEvent = new BrazilianEventData
            {
                eventName = "Carnaval das Lendas",
                description = "Celebre o Carnaval brasileiro com skins exclusivas e mapas temáticos!",
                startMonth = 2,
                startDay = 1,
                endMonth = 2,
                endDay = 28,
                rewards = new List<string> { "Skin Saci Folião", "Skin Iara Rainha", "Emote Samba" }
            };
            
            // Festa Junina Event (June)
            var festaJuninaEvent = new BrazilianEventData
            {
                eventName = "Festa Junina Arena",
                description = "Arraiá das lendas com fogueiras, quadrilha e muito forró!",
                startMonth = 6,
                startDay = 1,
                endMonth = 6,
                endDay = 30,
                rewards = new List<string> { "Skin Curupira Caipira", "Arma Espingarda Junina", "Emote Quadrilha" }
            };
            
            // Independence Day (September)
            var independenceEvent = new BrazilianEventData
            {
                eventName = "Independência das Lendas",
                description = "Celebre a independência do Brasil com orgulho!",
                startMonth = 9,
                startDay = 1,
                endMonth = 9,
                endDay = 7,
                rewards = new List<string> { "Skin Brasil Patriota", "Paraquedas Bandeira", "Emote Grito" }
            };
            
            brazilianEvents = new BrazilianEventData[] { carnavalEvent, festaJuninaEvent, independenceEvent };
        }
        
        void LoadActiveEvents()
        {
            // Simulated events - in production would load from backend
            var weekendEvent = new LiveEvent
            {
                eventId = "weekend_bonus_001",
                eventName = "Final de Semana Lendário",
                description = "Ganhe XP e moedas em dobro durante o final de semana!",
                startTime = DateTime.Now.AddHours(-1),
                endTime = DateTime.Now.AddDays(2),
                eventType = EventType.XPBonus,
                multiplier = 2.0f,
                isActive = true
            };
            
            activeEvents.Add(weekendEvent);
        }
        
        void GenerateDailyChallenges()
        {
            dailyChallenges.Clear();
            
            string[] challengeTypes = {
                "Elimine {0} inimigos",
                "Cause {0} de dano",
                "Sobreviva por {0} minutos",
                "Colete {0} itens",
                "Use a habilidade especial {0} vezes",
                "Vença {0} partidas",
                "Aterrisse em {0} locais diferentes"
            };
            
            for (int i = 0; i < 3; i++)
            {
                var challenge = new DailyChallenge
                {
                    challengeId = $"daily_{DateTime.Now:yyyyMMdd}_{i}",
                    description = string.Format(challengeTypes[UnityEngine.Random.Range(0, challengeTypes.Length)], 
                                               UnityEngine.Random.Range(3, 15)),
                    targetValue = UnityEngine.Random.Range(3, 15),
                    currentProgress = 0,
                    reward = new ChallengeReward
                    {
                        coins = UnityEngine.Random.Range(50, 200),
                        xp = UnityEngine.Random.Range(100, 500),
                        itemId = GetRandomRewardItem()
                    },
                    expiryTime = DateTime.Now.AddDays(1)
                };
                
                dailyChallenges.Add(challenge);
            }
        }
        
        void GenerateWeeklyChallenges()
        {
            weeklyChallenges.Clear();
            
            string[] weeklyTypes = {
                "Elimine {0} inimigos durante a semana",
                "Vença {0} partidas esta semana",
                "Cause {0} de dano total",
                "Complete {0} partidas",
                "Use cada Herói Lenda pelo menos {0} vezes"
            };
            
            for (int i = 0; i < 5; i++)
            {
                var challenge = new WeeklyChallenge
                {
                    challengeId = $"weekly_{DateTime.Now:yyyyMMdd}_{i}",
                    description = string.Format(weeklyTypes[UnityEngine.Random.Range(0, weeklyTypes.Length)], 
                                               UnityEngine.Random.Range(10, 50)),
                    targetValue = UnityEngine.Random.Range(10, 50),
                    currentProgress = 0,
                    reward = new ChallengeReward
                    {
                        coins = UnityEngine.Random.Range(200, 1000),
                        xp = UnityEngine.Random.Range(500, 2000),
                        itemId = GetRandomRareRewardItem()
                    },
                    expiryTime = DateTime.Now.AddDays(7)
                };
                
                weeklyChallenges.Add(challenge);
            }
        }
        
        void UpdateActiveEvents()
        {
            for (int i = activeEvents.Count - 1; i >= 0; i--)
            {
                var liveEvent = activeEvents[i];
                
                if (DateTime.Now > liveEvent.endTime && liveEvent.isActive)
                {
                    EndEvent(liveEvent);
                }
                else if (DateTime.Now >= liveEvent.startTime && !liveEvent.isActive)
                {
                    StartEvent(liveEvent);
                }
            }
        }
        
        void CheckEventSchedule()
        {
            // Check for Brazilian seasonal events
            CheckBrazilianSeasonalEvents();
            
            // Check for special weekend events
            if (DateTime.Now.DayOfWeek == DayOfWeek.Friday)
            {
                CreateWeekendEvent();
            }
        }
        
        void CheckBrazilianSeasonalEvents()
        {
            DateTime now = DateTime.Now;
            
            foreach (var brazilianEvent in brazilianEvents)
            {
                if (now.Month == brazilianEvent.startMonth && now.Day >= brazilianEvent.startDay)
                {
                    if (now.Month == brazilianEvent.endMonth && now.Day <= brazilianEvent.endDay)
                    {
                        // Event should be active
                        if (!IsEventActive(brazilianEvent.eventName))
                        {
                            CreateBrazilianEvent(brazilianEvent);
                        }
                    }
                }
            }
        }
        
        bool IsEventActive(string eventName)
        {
            return activeEvents.Exists(e => e.eventName == eventName && e.isActive);
        }
        
        void CreateBrazilianEvent(BrazilianEventData eventData)
        {
            var liveEvent = new LiveEvent
            {
                eventId = $"brazilian_{eventData.eventName.ToLower().Replace(" ", "_")}",
                eventName = eventData.eventName,
                description = eventData.description,
                startTime = new DateTime(DateTime.Now.Year, eventData.startMonth, eventData.startDay),
                endTime = new DateTime(DateTime.Now.Year, eventData.endMonth, eventData.endDay),
                eventType = EventType.Cultural,
                isActive = true,
                specialRewards = eventData.rewards
            };
            
            activeEvents.Add(liveEvent);
            StartEvent(liveEvent);
        }
        
        void CreateWeekendEvent()
        {
            if (IsEventActive("Final de Semana Lendário")) return;
            
            var weekendEvent = new LiveEvent
            {
                eventId = $"weekend_{DateTime.Now:yyyyMMdd}",
                eventName = "Final de Semana Lendário",
                description = "XP e moedas em dobro durante todo o final de semana!",
                startTime = DateTime.Now,
                endTime = DateTime.Now.AddDays(3), // Friday to Sunday
                eventType = EventType.XPBonus,
                multiplier = 2.0f,
                isActive = true
            };
            
            activeEvents.Add(weekendEvent);
            StartEvent(weekendEvent);
        }
        
        void StartEvent(LiveEvent liveEvent)
        {
            liveEvent.isActive = true;
            OnEventStarted?.Invoke(liveEvent);
            
            Debug.Log($"Arena Brasil - Evento iniciado: {liveEvent.eventName}");
            
            // Send push notification
            SendEventNotification(liveEvent, true);
        }
        
        void EndEvent(LiveEvent liveEvent)
        {
            liveEvent.isActive = false;
            OnEventEnded?.Invoke(liveEvent);
            
            Debug.Log($"Arena Brasil - Evento finalizado: {liveEvent.eventName}");
            
            // Send push notification
            SendEventNotification(liveEvent, false);
            
            // Remove from active events
            activeEvents.Remove(liveEvent);
        }
        
        void SendEventNotification(LiveEvent liveEvent, bool isStarting)
        {
            string message = isStarting ? 
                $"🎉 {liveEvent.eventName} começou! {liveEvent.description}" :
                $"⏰ {liveEvent.eventName} termina em breve! Não perca as recompensas!";
            
            // Integration with Firebase Cloud Messaging would go here
            Debug.Log($"Push Notification: {message}");
        }
        
        void UpdateChallenges()
        {
            // Remove expired challenges
            dailyChallenges.RemoveAll(c => DateTime.Now > c.expiryTime);
            weeklyChallenges.RemoveAll(c => DateTime.Now > c.expiryTime);
            
            // Generate new challenges if needed
            if (dailyChallenges.Count == 0)
            {
                GenerateDailyChallenges();
            }
            
            if (weeklyChallenges.Count == 0)
            {
                GenerateWeeklyChallenges();
            }
        }
        
        void CheckForContentUpdates()
        {
            if (Time.time - lastContentCheck > contentCheckInterval)
            {
                lastContentCheck = Time.time;
                CheckContentVersion();
            }
        }
        
        async void CheckContentVersion()
        {
            try
            {
                // In production, this would check server for new content
                Debug.Log("Verificando atualizações de conteúdo...");
                
                // Simulate content update check
                if (UnityEngine.Random.Range(0f, 1f) < 0.1f) // 10% chance
                {
                    OnContentUpdateAvailable?.Invoke("Nova atualização disponível com mapas e skins brasileiras!");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Erro ao verificar atualizações: {e.Message}");
            }
        }
        
        public void CompleteChallenge(string challengeId, int progressAmount)
        {
            // Check daily challenges
            var dailyChallenge = dailyChallenges.Find(c => c.challengeId == challengeId);
            if (dailyChallenge != null)
            {
                dailyChallenge.currentProgress += progressAmount;
                if (dailyChallenge.currentProgress >= dailyChallenge.targetValue)
                {
                    CompleteDailyChallenge(dailyChallenge);
                }
                return;
            }
            
            // Check weekly challenges
            var weeklyChallenge = weeklyChallenges.Find(c => c.challengeId == challengeId);
            if (weeklyChallenge != null)
            {
                weeklyChallenge.currentProgress += progressAmount;
                if (weeklyChallenge.currentProgress >= weeklyChallenge.targetValue)
                {
                    CompleteWeeklyChallenge(weeklyChallenge);
                }
            }
        }
        
        void CompleteDailyChallenge(DailyChallenge challenge)
        {
            Debug.Log($"Desafio diário completado: {challenge.description}");
            
            // Grant rewards
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.AddCoins(challenge.reward.coins);
                EconomyManager.Instance.AddXP(challenge.reward.xp);
                
                if (!string.IsNullOrEmpty(challenge.reward.itemId))
                {
                    EconomyManager.Instance.GrantItem(challenge.reward.itemId);
                }
            }
            
            OnDailyChallengeCompleted?.Invoke(challenge);
        }
        
        void CompleteWeeklyChallenge(WeeklyChallenge challenge)
        {
            Debug.Log($"Desafio semanal completado: {challenge.description}");
            
            // Grant rewards
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.AddCoins(challenge.reward.coins);
                EconomyManager.Instance.AddXP(challenge.reward.xp);
                
                if (!string.IsNullOrEmpty(challenge.reward.itemId))
                {
                    EconomyManager.Instance.GrantItem(challenge.reward.itemId);
                }
            }
            
            OnWeeklyChallengeCompleted?.Invoke(challenge);
        }
        
        string GetRandomRewardItem()
        {
            string[] commonItems = {
                "basic_health_potion",
                "common_grenade",
                "basic_ammo_pack",
                "bronze_chest"
            };
            
            return commonItems[UnityEngine.Random.Range(0, commonItems.Length)];
        }
        
        string GetRandomRareRewardItem()
        {
            string[] rareItems = {
                "legendary_skin_saci",
                "epic_weapon_m4",
                "rare_emote_samba",
                "golden_chest"
            };
            
            return rareItems[UnityEngine.Random.Range(0, rareItems.Length)];
        }
        
        public float GetActiveMultiplier(EventType eventType)
        {
            var activeEvent = activeEvents.Find(e => e.isActive && e.eventType == eventType);
            return activeEvent?.multiplier ?? 1.0f;
        }
        
        public List<LiveEvent> GetActiveEvents() => activeEvents.FindAll(e => e.isActive);
        public List<DailyChallenge> GetDailyChallenges() => dailyChallenges;
        public List<WeeklyChallenge> GetWeeklyChallenges() => weeklyChallenges;
    }
    
    [Serializable]
    public class LiveEvent
    {
        public string eventId;
        public string eventName;
        public string description;
        public DateTime startTime;
        public DateTime endTime;
        public EventType eventType;
        public float multiplier = 1.0f;
        public bool isActive;
        public List<string> specialRewards = new List<string>();
    }
    
    [Serializable]
    public class DailyChallenge
    {
        public string challengeId;
        public string description;
        public int targetValue;
        public int currentProgress;
        public ChallengeReward reward;
        public DateTime expiryTime;
    }
    
    [Serializable]
    public class WeeklyChallenge
    {
        public string challengeId;
        public string description;
        public int targetValue;
        public int currentProgress;
        public ChallengeReward reward;
        public DateTime expiryTime;
    }
    
    [Serializable]
    public class ChallengeReward
    {
        public int coins;
        public int xp;
        public string itemId;
    }
    
    [Serializable]
    public class BrazilianEventData
    {
        public string eventName;
        public string description;
        public int startMonth;
        public int startDay;
        public int endMonth;
        public int endDay;
        public List<string> rewards;
    }
    
    public enum EventType
    {
        XPBonus,
        CoinsBonus,
        Cultural,
        Special,
        Tournament
    }
}
