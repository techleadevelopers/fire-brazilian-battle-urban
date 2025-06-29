
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System;

namespace ArenaBrasil.Social
{
    public class SocialMediaIntegration : MonoBehaviour
    {
        public static SocialMediaIntegration Instance { get; private set; }
        
        [Header("Platform Configuration")]
        public List<SocialPlatformConfig> supportedPlatforms = new List<SocialPlatformConfig>();
        
        [Header("Brazilian Content")]
        public BrazilianContentTemplate[] contentTemplates;
        public string[] brazilianHashtags = {
            "#ArenaBrasil", "#BatalhadeLendas", "#GamesBrasil",
            "#FolcloreBR", "#SaciFire", "#CurupiraGamer"
        };
        
        [Header("Sharing Rewards")]
        public SharingReward[] sharingRewards;
        
        private Dictionary<string, SocialAccountData> connectedAccounts = new Dictionary<string, SocialAccountData>();
        private List<SharedContent> sharedContentHistory = new List<SharedContent>();
        
        public event Action<string, SocialPlatformType> OnAccountConnected;
        public event Action<SharedContent> OnContentShared;
        public event Action<SharingReward> OnSharingRewardEarned;
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeSocialIntegration();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void InitializeSocialIntegration()
        {
            Debug.Log("Arena Brasil - Inicializando integração social");
            
            SetupSocialPlatforms();
            SetupBrazilianContent();
            InitializeSharingRewards();
        }
        
        void SetupSocialPlatforms()
        {
            supportedPlatforms.Add(new SocialPlatformConfig
            {
                platformType = SocialPlatformType.TikTok,
                platformName = "TikTok",
                apiKey = "tiktok_api_key",
                isEnabled = true,
                maxVideoLength = 60,
                supportedFormats = new string[] { "mp4", "mov" },
                brazilianOptimization = true
            });
            
            supportedPlatforms.Add(new SocialPlatformConfig
            {
                platformType = SocialPlatformType.Instagram,
                platformName = "Instagram",
                apiKey = "instagram_api_key",
                isEnabled = true,
                maxVideoLength = 30,
                supportedFormats = new string[] { "mp4", "jpg", "png" },
                brazilianOptimization = true
            });
            
            supportedPlatforms.Add(new SocialPlatformConfig
            {
                platformType = SocialPlatformType.Twitter,
                platformName = "Twitter",
                apiKey = "twitter_api_key",
                isEnabled = true,
                maxVideoLength = 140,
                supportedFormats = new string[] { "mp4", "gif", "jpg" },
                brazilianOptimization = false
            });
            
            supportedPlatforms.Add(new SocialPlatformConfig
            {
                platformType = SocialPlatformType.WhatsApp,
                platformName = "WhatsApp",
                apiKey = "",
                isEnabled = true,
                maxVideoLength = 16,
                supportedFormats = new string[] { "mp4", "jpg" },
                brazilianOptimization = true
            });
            
            supportedPlatforms.Add(new SocialPlatformConfig
            {
                platformType = SocialPlatformType.YouTube,
                platformName = "YouTube Shorts",
                apiKey = "youtube_api_key",
                isEnabled = true,
                maxVideoLength = 60,
                supportedFormats = new string[] { "mp4" },
                brazilianOptimization = true
            });
        }
        
        void SetupBrazilianContent()
        {
            contentTemplates = new BrazilianContentTemplate[]
            {
                new BrazilianContentTemplate
                {
                    templateId = "victory_template",
                    templateText = "🇧🇷 VITÓRIA REAL! {playerName} dominou a arena com {heroName}! #ArenaBrasil #VitóriaReal",
                    category = ContentCategory.Victory,
                    brazilianFlavor = "Celebra vitórias com orgulho brasileiro"
                },
                new BrazilianContentTemplate
                {
                    templateId = "multikill_template",
                    templateText = "🔥 {playerName} fez um MULTI-KILL épico! {killCount} eliminações seguidas! #Mitou #ArenaBrasil",
                    category = ContentCategory.Achievement,
                    brazilianFlavor = "Usa gírias brasileiras autênticas"
                },
                new BrazilianContentTemplate
                {
                    templateId = "hero_showcase",
                    templateText = "⚡ Mostrando a força do {heroName}! Quem mais ama as lendas brasileiras? #FolcloreBR #ArenaBrasil",
                    category = ContentCategory.Hero,
                    brazilianFlavor = "Promove cultura folclórica brasileira"
                },
                new BrazilianContentTemplate
                {
                    templateId = "skin_show",
                    templateText = "✨ Olha que skin linda! {skinName} representa demais a cultura BR! #SkinBrasileira #ArenaBrasil",
                    category = ContentCategory.Cosmetic,
                    brazilianFlavor = "Destaca elementos culturais brasileiros"
                }
            };
        }
        
        void InitializeSharingRewards()
        {
            sharingRewards = new SharingReward[]
            {
                new SharingReward
                {
                    rewardId = "first_share",
                    description = "Primeira compartilhada",
                    coins = 100,
                    gems = 10,
                    platform = SocialPlatformType.Any,
                    isOneTime = true
                },
                new SharingReward
                {
                    rewardId = "tiktok_viral",
                    description = "Vídeo viral no TikTok (1000+ views)",
                    coins = 500,
                    gems = 50,
                    platform = SocialPlatformType.TikTok,
                    isOneTime = false
                },
                new SharingReward
                {
                    rewardId = "daily_share",
                    description = "Compartilhamento diário",
                    coins = 50,
                    gems = 5,
                    platform = SocialPlatformType.Any,
                    isOneTime = false
                }
            };
        }
        
        public void ConnectSocialAccount(SocialPlatformType platform, string accessToken)
        {
            var platformConfig = supportedPlatforms.Find(p => p.platformType == platform);
            if (platformConfig == null || !platformConfig.isEnabled) return;
            
            var accountData = new SocialAccountData
            {
                platform = platform,
                accessToken = accessToken,
                connectionDate = DateTime.Now,
                isVerified = true,
                username = GetUsernameFromToken(platform, accessToken)
            };
            
            connectedAccounts[platform.ToString()] = accountData;
            OnAccountConnected?.Invoke(accountData.username, platform);
            
            Debug.Log($"Conta {platform} conectada: {accountData.username}");
            
            // Give connection reward
            GiveConnectionReward(platform);
        }
        
        string GetUsernameFromToken(SocialPlatformType platform, string accessToken)
        {
            // In production, this would make API calls to get username
            return $"user_{platform}_{UnityEngine.Random.Range(1000, 9999)}";
        }
        
        void GiveConnectionReward(SocialPlatformType platform)
        {
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.AddCurrency(CurrencyType.Coins, 50);
                EconomyManager.Instance.AddCurrency(CurrencyType.Gems, 10);
                
                Debug.Log($"Recompensa de conexão {platform}: 50 moedas, 10 gemas");
            }
        }
        
        public void ShareContent(ContentToShare content)
        {
            if (!IsAccountConnected(content.targetPlatform)) return;
            
            var template = GetContentTemplate(content.category);
            var finalContent = ProcessContentTemplate(template, content);
            
            switch (content.targetPlatform)
            {
                case SocialPlatformType.TikTok:
                    ShareToTikTok(finalContent);
                    break;
                case SocialPlatformType.Instagram:
                    ShareToInstagram(finalContent);
                    break;
                case SocialPlatformType.Twitter:
                    ShareToTwitter(finalContent);
                    break;
                case SocialPlatformType.WhatsApp:
                    ShareToWhatsApp(finalContent);
                    break;
                case SocialPlatformType.YouTube:
                    ShareToYouTube(finalContent);
                    break;
            }
            
            TrackSharedContent(finalContent);
        }
        
        BrazilianContentTemplate GetContentTemplate(ContentCategory category)
        {
            var templates = Array.FindAll(contentTemplates, t => t.category == category);
            return templates.Length > 0 ? templates[UnityEngine.Random.Range(0, templates.Length)] : contentTemplates[0];
        }
        
        ProcessedContent ProcessContentTemplate(BrazilianContentTemplate template, ContentToShare content)
        {
            string processedText = template.templateText;
            
            // Replace placeholders
            processedText = processedText.Replace("{playerName}", content.playerName);
            processedText = processedText.Replace("{heroName}", content.heroName);
            processedText = processedText.Replace("{killCount}", content.killCount.ToString());
            processedText = processedText.Replace("{skinName}", content.skinName);
            
            // Add random Brazilian hashtags
            string hashtags = GetRandomBrazilianHashtags(3);
            processedText += $" {hashtags}";
            
            return new ProcessedContent
            {
                text = processedText,
                mediaType = content.mediaType,
                mediaData = content.mediaData,
                platform = content.targetPlatform,
                originalTemplate = template
            };
        }
        
        string GetRandomBrazilianHashtags(int count)
        {
            var selectedTags = new List<string>();
            var availableTags = new List<string>(brazilianHashtags);
            
            for (int i = 0; i < count && availableTags.Count > 0; i++)
            {
                int index = UnityEngine.Random.Range(0, availableTags.Count);
                selectedTags.Add(availableTags[index]);
                availableTags.RemoveAt(index);
            }
            
            return string.Join(" ", selectedTags);
        }
        
        void ShareToTikTok(ProcessedContent content)
        {
            Debug.Log($"Compartilhando no TikTok: {content.text}");
            
            // TikTok API integration would go here
            SimulateSuccessfulShare(content);
        }
        
        void ShareToInstagram(ProcessedContent content)
        {
            Debug.Log($"Compartilhando no Instagram: {content.text}");
            
            // Instagram API integration would go here
            SimulateSuccessfulShare(content);
        }
        
        void ShareToTwitter(ProcessedContent content)
        {
            Debug.Log($"Compartilhando no Twitter: {content.text}");
            
            // Twitter API integration would go here
            SimulateSuccessfulShare(content);
        }
        
        void ShareToWhatsApp(ProcessedContent content)
        {
            Debug.Log($"Compartilhando no WhatsApp: {content.text}");
            
            // WhatsApp sharing via system intent
            Application.OpenURL($"whatsapp://send?text={UnityEngine.Networking.UnityWebRequest.EscapeURL(content.text)}");
            
            SimulateSuccessfulShare(content);
        }
        
        void ShareToYouTube(ProcessedContent content)
        {
            Debug.Log($"Compartilhando no YouTube Shorts: {content.text}");
            
            // YouTube API integration would go here
            SimulateSuccessfulShare(content);
        }
        
        void SimulateSuccessfulShare(ProcessedContent content)
        {
            // Simulate sharing success and give rewards
            CheckAndGiveSharingRewards(content.platform);
            
            var sharedContent = new SharedContent
            {
                contentId = Guid.NewGuid().ToString(),
                platform = content.platform,
                text = content.text,
                shareDate = DateTime.Now,
                engagementMetrics = new EngagementMetrics()
            };
            
            sharedContentHistory.Add(sharedContent);
            OnContentShared?.Invoke(sharedContent);
        }
        
        void CheckAndGiveSharingRewards(SocialPlatformType platform)
        {
            foreach (var reward in sharingRewards)
            {
                if (reward.platform == platform || reward.platform == SocialPlatformType.Any)
                {
                    if (CanClaimReward(reward))
                    {
                        GiveSharingReward(reward);
                    }
                }
            }
        }
        
        bool CanClaimReward(SharingReward reward)
        {
            if (reward.isOneTime)
            {
                // Check if already claimed
                return !HasClaimedReward(reward.rewardId);
            }
            
            // For repeatable rewards, check daily limits etc.
            return true;
        }
        
        bool HasClaimedReward(string rewardId)
        {
            // Check against player save data
            return false; // Simplified for now
        }
        
        void GiveSharingReward(SharingReward reward)
        {
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.AddCurrency(CurrencyType.Coins, reward.coins);
                EconomyManager.Instance.AddCurrency(CurrencyType.Gems, reward.gems);
            }
            
            OnSharingRewardEarned?.Invoke(reward);
            Debug.Log($"Recompensa de compartilhamento: {reward.description} - {reward.coins} moedas, {reward.gems} gemas");
        }
        
        void TrackSharedContent(ProcessedContent content)
        {
            // Analytics tracking would go here
            Debug.Log($"Conteúdo compartilhado rastreado: {content.platform}");
        }
        
        public bool IsAccountConnected(SocialPlatformType platform)
        {
            return connectedAccounts.ContainsKey(platform.ToString());
        }
        
        public List<SharedContent> GetRecentShares(int count = 10)
        {
            var recentShares = new List<SharedContent>(sharedContentHistory);
            recentShares.Sort((a, b) => b.shareDate.CompareTo(a.shareDate));
            return recentShares.GetRange(0, Math.Min(count, recentShares.Count));
        }
        
        public void CreateSuggestedContent(ContentCategory category, Dictionary<string, object> data)
        {
            var template = GetContentTemplate(category);
            Debug.Log($"Conteúdo sugerido: {template.templateText}");
            
            // Show suggested content in UI
            if (UIManager.Instance != null)
            {
                // UIManager.Instance.ShowSuggestedContent(template, data);
            }
        }
    }
    
    [Serializable]
    public class SocialPlatformConfig
    {
        public SocialPlatformType platformType;
        public string platformName;
        public string apiKey;
        public bool isEnabled;
        public int maxVideoLength;
        public string[] supportedFormats;
        public bool brazilianOptimization;
    }
    
    [Serializable]
    public class BrazilianContentTemplate
    {
        public string templateId;
        public string templateText;
        public ContentCategory category;
        public string brazilianFlavor;
    }
    
    [Serializable]
    public class SharingReward
    {
        public string rewardId;
        public string description;
        public int coins;
        public int gems;
        public SocialPlatformType platform;
        public bool isOneTime;
    }
    
    [Serializable]
    public class SocialAccountData
    {
        public SocialPlatformType platform;
        public string accessToken;
        public DateTime connectionDate;
        public bool isVerified;
        public string username;
    }
    
    [Serializable]
    public class ContentToShare
    {
        public SocialPlatformType targetPlatform;
        public ContentCategory category;
        public string playerName;
        public string heroName;
        public int killCount;
        public string skinName;
        public MediaType mediaType;
        public byte[] mediaData;
    }
    
    [Serializable]
    public class ProcessedContent
    {
        public string text;
        public MediaType mediaType;
        public byte[] mediaData;
        public SocialPlatformType platform;
        public BrazilianContentTemplate originalTemplate;
    }
    
    [Serializable]
    public class SharedContent
    {
        public string contentId;
        public SocialPlatformType platform;
        public string text;
        public DateTime shareDate;
        public EngagementMetrics engagementMetrics;
    }
    
    [Serializable]
    public class EngagementMetrics
    {
        public int views;
        public int likes;
        public int shares;
        public int comments;
    }
    
    public enum SocialPlatformType
    {
        Any,
        TikTok,
        Instagram,
        Twitter,
        WhatsApp,
        YouTube,
        Facebook
    }
    
    public enum ContentCategory
    {
        Victory,
        Achievement,
        Hero,
        Cosmetic,
        Gameplay,
        Event
    }
    
    public enum MediaType
    {
        Text,
        Image,
        Video,
        GIF
    }
}
