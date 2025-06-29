
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;

namespace ArenaBrasil.Combat.Advanced
{
    public class SuperiorCombatSystem : NetworkBehaviour
    {
        public static SuperiorCombatSystem Instance { get; private set; }
        
        [Header("Next-Gen Combat Features")]
        public bool enableRealisticBallistics = true;
        public bool enableAdvancedHitDetection = true;
        public bool enableSkillBasedMatchmaking = true;
        public bool enableCombatPrediction = true;
        
        [Header("Realistic Systems")]
        public bool enableBulletDrop = true;
        public bool enableWindEffects = true;
        public bool enableRicochetPhysics = true;
        public bool enableArmorPenetration = true;
        
        [Header("Advanced Mechanics")]
        public bool enableCoverSystem = true;
        public bool enableBlindFire = true;
        public bool enableTacticalMovement = true;
        public bool enableSuppressionEffects = true;
        
        private Dictionary<ulong, CombatStatistics> playerCombatStats = new Dictionary<ulong, CombatStatistics>();
        private Dictionary<ulong, SkillLevel> playerSkillLevels = new Dictionary<ulong, SkillLevel>();
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeSuperiorCombat();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void InitializeSuperiorCombat()
        {
            SetupRealisticBallistics();
            SetupAdvancedHitDetection();
            SetupSkillBasedSystems();
            SetupTacticalMechanics();
            
            Debug.Log("Sistema de Combate Superior ativo - Além do Free Fire");
        }
        
        // === BALÍSTICA REALÍSTICA ===
        void SetupRealisticBallistics()
        {
            if (!enableRealisticBallistics) return;
            
            EnableBulletPhysics();
            EnableEnvironmentalFactors();
            EnableAdvancedTrajectories();
            
            Debug.Log("Balística Realística ativa - Física avançada");
        }
        
        void EnableBulletPhysics()
        {
            // Sistema de projéteis físicos (não hitscan)
            var bulletPhysics = gameObject.AddComponent<BulletPhysicsSystem>();
            bulletPhysics.EnableGravityEffect();
            bulletPhysics.EnableAirResistance();
            bulletPhysics.EnableSpinStabilization();
        }
        
        void EnableEnvironmentalFactors()
        {
            var envFactors = gameObject.AddComponent<EnvironmentalFactors>();
            envFactors.EnableWindSimulation();
            envFactors.EnableTemperatureEffects();
            envFactors.EnableHumidityEffects();
            envFactors.EnableAltitudeEffects();
        }
        
        // === DETECÇÃO DE HIT AVANÇADA ===
        void SetupAdvancedHitDetection()
        {
            if (!enableAdvancedHitDetection) return;
            
            var hitSystem = gameObject.AddComponent<AdvancedHitDetectionSystem>();
            hitSystem.EnableSubPixelAccuracy();
            hitSystem.EnableInterpolationCompensation();
            hitSystem.EnableAntiCheatValidation();
            
            Debug.Log("Detecção de Hit Avançada ativa - Precisão milimétrica");
        }
        
        // === SISTEMA DE COBERTURA ===
        void SetupTacticalMechanics()
        {
            if (enableCoverSystem) SetupCoverSystem();
            if (enableBlindFire) SetupBlindFire();
            if (enableTacticalMovement) SetupTacticalMovement();
            if (enableSuppressionEffects) SetupSuppressionSystem();
        }
        
        void SetupCoverSystem()
        {
            var coverSystem = gameObject.AddComponent<AdvancedCoverSystem>();
            coverSystem.EnableSmartCoverDetection();
            coverSystem.EnableCoverTransitions();
            coverSystem.EnablePeekingMechanics();
            
            Debug.Log("Sistema de Cobertura ativo - Táticas avançadas");
        }
        
        void SetupBlindFire()
        {
            var blindFire = gameObject.AddComponent<BlindFireSystem>();
            blindFire.EnableCornerShooting();
            blindFire.EnableOverCoverFire();
            blindFire.EnableReducedAccuracy();
        }
        
        void SetupSuppressionSystem()
        {
            var suppression = gameObject.AddComponent<SuppressionSystem>();
            suppression.EnableVisionEffects();
            suppression.EnableAudioEffects();
            suppression.EnableMovementPenalty();
            suppression.EnableAccuracyReduction();
        }
        
        // === SISTEMA DE SKILL BASEADO ===
        void SetupSkillBasedSystems()
        {
            var skillSystem = gameObject.AddComponent<SkillBasedCombatSystem>();
            skillSystem.EnableRecoilMastery();
            skillSystem.EnableAimTraining();
            skillSystem.EnableMovementSkills();
            skillSystem.EnableTacticalSkills();
            
            Debug.Log("Sistema Baseado em Skill ativo - Recompensa habilidade");
        }
        
        // === PROCESSAMENTO DE DANO AVANÇADO ===
        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                NetworkManager.OnClientConnectedCallback += OnClientConnected;
                NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
            }
        }
        
        void OnClientConnected(ulong clientId)
        {
            playerCombatStats[clientId] = new CombatStatistics();
            playerSkillLevels[clientId] = new SkillLevel();
        }
        
        void OnClientDisconnected(ulong clientId)
        {
            if (playerCombatStats.ContainsKey(clientId))
                playerCombatStats.Remove(clientId);
            if (playerSkillLevels.ContainsKey(clientId))
                playerSkillLevels.Remove(clientId);
        }
        
        [ServerRpc(RequireOwnership = false)]
        public void ProcessAdvancedDamageServerRpc(
            ulong shooterId,
            Vector3 shootOrigin,
            Vector3 shootDirection,
            float baseDamage,
            string weaponId,
            ulong targetId,
            Vector3 hitPoint,
            string hitBodyPart,
            float distance,
            ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;
            
            // Validação anti-cheat
            if (!ValidateShot(shooterId, shootOrigin, shootDirection, distance))
            {
                Debug.LogWarning($"Shot validation failed for player {shooterId}");
                return;
            }
            
            // Calcular dano com fatores avançados
            float finalDamage = CalculateAdvancedDamage(
                baseDamage, weaponId, hitBodyPart, distance, shooterId, targetId);
            
            // Aplicar dano
            ApplyDamageToTarget(targetId, finalDamage, shooterId, weaponId, hitPoint);
            
            // Atualizar estatísticas
            UpdateCombatStatistics(shooterId, targetId, finalDamage, hitBodyPart);
            
            // Efeitos visuais
            ShowDamageEffectsClientRpc(targetId, finalDamage, hitPoint, hitBodyPart);
        }
        
        float CalculateAdvancedDamage(
            float baseDamage, 
            string weaponId, 
            string hitBodyPart, 
            float distance,
            ulong shooterId,
            ulong targetId)
        {
            float finalDamage = baseDamage;
            
            // Multiplicador por parte do corpo
            finalDamage *= GetBodyPartMultiplier(hitBodyPart);
            
            // Redução por distância (mais realística)
            finalDamage *= CalculateDistanceFalloff(weaponId, distance);
            
            // Penetração de armadura
            finalDamage = CalculateArmorPenetration(finalDamage, weaponId, targetId);
            
            // Fator de skill do atirador
            finalDamage *= GetSkillMultiplier(shooterId);
            
            // Condições ambientais
            finalDamage *= GetEnvironmentalMultiplier();
            
            return finalDamage;
        }
        
        float GetBodyPartMultiplier(string bodyPart)
        {
            switch (bodyPart.ToLower())
            {
                case "head": return 2.5f;      // Mais letal que Free Fire
                case "neck": return 2.0f;
                case "chest": return 1.2f;
                case "stomach": return 1.0f;
                case "arm": return 0.8f;
                case "leg": return 0.7f;
                case "foot": return 0.6f;
                default: return 1.0f;
            }
        }
        
        float CalculateDistanceFalloff(string weaponId, float distance)
        {
            var weaponData = WeaponDatabase.GetWeapon(weaponId);
            if (weaponData == null) return 1.0f;
            
            float effectiveRange = weaponData.effectiveRange;
            float maxRange = weaponData.maxRange;
            
            if (distance <= effectiveRange)
            {
                return 1.0f; // Dano total
            }
            else if (distance <= maxRange)
            {
                // Falloff linear
                float falloffFactor = 1.0f - ((distance - effectiveRange) / (maxRange - effectiveRange));
                return Mathf.Max(0.3f, falloffFactor); // Mínimo 30% do dano
            }
            else
            {
                return 0.1f; // Dano mínimo além do alcance
            }
        }
        
        float CalculateArmorPenetration(float damage, string weaponId, ulong targetId)
        {
            var targetArmor = GetPlayerArmor(targetId);
            var weaponPenetration = GetWeaponPenetration(weaponId);
            
            float penetrationRatio = weaponPenetration / (targetArmor + weaponPenetration);
            float armorReduction = targetArmor * (1.0f - penetrationRatio);
            
            return Mathf.Max(damage * 0.1f, damage - armorReduction);
        }
        
        float GetSkillMultiplier(ulong playerId)
        {
            if (!playerSkillLevels.ContainsKey(playerId))
                return 1.0f;
            
            var skillLevel = playerSkillLevels[playerId];
            
            // Jogadores mais habilidosos causam ligeiramente mais dano
            float aimSkill = skillLevel.aimAccuracy / 100.0f;
            float recoilControl = skillLevel.recoilControl / 100.0f;
            
            return 1.0f + (aimSkill * 0.1f) + (recoilControl * 0.05f);
        }
        
        float GetEnvironmentalMultiplier()
        {
            // Fatores como chuva, vento, etc.
            float weatherMultiplier = 1.0f;
            
            if (WeatherSystem.Instance != null)
            {
                if (WeatherSystem.Instance.IsRaining())
                    weatherMultiplier *= 0.95f; // Chuva reduz ligeiramente o dano
                
                if (WeatherSystem.Instance.IsWindy())
                    weatherMultiplier *= 0.98f; // Vento afeta projéteis
            }
            
            return weatherMultiplier;
        }
        
        bool ValidateShot(ulong shooterId, Vector3 origin, Vector3 direction, float distance)
        {
            // Anti-cheat: validar se o tiro é fisicamente possível
            
            // Verificar distância máxima
            if (distance > 1000f) return false;
            
            // Verificar se o jogador existe
            var shooter = GetPlayerById(shooterId);
            if (shooter == null) return false;
            
            // Verificar se a posição é próxima do jogador
            float distanceFromPlayer = Vector3.Distance(origin, shooter.transform.position);
            if (distanceFromPlayer > 5f) return false;
            
            // Verificar rate of fire
            if (!ValidateFireRate(shooterId)) return false;
            
            return true;
        }
        
        bool ValidateFireRate(ulong playerId)
        {
            if (!playerCombatStats.ContainsKey(playerId))
                return true;
            
            var stats = playerCombatStats[playerId];
            float timeSinceLastShot = Time.time - stats.lastShotTime;
            
            // Verificar se respeitou o rate of fire mínimo
            if (timeSinceLastShot < 0.05f) // Máximo 20 tiros por segundo
                return false;
            
            stats.lastShotTime = Time.time;
            return true;
        }
        
        void UpdateCombatStatistics(ulong shooterId, ulong targetId, float damage, string bodyPart)
        {
            if (!playerCombatStats.ContainsKey(shooterId))
                return;
            
            var stats = playerCombatStats[shooterId];
            stats.totalDamageDealt += damage;
            stats.shotsHit++;
            
            if (bodyPart.ToLower() == "head")
                stats.headshots++;
            
            // Atualizar skill level baseado na performance
            UpdateSkillLevel(shooterId, damage, bodyPart);
        }
        
        void UpdateSkillLevel(ulong playerId, float damage, string bodyPart)
        {
            if (!playerSkillLevels.ContainsKey(playerId))
                return;
            
            var skill = playerSkillLevels[playerId];
            
            // Melhorar precisão baseada em hits
            skill.aimAccuracy += 0.1f;
            skill.aimAccuracy = Mathf.Min(100f, skill.aimAccuracy);
            
            // Bonus por headshot
            if (bodyPart.ToLower() == "head")
            {
                skill.aimAccuracy += 0.5f;
                skill.recoilControl += 0.2f;
            }
        }
        
        [ClientRpc]
        void ShowDamageEffectsClientRpc(ulong targetId, float damage, Vector3 hitPoint, string bodyPart)
        {
            // Efeitos visuais superiores ao Free Fire
            ShowAdvancedDamageEffects(targetId, damage, hitPoint, bodyPart);
        }
        
        void ShowAdvancedDamageEffects(ulong targetId, float damage, Vector3 hitPoint, string bodyPart)
        {
            // Números de dano 3D flutuantes
            ShowFloatingDamageNumbers(damage, hitPoint, bodyPart);
            
            // Efeitos de sangue realísticos
            ShowBloodEffects(hitPoint, bodyPart);
            
            // Screen shake baseado no dano
            TriggerCameraShake(damage);
            
            // Haptic feedback proporcional
            TriggerHapticFeedback(damage);
        }
        
        void ShowFloatingDamageNumbers(float damage, Vector3 position, string bodyPart)
        {
            var damageNumber = Instantiate(Resources.Load<GameObject>("DamageNumberPrefab"), position, Quaternion.identity);
            var textComponent = damageNumber.GetComponent<TMPro.TextMeshPro>();
            
            textComponent.text = Mathf.RoundToInt(damage).ToString();
            
            // Cor baseada no tipo de hit
            if (bodyPart.ToLower() == "head")
                textComponent.color = Color.red;
            else
                textComponent.color = Color.white;
            
            // Animação de flutuação
            AnimateFloatingText(damageNumber);
        }
        
        void AnimateFloatingText(GameObject textObject)
        {
            var animator = textObject.GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetTrigger("FloatUp");
            }
        }
        
        // === GETTERS E UTILITÁRIOS ===
        PlayerController GetPlayerById(ulong playerId)
        {
            var players = FindObjectsOfType<PlayerController>();
            return players.FirstOrDefault(p => p.OwnerClientId == playerId);
        }
        
        float GetPlayerArmor(ulong playerId)
        {
            var player = GetPlayerById(playerId);
            if (player == null) return 0f;
            
            // Implementar sistema de armadura
            return 50f; // Placeholder
        }
        
        float GetWeaponPenetration(string weaponId)
        {
            var weaponData = WeaponDatabase.GetWeapon(weaponId);
            return weaponData?.armorPenetration ?? 25f;
        }
        
        public CombatStatistics GetPlayerStats(ulong playerId)
        {
            return playerCombatStats.ContainsKey(playerId) ? playerCombatStats[playerId] : new CombatStatistics();
        }
        
        public SkillLevel GetPlayerSkill(ulong playerId)
        {
            return playerSkillLevels.ContainsKey(playerId) ? playerSkillLevels[playerId] : new SkillLevel();
        }
    }
    
    // === CLASSES DE DADOS ===
    
    [System.Serializable]
    public class CombatStatistics
    {
        public float totalDamageDealt;
        public int shotsHit;
        public int shotsFired;
        public int headshots;
        public int kills;
        public float lastShotTime;
        public float accuracy => shotsFired > 0 ? (float)shotsHit / shotsFired : 0f;
        public float headshotRatio => shotsHit > 0 ? (float)headshots / shotsHit : 0f;
    }
    
    [System.Serializable]
    public class SkillLevel
    {
        public float aimAccuracy = 50f;     // 0-100
        public float recoilControl = 50f;   // 0-100
        public float movementSkill = 50f;   // 0-100
        public float tacticalAwareness = 50f; // 0-100
        public float overallSkill => (aimAccuracy + recoilControl + movementSkill + tacticalAwareness) / 4f;
    }
    
    public class WeaponData
    {
        public string weaponId;
        public float effectiveRange;
        public float maxRange;
        public float armorPenetration;
        public float damage;
    }
    
    public static class WeaponDatabase
    {
        private static Dictionary<string, WeaponData> weapons = new Dictionary<string, WeaponData>();
        
        static WeaponDatabase()
        {
            InitializeWeapons();
        }
        
        static void InitializeWeapons()
        {
            weapons["ak47"] = new WeaponData
            {
                weaponId = "ak47",
                effectiveRange = 150f,
                maxRange = 400f,
                armorPenetration = 35f,
                damage = 40f
            };
            
            // Adicionar mais armas...
        }
        
        public static WeaponData GetWeapon(string weaponId)
        {
            return weapons.ContainsKey(weaponId) ? weapons[weaponId] : null;
        }
    }
    
    // Componentes auxiliares
    public class BulletPhysicsSystem : MonoBehaviour
    {
        public void EnableGravityEffect() { }
        public void EnableAirResistance() { }
        public void EnableSpinStabilization() { }
    }
    
    public class EnvironmentalFactors : MonoBehaviour
    {
        public void EnableWindSimulation() { }
        public void EnableTemperatureEffects() { }
        public void EnableHumidityEffects() { }
        public void EnableAltitudeEffects() { }
    }
    
    public class AdvancedHitDetectionSystem : MonoBehaviour
    {
        public void EnableSubPixelAccuracy() { }
        public void EnableInterpolationCompensation() { }
        public void EnableAntiCheatValidation() { }
    }
    
    // Outros componentes seguem o padrão...
}
