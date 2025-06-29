
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System;
using System.IO;

namespace ArenaBrasil.Replay
{
    public class ReplaySystem : NetworkBehaviour
    {
        public static ReplaySystem Instance { get; private set; }
        
        [Header("Replay Configuration")]
        public bool enableReplayRecording = true;
        public float maxReplayDuration = 300f; // 5 minutes
        public float recordingInterval = 0.1f; // 10 FPS
        public int maxStoredReplays = 10;
        
        [Header("Clip System")]
        public float clipDuration = 30f; // 30 seconds
        public int maxClipsPerMatch = 5;
        
        [Header("Brazilian Content")]
        public string[] brazilianClipTags = {
            "GOLAÇO!", "JOGADA LINDA!", "SHOW DE BOLA!",
            "MITOU!", "QUEBROU TUDO!", "ARRASOU!"
        };
        
        private List<ReplayFrame> currentReplayFrames = new List<ReplayFrame>();
        private List<GameClip> savedClips = new List<GameClip>();
        private bool isRecording = false;
        private float recordingTimer = 0f;
        
        public event Action<GameClip> OnClipCreated;
        public event Action<ReplayData> OnReplayRecorded;
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeReplaySystem();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void InitializeReplaySystem()
        {
            Debug.Log("Arena Brasil - Inicializando sistema de replay");
            
            LoadSavedReplays();
            SetupAutoClipTriggers();
        }
        
        void Update()
        {
            if (isRecording && enableReplayRecording)
            {
                recordingTimer += Time.deltaTime;
                
                if (recordingTimer >= recordingInterval)
                {
                    RecordFrame();
                    recordingTimer = 0f;
                }
            }
        }
        
        public void StartRecording()
        {
            if (!enableReplayRecording) return;
            
            isRecording = true;
            currentReplayFrames.Clear();
            recordingTimer = 0f;
            
            Debug.Log("Arena Brasil - Gravação de replay iniciada");
        }
        
        public void StopRecording()
        {
            if (!isRecording) return;
            
            isRecording = false;
            
            if (currentReplayFrames.Count > 0)
            {
                SaveReplay();
            }
            
            Debug.Log("Arena Brasil - Gravação de replay finalizada");
        }
        
        void RecordFrame()
        {
            var allPlayers = FindObjectsOfType<PlayerController>();
            var frameData = new ReplayFrame
            {
                timestamp = Time.time,
                playerStates = new List<PlayerState>()
            };
            
            foreach (var player in allPlayers)
            {
                if (player.IsSpawned)
                {
                    var playerState = new PlayerState
                    {
                        playerId = player.OwnerClientId,
                        position = player.transform.position,
                        rotation = player.transform.rotation,
                        health = player.GetComponent<CombatSystem>()?.GetHealth() ?? 100f,
                        weaponId = player.GetComponent<WeaponController>()?.GetCurrentWeaponId() ?? "",
                        actionType = GetPlayerActionType(player)
                    };
                    
                    frameData.playerStates.Add(playerState);
                }
            }
            
            currentReplayFrames.Add(frameData);
            
            // Remove old frames if exceeding max duration
            float oldestTime = Time.time - maxReplayDuration;
            currentReplayFrames.RemoveAll(frame => frame.timestamp < oldestTime);
        }
        
        string GetPlayerActionType(PlayerController player)
        {
            // Detect what the player is doing
            if (player.GetComponent<Rigidbody>().velocity.magnitude > 5f)
                return "running";
            if (Input.GetButton("Fire1"))
                return "shooting";
            if (player.transform.position.y > 10f)
                return "parachuting";
            
            return "idle";
        }
        
        void SaveReplay()
        {
            var replayData = new ReplayData
            {
                replayId = Guid.NewGuid().ToString(),
                matchId = ArenaBrasilGameManager.Instance?.GetCurrentMatchId() ?? "unknown",
                recordingDate = DateTime.Now,
                duration = currentReplayFrames.Count * recordingInterval,
                frames = new List<ReplayFrame>(currentReplayFrames),
                playerName = GetLocalPlayerName(),
                matchResult = GetMatchResult()
            };
            
            // Save to local storage
            SaveReplayToFile(replayData);
            
            OnReplayRecorded?.Invoke(replayData);
        }
        
        void SaveReplayToFile(ReplayData replayData)
        {
            try
            {
                string replayDirectory = Path.Combine(Application.persistentDataPath, "Replays");
                if (!Directory.Exists(replayDirectory))
                {
                    Directory.CreateDirectory(replayDirectory);
                }
                
                string fileName = $"replay_{replayData.replayId}.json";
                string filePath = Path.Combine(replayDirectory, fileName);
                
                string jsonData = JsonUtility.ToJson(replayData, true);
                File.WriteAllText(filePath, jsonData);
                
                Debug.Log($"Replay salvo: {filePath}");
                
                // Manage storage limit
                ManageReplayStorage();
            }
            catch (Exception e)
            {
                Debug.LogError($"Erro ao salvar replay: {e.Message}");
            }
        }
        
        void LoadSavedReplays()
        {
            try
            {
                string replayDirectory = Path.Combine(Application.persistentDataPath, "Replays");
                if (Directory.Exists(replayDirectory))
                {
                    string[] replayFiles = Directory.GetFiles(replayDirectory, "*.json");
                    Debug.Log($"Encontrados {replayFiles.Length} replays salvos");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Erro ao carregar replays: {e.Message}");
            }
        }
        
        void ManageReplayStorage()
        {
            string replayDirectory = Path.Combine(Application.persistentDataPath, "Replays");
            string[] replayFiles = Directory.GetFiles(replayDirectory, "*.json");
            
            if (replayFiles.Length > maxStoredReplays)
            {
                // Sort by creation date and remove oldest
                Array.Sort(replayFiles, (a, b) => File.GetCreationTime(a).CompareTo(File.GetCreationTime(b)));
                
                int filesToRemove = replayFiles.Length - maxStoredReplays;
                for (int i = 0; i < filesToRemove; i++)
                {
                    File.Delete(replayFiles[i]);
                    Debug.Log($"Replay antigo removido: {replayFiles[i]}");
                }
            }
        }
        
        public void CreateClip(ClipTriggerType triggerType, string description = "")
        {
            if (savedClips.Count >= maxClipsPerMatch) return;
            
            var clipFrames = GetRecentFrames(clipDuration);
            if (clipFrames.Count == 0) return;
            
            var clip = new GameClip
            {
                clipId = Guid.NewGuid().ToString(),
                triggerType = triggerType,
                description = description,
                timestamp = DateTime.Now,
                duration = clipDuration,
                frames = clipFrames,
                playerName = GetLocalPlayerName(),
                brazilianTag = GetRandomBrazilianTag()
            };
            
            savedClips.Add(clip);
            OnClipCreated?.Invoke(clip);
            
            Debug.Log($"Clip criado: {clip.brazilianTag} - {description}");
        }
        
        List<ReplayFrame> GetRecentFrames(float duration)
        {
            float startTime = Time.time - duration;
            return currentReplayFrames.FindAll(frame => frame.timestamp >= startTime);
        }
        
        string GetRandomBrazilianTag()
        {
            return brazilianClipTags[UnityEngine.Random.Range(0, brazilianClipTags.Length)];
        }
        
        void SetupAutoClipTriggers()
        {
            // Auto-create clips for impressive moments
            
            // Subscribe to combat events
            if (CombatSystem.Instance != null)
            {
                // CombatSystem.Instance.OnMultiKill += (killCount) => {
                //     if (killCount >= 3)
                //         CreateClip(ClipTriggerType.MultiKill, $"Multi-kill x{killCount}!");
                // };
            }
            
            // Subscribe to match events
            if (ArenaBrasilGameManager.Instance != null)
            {
                // ArenaBrasilGameManager.Instance.OnVictoryRoyale += () => {
                //     CreateClip(ClipTriggerType.Victory, "VITÓRIA REAL!");
                // };
            }
        }
        
        public void ShareClip(string clipId, SocialPlatform platform)
        {
            var clip = savedClips.Find(c => c.clipId == clipId);
            if (clip == null) return;
            
            string shareText = $"🇧🇷 {clip.brazilianTag} Confira minha jogada épica no Arena Brasil! #ArenaBrasil #BatalhadeLendas";
            
            switch (platform)
            {
                case SocialPlatform.TikTok:
                    ShareToTikTok(clip, shareText);
                    break;
                case SocialPlatform.Instagram:
                    ShareToInstagram(clip, shareText);
                    break;
                case SocialPlatform.Twitter:
                    ShareToTwitter(clip, shareText);
                    break;
                case SocialPlatform.WhatsApp:
                    ShareToWhatsApp(clip, shareText);
                    break;
            }
        }
        
        void ShareToTikTok(GameClip clip, string text)
        {
            Debug.Log($"Compartilhando no TikTok: {text}");
            // Integration with TikTok API would go here
        }
        
        void ShareToInstagram(GameClip clip, string text)
        {
            Debug.Log($"Compartilhando no Instagram: {text}");
            // Integration with Instagram API would go here
        }
        
        void ShareToTwitter(GameClip clip, string text)
        {
            Debug.Log($"Compartilhando no Twitter: {text}");
            // Integration with Twitter API would go here
        }
        
        void ShareToWhatsApp(GameClip clip, string text)
        {
            Debug.Log($"Compartilhando no WhatsApp: {text}");
            // WhatsApp sharing via intent
        }
        
        public ReplayData LoadReplay(string replayId)
        {
            try
            {
                string replayDirectory = Path.Combine(Application.persistentDataPath, "Replays");
                string filePath = Path.Combine(replayDirectory, $"replay_{replayId}.json");
                
                if (File.Exists(filePath))
                {
                    string jsonData = File.ReadAllText(filePath);
                    return JsonUtility.FromJson<ReplayData>(jsonData);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Erro ao carregar replay: {e.Message}");
            }
            
            return null;
        }
        
        string GetLocalPlayerName()
        {
            if (FirebaseBackendService.Instance?.CurrentUser != null)
            {
                return FirebaseBackendService.Instance.CurrentUser.DisplayName ?? "Jogador";
            }
            return "Jogador";
        }
        
        string GetMatchResult()
        {
            // Get current match result
            return "Em progresso";
        }
        
        public List<GameClip> GetSavedClips() => savedClips;
        public bool IsRecording() => isRecording;
    }
    
    [Serializable]
    public class ReplayData
    {
        public string replayId;
        public string matchId;
        public DateTime recordingDate;
        public float duration;
        public List<ReplayFrame> frames;
        public string playerName;
        public string matchResult;
    }
    
    [Serializable]
    public class ReplayFrame
    {
        public float timestamp;
        public List<PlayerState> playerStates;
    }
    
    [Serializable]
    public class PlayerState
    {
        public ulong playerId;
        public Vector3 position;
        public Quaternion rotation;
        public float health;
        public string weaponId;
        public string actionType;
    }
    
    [Serializable]
    public class GameClip
    {
        public string clipId;
        public ClipTriggerType triggerType;
        public string description;
        public DateTime timestamp;
        public float duration;
        public List<ReplayFrame> frames;
        public string playerName;
        public string brazilianTag;
    }
    
    public enum ClipTriggerType
    {
        Manual,
        Kill,
        MultiKill,
        Victory,
        NearDeath,
        LongShot,
        Headshot
    }
    
    public enum SocialPlatform
    {
        TikTok,
        Instagram,
        Twitter,
        WhatsApp,
        Facebook
    }
}
