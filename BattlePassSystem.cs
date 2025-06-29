
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System;

namespace ArenaBrasil.Systems
{
    public class BattlePassSystem : NetworkBehaviour
    {
        public static BattlePassSystem Instance { get; private set; }
        
        [Header("Battle Pass Configuration")]
        public int maxTier = 100;
        public int xpPerTier = 1000;
        public float premiumCost = 9.99f;
        
        [Header("Current Season")]
        public BattlePassSeason currentSeason;
        
        private Dictionary<ulong, PlayerBattlePassData> playerData = new Dictionary<ulong, PlayerBattlePassData>();
        
        public event Action<ulong, int> OnTierUnlocked;
        public event Action<ulong, BattlePassReward> OnRewardClaimed;
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeBattlePass();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void InitializeBattlePass()
        {
            CreateCurrentSeason();
        }
        
        void CreateCurrentSeason()
        {
            currentSeason = new BattlePassSeason
            {
                seasonId = "temporada_folclore_2024",
                name = "Temporada do Folclore",
                description = "Celebre as lendas brasileiras nesta temporada especial!",
                startDate = DateTime.Now,
                endDate = DateTime.Now.AddDays(90), // 3 meses
                freeRewards = CreateFreeRewards(),
                premiumRewards = CreatePremiumRewards()
            };
        }
        
        List<BattlePassReward> CreateFreeRewards()
        {
            var rewards = new List<BattlePassReward>();
            
            for (int tier = 1; tier <= maxTier; tier++)
            {
                BattlePassReward reward = null;
                
                if (tier % 10 == 0) // A cada 10 níveis
                {
                    reward = new BattlePassReward
                    {
                        tier = tier,
                        rewardType = RewardType.Character,
                        itemId = GetCharacterForTier(tier),
                        quantity = 1,
                        name = $"Herói Lenda - Tier {tier}",
                        description = "Desbloqueie um novo herói folclórico!"
                    };
                }
                else if (tier % 5 == 0) // A cada 5 níveis
                {
                    reward = new BattlePassReward
                    {
                        tier = tier,
                        rewardType = RewardType.Currency,
                        itemId = "coins",
                        quantity = 250,
                        name = "Moedas de Ouro",
                        description = "Moedas para comprar itens na loja"
                    };
                }
                else // Outros níveis
                {
                    reward = new BattlePassReward
                    {
                        tier = tier,
                        rewardType = RewardType.Experience,
                        itemId = "xp_boost",
                        quantity = 50,
                        name = "Boost de XP",
                        description = "Experiência extra para acelerar seu progresso"
                    };
                }
                
                rewards.Add(reward);
            }
            
            return rewards;
        }
        
        List<BattlePassReward> CreatePremiumRewards()
        {
            var rewards = new List<BattlePassReward>();
            
            for (int tier = 1; tier <= maxTier; tier++)
            {
                BattlePassReward reward = null;
                
                if (tier % 25 == 0) // A cada 25 níveis - recompensas épicas
                {
                    reward = new BattlePassReward
                    {
                        tier = tier,
                        rewardType = RewardType.Skin,
                        itemId = GetEpicSkinForTier(tier),
                        quantity = 1,
                        name = $"Skin Épica - {GetEpicSkinName(tier)}",
                        description = "Skin exclusiva desta temporada!"
                    };
                }
                else if (tier % 10 == 0) // A cada 10 níveis
                {
                    reward = new BattlePassReward
                    {
                        tier = tier,
                        rewardType = RewardType.Weapon,
                        itemId = GetWeaponForTier(tier),
                        quantity = 1,
                        name = $"Arma Especial - Tier {tier}",
                        description = "Arma com design brasileiro exclusivo"
                    };
                }
                else if (tier % 5 == 0) // A cada 5 níveis
                {
                    reward = new BattlePassReward
                    {
                        tier = tier,
                        rewardType = RewardType.Currency,
                        itemId = "premium_coins",
                        quantity = 500,
                        name = "Moedas Premium",
                        description = "Moedas especiais para itens exclusivos"
                    };
                }
                else // Outros níveis
                {
                    reward = new BattlePassReward
                    {
                        tier = tier,
                        rewardType = RewardType.Cosmetic,
                        itemId = GetCosmeticForTier(tier),
                        quantity = 1,
                        name = "Item Cosmético",
                        description = "Personalize seu herói com estilo brasileiro"
                    };
                }
                
                rewards.Add(reward);
            }
            
            return rewards;
        }
        
        string GetCharacterForTier(int tier)
        {
            var characters = new[] { "saci", "curupira", "iara", "boitata", "mula_sem_cabeca" };
            return characters[(tier / 10 - 1) % characters.Length];
        }
        
        string GetEpicSkinForTier(int tier)
        {
            var skins = new[] { "saci_carnaval", "curupira_amazonia", "iara_copacabana", "boitata_sertao" };
            return skins[(tier / 25 - 1) % skins.Length];
        }
        
        string GetEpicSkinName(int tier)
        {
            var names = new[] { "Saci do Carnaval", "Curupira da Amazônia", "Iara de Copacabana", "Boitatá do Sertão" };
            return names[(tier / 25 - 1) % names.Length];
        }
        
        string GetWeaponForTier(int tier)
        {
            var weapons = new[] { "berimbau_rifle", "pandeiro_shotgun", "viola_sniper", "tambor_machine_gun" };
            return weapons[(tier / 10 - 1) % weapons.Length];
        }
        
        string GetCosmeticForTier(int tier)
        {
            var cosmetics = new[] { "bandana_brasil", "camiseta_verde_amarela", "chapeu_cangaceiro", "cordao_ouro" };
            return cosmetics[tier % cosmetics.Length];
        }
        
        public void AddBattlePassXP(ulong playerId, int xp)
        {
            if (!IsServer) return;
            
            if (!playerData.ContainsKey(playerId))
            {
                playerData[playerId] = new PlayerBattlePassData();
            }
            
            var data = playerData[playerId];
            data.totalXP += xp;
            
            int newTier = Mathf.Min(data.totalXP / xpPerTier + 1, maxTier);
            
            if (newTier > data.currentTier)
            {
                for (int tier = data.currentTier + 1; tier <= newTier; tier++)
                {
                    UnlockTier(playerId, tier);
                }
                data.currentTier = newTier;
            }
            
            BattlePassProgressClientRpc(playerId, data.totalXP, data.currentTier);
        }
        
        void UnlockTier(ulong playerId, int tier)
        {
            OnTierUnlocked?.Invoke(playerId, tier);
            
            // Auto-claim free rewards
            var freeReward = currentSeason.freeRewards.Find(r => r.tier == tier);
            if (freeReward != null)
            {
                ClaimReward(playerId, freeReward, false);
            }
            
            Debug.Log($"Player {playerId} unlocked Battle Pass tier {tier}");
        }
        
        public void PurchasePremiumPass(ulong playerId)
        {
            if (!IsServer) return;
            
            if (!playerData.ContainsKey(playerId))
            {
                playerData[playerId] = new PlayerBattlePassData();
            }
            
            var data = playerData[playerId];
            if (!data.hasPremiumPass)
            {
                data.hasPremiumPass = true;
                
                // Auto-claim all premium rewards up to current tier
                for (int tier = 1; tier <= data.currentTier; tier++)
                {
                    var premiumReward = currentSeason.premiumRewards.Find(r => r.tier == tier);
                    if (premiumReward != null)
                    {
                        ClaimReward(playerId, premiumReward, true);
                    }
                }
                
                PremiumPassPurchasedClientRpc(playerId);
            }
        }
        
        void ClaimReward(ulong playerId, BattlePassReward reward, bool isPremium)
        {
            OnRewardClaimed?.Invoke(playerId, reward);
            
            // Give reward to player based on type
            switch (reward.rewardType)
            {
                case RewardType.Currency:
                    if (EconomyManager.Instance != null)
                    {
                        if (reward.itemId == "coins")
                        {
                            EconomyManager.Instance.AddCoins(playerId, reward.quantity);
                        }
                        else if (reward.itemId == "premium_coins")
                        {
                            EconomyManager.Instance.AddPremiumCoins(playerId, reward.quantity);
                        }
                    }
                    break;
                    
                case RewardType.Experience:
                    if (EconomyManager.Instance != null)
                    {
                        EconomyManager.Instance.AddExperience(playerId, reward.quantity);
                    }
                    break;
                    
                case RewardType.Character:
                case RewardType.Skin:
                case RewardType.Weapon:
                case RewardType.Cosmetic:
                    // Add to player inventory
                    AddToPlayerInventory(playerId, reward);
                    break;
            }
            
            RewardClaimedClientRpc(playerId, reward.tier, isPremium);
        }
        
        void AddToPlayerInventory(ulong playerId, BattlePassReward reward)
        {
            // This would integrate with inventory system
            Debug.Log($"Added {reward.name} to player {playerId} inventory");
        }
        
        [ClientRpc]
        void BattlePassProgressClientRpc(ulong playerId, int totalXP, int currentTier)
        {
            if (NetworkManager.Singleton.LocalClientId == playerId)
            {
                Debug.Log($"Battle Pass Progress: XP {totalXP}, Tier {currentTier}");
                // Update UI
            }
        }
        
        [ClientRpc]
        void PremiumPassPurchasedClientRpc(ulong playerId)
        {
            if (NetworkManager.Singleton.LocalClientId == playerId)
            {
                Debug.Log("Premium Battle Pass purchased!");
                // Update UI
            }
        }
        
        [ClientRpc]
        void RewardClaimedClientRpc(ulong playerId, int tier, bool isPremium)
        {
            if (NetworkManager.Singleton.LocalClientId == playerId)
            {
                Debug.Log($"Claimed {(isPremium ? "Premium" : "Free")} reward from tier {tier}");
                // Update UI
            }
        }
        
        public PlayerBattlePassData GetPlayerData(ulong playerId)
        {
            return playerData.ContainsKey(playerId) ? playerData[playerId] : new PlayerBattlePassData();
        }
        
        public int GetPlayerTier(ulong playerId)
        {
            return playerData.ContainsKey(playerId) ? playerData[playerId].currentTier : 0;
        }
        
        public bool HasPremiumPass(ulong playerId)
        {
            return playerData.ContainsKey(playerId) && playerData[playerId].hasPremiumPass;
        }
    }
    
    [System.Serializable]
    public class BattlePassSeason
    {
        public string seasonId;
        public string name;
        public string description;
        public DateTime startDate;
        public DateTime endDate;
        public List<BattlePassReward> freeRewards;
        public List<BattlePassReward> premiumRewards;
    }
    
    [System.Serializable]
    public class BattlePassReward
    {
        public int tier;
        public RewardType rewardType;
        public string itemId;
        public int quantity;
        public string name;
        public string description;
        public string iconPath;
    }
    
    [System.Serializable]
    public class PlayerBattlePassData
    {
        public int currentTier = 0;
        public int totalXP = 0;
        public bool hasPremiumPass = false;
        public List<int> claimedFreeRewards = new List<int>();
        public List<int> claimedPremiumRewards = new List<int>();
    }
    
    public enum RewardType
    {
        Currency,
        Experience,
        Character,
        Skin,
        Weapon,
        Cosmetic,
        Emote
    }
}
