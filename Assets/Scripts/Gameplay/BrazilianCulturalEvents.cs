
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System;
using ArenaBrasil.Core;
using ArenaBrasil.Economy;

namespace ArenaBrasil.Culture
{
    public class BrazilianCulturalEvents : MonoBehaviour
    {
        public static BrazilianCulturalEvents Instance { get; private set; }
        
        [Header("Cultural Events Configuration")]
        public bool enableSeasonalEvents = true;
        public bool enableRealTimeEvents = true;
        public float eventCheckInterval = 3600f; // Check every hour
        
        [Header("Event Rewards")]
        public int baseEventReward = 100;
        public float rewardMultiplier = 1.5f;
        
        private Dictionary<string, CulturalEvent> availableEvents = new Dictionary<string, CulturalEvent>();
        private Dictionary<string, CulturalEvent> activeEvents = new Dictionary<string, CulturalEvent>();
        private List<CulturalEvent> completedEvents = new List<CulturalEvent>();
        
        // Events
        public event Action<CulturalEvent> OnEventStarted;
        public event Action<CulturalEvent> OnEventEnded;
        public event Action<CulturalEvent, string> OnEventCompleted;
        
        [System.Serializable]
        public class CulturalEvent
        {
            public string eventId;
            public string eventName;
            public string description;
            public CulturalEventType eventType;
            public DateTime startDate;
            public DateTime endDate;
            public bool isActive;
            public bool isCompleted;
            
            [Header("Event Content")]
            public List<EventMission> missions = new List<EventMission>();
            public List<EventReward> rewards = new List<EventReward>();
            public EventCosmetics cosmetics = new EventCosmetics();
            public EventGameplayModifiers modifiers = new EventGameplayModifiers();
            
            [Header("Brazilian Cultural Elements")]
            public BrazilianHoliday holiday;
            public List<string> culturalReferences = new List<string>();
            public string eventMusic;
            public string eventNarration;
            public Color eventThemeColor = Color.green;
        }
        
        [System.Serializable]
        public class EventMission
        {
            public string missionId;
            public string title;
            public string description;
            public MissionType type;
            public int targetValue;
            public int currentProgress;
            public bool isCompleted;
            public List<EventReward> rewards = new List<EventReward>();
        }
        
        [System.Serializable]
        public class EventReward
        {
            public RewardType type;
            public string itemId;
            public int quantity;
            public string displayName;
            public Sprite icon;
        }
        
        [System.Serializable]
        public class EventCosmetics
        {
            public List<string> limitedSkins = new List<string>();
            public List<string> eventEmotes = new List<string>();
            public List<string> specialTitles = new List<string>();
            public List<string> themeLobbyDecorations = new List<string>();
        }
        
        [System.Serializable]
        public class EventGameplayModifiers
        {
            public bool enableSpecialWeapons;
            public bool modifyMapLayout;
            public bool addCulturalMusic;
            public bool specialVoiceLines;
            public float xpMultiplier = 1.0f;
            public float gemsMultiplier = 1.0f;
        }
        
        public enum CulturalEventType
        {
            Carnival,
            FestaJunina,
            Independence,
            Christmas,
            NewYear,
            SoccerWorldCup,
            Olympics,
            FolkloreDay,
            ConsciousnessDay,
            StValentine,
            EasterBrazilian,
            MotherDay,
            FatherDay,
            ChildrenDay,
            StudentDay,
            TeacherDay
        }
        
        public enum BrazilianHoliday
        {
            Carnaval,
            FestasJuninas,
            DiaDoFolclore,
            DiaIndependencia,
            NossoSenhoraAparecida,
            Finados,
            ProclamacaoRepublica,
            Natal,
            AnoNovo,
            Tiradentes,
            DiaTrabalho,
            DiaConscienciaNegra
        }
        
        public enum MissionType
        {
            KillsWithBrazilianWeapon,
            WinMatchesInBrazilianMap,
            DanceEmotes,
            PlayWithFriends,
            SurviveTime,
            CollectCulturalItems,
            ParticipateInEvent
        }
        
        public enum RewardType
        {
            Gems,
            XP,
            Skin,
            Emote,
            Title,
            Weapon,
            Currency
        }
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeCulturalEvents();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void InitializeCulturalEvents()
        {
            Debug.Log("Arena Brasil - Initializing Brazilian Cultural Events");
            
            CreateCulturalEvents();
            LoadEventProgress();
            
            if (enableRealTimeEvents)
            {
                InvokeRepeating(nameof(CheckForEvents), 0f, eventCheckInterval);
            }
            
            CheckCurrentEvents();
        }
        
        void CreateCulturalEvents()
        {
            // Carnaval Event
            var carnavalEvent = new CulturalEvent
            {
                eventId = "carnaval_2024",
                eventName = "Carnaval no Arena Brasil",
                description = "Venha sambar e brigar no maior carnaval do Battle Royale!",
                eventType = CulturalEventType.Carnival,
                holiday = BrazilianHoliday.Carnaval,
                eventThemeColor = new Color(1f, 0.8f, 0f), // Gold
                culturalReferences = new List<string> { "Samba", "Frevo", "Marchinha", "Escola de Samba", "Trio Elétrico" }
            };
            
            carnavalEvent.missions.Add(new EventMission
            {
                missionId = "samba_kills",
                title = "Samba Letal",
                description = "Elimine 20 inimigos dançando",
                type = MissionType.KillsWithBrazilianWeapon,
                targetValue = 20
            });
            
            carnavalEvent.cosmetics.limitedSkins.AddRange(new string[] 
            { 
                "Rei Momo", "Rainha do Carnaval", "Pierrô", "Colombina", "Malandro" 
            });
            
            availableEvents["carnaval_2024"] = carnavalEvent;
            
            // Festa Junina Event
            var festaJuninaEvent = new CulturalEvent
            {
                eventId = "festa_junina_2024",
                eventName = "Arraiá do Arena Brasil",
                description = "Quadrilha de guerra nas terras brasileiras!",
                eventType = CulturalEventType.FestaJunina,
                holiday = BrazilianHoliday.FestasJuninas,
                eventThemeColor = new Color(0.8f, 0.4f, 0.2f), // Brown
                culturalReferences = new List<string> { "Quadrilha", "Fogueira", "Milho", "Pipoca", "Quentão" }
            };
            
            festaJuninaEvent.missions.Add(new EventMission
            {
                missionId = "fogos_artificio",
                title = "Fogos de Artifício",
                description = "Cause 5000 de dano com explosivos",
                type = MissionType.KillsWithBrazilianWeapon,
                targetValue = 5000
            });
            
            availableEvents["festa_junina_2024"] = festaJuninaEvent;
            
            // Copa do Mundo Event
            var copaEvent = new CulturalEvent
            {
                eventId = "copa_mundo_2024",
                eventName = "Arena Brasil Copa Edition",
                description = "O pentacampeão mundial entra em campo... de batalha!",
                eventType = CulturalEventType.SoccerWorldCup,
                eventThemeColor = new Color(0f, 0.8f, 0f), // Green
                culturalReferences = new List<string> { "Futebol", "Canarinho", "Jogo Bonito", "Pelé", "Ronaldinho" }
            };
            
            copaEvent.missions.Add(new EventMission
            {
                missionId = "gol_de_placa",
                title = "Gol de Placa",
                description = "Faça 10 headshots seguidos",
                type = MissionType.KillsWithBrazilianWeapon,
                targetValue = 10
            });
            
            availableEvents["copa_mundo_2024"] = copaEvent;
            
            // Dia da Independência
            var independenciaEvent = new CulturalEvent
            {
                eventId = "independencia_2024",
                eventName = "Independência ou Morte",
                description = "Lute pela liberdade nas terras tupiniquins!",
                eventType = CulturalEventType.Independence,
                holiday = BrazilianHoliday.DiaIndependencia,
                eventThemeColor = new Color(0f, 0.5f, 0f), // Dark Green
                culturalReferences = new List<string> { "Dom Pedro I", "Ipiranga", "Independência", "Brasil Império" }
            };
            
            availableEvents["independencia_2024"] = independenciaEvent;
            
            // Dia do Folclore
            var folcloreEvent = new CulturalEvent
            {
                eventId = "folclore_2024",
                eventName = "Lendas Brasileiras",
                description = "As lendas do folclore brasileiro ganham vida!",
                eventType = CulturalEventType.FolkloreDay,
                holiday = BrazilianHoliday.DiaDoFolclore,
                eventThemeColor = new Color(0.5f, 0f, 0.8f), // Purple
                culturalReferences = new List<string> { "Curupira", "Saci", "Iara", "Boitatá", "Caipora" }
            };
            
            availableEvents["folclore_2024"] = folcloreEvent;
            
            Debug.Log($"Created {availableEvents.Count} cultural events");
        }
        
        void CheckForEvents()
        {
            DateTime now = DateTime.Now;
            
            foreach (var eventPair in availableEvents)
            {
                var culturalEvent = eventPair.Value;
                
                // Check if event should start
                if (!culturalEvent.isActive && 
                    now >= culturalEvent.startDate && 
                    now <= culturalEvent.endDate)
                {
                    StartEvent(culturalEvent);
                }
                
                // Check if event should end
                if (culturalEvent.isActive && now > culturalEvent.endDate)
                {
                    EndEvent(culturalEvent);
                }
            }
            
            // Check for seasonal events based on Brazilian calendar
            CheckSeasonalEvents(now);
        }
        
        void CheckSeasonalEvents(DateTime currentDate)
        {
            if (!enableSeasonalEvents) return;
            
            // Carnaval (February/March - variable dates)
            if (IsCarnavalSeason(currentDate) && !IsEventActive("carnaval_2024"))
            {
                ActivateSeasonalEvent("carnaval_2024", GetCarnavalDates(currentDate.Year));
            }
            
            // Festa Junina (June)
            if (currentDate.Month == 6 && !IsEventActive("festa_junina_2024"))
            {
                ActivateSeasonalEvent("festa_junina_2024", 
                    new DateTime(currentDate.Year, 6, 1), 
                    new DateTime(currentDate.Year, 6, 30));
            }
            
            // Independence Day (September 7)
            if (currentDate.Month == 9 && currentDate.Day >= 1 && currentDate.Day <= 15 && 
                !IsEventActive("independencia_2024"))
            {
                ActivateSeasonalEvent("independencia_2024",
                    new DateTime(currentDate.Year, 9, 1),
                    new DateTime(currentDate.Year, 9, 15));
            }
            
            // Folklore Day (August 22)
            if (currentDate.Month == 8 && currentDate.Day >= 15 && currentDate.Day <= 30 &&
                !IsEventActive("folclore_2024"))
            {
                ActivateSeasonalEvent("folclore_2024",
                    new DateTime(currentDate.Year, 8, 15),
                    new DateTime(currentDate.Year, 8, 30));
            }
        }
        
        bool IsCarnavalSeason(DateTime date)
        {
            // Carnaval dates vary each year - simplified check for February/March
            return (date.Month == 2 && date.Day >= 15) || (date.Month == 3 && date.Day <= 15);
        }
        
        (DateTime start, DateTime end) GetCarnavalDates(int year)
        {
            // Simplified Carnaval dates - in reality, this would be calculated based on Easter
            return (new DateTime(year, 2, 20), new DateTime(year, 2, 25));
        }
        
        void ActivateSeasonalEvent(string eventId, DateTime startDate, DateTime endDate)
        {
            if (availableEvents.ContainsKey(eventId))
            {
                var culturalEvent = availableEvents[eventId];
                culturalEvent.startDate = startDate;
                culturalEvent.endDate = endDate;
                StartEvent(culturalEvent);
            }
        }
        
        void ActivateSeasonalEvent(string eventId, (DateTime start, DateTime end) dates)
        {
            ActivateSeasonalEvent(eventId, dates.start, dates.end);
        }
        
        bool IsEventActive(string eventId)
        {
            return activeEvents.ContainsKey(eventId);
        }
        
        void StartEvent(CulturalEvent culturalEvent)
        {
            culturalEvent.isActive = true;
            activeEvents[culturalEvent.eventId] = culturalEvent;
            
            // Apply event modifiers
            ApplyEventModifiers(culturalEvent);
            
            // Notify players
            OnEventStarted?.Invoke(culturalEvent);
            
            // Play event announcement
            PlayEventAnnouncement(culturalEvent);
            
            Debug.Log($"Started cultural event: {culturalEvent.eventName}");
        }
        
        void EndEvent(CulturalEvent culturalEvent)
        {
            culturalEvent.isActive = false;
            activeEvents.Remove(culturalEvent.eventId);
            completedEvents.Add(culturalEvent);
            
            // Remove event modifiers
            RemoveEventModifiers(culturalEvent);
            
            OnEventEnded?.Invoke(culturalEvent);
            
            Debug.Log($"Ended cultural event: {culturalEvent.eventName}");
        }
        
        void ApplyEventModifiers(CulturalEvent culturalEvent)
        {
            var modifiers = culturalEvent.modifiers;
            
            if (modifiers.addCulturalMusic && !string.IsNullOrEmpty(culturalEvent.eventMusic))
            {
                // Play cultural music
                if (AudioManager.Instance != null)
                {
                    // AudioManager.Instance.PlayEventMusic(culturalEvent.eventMusic);
                }
            }
            
            if (modifiers.specialVoiceLines && !string.IsNullOrEmpty(culturalEvent.eventNarration))
            {
                // Enable special narration
                if (BrazilianNarrationSystem.Instance != null)
                {
                    BrazilianNarrationSystem.Instance.PlaySpecialEvent(culturalEvent.eventType.ToString().ToLower());
                }
            }
        }
        
        void RemoveEventModifiers(CulturalEvent culturalEvent)
        {
            // Remove any temporary modifiers applied during the event
            Debug.Log($"Removing modifiers for event: {culturalEvent.eventName}");
        }
        
        void PlayEventAnnouncement(CulturalEvent culturalEvent)
        {
            string announcement = $"🇧🇷 {culturalEvent.eventName} começou! {culturalEvent.description}";
            
            // Show in-game announcement
            if (UIManager.Instance != null)
            {
                // UIManager.Instance.ShowEventAnnouncement(announcement);
            }
            
            Debug.Log($"Event Announcement: {announcement}");
        }
        
        void CheckCurrentEvents()
        {
            DateTime now = DateTime.Now;
            
            foreach (var eventPair in availableEvents)
            {
                var culturalEvent = eventPair.Value;
                
                if (now >= culturalEvent.startDate && now <= culturalEvent.endDate && !culturalEvent.isActive)
                {
                    StartEvent(culturalEvent);
                }
            }
        }
        
        public void CompleteEventMission(string eventId, string missionId, string playerId)
        {
            if (!activeEvents.ContainsKey(eventId))
                return;
                
            var culturalEvent = activeEvents[eventId];
            var mission = culturalEvent.missions.Find(m => m.missionId == missionId);
            
            if (mission != null && !mission.isCompleted)
            {
                mission.isCompleted = true;
                
                // Give rewards
                foreach (var reward in mission.rewards)
                {
                    GiveEventReward(playerId, reward);
                }
                
                OnEventCompleted?.Invoke(culturalEvent, playerId);
                
                Debug.Log($"Player {playerId} completed mission: {mission.title}");
            }
        }
        
        void GiveEventReward(string playerId, EventReward reward)
        {
            if (EconomyManager.Instance == null) return;
            
            switch (reward.type)
            {
                case RewardType.Gems:
                    EconomyManager.Instance.AddGems(playerId, reward.quantity);
                    break;
                case RewardType.XP:
                    EconomyManager.Instance.AddXP(playerId, reward.quantity);
                    break;
                case RewardType.Skin:
                    EconomyManager.Instance.UnlockSkin(playerId, reward.itemId);
                    break;
                case RewardType.Title:
                    EconomyManager.Instance.UnlockTitle(playerId, reward.itemId);
                    break;
            }
            
            Debug.Log($"Gave event reward to {playerId}: {reward.quantity} {reward.type}");
        }
        
        void LoadEventProgress()
        {
            // Load from Firebase or PlayerPrefs
            string progressData = PlayerPrefs.GetString("EventProgress", "{}");
            
            try
            {
                // Parse and apply saved progress
                Debug.Log("Event progress loaded");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load event progress: {e.Message}");
            }
        }
        
        void SaveEventProgress()
        {
            try
            {
                // Save to Firebase and PlayerPrefs
                string progressJson = JsonUtility.ToJson(activeEvents);
                PlayerPrefs.SetString("EventProgress", progressJson);
                PlayerPrefs.Save();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save event progress: {e.Message}");
            }
        }
        
        // Public API
        public List<CulturalEvent> GetActiveEvents()
        {
            return new List<CulturalEvent>(activeEvents.Values);
        }
        
        public CulturalEvent GetEvent(string eventId)
        {
            return availableEvents.ContainsKey(eventId) ? availableEvents[eventId] : null;
        }
        
        public List<CulturalEvent> GetEventsForHoliday(BrazilianHoliday holiday)
        {
            var events = new List<CulturalEvent>();
            
            foreach (var culturalEvent in availableEvents.Values)
            {
                if (culturalEvent.holiday == holiday)
                {
                    events.Add(culturalEvent);
                }
            }
            
            return events;
        }
        
        public void UpdateMissionProgress(string eventId, string missionId, int progress, string playerId)
        {
            if (!activeEvents.ContainsKey(eventId))
                return;
                
            var culturalEvent = activeEvents[eventId];
            var mission = culturalEvent.missions.Find(m => m.missionId == missionId);
            
            if (mission != null && !mission.isCompleted)
            {
                mission.currentProgress = Mathf.Min(mission.currentProgress + progress, mission.targetValue);
                
                if (mission.currentProgress >= mission.targetValue)
                {
                    CompleteEventMission(eventId, missionId, playerId);
                }
                
                SaveEventProgress();
            }
        }
        
        public void ForceStartEvent(string eventId, int durationHours = 24)
        {
            if (availableEvents.ContainsKey(eventId))
            {
                var culturalEvent = availableEvents[eventId];
                culturalEvent.startDate = DateTime.Now;
                culturalEvent.endDate = DateTime.Now.AddHours(durationHours);
                StartEvent(culturalEvent);
            }
        }
        
        public bool IsHolidayToday(BrazilianHoliday holiday)
        {
            DateTime today = DateTime.Now;
            
            switch (holiday)
            {
                case BrazilianHoliday.DiaIndependencia:
                    return today.Month == 9 && today.Day == 7;
                case BrazilianHoliday.DiaDoFolclore:
                    return today.Month == 8 && today.Day == 22;
                case BrazilianHoliday.Natal:
                    return today.Month == 12 && today.Day == 25;
                case BrazilianHoliday.AnoNovo:
                    return today.Month == 1 && today.Day == 1;
                default:
                    return false;
            }
        }
    }
}
