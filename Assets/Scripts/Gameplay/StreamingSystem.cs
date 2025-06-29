
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System;
using ArenaBrasil.Backend;

namespace ArenaBrasil.Streaming
{
    public class StreamingSystem : NetworkBehaviour
    {
        public static StreamingSystem Instance { get; private set; }
        
        [Header("Streaming Configuration")]
        public bool enableStreaming = true;
        public int maxConcurrentStreams = 100;
        public float streamQuality = 0.8f;
        
        [Header("Platform Integration")]
        public StreamingPlatform[] supportedPlatforms;
        
        [Header("Brazilian Streamers")]
        public BrazilianStreamerData[] partneredStreamers;
        
        private Dictionary<ulong, StreamData> activeStreams = new Dictionary<ulong, StreamData>();
        private List<ViewerData> currentViewers = new List<ViewerData>();
        
        public event Action<StreamData> OnStreamStarted;
        public event Action<StreamData> OnStreamEnded;
        public event Action<ulong, int> OnViewerCountChanged;
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeStreaming();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void InitializeStreaming()
        {
            Debug.Log("Arena Brasil - Inicializando sistema de streaming");
            
            SetupStreamingPlatforms();
            LoadPartneredStreamers();
            InitializeSpectatorMode();
        }
        
        void SetupStreamingPlatforms()
        {
            supportedPlatforms = new StreamingPlatform[]
            {
                new StreamingPlatform 
                { 
                    name = "Twitch", 
                    apiKey = "twitch_api_key",
                    isEnabled = true,
                    maxResolution = "1080p"
                },
                new StreamingPlatform 
                { 
                    name = "YouTube Gaming", 
                    apiKey = "youtube_api_key",
                    isEnabled = true,
                    maxResolution = "1080p"
                },
                new StreamingPlatform 
                { 
                    name = "Facebook Gaming", 
                    apiKey = "facebook_api_key",
                    isEnabled = true,
                    maxResolution = "720p"
                },
                new StreamingPlatform 
                { 
                    name = "Booyah!", 
                    apiKey = "booyah_api_key",
                    isEnabled = true,
                    maxResolution = "720p"
                }
            };
        }
        
        void LoadPartneredStreamers()
        {
            partneredStreamers = new BrazilianStreamerData[]
            {
                new BrazilianStreamerData
                {
                    streamerId = "nobru_arena",
                    displayName = "Nobru",
                    platform = "Twitch",
                    region = "São Paulo",
                    specialRewards = new string[] { "Skin Exclusiva Nobru", "Emote Signature" },
                    isVerified = true
                },
                new BrazilianStreamerData
                {
                    streamerId = "loud_arena",
                    displayName = "LOUD Squad",
                    platform = "YouTube",
                    region = "Rio de Janeiro",
                    specialRewards = new string[] { "Uniforme LOUD", "Paraquedas LOUD" },
                    isVerified = true
                }
            };
        }
        
        void InitializeSpectatorMode()
        {
            // Configurar modo espectador para streamers
            Camera.main.cullingMask |= LayerMask.GetMask("StreamerOverlay");
        }
        
        [ServerRpc(RequireOwnership = false)]
        public void StartStreamServerRpc(ulong playerId, string platform, string streamKey)
        {
            if (activeStreams.Count >= maxConcurrentStreams) return;
            
            var streamData = new StreamData
            {
                streamerId = playerId,
                platform = platform,
                streamKey = streamKey,
                startTime = DateTime.Now,
                viewerCount = 0,
                isLive = true
            };
            
            activeStreams[playerId] = streamData;
            OnStreamStarted?.Invoke(streamData);
            
            // Notify all clients
            StreamStartedClientRpc(playerId, platform);
            
            Debug.Log($"Stream iniciada por jogador {playerId} na plataforma {platform}");
        }
        
        [ClientRpc]
        void StreamStartedClientRpc(ulong streamerId, string platform)
        {
            // Show stream notification
            if (UIManager.Instance != null)
            {
                ShowStreamNotification($"🔴 LIVE: Jogador {streamerId} está transmitindo no {platform}!");
            }
        }
        
        [ServerRpc(RequireOwnership = false)]
        public void EndStreamServerRpc(ulong playerId)
        {
            if (activeStreams.ContainsKey(playerId))
            {
                var streamData = activeStreams[playerId];
                streamData.isLive = false;
                streamData.endTime = DateTime.Now;
                
                OnStreamEnded?.Invoke(streamData);
                activeStreams.Remove(playerId);
                
                StreamEndedClientRpc(playerId);
            }
        }
        
        [ClientRpc]
        void StreamEndedClientRpc(ulong streamerId)
        {
            Debug.Log($"Stream do jogador {streamerId} finalizada");
        }
        
        public void WatchStream(ulong streamerId)
        {
            if (activeStreams.ContainsKey(streamerId))
            {
                var viewer = new ViewerData
                {
                    viewerId = NetworkManager.Singleton.LocalClientId,
                    watchingStreamerId = streamerId,
                    joinTime = DateTime.Now
                };
                
                currentViewers.Add(viewer);
                JoinStreamServerRpc(streamerId);
            }
        }
        
        [ServerRpc(RequireOwnership = false)]
        void JoinStreamServerRpc(ulong streamerId)
        {
            if (activeStreams.ContainsKey(streamerId))
            {
                activeStreams[streamerId].viewerCount++;
                OnViewerCountChanged?.Invoke(streamerId, activeStreams[streamerId].viewerCount);
            }
        }
        
        void ShowStreamNotification(string message)
        {
            // Show in-game notification
            Debug.Log($"Notificação de Stream: {message}");
        }
        
        public void EnableStreamMode(bool isStreamerMode)
        {
            if (isStreamerMode)
            {
                // Hide sensitive UI elements
                DisableSensitiveUI();
                
                // Enable streamer-friendly features
                EnableStreamerOverlay();
                
                // Adjust audio for copyright
                AdjustAudioForStreaming();
            }
        }
        
        void DisableSensitiveUI()
        {
            // Hide chat, personal info, etc.
            GameObject.FindGameObjectWithTag("ChatUI")?.SetActive(false);
        }
        
        void EnableStreamerOverlay()
        {
            // Show Arena Brasil branding
            var overlay = GameObject.FindGameObjectWithTag("StreamerOverlay");
            if (overlay != null)
            {
                overlay.SetActive(true);
            }
        }
        
        void AdjustAudioForStreaming()
        {
            // Use copyright-free Brazilian music
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.EnableStreamingMode();
            }
        }
        
        public List<StreamData> GetActiveStreams() => new List<StreamData>(activeStreams.Values);
        public int GetTotalViewers() => currentViewers.Count;
    }
    
    [Serializable]
    public class StreamData
    {
        public ulong streamerId;
        public string platform;
        public string streamKey;
        public DateTime startTime;
        public DateTime endTime;
        public int viewerCount;
        public bool isLive;
        public string streamTitle;
        public string category = "Arena Brasil";
    }
    
    [Serializable]
    public class ViewerData
    {
        public ulong viewerId;
        public ulong watchingStreamerId;
        public DateTime joinTime;
        public bool isSubscriber;
    }
    
    [Serializable]
    public class StreamingPlatform
    {
        public string name;
        public string apiKey;
        public bool isEnabled;
        public string maxResolution;
    }
    
    [Serializable]
    public class BrazilianStreamerData
    {
        public string streamerId;
        public string displayName;
        public string platform;
        public string region;
        public string[] specialRewards;
        public bool isVerified;
    }
}
