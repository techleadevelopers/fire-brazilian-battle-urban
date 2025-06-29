
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System;
using ArenaBrasil.Backend;
using ArenaBrasil.Economy;

namespace ArenaBrasil.Social
{
    public class ReferralSystem : MonoBehaviour
    {
        public static ReferralSystem Instance { get; private set; }
        
        [Header("Referral Configuration")]
        public int referralRewardGems = 100;
        public int inviteeRewardGems = 50;
        public int maxReferralsPerDay = 10;
        
        [Header("Bonus Rewards")]
        public ReferralTier[] referralTiers = {
            new ReferralTier { friendsRequired = 5, rewardGems = 500, rewardTitle = "Recruta BR" },
            new ReferralTier { friendsRequired = 10, rewardGems = 1000, rewardTitle = "Capitão BR" },
            new ReferralTier { friendsRequired = 25, rewardGems = 2500, rewardTitle = "General BR" },
            new ReferralTier { friendsRequired = 50, rewardGems = 5000, rewardTitle = "Lenda BR" }
        };
        
        private Dictionary<string, ReferralData> referralDatabase = new Dictionary<string, ReferralData>();
        
        // Events
        public event Action<string> OnFriendReferred;
        public event Action<ReferralTier> OnTierReached;
        public event Action<int> OnReferralRewardReceived;
        
        [System.Serializable]
        public class ReferralTier
        {
            public int friendsRequired;
            public int rewardGems;
            public string rewardTitle;
            public string rewardSkin;
        }
        
        [System.Serializable]
        public class ReferralData
        {
            public string referrerUserId;
            public List<string> referredFriends = new List<string>();
            public int totalReferrals;
            public int dailyReferrals;
            public DateTime lastReferralDate;
            public List<int> tiersReached = new List<int>();
            public string referralCode;
        }
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeReferralSystem();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void InitializeReferralSystem()
        {
            Debug.Log("Arena Brasil - Initializing Referral System");
            LoadReferralData();
            ResetDailyCountersIfNeeded();
        }
        
        public string GenerateReferralCode(string userId)
        {
            // Generate unique referral code
            string code = "BR" + userId.Substring(0, Math.Min(4, userId.Length)).ToUpper() + 
                         UnityEngine.Random.Range(1000, 9999).ToString();
            
            var referralData = GetOrCreateReferralData(userId);
            referralData.referralCode = code;
            SaveReferralData();
            
            return code;
        }
        
        public bool ValidateReferralCode(string code)
        {
            foreach (var data in referralDatabase.Values)
            {
                if (data.referralCode == code)
                {
                    return true;
                }
            }
            return false;
        }
        
        public void ProcessReferral(string referralCode, string newUserId)
        {
            string referrerUserId = FindReferrerByCode(referralCode);
            if (string.IsNullOrEmpty(referrerUserId))
            {
                Debug.LogWarning($"Invalid referral code: {referralCode}");
                return;
            }
            
            var referrerData = GetOrCreateReferralData(referrerUserId);
            
            // Check daily limit
            if (referrerData.dailyReferrals >= maxReferralsPerDay)
            {
                Debug.LogWarning("Daily referral limit reached");
                return;
            }
            
            // Check if user already referred
            if (referrerData.referredFriends.Contains(newUserId))
            {
                Debug.LogWarning("User already referred");
                return;
            }
            
            // Process referral
            referrerData.referredFriends.Add(newUserId);
            referrerData.totalReferrals++;
            referrerData.dailyReferrals++;
            referrerData.lastReferralDate = DateTime.Now;
            
            // Give rewards
            GiveReferralRewards(referrerUserId, newUserId);
            
            // Check tier progression
            CheckTierProgression(referrerUserId);
            
            SaveReferralData();
            
            OnFriendReferred?.Invoke(newUserId);
            
            Debug.Log($"Referral processed: {referrerUserId} referred {newUserId}");
        }
        
        void GiveReferralRewards(string referrerUserId, string newUserId)
        {
            // Reward referrer
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.AddGems(referrerUserId, referralRewardGems);
                EconomyManager.Instance.AddGems(newUserId, inviteeRewardGems);
            }
            
            OnReferralRewardReceived?.Invoke(referralRewardGems);
            
            Debug.Log($"Referral rewards given: {referralRewardGems} gems to referrer, {inviteeRewardGems} gems to invitee");
        }
        
        void CheckTierProgression(string userId)
        {
            var referralData = GetOrCreateReferralData(userId);
            
            for (int i = 0; i < referralTiers.Length; i++)
            {
                var tier = referralTiers[i];
                
                if (referralData.totalReferrals >= tier.friendsRequired && 
                    !referralData.tiersReached.Contains(i))
                {
                    referralData.tiersReached.Add(i);
                    GiveTierReward(userId, tier);
                    OnTierReached?.Invoke(tier);
                    
                    Debug.Log($"User {userId} reached referral tier: {tier.rewardTitle}");
                }
            }
        }
        
        void GiveTierReward(string userId, ReferralTier tier)
        {
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.AddGems(userId, tier.rewardGems);
                
                // Give title or skin if available
                if (!string.IsNullOrEmpty(tier.rewardTitle))
                {
                    EconomyManager.Instance.UnlockTitle(userId, tier.rewardTitle);
                }
                
                if (!string.IsNullOrEmpty(tier.rewardSkin))
                {
                    EconomyManager.Instance.UnlockSkin(userId, tier.rewardSkin);
                }
            }
        }
        
        string FindReferrerByCode(string code)
        {
            foreach (var kvp in referralDatabase)
            {
                if (kvp.Value.referralCode == code)
                {
                    return kvp.Key;
                }
            }
            return null;
        }
        
        ReferralData GetOrCreateReferralData(string userId)
        {
            if (!referralDatabase.ContainsKey(userId))
            {
                referralDatabase[userId] = new ReferralData
                {
                    referrerUserId = userId,
                    referralCode = GenerateReferralCode(userId)
                };
            }
            return referralDatabase[userId];
        }
        
        void ResetDailyCountersIfNeeded()
        {
            DateTime today = DateTime.Now.Date;
            
            foreach (var data in referralDatabase.Values)
            {
                if (data.lastReferralDate.Date < today)
                {
                    data.dailyReferrals = 0;
                }
            }
        }
        
        void LoadReferralData()
        {
            // Load from Firebase/local storage
            string dataJson = PlayerPrefs.GetString("ReferralData", "{}");
            try
            {
                // Parse JSON data
                Debug.Log("Referral data loaded");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load referral data: {e.Message}");
            }
        }
        
        void SaveReferralData()
        {
            try
            {
                // Save to Firebase/local storage
                string dataJson = JsonUtility.ToJson(referralDatabase);
                PlayerPrefs.SetString("ReferralData", dataJson);
                PlayerPrefs.Save();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save referral data: {e.Message}");
            }
        }
        
        // Public API
        public ReferralData GetUserReferralData(string userId)
        {
            return GetOrCreateReferralData(userId);
        }
        
        public List<string> GetReferredFriends(string userId)
        {
            var data = GetOrCreateReferralData(userId);
            return new List<string>(data.referredFriends);
        }
        
        public string GetUserReferralCode(string userId)
        {
            var data = GetOrCreateReferralData(userId);
            return data.referralCode;
        }
        
        public int GetTotalReferrals(string userId)
        {
            var data = GetOrCreateReferralData(userId);
            return data.totalReferrals;
        }
        
        public void ShareReferralCode(string userId)
        {
            string code = GetUserReferralCode(userId);
            string shareText = $"Jogue Arena Brasil comigo! Use meu código {code} e ganhe {inviteeRewardGems} gemas grátis! 🇧🇷⚔️";
            
            // Platform-specific sharing
            #if UNITY_ANDROID
                AndroidShare(shareText);
            #elif UNITY_IOS
                IOSShare(shareText);
            #else
                Debug.Log($"Share text: {shareText}");
            #endif
        }
        
        #if UNITY_ANDROID
        void AndroidShare(string text)
        {
            using (AndroidJavaClass intentClass = new AndroidJavaClass("android.content.Intent"))
            using (AndroidJavaObject intentObject = new AndroidJavaObject("android.content.Intent"))
            {
                intentObject.Call<AndroidJavaObject>("setAction", intentClass.GetStatic<string>("ACTION_SEND"));
                intentObject.Call<AndroidJavaObject>("setType", "text/plain");
                intentObject.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_TEXT"), text);
                
                using (AndroidJavaClass unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject currentActivity = unity.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    currentActivity.Call("startActivity", intentObject);
                }
            }
        }
        #endif
        
        #if UNITY_IOS
        void IOSShare(string text)
        {
            // iOS sharing implementation
            Debug.Log($"iOS Share: {text}");
        }
        #endif
    }
}
