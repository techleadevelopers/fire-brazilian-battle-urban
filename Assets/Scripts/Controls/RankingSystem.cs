
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;

namespace ArenaBrasil.Systems
{
    public class RankingSystem : NetworkBehaviour
    {
        public static RankingSystem Instance { get; private set; }
        
        [Header("Ranking Configuration")]
        public int seasonDurationDays = 90;
        public float rankDecayRate = 0.95f; // 5% decay per week of inactivity
        
        [Header("Brazilian Leagues")]
        public List<RankTier> rankTiers = new List<RankTier>();
        
        private Dictionary<ulong, PlayerRankData> playerRanks = new Dictionary<ulong, PlayerRankData>();
        private RankingSeason currentSeason;
        
        public event System.Action<ulong, RankTier, RankTier> OnPlayerRankChanged;
        public event System.Action<RankingSeason> OnNewSeasonStarted;
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeRankingSystem();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void InitializeRankingSystem()
        {
            Debug.Log("Arena Brasil - Initializing Ranking System");
            CreateBrazilianRankTiers();
            StartNewSeason();
        }
        
        void CreateBrazilianRankTiers()
        {
            rankTiers = new List<RankTier>
            {
                // Bronze - Regionais
                new RankTier { id = "bronze_4", name = "Bronze IV", displayName = "Novato do Interior", minPoints = 0, maxPoints = 199, iconPath = "bronze_4", color = "#CD7F32" },
                new RankTier { id = "bronze_3", name = "Bronze III", displayName = "Batalhadorde Cidade", minPoints = 200, maxPoints = 399, iconPath = "bronze_3", color = "#CD7F32" },
                new RankTier { id = "bronze_2", name = "Bronze II", displayName = "Guerreiro Municipal", minPoints = 400, maxPoints = 599, iconPath = "bronze_2", color = "#CD7F32" },
                new RankTier { id = "bronze_1", name = "Bronze I", displayName = "Campeão Regional", minPoints = 600, maxPoints = 799, iconPath = "bronze_1", color = "#CD7F32" },
                
                // Prata - Estaduais
                new RankTier { id = "silver_4", name = "Prata IV", displayName = "Lutador Estadual", minPoints = 800, maxPoints = 1199, iconPath = "silver_4", color = "#C0C0C0" },
                new RankTier { id = "silver_3", name = "Prata III", displayName = "Guerreiro Bandeirante", minPoints = 1200, maxPoints = 1599, iconPath = "silver_3", color = "#C0C0C0" },
                new RankTier { id = "silver_2", name = "Prata II", displayName = "Herói Gaúcho", minPoints = 1600, maxPoints = 1999, iconPath = "silver_2", color = "#C0C0C0" },
                new RankTier { id = "silver_1", name = "Prata I", displayName = "Lenda Carioca", minPoints = 2000, maxPoints = 2399, iconPath = "silver_1", color = "#C0C0C0" },
                
                // Ouro - Nacionais
                new RankTier { id = "gold_4", name = "Ouro IV", displayName = "Gladiador Nacional", minPoints = 2400, maxPoints = 2999, iconPath = "gold_4", color = "#FFD700" },
                new RankTier { id = "gold_3", name = "Ouro III", displayName = "Campeão Brasileiro", minPoints = 3000, maxPoints = 3599, iconPath = "gold_3", color = "#FFD700" },
                new RankTier { id = "gold_2", name = "Ouro II", displayName = "Mestre Capoeirista", minPoints = 3600, maxPoints = 4199, iconPath = "gold_2", color = "#FFD700" },
                new RankTier { id = "gold_1", name = "Ouro I", displayName = "Rei do Carnaval", minPoints = 4200, maxPoints = 4799, iconPath = "gold_1", color = "#FFD700" },
                
                // Platina - Sul-Americanos
                new RankTier { id = "platinum_4", name = "Platina IV", displayName = "Guerreiro Sul-Americano", minPoints = 4800, maxPoints = 5599, iconPath = "platinum_4", color = "#E5E4E2" },
                new RankTier { id = "platinum_3", name = "Platina III", displayName = "Conquistador Latino", minPoints = 5600, maxPoints = 6399, iconPath = "platinum_3", color = "#E5E4E2" },
                new RankTier { id = "platinum_2", name = "Platina II", displayName = "Lenda Amazônica", minPoints = 6400, maxPoints = 7199, iconPath = "platinum_2", color = "#E5E4E2" },
                new RankTier { id = "platinum_1", name = "Platina I", displayName = "Imperador Tupinambá", minPoints = 7200, maxPoints = 7999, iconPath = "platinum_1", color = "#E5E4E2" },
                
                // Diamante - Mundiais
                new RankTier { id = "diamond_4", name = "Diamante IV", displayName = "Gladiador Mundial", minPoints = 8000, maxPoints = 8999, iconPath = "diamond_4", color = "#B9F2FF" },
                new RankTier { id = "diamond_3", name = "Diamante III", displayName = "Lenda Internacional", minPoints = 9000, maxPoints = 9999, iconPath = "diamond_3", color = "#B9F2FF" },
                new RankTier { id = "diamond_2", name = "Diamante II", displayName = "Mito Planetário", minPoints = 10000, maxPoints = 11999, iconPath = "diamond_2", color = "#B9F2FF" },
                new RankTier { id = "diamond_1", name = "Diamante I", displayName = "Imortal Brasileiro", minPoints = 12000, maxPoints = 14999, iconPath = "diamond_1", color = "#B9F2FF" },
                
                // Mestre - Elite
                new RankTier { id = "master", name = "Mestre", displayName = "Mestre das Arenas", minPoints = 15000, maxPoints = 19999, iconPath = "master", color = "#FF6600" },
                
                // Predador - Top 500
                new RankTier { id = "predator", name = "Predador", displayName = "Predador Supremo", minPoints = 20000, maxPoints = int.MaxValue, iconPath = "predator", color = "#FF0000", isTopRank = true }
            };
        }
        
        void StartNewSeason()
        {
            currentSeason = new RankingSeason
            {
                seasonId = System.Guid.NewGuid().ToString(),
                seasonNumber = GetNextSeasonNumber(),
                name = $"Temporada {GetNextSeasonNumber()} - {GetSeasonTheme()}",
                startDate = System.DateTime.Now,
                endDate = System.DateTime.Now.AddDays(seasonDurationDays),
                isActive = true
            };
            
            Debug.Log($"New ranking season started: {currentSeason.name}");
            OnNewSeasonStarted?.Invoke(currentSeason);
        }
        
        int GetNextSeasonNumber()
        {
            // This would be loaded from persistent storage
            return 1;
        }
        
        string GetSeasonTheme()
        {
            var themes = new[] { "Folclore", "Carnaval", "Copa do Mundo", "Amazônia", "Nordeste", "Pantanal" };
            return themes[Random.Range(0, themes.Length)];
        }
        
        public void AddRankPoints(ulong playerId, int points, string reason = "Match Victory")
        {
            if (!IsServer) return;
            
            if (!playerRanks.ContainsKey(playerId))
            {
                playerRanks[playerId] = new PlayerRankData
                {
                    playerId = playerId,
                    currentTier = rankTiers[0],
                    rankPoints = 0,
                    seasonId = currentSeason.seasonId
                };
            }
            
            var playerRank = playerRanks[playerId];
            var oldTier = playerRank.currentTier;
            
            playerRank.rankPoints += points;
            playerRank.lastMatchDate = System.DateTime.Now;
            
            // Update tier based on new points
            var newTier = GetTierForPoints(playerRank.rankPoints);
            if (newTier.id != oldTier.id)
            {
                playerRank.currentTier = newTier;
                OnPlayerRankChanged?.Invoke(playerId, oldTier, newTier);
                
                // Notify player
                RankChangedClientRpc(playerId, oldTier.displayName, newTier.displayName, points > 0);
                
                // Give rank-up rewards
                if (GetTierIndex(newTier) > GetTierIndex(oldTier))
                {
                    GiveRankUpRewards(playerId, newTier);
                }
            }
            else
            {
                // Just points change
                RankPointsUpdatedClientRpc(playerId, playerRank.rankPoints, points);
            }
            
            Debug.Log($"Player {playerId} gained {points} rank points. Reason: {reason}");
        }
        
        public void SubtractRankPoints(ulong playerId, int points, string reason = "Match Defeat")
        {
            AddRankPoints(playerId, -points, reason);
        }
        
        RankTier GetTierForPoints(int points)
        {
            for (int i = rankTiers.Count - 1; i >= 0; i--)
            {
                if (points >= rankTiers[i].minPoints)
                {
                    return rankTiers[i];
                }
            }
            return rankTiers[0];
        }
        
        int GetTierIndex(RankTier tier)
        {
            return rankTiers.FindIndex(t => t.id == tier.id);
        }
        
        void GiveRankUpRewards(ulong playerId, RankTier newTier)
        {
            int coinReward = GetTierIndex(newTier) * 100 + 200;
            int xpReward = GetTierIndex(newTier) * 50 + 100;
            
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.AddCoins(playerId, coinReward);
                EconomyManager.Instance.AddExperience(playerId, xpReward);
            }
            
            Debug.Log($"Rank-up rewards given to player {playerId}: {coinReward} coins, {xpReward} XP");
        }
        
        public void ApplyRankDecay()
        {
            if (!IsServer) return;
            
            var oneWeekAgo = System.DateTime.Now.AddDays(-7);
            
            foreach (var kvp in playerRanks.ToList())
            {
                var playerRank = kvp.Value;
                
                if (playerRank.lastMatchDate < oneWeekAgo)
                {
                    int pointsToLose = Mathf.RoundToInt(playerRank.rankPoints * (1f - rankDecayRate));
                    
                    if (pointsToLose > 0)
                    {
                        SubtractRankPoints(playerRank.playerId, pointsToLose, "Rank Decay");
                        Debug.Log($"Applied rank decay to player {playerRank.playerId}: -{pointsToLose} points");
                    }
                }
            }
        }
        
        public List<PlayerRankData> GetLeaderboard(int count = 100)
        {
            return playerRanks.Values
                .OrderByDescending(p => p.rankPoints)
                .Take(count)
                .ToList();
        }
        
        public List<PlayerRankData> GetTierLeaderboard(string tierId, int count = 50)
        {
            var tier = rankTiers.Find(t => t.id == tierId);
            if (tier == null) return new List<PlayerRankData>();
            
            return playerRanks.Values
                .Where(p => p.currentTier.id == tierId)
                .OrderByDescending(p => p.rankPoints)
                .Take(count)
                .ToList();
        }
        
        public int GetPlayerRanking(ulong playerId)
        {
            var leaderboard = GetLeaderboard(int.MaxValue);
            return leaderboard.FindIndex(p => p.playerId == playerId) + 1;
        }
        
        // Client RPCs
        [ClientRpc]
        void RankChangedClientRpc(ulong playerId, string oldTierName, string newTierName, bool isRankUp)
        {
            if (NetworkManager.Singleton.LocalClientId == playerId)
            {
                string message = isRankUp ? 
                    $"🎉 Parabéns! Você subiu para {newTierName}!" :
                    $"📉 Você desceu para {newTierName}";
                    
                Debug.Log(message);
            }
        }
        
        [ClientRpc]
        void RankPointsUpdatedClientRpc(ulong playerId, int totalPoints, int pointsGained)
        {
            if (NetworkManager.Singleton.LocalClientId == playerId)
            {
                string sign = pointsGained >= 0 ? "+" : "";
                Debug.Log($"📊 Pontos de Rank: {totalPoints} ({sign}{pointsGained})");
            }
        }
        
        // Public getters
        public PlayerRankData GetPlayerRank(ulong playerId)
        {
            return playerRanks.ContainsKey(playerId) ? playerRanks[playerId] : null;
        }
        
        public RankTier GetPlayerTier(ulong playerId)
        {
            var rankData = GetPlayerRank(playerId);
            return rankData?.currentTier ?? rankTiers[0];
        }
        
        public RankingSeason GetCurrentSeason() => currentSeason;
        
        public List<RankTier> GetAllTiers() => rankTiers.ToList();
    }
    
    [System.Serializable]
    public class PlayerRankData
    {
        public ulong playerId;
        public RankTier currentTier;
        public int rankPoints;
        public string seasonId;
        public System.DateTime lastMatchDate;
        public int wins;
        public int losses;
        public int kills;
        public int deaths;
        public float averagePlacement;
    }
    
    [System.Serializable]
    public class RankTier
    {
        public string id;
        public string name;
        public string displayName;
        public int minPoints;
        public int maxPoints;
        public string iconPath;
        public string color;
        public bool isTopRank = false;
    }
    
    [System.Serializable]
    public class RankingSeason
    {
        public string seasonId;
        public int seasonNumber;
        public string name;
        public System.DateTime startDate;
        public System.DateTime endDate;
        public bool isActive;
    }
}
