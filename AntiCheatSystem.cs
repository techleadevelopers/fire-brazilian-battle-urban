
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace ArenaBrasil.Security
{
    public class AntiCheatSystem : MonoBehaviour
    {
        public static AntiCheatSystem Instance { get; private set; }
        
        [Header("Anti-Cheat Configuration")]
        public bool enableAntiCheat = true;
        public float positionCheckInterval = 0.1f;
        public float maxAllowedSpeed = 15f;
        public float maxAllowedAcceleration = 50f;
        public int maxViolationsBeforeKick = 5;
        
        [Header("Detection Settings")]
        public bool detectSpeedHacks = true;
        public bool detectTeleportHacks = true;
        public bool detectAimBot = true;
        public bool detectWallHacks = true;
        public bool detectProcessInjection = true;
        
        // Player monitoring data
        private Dictionary<ulong, PlayerSecurityData> playerSecurityData = new Dictionary<ulong, PlayerSecurityData>();
        private Dictionary<string, float> suspiciousProcesses = new Dictionary<string, float>();
        
        // Security events
        public event System.Action<ulong, CheatType, string> OnCheatDetected;
        public event System.Action<ulong, int> OnViolationWarning;
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeAntiCheat();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void InitializeAntiCheat()
        {
            if (!enableAntiCheat) return;
            
            UnityEngine.Debug.Log("Arena Brasil - Initializing Anti-Cheat System");
            
            SetupSuspiciousProcessList();
            
            if (detectProcessInjection)
            {
                InvokeRepeating(nameof(CheckForSuspiciousProcesses), 5f, 30f);
            }
            
            InvokeRepeating(nameof(PerformSecurityChecks), 1f, 5f);
        }
        
        void SetupSuspiciousProcessList()
        {
            // Known cheat engine and hacking tool process names
            suspiciousProcesses = new Dictionary<string, float>
            {
                { "cheatengine", 1.0f },
                { "cheat engine", 1.0f },
                { "artmoney", 0.9f },
                { "speedhack", 1.0f },
                { "gameguardian", 1.0f },
                { "x64dbg", 0.8f },
                { "ollydbg", 0.8f },
                { "wireshark", 0.6f },
                { "fiddler", 0.5f },
                { "processhacker", 0.7f },
                { "injector", 0.9f },
                { "trainer", 0.8f }
            };
        }
        
        public void RegisterPlayer(ulong playerId)
        {
            if (!enableAntiCheat) return;
            
            playerSecurityData[playerId] = new PlayerSecurityData
            {
                playerId = playerId,
                lastPosition = Vector3.zero,
                lastValidTime = Time.time,
                violationCount = 0,
                suspicionLevel = 0f
            };
        }
        
        public void UpdatePlayerPosition(ulong playerId, Vector3 position, Vector3 velocity)
        {
            if (!enableAntiCheat || !playerSecurityData.ContainsKey(playerId)) return;
            
            var data = playerSecurityData[playerId];
            
            if (detectSpeedHacks)
            {
                CheckSpeedHack(playerId, position, velocity, data);
            }
            
            if (detectTeleportHacks)
            {
                CheckTeleportHack(playerId, position, data);
            }
            
            data.lastPosition = position;
            data.lastValidTime = Time.time;
        }
        
        void CheckSpeedHack(ulong playerId, Vector3 position, Vector3 velocity, PlayerSecurityData data)
        {
            float speed = velocity.magnitude;
            
            if (speed > maxAllowedSpeed)
            {
                data.speedViolations++;
                data.suspicionLevel += 0.2f;
                
                ReportViolation(playerId, CheatType.SpeedHack, 
                    $"Speed: {speed:F2} (Max: {maxAllowedSpeed:F2})");
                
                UnityEngine.Debug.LogWarning($"Speed hack detected for player {playerId}: {speed:F2} m/s");
            }
            
            // Check acceleration
            float deltaTime = Time.time - data.lastValidTime;
            if (deltaTime > 0 && data.lastPosition != Vector3.zero)
            {
                Vector3 displacement = position - data.lastPosition;
                float actualSpeed = displacement.magnitude / deltaTime;
                
                if (actualSpeed > maxAllowedSpeed * 1.5f) // Allow some tolerance
                {
                    data.accelerationViolations++;
                    data.suspicionLevel += 0.3f;
                    
                    ReportViolation(playerId, CheatType.SpeedHack, 
                        $"Excessive acceleration detected: {actualSpeed:F2} m/s");
                }
            }
        }
        
        void CheckTeleportHack(ulong playerId, Vector3 position, PlayerSecurityData data)
        {
            if (data.lastPosition == Vector3.zero) return;
            
            float distance = Vector3.Distance(position, data.lastPosition);
            float deltaTime = Time.time - data.lastValidTime;
            
            if (deltaTime > 0)
            {
                float maxPossibleDistance = maxAllowedSpeed * deltaTime * 2f; // Allow some tolerance
                
                if (distance > maxPossibleDistance && deltaTime < 1f) // Teleport detection
                {
                    data.teleportViolations++;
                    data.suspicionLevel += 0.5f;
                    
                    ReportViolation(playerId, CheatType.Teleport, 
                        $"Teleport detected: {distance:F2}m in {deltaTime:F2}s");
                    
                    UnityEngine.Debug.LogWarning($"Teleport hack detected for player {playerId}");
                }
            }
        }
        
        public void CheckAimbotSuspicion(ulong playerId, Vector3 targetPosition, float accuracy, float reactionTime)
        {
            if (!enableAntiCheat || !detectAimBot || !playerSecurityData.ContainsKey(playerId)) return;
            
            var data = playerSecurityData[playerId];
            
            // Track accuracy patterns
            data.shotAccuracy.Add(accuracy);
            data.reactionTimes.Add(reactionTime);
            
            // Keep only recent data
            if (data.shotAccuracy.Count > 20)
            {
                data.shotAccuracy.RemoveAt(0);
                data.reactionTimes.RemoveAt(0);
            }
            
            // Analyze patterns
            if (data.shotAccuracy.Count >= 10)
            {
                float avgAccuracy = GetAverageAccuracy(data.shotAccuracy);
                float avgReactionTime = GetAverageReactionTime(data.reactionTimes);
                
                // Suspiciously high accuracy with low reaction time
                if (avgAccuracy > 0.9f && avgReactionTime < 0.1f)
                {
                    data.aimbotViolations++;
                    data.suspicionLevel += 0.4f;
                    
                    ReportViolation(playerId, CheatType.Aimbot, 
                        $"Suspicious aim: {avgAccuracy:P1} accuracy, {avgReactionTime:F3}s reaction");
                }
                
                // Perfect headshot patterns
                if (avgAccuracy > 0.95f && data.consecutiveHeadshots > 10)
                {
                    data.aimbotViolations++;
                    data.suspicionLevel += 0.6f;
                    
                    ReportViolation(playerId, CheatType.Aimbot, 
                        $"Perfect aim pattern detected: {data.consecutiveHeadshots} consecutive headshots");
                }
            }
        }
        
        public void CheckWallHackSuspicion(ulong playerId, Vector3 shotOrigin, Vector3 targetPosition, bool targetVisible)
        {
            if (!enableAntiCheat || !detectWallHacks || !playerSecurityData.ContainsKey(playerId)) return;
            
            var data = playerSecurityData[playerId];
            
            if (!targetVisible)
            {
                data.wallhackViolations++;
                data.suspicionLevel += 0.3f;
                
                ReportViolation(playerId, CheatType.WallHack, 
                    "Shot fired at non-visible target");
                
                UnityEngine.Debug.LogWarning($"Wall hack suspected for player {playerId}");
            }
        }
        
        void CheckForSuspiciousProcesses()
        {
            if (!enableAntiCheat || !detectProcessInjection) return;
            
            try
            {
                Process[] processes = Process.GetProcesses();
                
                foreach (Process process in processes)
                {
                    string processName = process.ProcessName.ToLower();
                    
                    foreach (var suspiciousProcess in suspiciousProcesses)
                    {
                        if (processName.Contains(suspiciousProcess.Key))
                        {
                            float suspicionIncrease = suspiciousProcess.Value;
                            
                            ReportSecurityThreat(CheatType.ProcessInjection, 
                                $"Suspicious process detected: {process.ProcessName}");
                            
                            UnityEngine.Debug.LogError($"Suspicious process detected: {process.ProcessName}");
                            
                            // Take action based on severity
                            if (suspicionIncrease >= 0.9f)
                            {
                                // High severity - immediate kick
                                KickLocalPlayer("Cheat software detected");
                                return;
                            }
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"Error checking processes: {e.Message}");
            }
        }
        
        void PerformSecurityChecks()
        {
            if (!enableAntiCheat) return;
            
            CheckMemoryIntegrity();
            CheckTimeManipulation();
            CheckDebuggerAttachment();
        }
        
        void CheckMemoryIntegrity()
        {
            // Basic memory integrity check
            string gameVersion = Application.version;
            string expectedHash = GetExpectedVersionHash();
            string actualHash = ComputeStringHash(gameVersion);
            
            if (actualHash != expectedHash)
            {
                ReportSecurityThreat(CheatType.MemoryModification, 
                    "Game version integrity compromised");
            }
        }
        
        void CheckTimeManipulation()
        {
            // Check for time manipulation by comparing different time sources
            float realtimeSinceStartup = Time.realtimeSinceStartup;
            float unscaledTime = Time.unscaledTime;
            
            if (Mathf.Abs(realtimeSinceStartup - unscaledTime) > 5f)
            {
                ReportSecurityThreat(CheatType.TimeManipulation, 
                    "Time manipulation detected");
            }
        }
        
        void CheckDebuggerAttachment()
        {
            // Simple debugger detection
            if (System.Diagnostics.Debugger.IsAttached)
            {
                ReportSecurityThreat(CheatType.DebuggerAttached, 
                    "Debugger attachment detected");
            }
        }
        
        void ReportViolation(ulong playerId, CheatType cheatType, string details)
        {
            if (!playerSecurityData.ContainsKey(playerId)) return;
            
            var data = playerSecurityData[playerId];
            data.violationCount++;
            
            OnCheatDetected?.Invoke(playerId, cheatType, details);
            
            UnityEngine.Debug.LogWarning($"Cheat violation for player {playerId}: {cheatType} - {details}");
            
            if (data.violationCount >= maxViolationsBeforeKick)
            {
                KickPlayer(playerId, $"Multiple cheat violations: {cheatType}");
            }
            else
            {
                OnViolationWarning?.Invoke(playerId, data.violationCount);
            }
        }
        
        void ReportSecurityThreat(CheatType cheatType, string details)
        {
            OnCheatDetected?.Invoke(NetworkManager.Singleton.LocalClientId, cheatType, details);
            
            UnityEngine.Debug.LogError($"Security threat detected: {cheatType} - {details}");
            
            // Local player threat - take immediate action
            if (cheatType == CheatType.ProcessInjection || cheatType == CheatType.DebuggerAttached)
            {
                KickLocalPlayer($"Security violation: {cheatType}");
            }
        }
        
        void KickPlayer(ulong playerId, string reason)
        {
            UnityEngine.Debug.LogError($"Kicking player {playerId}: {reason}");
            
            // This would integrate with NetworkManager to kick the player
            // NetworkManager.Singleton.DisconnectClient(playerId);
        }
        
        void KickLocalPlayer(string reason)
        {
            UnityEngine.Debug.LogError($"Kicking local player: {reason}");
            
            // Disconnect from server
            if (NetworkManager.Singleton.IsClient)
            {
                NetworkManager.Singleton.Shutdown();
            }
            
            // Show kick message
            // UIManager.Instance?.ShowKickMessage(reason);
        }
        
        float GetAverageAccuracy(List<float> accuracyList)
        {
            if (accuracyList.Count == 0) return 0f;
            
            float sum = 0f;
            foreach (float accuracy in accuracyList)
            {
                sum += accuracy;
            }
            return sum / accuracyList.Count;
        }
        
        float GetAverageReactionTime(List<float> reactionTimeList)
        {
            if (reactionTimeList.Count == 0) return 0f;
            
            float sum = 0f;
            foreach (float time in reactionTimeList)
            {
                sum += time;
            }
            return sum / reactionTimeList.Count;
        }
        
        string GetExpectedVersionHash()
        {
            // This would be a predefined hash of the game version
            return ComputeStringHash(Application.version + "ArenaBrasil2024");
        }
        
        string ComputeStringHash(string input)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(input));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
        
        // Public getters
        public float GetPlayerSuspicionLevel(ulong playerId)
        {
            return playerSecurityData.ContainsKey(playerId) ? playerSecurityData[playerId].suspicionLevel : 0f;
        }
        
        public int GetPlayerViolationCount(ulong playerId)
        {
            return playerSecurityData.ContainsKey(playerId) ? playerSecurityData[playerId].violationCount : 0;
        }
    }
    
    [System.Serializable]
    public class PlayerSecurityData
    {
        public ulong playerId;
        public Vector3 lastPosition;
        public float lastValidTime;
        public int violationCount;
        public float suspicionLevel;
        
        // Specific violation counters
        public int speedViolations;
        public int teleportViolations;
        public int aimbotViolations;
        public int wallhackViolations;
        public int accelerationViolations;
        
        // Aimbot detection data
        public List<float> shotAccuracy = new List<float>();
        public List<float> reactionTimes = new List<float>();
        public int consecutiveHeadshots;
    }
    
    public enum CheatType
    {
        SpeedHack,
        Teleport,
        Aimbot,
        WallHack,
        ProcessInjection,
        MemoryModification,
        TimeManipulation,
        DebuggerAttached,
        NetworkManipulation
    }
}
