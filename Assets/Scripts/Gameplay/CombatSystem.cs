
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using ArenaBrasil.Gameplay.Characters;
using ArenaBrasil.Gameplay.Weapons;

namespace ArenaBrasil.Combat
{
    public class CombatSystem : NetworkBehaviour
    {
        public static CombatSystem Instance { get; private set; }
        
        [Header("Combat Configuration")]
        public LayerMask combatLayers;
        public float maxCombatRange = 500f;
        public int maxPlayersInCombat = 60;
        
        [Header("Damage Settings")]
        public float headshotMultiplier = 2.0f;
        public float bodyDamageMultiplier = 1.0f;
        public float limbDamageMultiplier = 0.8f;
        
        // Combat tracking
        private Dictionary<ulong, CombatData> activeCombatSessions = new Dictionary<ulong, CombatData>();
        private Dictionary<ulong, float> lastDamageTime = new Dictionary<ulong, float>();
        
        // Events
        public event System.Action<ulong, ulong, float> OnPlayerDamaged; // attacker, victim, damage
        public event System.Action<ulong, ulong, string> OnPlayerEliminated; // killer, victim, weapon
        public event System.Action<int> OnPlayersAliveChanged;
        
        private int playersAlive = 0;
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                playersAlive = maxPlayersInCombat;
                OnPlayersAliveChanged?.Invoke(playersAlive);
            }
        }
        
        [ServerRpc(RequireOwnership = false)]
        public void ProcessDamageServerRpc(ulong attackerId, Vector3 origin, Vector3 direction, float damage, string weaponId)
        {
            if (!IsServer) return;
            
            // Perform authoritative hit detection
            RaycastHit hit;
            if (Physics.Raycast(origin, direction, out hit, maxCombatRange, combatLayers))
            {
                var hitPlayer = hit.collider.GetComponent<PlayerController>();
                if (hitPlayer != null && hitPlayer.OwnerClientId != attackerId)
                {
                    // Calculate damage based on hit location
                    float finalDamage = CalculateDamage(damage, hit.collider.tag);
                    
                    // Apply damage
                    ApplyDamage(attackerId, hitPlayer.OwnerClientId, finalDamage, weaponId);
                    
                    // Notify clients about hit
                    NotifyHitClientRpc(attackerId, hitPlayer.OwnerClientId, hit.point, finalDamage);
                }
            }
        }
        
        float CalculateDamage(float baseDamage, string hitTag)
        {
            switch (hitTag)
            {
                case "Head":
                    return baseDamage * headshotMultiplier;
                case "Body":
                    return baseDamage * bodyDamageMultiplier;
                case "Limb":
                    return baseDamage * limbDamageMultiplier;
                default:
                    return baseDamage * bodyDamageMultiplier;
            }
        }
        
        void ApplyDamage(ulong attackerId, ulong victimId, float damage, string weaponId)
        {
            // Get victim player
            var victim = GetPlayerById(victimId);
            if (victim == null) return;
            
            // Apply damage
            victim.TakeDamage(damage);
            lastDamageTime[victimId] = Time.time;
            
            // Track combat session
            TrackCombatSession(attackerId, victimId, damage);
            
            OnPlayerDamaged?.Invoke(attackerId, victimId, damage);
            
            // Check if player is eliminated
            if (victim.GetCurrentHealth() <= 0)
            {
                EliminatePlayer(attackerId, victimId, weaponId);
            }
        }
        
        void EliminatePlayer(ulong killerId, ulong victimId, string weaponId)
        {
            playersAlive--;
            OnPlayersAliveChanged?.Invoke(playersAlive);
            OnPlayerEliminated?.Invoke(killerId, victimId, weaponId);
            
            // Award kill to attacker
            var killer = GetPlayerById(killerId);
            if (killer != null)
            {
                killer.AddKill();
            }
            
            // Notify all clients
            AnnounceEliminationClientRpc(killerId, victimId, weaponId, playersAlive);
            
            // Check for match end
            if (playersAlive <= 1)
            {
                EndMatch();
            }
        }
        
        [ClientRpc]
        void NotifyHitClientRpc(ulong attackerId, ulong victimId, Vector3 hitPoint, float damage)
        {
            // Play hit effects
            // Update UI if local player
            if (victimId == NetworkManager.Singleton.LocalClientId)
            {
                // Show damage indicator
                ShowDamageIndicator(damage);
            }
            
            if (attackerId == NetworkManager.Singleton.LocalClientId)
            {
                // Show hit confirmation
                ShowHitMarker();
            }
        }
        
        [ClientRpc]
        void AnnounceEliminationClientRpc(ulong killerId, ulong victimId, string weaponId, int remainingPlayers)
        {
            var killerName = GetPlayerNameById(killerId);
            var victimName = GetPlayerNameById(victimId);
            
            Debug.Log($"{killerName} eliminated {victimName} with {weaponId}. {remainingPlayers} players remaining.");
            
            // Update UI
            UpdateEliminationFeed(killerName, victimName, weaponId);
            UpdatePlayersAliveUI(remainingPlayers);
        }
        
        void TrackCombatSession(ulong attackerId, ulong victimId, float damage)
        {
            if (!activeCombatSessions.ContainsKey(attackerId))
            {
                activeCombatSessions[attackerId] = new CombatData();
            }
            
            activeCombatSessions[attackerId].totalDamage += damage;
            activeCombatSessions[attackerId].hits++;
            activeCombatSessions[attackerId].lastHitTime = Time.time;
        }
        
        PlayerController GetPlayerById(ulong clientId)
        {
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
            {
                return client.PlayerObject?.GetComponent<PlayerController>();
            }
            return null;
        }
        
        string GetPlayerNameById(ulong clientId)
        {
            var player = GetPlayerById(clientId);
            return player?.GetPlayerName() ?? $"Player {clientId}";
        }
        
        void ShowDamageIndicator(float damage)
        {
            // Implement damage indicator UI
        }
        
        void ShowHitMarker()
        {
            // Implement hit marker UI
        }
        
        void UpdateEliminationFeed(string killerName, string victimName, string weaponId)
        {
            // Implement elimination feed UI
        }
        
        void UpdatePlayersAliveUI(int count)
        {
            // Implement players alive counter
        }
        
        void EndMatch()
        {
            // Determine winner
            ulong winnerId = 0;
            foreach (var client in NetworkManager.Singleton.ConnectedClients)
            {
                var player = client.Value.PlayerObject?.GetComponent<PlayerController>();
                if (player != null && player.GetCurrentHealth() > 0)
                {
                    winnerId = client.Key;
                    break;
                }
            }
            
            // End match
            if (GameManager.Instance != null)
            {
                GameManager.Instance.EndMatch();
            }
            
            AnnounceWinnerClientRpc(winnerId);
        }
        
        [ClientRpc]
        void AnnounceWinnerClientRpc(ulong winnerId)
        {
            var winnerName = GetPlayerNameById(winnerId);
            Debug.Log($"Winner: {winnerName}!");
            
            // Show victory screen
            if (winnerId == NetworkManager.Singleton.LocalClientId)
            {
                ShowVictoryScreen();
            }
            else
            {
                ShowDefeatScreen(winnerName);
            }
        }
        
        void ShowVictoryScreen()
        {
            // Implement victory UI
        }
        
        void ShowDefeatScreen(string winnerName)
        {
            // Implement defeat UI
        }
        
        public int GetPlayersAlive() => playersAlive;
        public float GetLastDamageTime(ulong clientId) => lastDamageTime.ContainsKey(clientId) ? lastDamageTime[clientId] : 0f;
    }
    
    [System.Serializable]
    public class CombatData
    {
        public float totalDamage;
        public int hits;
        public float lastHitTime;
    }
}
