
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;

namespace ArenaBrasil.Systems
{
    public class ClanSystem : NetworkBehaviour
    {
        public static ClanSystem Instance { get; private set; }
        
        [Header("Clan Configuration")]
        public int maxMembersPerClan = 50;
        public int clanCreationCost = 1000;
        public int maxClanNameLength = 20;
        
        private Dictionary<string, Clan> clans = new Dictionary<string, Clan>();
        private Dictionary<ulong, string> playerClanMembership = new Dictionary<ulong, string>();
        
        public event System.Action<Clan> OnClanCreated;
        public event System.Action<ulong, string> OnPlayerJoinedClan;
        public event System.Action<ulong, string> OnPlayerLeftClan;
        public event System.Action<string, ClanEvent> OnClanEvent;
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeClanSystem();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void InitializeClanSystem()
        {
            Debug.Log("Arena Brasil - Initializing Clan System");
            CreateDefaultClans();
        }
        
        void CreateDefaultClans()
        {
            // Clãs temáticos brasileiros
            var defaultClans = new[]
            {
                new Clan
                {
                    id = "guerreiros_da_amazonia",
                    name = "Guerreiros da Amazônia",
                    tag = "AMZN",
                    description = "Protegemos a floresta com garra e determinação!",
                    region = "Norte",
                    emblem = "amazon_emblem",
                    level = 5,
                    experience = 2500
                },
                new Clan
                {
                    id = "cangaceiros_do_sertao",
                    name = "Cangaceiros do Sertão",
                    tag = "CANG",
                    description = "Do sertão nordestino, com a força do sol!",
                    region = "Nordeste",
                    emblem = "sertao_emblem",
                    level = 3,
                    experience = 1200
                },
                new Clan
                {
                    id = "leoes_de_copacabana",
                    name = "Leões de Copacabana",
                    tag = "COPA",
                    description = "Da cidade maravilhosa para o mundo!",
                    region = "Sudeste",
                    emblem = "rio_emblem",
                    level = 7,
                    experience = 4200
                }
            };
            
            foreach (var clan in defaultClans)
            {
                clans[clan.id] = clan;
            }
        }
        
        public void CreateClan(ulong leaderId, string clanName, string clanTag, string description, string region)
        {
            if (!IsServer) return;
            
            // Validations
            if (clanName.Length > maxClanNameLength || clanTag.Length > 4)
            {
                ClanCreationFailedClientRpc(leaderId, "Nome ou tag muito longos");
                return;
            }
            
            if (clans.Values.Any(c => c.name == clanName || c.tag == clanTag))
            {
                ClanCreationFailedClientRpc(leaderId, "Nome ou tag já existe");
                return;
            }
            
            if (playerClanMembership.ContainsKey(leaderId))
            {
                ClanCreationFailedClientRpc(leaderId, "Você já pertence a um clã");
                return;
            }
            
            // Check if player has enough coins
            if (EconomyManager.Instance != null)
            {
                var playerData = EconomyManager.Instance.GetPlayerData(leaderId);
                if (playerData.coins < clanCreationCost)
                {
                    ClanCreationFailedClientRpc(leaderId, "Moedas insuficientes");
                    return;
                }
                
                EconomyManager.Instance.SpendCoins(leaderId, clanCreationCost);
            }
            
            // Create clan
            string clanId = System.Guid.NewGuid().ToString();
            var newClan = new Clan
            {
                id = clanId,
                name = clanName,
                tag = clanTag,
                description = description,
                region = region,
                leaderId = leaderId,
                creationDate = System.DateTime.Now,
                level = 1,
                experience = 0,
                emblem = "default_emblem"
            };
            
            newClan.members.Add(new ClanMember
            {
                playerId = leaderId,
                playerName = GetPlayerName(leaderId),
                role = ClanRole.Leader,
                joinDate = System.DateTime.Now,
                contributionPoints = 0
            });
            
            clans[clanId] = newClan;
            playerClanMembership[leaderId] = clanId;
            
            OnClanCreated?.Invoke(newClan);
            ClanCreatedClientRpc(leaderId, clanId);
            
            Debug.Log($"Clan created: {clanName} by player {leaderId}");
        }
        
        public void JoinClan(ulong playerId, string clanId)
        {
            if (!IsServer) return;
            
            if (playerClanMembership.ContainsKey(playerId))
            {
                JoinClanFailedClientRpc(playerId, "Você já pertence a um clã");
                return;
            }
            
            if (!clans.ContainsKey(clanId))
            {
                JoinClanFailedClientRpc(playerId, "Clã não encontrado");
                return;
            }
            
            var clan = clans[clanId];
            if (clan.members.Count >= maxMembersPerClan)
            {
                JoinClanFailedClientRpc(playerId, "Clã lotado");
                return;
            }
            
            // Add player to clan
            clan.members.Add(new ClanMember
            {
                playerId = playerId,
                playerName = GetPlayerName(playerId),
                role = ClanRole.Member,
                joinDate = System.DateTime.Now,
                contributionPoints = 0
            });
            
            playerClanMembership[playerId] = clanId;
            
            OnPlayerJoinedClan?.Invoke(playerId, clanId);
            PlayerJoinedClanClientRpc(playerId, clanId);
            
            // Notify clan members
            NotifyClanMembers(clanId, $"{GetPlayerName(playerId)} entrou no clã!");
        }
        
        public void LeaveClan(ulong playerId)
        {
            if (!IsServer) return;
            
            if (!playerClanMembership.ContainsKey(playerId))
            {
                LeaveClanFailedClientRpc(playerId, "Você não pertence a nenhum clã");
                return;
            }
            
            string clanId = playerClanMembership[playerId];
            var clan = clans[clanId];
            
            // Remove player from clan
            clan.members.RemoveAll(m => m.playerId == playerId);
            playerClanMembership.Remove(playerId);
            
            OnPlayerLeftClan?.Invoke(playerId, clanId);
            PlayerLeftClanClientRpc(playerId, clanId);
            
            // If leader left, assign new leader or disband clan
            if (clan.leaderId == playerId)
            {
                if (clan.members.Count > 0)
                {
                    // Promote senior member to leader
                    var newLeader = clan.members.OrderBy(m => m.joinDate).First();
                    newLeader.role = ClanRole.Leader;
                    clan.leaderId = newLeader.playerId;
                    
                    NotifyClanMembers(clanId, $"{newLeader.playerName} é o novo líder do clã!");
                }
                else
                {
                    // Disband empty clan
                    clans.Remove(clanId);
                    Debug.Log($"Clan {clan.name} disbanded (no members)");
                }
            }
            
            NotifyClanMembers(clanId, $"{GetPlayerName(playerId)} saiu do clã.");
        }
        
        public void PromoteMember(ulong leaderId, ulong targetPlayerId, ClanRole newRole)
        {
            if (!IsServer) return;
            
            if (!playerClanMembership.ContainsKey(leaderId) || !playerClanMembership.ContainsKey(targetPlayerId))
                return;
            
            string clanId = playerClanMembership[leaderId];
            if (playerClanMembership[targetPlayerId] != clanId)
                return;
            
            var clan = clans[clanId];
            if (clan.leaderId != leaderId)
                return;
            
            var targetMember = clan.members.Find(m => m.playerId == targetPlayerId);
            if (targetMember != null)
            {
                targetMember.role = newRole;
                
                string roleName = newRole == ClanRole.Officer ? "oficial" : "membro";
                NotifyClanMembers(clanId, $"{targetMember.playerName} foi promovido a {roleName}!");
                
                MemberPromotedClientRpc(targetPlayerId, newRole);
            }
        }
        
        public void AddClanExperience(string clanId, int experience)
        {
            if (!IsServer || !clans.ContainsKey(clanId)) return;
            
            var clan = clans[clanId];
            clan.experience += experience;
            
            // Check for level up
            int newLevel = CalculateClanLevel(clan.experience);
            if (newLevel > clan.level)
            {
                clan.level = newLevel;
                NotifyClanMembers(clanId, $"🎉 Clã subiu para o nível {newLevel}!");
                
                // Give clan rewards
                GiveClanLevelRewards(clanId, newLevel);
            }
        }
        
        int CalculateClanLevel(int experience)
        {
            return Mathf.FloorToInt(experience / 1000f) + 1;
        }
        
        void GiveClanLevelRewards(string clanId, int level)
        {
            var clan = clans[clanId];
            int coinReward = level * 100;
            
            foreach (var member in clan.members)
            {
                if (EconomyManager.Instance != null)
                {
                    EconomyManager.Instance.AddCoins(member.playerId, coinReward);
                }
            }
            
            NotifyClanMembers(clanId, $"💰 Todos os membros receberam {coinReward} moedas!");
        }
        
        void NotifyClanMembers(string clanId, string message)
        {
            if (!clans.ContainsKey(clanId)) return;
            
            var clan = clans[clanId];
            foreach (var member in clan.members)
            {
                ClanNotificationClientRpc(member.playerId, message);
            }
        }
        
        string GetPlayerName(ulong playerId)
        {
            // This would integrate with player data system
            return $"Player{playerId}";
        }
        
        // Client RPCs
        [ClientRpc]
        void ClanCreatedClientRpc(ulong playerId, string clanId)
        {
            if (NetworkManager.Singleton.LocalClientId == playerId)
            {
                Debug.Log($"✅ Clã criado com sucesso! ID: {clanId}");
            }
        }
        
        [ClientRpc]
        void ClanCreationFailedClientRpc(ulong playerId, string reason)
        {
            if (NetworkManager.Singleton.LocalClientId == playerId)
            {
                Debug.Log($"❌ Falha ao criar clã: {reason}");
            }
        }
        
        [ClientRpc]
        void PlayerJoinedClanClientRpc(ulong playerId, string clanId)
        {
            if (NetworkManager.Singleton.LocalClientId == playerId)
            {
                Debug.Log($"✅ Você entrou no clã!");
            }
        }
        
        [ClientRpc]
        void JoinClanFailedClientRpc(ulong playerId, string reason)
        {
            if (NetworkManager.Singleton.LocalClientId == playerId)
            {
                Debug.Log($"❌ Falha ao entrar no clã: {reason}");
            }
        }
        
        [ClientRpc]
        void PlayerLeftClanClientRpc(ulong playerId, string clanId)
        {
            if (NetworkManager.Singleton.LocalClientId == playerId)
            {
                Debug.Log($"✅ Você saiu do clã.");
            }
        }
        
        [ClientRpc]
        void LeaveClanFailedClientRpc(ulong playerId, string reason)
        {
            if (NetworkManager.Singleton.LocalClientId == playerId)
            {
                Debug.Log($"❌ Falha ao sair do clã: {reason}");
            }
        }
        
        [ClientRpc]
        void MemberPromotedClientRpc(ulong playerId, ClanRole newRole)
        {
            if (NetworkManager.Singleton.LocalClientId == playerId)
            {
                Debug.Log($"🎉 Você foi promovido a {newRole}!");
            }
        }
        
        [ClientRpc]
        void ClanNotificationClientRpc(ulong playerId, string message)
        {
            if (NetworkManager.Singleton.LocalClientId == playerId)
            {
                Debug.Log($"🏰 [Clã] {message}");
            }
        }
        
        // Public getters
        public Clan GetPlayerClan(ulong playerId)
        {
            if (playerClanMembership.ContainsKey(playerId))
            {
                string clanId = playerClanMembership[playerId];
                return clans.ContainsKey(clanId) ? clans[clanId] : null;
            }
            return null;
        }
        
        public List<Clan> GetTopClans(int count = 10)
        {
            return clans.Values
                .OrderByDescending(c => c.level)
                .ThenByDescending(c => c.experience)
                .Take(count)
                .ToList();
        }
        
        public List<Clan> SearchClans(string searchTerm)
        {
            return clans.Values
                .Where(c => c.name.ToLower().Contains(searchTerm.ToLower()) || 
                           c.tag.ToLower().Contains(searchTerm.ToLower()))
                .ToList();
        }
    }
    
    [System.Serializable]
    public class Clan
    {
        public string id;
        public string name;
        public string tag;
        public string description;
        public string region;
        public ulong leaderId;
        public System.DateTime creationDate;
        public int level = 1;
        public int experience = 0;
        public string emblem = "default_emblem";
        public List<ClanMember> members = new List<ClanMember>();
        public ClanSettings settings = new ClanSettings();
        public List<ClanEvent> recentEvents = new List<ClanEvent>();
    }
    
    [System.Serializable]
    public class ClanMember
    {
        public ulong playerId;
        public string playerName;
        public ClanRole role;
        public System.DateTime joinDate;
        public int contributionPoints;
        public System.DateTime lastActive;
    }
    
    [System.Serializable]
    public class ClanSettings
    {
        public bool openToJoin = true;
        public int minimumLevel = 1;
        public string welcomeMessage = "Bem-vindo ao clã!";
    }
    
    [System.Serializable]
    public class ClanEvent
    {
        public string eventType;
        public string description;
        public System.DateTime timestamp;
        public ulong playerId;
    }
    
    public enum ClanRole
    {
        Member,
        Officer,
        Leader
    }
}
