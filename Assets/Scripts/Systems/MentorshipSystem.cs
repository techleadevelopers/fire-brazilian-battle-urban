
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System;
using ArenaBrasil.Backend;
using ArenaBrasil.Analytics;

namespace ArenaBrasil.Social
{
    public class MentorshipSystem : MonoBehaviour
    {
        public static MentorshipSystem Instance { get; private set; }
        
        [Header("Mentorship Configuration")]
        public int minLevelToMentor = 50;
        public int minMatchesToMentor = 100;
        public float minWinRateToMentor = 0.15f;
        public int maxStudentsPerMentor = 5;
        public int rewardGemsPerSession = 50;
        
        [Header("Coaching Features")]
        public bool enableVoiceChat = true;
        public bool enableScreenShare = false;
        public bool enableReplayAnalysis = true;
        
        private Dictionary<string, MentorProfile> mentors = new Dictionary<string, MentorProfile>();
        private Dictionary<string, StudentProfile> students = new Dictionary<string, StudentProfile>();
        private Dictionary<string, MentorshipSession> activeSessions = new Dictionary<string, MentorshipSession>();
        
        // Events
        public event Action<MentorshipSession> OnSessionStarted;
        public event Action<MentorshipSession> OnSessionEnded;
        public event Action<string, string> OnMentorshipRequested;
        public event Action<MentorProfile> OnMentorRegistered;
        
        [System.Serializable]
        public class MentorProfile
        {
            public string userId;
            public string displayName;
            public int level;
            public float winRate;
            public int totalMatches;
            public List<string> specialties = new List<string>();
            public float rating;
            public int totalStudents;
            public int sessionsCompleted;
            public List<string> currentStudents = new List<string>();
            public List<string> languages = new List<string>();
            public MentorshipAvailability availability = new MentorshipAvailability();
            public bool isActive;
            public DateTime lastActiveDate;
            public List<StudentFeedback> feedback = new List<StudentFeedback>();
        }
        
        [System.Serializable]
        public class StudentProfile
        {
            public string userId;
            public string displayName;
            public int level;
            public List<string> areasToImprove = new List<string>();
            public string currentMentorId;
            public List<string> pastMentors = new List<string>();
            public int sessionsAttended;
            public List<string> goals = new List<string>();
            public DateTime joinDate;
            public bool isLookingForMentor;
        }
        
        [System.Serializable]
        public class MentorshipSession
        {
            public string sessionId;
            public string mentorId;
            public string studentId;
            public DateTime startTime;
            public DateTime endTime;
            public float duration;
            public string sessionType; // "individual", "group", "replay_analysis"
            public List<string> topicsCovered = new List<string>();
            public string notes;
            public int studentRating;
            public int mentorRating;
            public bool completed;
            public SessionRecording recording = new SessionRecording();
        }
        
        [System.Serializable]
        public class MentorshipAvailability
        {
            public List<TimeSlot> weeklySchedule = new List<TimeSlot>();
            public string timezone;
            public bool isAvailableNow;
        }
        
        [System.Serializable]
        public class TimeSlot
        {
            public DayOfWeek dayOfWeek;
            public int startHour;
            public int endHour;
            public bool isAvailable;
        }
        
        [System.Serializable]
        public class StudentFeedback
        {
            public string studentId;
            public int rating;
            public string comment;
            public DateTime date;
            public List<string> tags = new List<string>();
        }
        
        [System.Serializable]
        public class SessionRecording
        {
            public List<Vector3> playerPositions = new List<Vector3>();
            public List<string> actionsTaken = new List<string>();
            public List<string> mentorComments = new List<string>();
            public string replayFileId;
        }
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeMentorshipSystem();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void InitializeMentorshipSystem()
        {
            Debug.Log("Arena Brasil - Initializing Mentorship System");
            LoadMentorshipData();
            StartCoroutine(UpdateMentorAvailability());
        }
        
        System.Collections.IEnumerator UpdateMentorAvailability()
        {
            while (true)
            {
                UpdateAllMentorAvailability();
                yield return new WaitForSeconds(60f); // Update every minute
            }
        }
        
        public bool RegisterAsMentor(string userId)
        {
            var playerData = PlayerAnalytics.Instance?.GetPlayerData(userId);
            if (playerData == null)
            {
                Debug.LogWarning("Player data not found for mentor registration");
                return false;
            }
            
            // Check qualifications
            if (playerData.totalMatches < minMatchesToMentor ||
                playerData.winRate < minWinRateToMentor)
            {
                Debug.LogWarning($"Player {userId} doesn't meet mentor requirements");
                return false;
            }
            
            var mentorProfile = new MentorProfile
            {
                userId = userId,
                displayName = GetPlayerDisplayName(userId),
                level = GetPlayerLevel(userId),
                winRate = playerData.winRate,
                totalMatches = playerData.totalMatches,
                isActive = true,
                lastActiveDate = DateTime.Now
            };
            
            // Determine specialties based on player performance
            mentorProfile.specialties = DetermineSpecialties(playerData);
            mentorProfile.languages.Add("pt-BR");
            
            mentors[userId] = mentorProfile;
            SaveMentorshipData();
            
            OnMentorRegistered?.Invoke(mentorProfile);
            
            Debug.Log($"Player {userId} registered as mentor");
            return true;
        }
        
        List<string> DetermineSpecialties(PlayerAnalytics.PlayerAnalyticsData playerData)
        {
            var specialties = new List<string>();
            
            if (playerData.kdrRatio > 2.0f)
                specialties.Add("Combat");
            
            if (playerData.averagePlacement <= 10)
                specialties.Add("Positioning");
            
            if (playerData.behavior.itemsLooted > 1000)
                specialties.Add("Looting");
            
            if (playerData.behavior.survivalRate > 0.8f)
                specialties.Add("Survival");
            
            if (specialties.Count == 0)
                specialties.Add("General");
            
            return specialties;
        }
        
        public void RegisterAsStudent(string userId, List<string> areasToImprove)
        {
            var studentProfile = new StudentProfile
            {
                userId = userId,
                displayName = GetPlayerDisplayName(userId),
                level = GetPlayerLevel(userId),
                areasToImprove = areasToImprove,
                joinDate = DateTime.Now,
                isLookingForMentor = true
            };
            
            students[userId] = studentProfile;
            SaveMentorshipData();
            
            Debug.Log($"Player {userId} registered as student");
        }
        
        public List<MentorProfile> FindAvailableMentors(string studentId, List<string> preferredSpecialties = null)
        {
            var availableMentors = new List<MentorProfile>();
            
            foreach (var mentor in mentors.Values)
            {
                if (!mentor.isActive || mentor.currentStudents.Count >= maxStudentsPerMentor)
                    continue;
                
                if (preferredSpecialties != null && preferredSpecialties.Count > 0)
                {
                    bool hasPreferredSpecialty = false;
                    foreach (var specialty in preferredSpecialties)
                    {
                        if (mentor.specialties.Contains(specialty))
                        {
                            hasPreferredSpecialty = true;
                            break;
                        }
                    }
                    if (!hasPreferredSpecialty) continue;
                }
                
                if (mentor.availability.isAvailableNow)
                {
                    availableMentors.Add(mentor);
                }
            }
            
            // Sort by rating
            availableMentors.Sort((a, b) => b.rating.CompareTo(a.rating));
            
            return availableMentors;
        }
        
        public void RequestMentorship(string studentId, string mentorId)
        {
            if (!mentors.ContainsKey(mentorId) || !students.ContainsKey(studentId))
            {
                Debug.LogWarning("Invalid mentor or student ID");
                return;
            }
            
            var mentor = mentors[mentorId];
            if (mentor.currentStudents.Count >= maxStudentsPerMentor)
            {
                Debug.LogWarning("Mentor has reached maximum student capacity");
                return;
            }
            
            OnMentorshipRequested?.Invoke(studentId, mentorId);
            
            Debug.Log($"Mentorship requested: Student {studentId} -> Mentor {mentorId}");
        }
        
        public string StartMentorshipSession(string mentorId, string studentId, string sessionType = "individual")
        {
            string sessionId = Guid.NewGuid().ToString();
            
            var session = new MentorshipSession
            {
                sessionId = sessionId,
                mentorId = mentorId,
                studentId = studentId,
                startTime = DateTime.Now,
                sessionType = sessionType
            };
            
            activeSessions[sessionId] = session;
            
            // Add student to mentor's current students if not already
            var mentor = mentors[mentorId];
            if (!mentor.currentStudents.Contains(studentId))
            {
                mentor.currentStudents.Add(studentId);
            }
            
            // Update student's mentor
            var student = students[studentId];
            student.currentMentorId = mentorId;
            
            OnSessionStarted?.Invoke(session);
            
            Debug.Log($"Mentorship session started: {sessionId}");
            return sessionId;
        }
        
        public void EndMentorshipSession(string sessionId, List<string> topicsCovered, string notes = "")
        {
            if (!activeSessions.ContainsKey(sessionId))
            {
                Debug.LogWarning($"Session not found: {sessionId}");
                return;
            }
            
            var session = activeSessions[sessionId];
            session.endTime = DateTime.Now;
            session.duration = (float)(session.endTime - session.startTime).TotalMinutes;
            session.topicsCovered = topicsCovered;
            session.notes = notes;
            session.completed = true;
            
            // Update mentor stats
            var mentor = mentors[session.mentorId];
            mentor.sessionsCompleted++;
            
            // Update student stats
            var student = students[session.studentId];
            student.sessionsAttended++;
            
            // Give rewards
            GiveSessionRewards(session.mentorId, session.studentId);
            
            activeSessions.Remove(sessionId);
            OnSessionEnded?.Invoke(session);
            
            Debug.Log($"Mentorship session ended: {sessionId}, Duration: {session.duration} minutes");
        }
        
        void GiveSessionRewards(string mentorId, string studentId)
        {
            // Reward mentor
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.AddGems(mentorId, rewardGemsPerSession);
                EconomyManager.Instance.AddXP(mentorId, 100);
                
                // Give student smaller reward
                EconomyManager.Instance.AddXP(studentId, 50);
            }
        }
        
        public void RateSession(string sessionId, int rating, bool isFromStudent, string comment = "")
        {
            // Find completed session in database
            // For now, we'll just update the mentor's rating
            
            if (isFromStudent)
            {
                // Find mentor from session and update rating
                // This would typically come from a database query
                Debug.Log($"Student rated session {sessionId}: {rating}/5");
            }
            else
            {
                Debug.Log($"Mentor rated session {sessionId}: {rating}/5");
            }
        }
        
        void UpdateAllMentorAvailability()
        {
            DateTime now = DateTime.Now;
            
            foreach (var mentor in mentors.Values)
            {
                mentor.availability.isAvailableNow = IsMentorAvailableNow(mentor, now);
            }
        }
        
        bool IsMentorAvailableNow(MentorProfile mentor, DateTime currentTime)
        {
            var currentDay = currentTime.DayOfWeek;
            var currentHour = currentTime.Hour;
            
            foreach (var timeSlot in mentor.availability.weeklySchedule)
            {
                if (timeSlot.dayOfWeek == currentDay &&
                    currentHour >= timeSlot.startHour &&
                    currentHour < timeSlot.endHour &&
                    timeSlot.isAvailable)
                {
                    return true;
                }
            }
            
            return false;
        }
        
        void LoadMentorshipData()
        {
            // Load from Firebase/PlayerPrefs
            string mentorData = PlayerPrefs.GetString("MentorData", "{}");
            string studentData = PlayerPrefs.GetString("StudentData", "{}");
            
            try
            {
                // Parse JSON data
                Debug.Log("Mentorship data loaded");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load mentorship data: {e.Message}");
            }
        }
        
        void SaveMentorshipData()
        {
            try
            {
                // Save to Firebase/PlayerPrefs
                string mentorJson = JsonUtility.ToJson(mentors);
                string studentJson = JsonUtility.ToJson(students);
                
                PlayerPrefs.SetString("MentorData", mentorJson);
                PlayerPrefs.SetString("StudentData", studentJson);
                PlayerPrefs.Save();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save mentorship data: {e.Message}");
            }
        }
        
        // Utility methods
        string GetPlayerDisplayName(string userId)
        {
            // Get from player profile
            return $"Jogador_{userId.Substring(0, Math.Min(4, userId.Length))}";
        }
        
        int GetPlayerLevel(string userId)
        {
            var playerData = PlayerAnalytics.Instance?.GetPlayerData(userId);
            return playerData?.totalMatches / 10 ?? 1; // Simple level calculation
        }
        
        // Public API
        public MentorProfile GetMentorProfile(string userId)
        {
            return mentors.ContainsKey(userId) ? mentors[userId] : null;
        }
        
        public StudentProfile GetStudentProfile(string userId)
        {
            return students.ContainsKey(userId) ? students[userId] : null;
        }
        
        public List<MentorshipSession> GetUserSessions(string userId)
        {
            var sessions = new List<MentorshipSession>();
            
            foreach (var session in activeSessions.Values)
            {
                if (session.mentorId == userId || session.studentId == userId)
                {
                    sessions.Add(session);
                }
            }
            
            return sessions;
        }
        
        public void SetMentorAvailability(string mentorId, List<TimeSlot> schedule)
        {
            if (mentors.ContainsKey(mentorId))
            {
                mentors[mentorId].availability.weeklySchedule = schedule;
                SaveMentorshipData();
            }
        }
        
        public void ToggleMentorActive(string mentorId, bool isActive)
        {
            if (mentors.ContainsKey(mentorId))
            {
                mentors[mentorId].isActive = isActive;
                mentors[mentorId].lastActiveDate = DateTime.Now;
                SaveMentorshipData();
            }
        }
    }
}
