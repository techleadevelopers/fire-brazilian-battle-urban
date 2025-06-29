
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;

namespace ArenaBrasil.Systems
{
    public class AchievementSystem : NetworkBehaviour
    {
        public static AchievementSystem Instance { get; private set; }
        
        [Header("Achievement Configuration")]
        public List<Achievement> availableAchievements = new List<Achievement>();
        
        private Dictionary<string, Achievement> achievementsDictionary = new Dictionary<string, Achievement>();
        private Dictionary<ulong, List<string>> playerAchievements = new Dictionary<ulong, List<string>>();
        
        public event System.Action<Achievement, ulong> OnAchievementUnlocked;
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeAchievements();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void InitializeAchievements()
        {
            // Conquistas brasileiras temáticas
            var achievements = new List<Achievement>
            {
                new Achievement
                {
                    id = "primeira_vitoria",
                    name = "Primeira Vitória",
                    description = "Vença sua primeira partida!",
                    icon = "victory_icon",
                    xpReward = 100,
                    coinsReward = 50,
                    category = AchievementCategory.Gameplay
                },
                new Achievement
                {
                    id = "saci_master",
                    name = "Mestre do Saci",
                    description = "Vença 10 partidas usando o Saci",
                    icon = "saci_icon",
                    xpReward = 200,
                    coinsReward = 100,
                    category = AchievementCategory.Heroes,
                    targetValue = 10
                },
                new Achievement
                {
                    id = "lenda_brasileira",
                    name = "Lenda Brasileira",
                    description = "Jogue com todos os heróis folclóricos",
                    icon = "folklore_icon",
                    xpReward = 500,
                    coinsReward = 250,
                    category = AchievementCategory.Collection,
                    targetValue = 5
                },
                new Achievement
                {
                    id = "matador_favela",
                    name = "Rei da Favela",
                    description = "Elimine 100 inimigos na Favela da Vitória",
                    icon = "favela_icon",
                    xpReward = 300,
                    coinsReward = 150,
                    category = AchievementCategory.Combat,
                    targetValue = 100
                },
                new Achievement
                {
                    id = "explorador_amazonia",
                    name = "Explorador da Amazônia",
                    description = "Visite todas as áreas do mapa Amazônia",
                    icon = "amazon_icon",
                    xpReward = 150,
                    coinsReward = 75,
                    category = AchievementCategory.Exploration,
                    targetValue = 8
                },
                new Achievement
                {
                    id = "sobrevivente_sertao",
                    name = "Cabra da Peste",
                    description = "Sobreviva 20 minutos no Sertão Nordestino",
                    icon = "sertao_icon",
                    xpReward = 250,
                    coinsReward = 125,
                    category = AchievementCategory.Survival,
                    targetValue = 1200 // 20 minutes in seconds
                },
                new Achievement
                {
                    id = "capoeirista",
                    name = "Capoeirista",
                    description = "Elimine 5 inimigos seguidos sem ser atingido",
                    icon = "capoeira_icon",
                    xpReward = 400,
                    coinsReward = 200,
                    category = AchievementCategory.Combat,
                    targetValue = 5
                }
            };
            
            availableAchievements = achievements;
            
            foreach (var achievement in availableAchievements)
            {
                achievementsDictionary[achievement.id] = achievement;
            }
        }
        
        public void TrackProgress(ulong playerId, string achievementId, float progress = 1f)
        {
            if (!IsServer) return;
            
            if (achievementsDictionary.ContainsKey(achievementId))
            {
                var achievement = achievementsDictionary[achievementId];
                
                if (!playerAchievements.ContainsKey(playerId))
                {
                    playerAchievements[playerId] = new List<string>();
                }
                
                if (!playerAchievements[playerId].Contains(achievementId))
                {
                    achievement.currentValue += progress;
                    
                    if (achievement.currentValue >= achievement.targetValue)
                    {
                        UnlockAchievement(playerId, achievementId);
                    }
                }
            }
        }
        
        void UnlockAchievement(ulong playerId, string achievementId)
        {
            if (!playerAchievements.ContainsKey(playerId))
            {
                playerAchievements[playerId] = new List<string>();
            }
            
            if (!playerAchievements[playerId].Contains(achievementId))
            {
                playerAchievements[playerId].Add(achievementId);
                var achievement = achievementsDictionary[achievementId];
                
                OnAchievementUnlocked?.Invoke(achievement, playerId);
                AchievementUnlockedClientRpc(playerId, achievementId);
                
                // Reward player
                if (EconomyManager.Instance != null)
                {
                    EconomyManager.Instance.AddExperience(playerId, achievement.xpReward);
                    EconomyManager.Instance.AddCoins(playerId, achievement.coinsReward);
                }
                
                Debug.Log($"Achievement unlocked: {achievement.name} for player {playerId}");
            }
        }
        
        [ClientRpc]
        void AchievementUnlockedClientRpc(ulong playerId, string achievementId)
        {
            if (achievementsDictionary.ContainsKey(achievementId))
            {
                var achievement = achievementsDictionary[achievementId];
                ShowAchievementNotification(achievement);
            }
        }
        
        void ShowAchievementNotification(Achievement achievement)
        {
            // Create achievement popup UI
            Debug.Log($"🏆 Conquista Desbloqueada: {achievement.name}!");
            Debug.Log($"📝 {achievement.description}");
            Debug.Log($"🎁 Recompensa: {achievement.xpReward} XP, {achievement.coinsReward} moedas");
        }
        
        public List<Achievement> GetPlayerAchievements(ulong playerId)
        {
            if (playerAchievements.ContainsKey(playerId))
            {
                return playerAchievements[playerId]
                    .Select(id => achievementsDictionary[id])
                    .ToList();
            }
            return new List<Achievement>();
        }
        
        public float GetAchievementProgress(string achievementId)
        {
            if (achievementsDictionary.ContainsKey(achievementId))
            {
                var achievement = achievementsDictionary[achievementId];
                return achievement.currentValue / achievement.targetValue;
            }
            return 0f;
        }
    }
    
    [System.Serializable]
    public class Achievement
    {
        public string id;
        public string name;
        public string description;
        public string icon;
        public int xpReward;
        public int coinsReward;
        public AchievementCategory category;
        public float targetValue = 1f;
        public float currentValue = 0f;
        public bool isSecret = false;
    }
    
    public enum AchievementCategory
    {
        Gameplay,
        Combat,
        Heroes,
        Collection,
        Exploration,
        Survival,
        Social
    }
}
