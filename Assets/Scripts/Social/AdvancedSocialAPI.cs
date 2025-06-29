
using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Text;
using UnityEngine.Networking;
using Newtonsoft.Json;

namespace ArenaBrasil.Social.Advanced
{
    public class AdvancedSocialAPI : MonoBehaviour
    {
        public static AdvancedSocialAPI Instance { get; private set; }
        
        [Header("API Configurations")]
        public TikTokConfig tiktokConfig;
        public InstagramConfig instagramConfig;
        public TwitterConfig twitterConfig;
        public WhatsAppConfig whatsappConfig;
        
        [Header("Content Settings")]
        public VideoQualitySettings videoSettings;
        public HashtagManager hashtagManager;
        
        private Dictionary<string, SocialAccount> connectedAccounts = new Dictionary<string, SocialAccount>();
        
        public event Action<string, bool> OnAccountConnectionResult;
        public event Action<SocialPost> OnContentShared;
        public event Action<SocialMetrics> OnMetricsUpdated;
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeAdvancedSocial();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void InitializeAdvancedSocial()
        {
            Debug.Log("🚀 Inicializando APIs sociais avançadas - Arena Brasil");
            
            SetupAPIConfigurations();
            SetupVideoSettings();
            SetupHashtagManager();
        }
        
        void SetupAPIConfigurations()
        {
            // TikTok API Configuration
            tiktokConfig = new TikTokConfig
            {
                clientKey = "YOUR_TIKTOK_CLIENT_KEY",
                clientSecret = "YOUR_TIKTOK_CLIENT_SECRET",
                redirectUri = "https://arena-brasil.com/auth/tiktok",
                scopes = new string[] { "user.info.basic", "video.upload", "video.publish" },
                apiBaseUrl = "https://open-api.tiktok.com"
            };
            
            // Instagram Basic Display API
            instagramConfig = new InstagramConfig
            {
                clientId = "YOUR_INSTAGRAM_CLIENT_ID",
                clientSecret = "YOUR_INSTAGRAM_CLIENT_SECRET",
                redirectUri = "https://arena-brasil.com/auth/instagram",
                scopes = new string[] { "user_profile", "user_media" },
                apiBaseUrl = "https://graph.instagram.com"
            };
            
            // Twitter API v2
            twitterConfig = new TwitterConfig
            {
                apiKey = "YOUR_TWITTER_API_KEY",
                apiSecretKey = "YOUR_TWITTER_API_SECRET",
                bearerToken = "YOUR_TWITTER_BEARER_TOKEN",
                apiBaseUrl = "https://api.twitter.com/2"
            };
            
            // WhatsApp Business API
            whatsappConfig = new WhatsAppConfig
            {
                phoneNumberId = "YOUR_PHONE_NUMBER_ID",
                accessToken = "YOUR_WHATSAPP_ACCESS_TOKEN",
                businessAccountId = "YOUR_BUSINESS_ACCOUNT_ID",
                apiBaseUrl = "https://graph.facebook.com/v18.0"
            };
        }
        
        void SetupVideoSettings()
        {
            videoSettings = new VideoQualitySettings
            {
                tiktokMaxDuration = 60, // seconds
                instagramReelsMaxDuration = 90,
                twitterMaxDuration = 140,
                maxFileSize = 100 * 1024 * 1024, // 100MB
                supportedFormats = new string[] { "mp4", "mov", "avi" },
                recommendedResolution = "1080x1920", // Vertical for mobile
                frameRate = 30,
                bitrate = 5000 // kbps
            };
        }
        
        void SetupHashtagManager()
        {
            hashtagManager = new HashtagManager();
            
            // Hashtags brasileiras populares
            hashtagManager.brazilianTags = new string[]
            {
                "#ArenaBrasil", "#BatalhadeLendas", "#GamesBR", "#FolcloreBR",
                "#SaciFire", "#CurupiraGamer", "#Brasil", "#Gaming",
                "#MobileGames", "#BattleRoyale", "#Cultura", "#Mitologia"
            };
            
            // Hashtags específicas por plataforma
            hashtagManager.platformSpecific = new Dictionary<string, string[]>
            {
                ["tiktok"] = new string[] { "#TikTokGaming", "#ViralBrasil", "#GameTok", "#FYP" },
                ["instagram"] = new string[] { "#InstaGaming", "#Reels", "#Explore", "#GamerBR" },
                ["twitter"] = new string[] { "#TwitterGaming", "#GameDev", "#IndieGame", "#Trending" }
            };
        }
        
        // TikTok Integration
        public async Task<bool> ConnectTikTokAccount()
        {
            try
            {
                string authUrl = BuildTikTokAuthUrl();
                Application.OpenURL(authUrl);
                
                // Em produção, isso seria tratado via deep link callback
                Debug.Log("🎵 TikTok: Redirecionando para autenticação...");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Erro ao conectar TikTok: {e.Message}");
                return false;
            }
        }
        
        string BuildTikTokAuthUrl()
        {
            var parameters = new Dictionary<string, string>
            {
                ["client_key"] = tiktokConfig.clientKey,
                ["response_type"] = "code",
                ["scope"] = string.Join(",", tiktokConfig.scopes),
                ["redirect_uri"] = tiktokConfig.redirectUri,
                ["state"] = GenerateRandomState()
            };
            
            var queryString = string.Join("&", 
                parameters.Select(kvp => $"{kvp.Key}={UnityWebRequest.EscapeURL(kvp.Value)}"));
            
            return $"{tiktokConfig.apiBaseUrl}/platform/oauth/authorize/?{queryString}";
        }
        
        public async Task<bool> ShareToTikTok(VideoContent content)
        {
            try
            {
                if (!connectedAccounts.ContainsKey("tiktok"))
                {
                    Debug.LogWarning("TikTok não conectado");
                    return false;
                }
                
                var account = connectedAccounts["tiktok"];
                
                // 1. Upload do vídeo
                string videoUrl = await UploadVideoToTikTok(content.videoData, account.accessToken);
                
                if (string.IsNullOrEmpty(videoUrl))
                {
                    Debug.LogError("Falha no upload do vídeo TikTok");
                    return false;
                }
                
                // 2. Criar post
                var postData = new TikTokPostRequest
                {
                    video_url = videoUrl,
                    caption = GenerateCaption(content, "tiktok"),
                    privacy_level = "PUBLIC_TO_EVERYONE",
                    disable_duet = false,
                    disable_comment = false,
                    disable_stitch = false,
                    brand_content_toggle = false
                };
                
                string jsonData = JsonConvert.SerializeObject(postData);
                
                using (var request = new UnityWebRequest($"{tiktokConfig.apiBaseUrl}/video/publish/", "POST"))
                {
                    request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonData));
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.SetRequestHeader("Authorization", $"Bearer {account.accessToken}");
                    request.SetRequestHeader("Content-Type", "application/json");
                    
                    await request.SendWebRequest();
                    
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        var response = JsonConvert.DeserializeObject<TikTokPostResponse>(request.downloadHandler.text);
                        
                        if (response.data != null)
                        {
                            Debug.Log($"✅ Vídeo publicado no TikTok: {response.data.share_url}");
                            OnContentShared?.Invoke(new SocialPost
                            {
                                platform = "TikTok",
                                postId = response.data.share_url,
                                content = content,
                                timestamp = DateTime.Now
                            });
                            return true;
                        }
                    }
                    
                    Debug.LogError($"Erro TikTok: {request.error}");
                    return false;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Erro ao compartilhar no TikTok: {e.Message}");
                return false;
            }
        }
        
        async Task<string> UploadVideoToTikTok(byte[] videoData, string accessToken)
        {
            // Implementar upload de vídeo para TikTok
            // Por enquanto, retornar URL simulada
            await Task.Delay(1000); // Simular upload
            return "https://tiktok-upload-url.com/video123";
        }
        
        // Instagram Integration
        public async Task<bool> ShareToInstagram(VideoContent content)
        {
            try
            {
                if (!connectedAccounts.ContainsKey("instagram"))
                {
                    Debug.LogWarning("Instagram não conectado");
                    return false;
                }
                
                var account = connectedAccounts["instagram"];
                
                // 1. Upload como Instagram Reel
                var uploadData = new InstagramMediaRequest
                {
                    media_type = "REELS",
                    video_url = await UploadVideoToInstagram(content.videoData),
                    caption = GenerateCaption(content, "instagram"),
                    location_id = GetBrazilianLocationId(),
                    thumb_offset = 0
                };
                
                string jsonData = JsonConvert.SerializeObject(uploadData);
                
                using (var request = new UnityWebRequest($"{instagramConfig.apiBaseUrl}/me/media", "POST"))
                {
                    request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonData));
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.SetRequestHeader("Authorization", $"Bearer {account.accessToken}");
                    request.SetRequestHeader("Content-Type", "application/json");
                    
                    await request.SendWebRequest();
                    
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        var response = JsonConvert.DeserializeObject<InstagramMediaResponse>(request.downloadHandler.text);
                        
                        // 2. Publicar o media
                        await PublishInstagramMedia(response.id, account.accessToken);
                        
                        Debug.Log($"✅ Reel publicado no Instagram: {response.id}");
                        return true;
                    }
                    
                    Debug.LogError($"Erro Instagram: {request.error}");
                    return false;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Erro ao compartilhar no Instagram: {e.Message}");
                return false;
            }
        }
        
        async Task<string> UploadVideoToInstagram(byte[] videoData)
        {
            // Implementar upload para Instagram
            await Task.Delay(1000);
            return "https://instagram-upload-url.com/reel123";
        }
        
        async Task PublishInstagramMedia(string mediaId, string accessToken)
        {
            var publishData = new { creation_id = mediaId };
            string jsonData = JsonConvert.SerializeObject(publishData);
            
            using (var request = new UnityWebRequest($"{instagramConfig.apiBaseUrl}/me/media_publish", "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonData));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Authorization", $"Bearer {accessToken}");
                request.SetRequestHeader("Content-Type", "application/json");
                
                await request.SendWebRequest();
            }
        }
        
        string GetBrazilianLocationId()
        {
            // IDs de localização populares no Brasil
            var locations = new string[] { "213385402", "106078429431815", "110970792271801" };
            return locations[UnityEngine.Random.Range(0, locations.Length)];
        }
        
        // WhatsApp Business Integration
        public async Task<bool> ShareToWhatsApp(TextContent content)
        {
            try
            {
                // Compartilhamento via WhatsApp Business API ou link direto
                string whatsappUrl = $"https://wa.me/?text={UnityWebRequest.EscapeURL(content.text)}";
                Application.OpenURL(whatsappUrl);
                
                Debug.Log("📱 WhatsApp: Link de compartilhamento aberto");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Erro ao compartilhar no WhatsApp: {e.Message}");
                return false;
            }
        }
        
        public async Task SendWhatsAppTemplateMessage(string phoneNumber, string templateName, Dictionary<string, string> parameters)
        {
            try
            {
                var messageData = new WhatsAppTemplateMessage
                {
                    messaging_product = "whatsapp",
                    to = phoneNumber,
                    type = "template",
                    template = new WhatsAppTemplate
                    {
                        name = templateName,
                        language = new WhatsAppLanguage { code = "pt_BR" },
                        components = BuildTemplateComponents(parameters)
                    }
                };
                
                string jsonData = JsonConvert.SerializeObject(messageData);
                
                using (var request = new UnityWebRequest($"{whatsappConfig.apiBaseUrl}/{whatsappConfig.phoneNumberId}/messages", "POST"))
                {
                    request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonData));
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.SetRequestHeader("Authorization", $"Bearer {whatsappConfig.accessToken}");
                    request.SetRequestHeader("Content-Type", "application/json");
                    
                    await request.SendWebRequest();
                    
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        Debug.Log("📱 Mensagem WhatsApp enviada com sucesso");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Erro ao enviar mensagem WhatsApp: {e.Message}");
            }
        }
        
        WhatsAppComponent[] BuildTemplateComponents(Dictionary<string, string> parameters)
        {
            var components = new List<WhatsAppComponent>();
            
            if (parameters.Count > 0)
            {
                var bodyComponent = new WhatsAppComponent
                {
                    type = "body",
                    parameters = parameters.Select(kvp => new WhatsAppParameter
                    {
                        type = "text",
                        text = kvp.Value
                    }).ToArray()
                };
                
                components.Add(bodyComponent);
            }
            
            return components.ToArray();
        }
        
        // Utility Methods
        string GenerateCaption(ContentBase content, string platform)
        {
            var caption = new StringBuilder();
            
            // Adicionar texto principal
            caption.AppendLine(content.text);
            caption.AppendLine();
            
            // Adicionar hashtags brasileiras
            var tags = hashtagManager.GetHashtagsForPlatform(platform, 10);
            caption.AppendLine(string.Join(" ", tags));
            
            // Call to action específico para Arena Brasil
            caption.AppendLine();
            caption.AppendLine("🎮 Baixe Arena Brasil e jogue você também!");
            caption.AppendLine("🇧🇷 O Battle Royale que celebra nossa cultura!");
            
            return caption.ToString();
        }
        
        string GenerateRandomState()
        {
            return Guid.NewGuid().ToString("N")[..16];
        }
        
        public void TrackSocialMetrics(string platform, SocialMetrics metrics)
        {
            OnMetricsUpdated?.Invoke(metrics);
            
            // Integrar com analytics
            if (PlayerAnalytics.Instance != null)
            {
                PlayerAnalytics.Instance.TrackSocialEngagement(platform, metrics);
            }
        }
        
        public async Task<SocialMetrics> GetPostMetrics(string platform, string postId)
        {
            // Implementar busca de métricas por plataforma
            var metrics = new SocialMetrics
            {
                postId = postId,
                platform = platform,
                views = UnityEngine.Random.Range(100, 10000),
                likes = UnityEngine.Random.Range(10, 1000),
                shares = UnityEngine.Random.Range(5, 100),
                comments = UnityEngine.Random.Range(2, 50),
                timestamp = DateTime.Now
            };
            
            return metrics;
        }
    }
    
    // Data Classes
    [Serializable]
    public class TikTokConfig
    {
        public string clientKey;
        public string clientSecret;
        public string redirectUri;
        public string[] scopes;
        public string apiBaseUrl;
    }
    
    [Serializable]
    public class InstagramConfig
    {
        public string clientId;
        public string clientSecret;
        public string redirectUri;
        public string[] scopes;
        public string apiBaseUrl;
    }
    
    [Serializable]
    public class TwitterConfig
    {
        public string apiKey;
        public string apiSecretKey;
        public string bearerToken;
        public string apiBaseUrl;
    }
    
    [Serializable]
    public class WhatsAppConfig
    {
        public string phoneNumberId;
        public string accessToken;
        public string businessAccountId;
        public string apiBaseUrl;
    }
    
    [Serializable]
    public class VideoQualitySettings
    {
        public int tiktokMaxDuration;
        public int instagramReelsMaxDuration;
        public int twitterMaxDuration;
        public int maxFileSize;
        public string[] supportedFormats;
        public string recommendedResolution;
        public int frameRate;
        public int bitrate;
    }
    
    [Serializable]
    public class HashtagManager
    {
        public string[] brazilianTags;
        public Dictionary<string, string[]> platformSpecific;
        
        public string[] GetHashtagsForPlatform(string platform, int count)
        {
            var allTags = new List<string>(brazilianTags);
            
            if (platformSpecific.ContainsKey(platform))
            {
                allTags.AddRange(platformSpecific[platform]);
            }
            
            // Embaralhar e retornar quantidade solicitada
            var shuffled = allTags.OrderBy(x => UnityEngine.Random.value).Take(count).ToArray();
            return shuffled;
        }
    }
    
    [Serializable]
    public class SocialAccount
    {
        public string platform;
        public string accessToken;
        public string refreshToken;
        public DateTime expiresAt;
        public string username;
        public bool isVerified;
    }
    
    [Serializable]
    public abstract class ContentBase
    {
        public string text;
        public string playerName;
        public string heroName;
        public DateTime createdAt;
    }
    
    [Serializable]
    public class VideoContent : ContentBase
    {
        public byte[] videoData;
        public int duration;
        public string resolution;
        public string format;
    }
    
    [Serializable]
    public class TextContent : ContentBase
    {
        public string[] hashtags;
        public string callToAction;
    }
    
    [Serializable]
    public class SocialPost
    {
        public string platform;
        public string postId;
        public ContentBase content;
        public DateTime timestamp;
        public SocialMetrics metrics;
    }
    
    [Serializable]
    public class SocialMetrics
    {
        public string postId;
        public string platform;
        public int views;
        public int likes;
        public int shares;
        public int comments;
        public DateTime timestamp;
    }
    
    // API Request/Response Classes
    [Serializable]
    public class TikTokPostRequest
    {
        public string video_url;
        public string caption;
        public string privacy_level;
        public bool disable_duet;
        public bool disable_comment;
        public bool disable_stitch;
        public bool brand_content_toggle;
    }
    
    [Serializable]
    public class TikTokPostResponse
    {
        public TikTokPostData data;
        public TikTokError error;
    }
    
    [Serializable]
    public class TikTokPostData
    {
        public string share_url;
        public string video_id;
    }
    
    [Serializable]
    public class TikTokError
    {
        public string code;
        public string message;
    }
    
    [Serializable]
    public class InstagramMediaRequest
    {
        public string media_type;
        public string video_url;
        public string caption;
        public string location_id;
        public int thumb_offset;
    }
    
    [Serializable]
    public class InstagramMediaResponse
    {
        public string id;
    }
    
    [Serializable]
    public class WhatsAppTemplateMessage
    {
        public string messaging_product;
        public string to;
        public string type;
        public WhatsAppTemplate template;
    }
    
    [Serializable]
    public class WhatsAppTemplate
    {
        public string name;
        public WhatsAppLanguage language;
        public WhatsAppComponent[] components;
    }
    
    [Serializable]
    public class WhatsAppLanguage
    {
        public string code;
    }
    
    [Serializable]
    public class WhatsAppComponent
    {
        public string type;
        public WhatsAppParameter[] parameters;
    }
    
    [Serializable]
    public class WhatsAppParameter
    {
        public string type;
        public string text;
    }
}
