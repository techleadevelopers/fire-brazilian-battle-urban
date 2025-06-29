
using UnityEngine;

namespace ArenaBrasil.Gameplay.Characters
{
    [CreateAssetMenu(fileName = "New Hero Lenda", menuName = "Arena Brasil/Hero Lenda")]
    public class HeroLenda : ScriptableObject
    {
        [Header("Hero Info")]
        public string heroName;
        public string realName;
        public HeroType heroType;
        
        [TextArea(3, 5)]
        public string heroDescription;
        
        [TextArea(2, 3)]
        public string abilityDescription;
        
        [Header("Stats")]
        public float baseHealth = 100f;
        public float moveSpeedMultiplier = 1f;
        public float damageMultiplier = 1f;
        
        [Header("Ability Settings")]
        public float abilityCooldown = 30f;
        public float abilityDuration = 5f;
        public string abilityName;
        
        [Header("Audio & Visual")]
        public AudioClip heroVoiceLine;
        public AudioClip abilitySound;
        public GameObject abilityEffect;
        public Sprite heroIcon;
        public GameObject heroModel;
        
        private float lastAbilityUse = -999f;
        
        public bool CanUseAbility()
        {
            return Time.time >= lastAbilityUse + abilityCooldown;
        }
        
        public void UseAbility(PlayerController player)
        {
            if (!CanUseAbility()) return;
            
            lastAbilityUse = Time.time;
            
            switch (heroType)
            {
                case HeroType.Saci:
                    UseSaciAbility(player);
                    break;
                case HeroType.Curupira:
                    UseCurupiraAbility(player);
                    break;
                case HeroType.IaraMae:
                    UseIaraAbility(player);
                    break;
                case HeroType.Boitata:
                    UseBoitataAbility(player);
                    break;
                case HeroType.MataCavalos:
                    UseMataCavalosAbility(player);
                    break;
            }
        }
        
        void UseSaciAbility(PlayerController player)
        {
            // Saci - Teletransporte + Invisibilidade temporária
            Debug.Log($"{heroName}: Redemoinho Mágico!");
            
            // Teletransportar para posição aleatória próxima
            Vector3 teleportPos = player.transform.position + Random.insideUnitSphere * 10f;
            teleportPos.y = player.transform.position.y;
            player.transform.position = teleportPos;
            
            // Aplicar invisibilidade temporária (implementar sistema de invisibilidade)
            player.StartCoroutine(ApplyInvisibility(player));
        }
        
        void UseCurupiraAbility(PlayerController player)
        {
            // Curupira - Velocidade aumentada + Rastros confusos
            Debug.Log($"{heroName}: Pés Virados!");
            
            // Aumentar velocidade temporariamente
            player.StartCoroutine(ApplySpeedBoost(player));
        }
        
        void UseIaraAbility(PlayerController player)
        {
            // Iara - Cura + Atração de inimigos
            Debug.Log($"{heroName}: Canto da Sereia!");
            
            // Curar o jogador
            // player.Heal(30f); // Implementar sistema de cura
            
            // Atrair inimigos próximos (implementar sistema de atração)
        }
        
        void UseBoitataAbility(PlayerController player)
        {
            // Boitatá - Área de dano de fogo
            Debug.Log($"{heroName}: Chamas Protetoras!");
            
            // Criar área de dano ao redor do jogador
            CreateFireArea(player.transform.position);
        }
        
        void UseMataCavalosAbility(PlayerController player)
        {
            // Mula-sem-Cabeça - Dash com dano
            Debug.Log($"{heroName}: Galope Sombrio!");
            
            // Dash poderoso que causa dano
            Vector3 dashDirection = player.transform.forward;
            player.GetComponent<Rigidbody>().AddForce(dashDirection * 1000f);
        }
        
        System.Collections.IEnumerator ApplyInvisibility(PlayerController player)
        {
            // Tornar jogador parcialmente invisível
            Renderer[] renderers = player.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                Color color = renderer.material.color;
                color.a = 0.3f;
                renderer.material.color = color;
            }
            
            yield return new WaitForSeconds(abilityDuration);
            
            // Restaurar visibilidade
            foreach (var renderer in renderers)
            {
                Color color = renderer.material.color;
                color.a = 1f;
                renderer.material.color = color;
            }
        }
        
        System.Collections.IEnumerator ApplySpeedBoost(PlayerController player)
        {
            float originalSpeed = player.moveSpeed;
            player.moveSpeed *= 2f;
            
            yield return new WaitForSeconds(abilityDuration);
            
            player.moveSpeed = originalSpeed;
        }
        
        void CreateFireArea(Vector3 position)
        {
            if (abilityEffect != null)
            {
                GameObject fireArea = Instantiate(abilityEffect, position, Quaternion.identity);
                Destroy(fireArea, abilityDuration);
                
                // Implementar dano contínuo na área
            }
        }
        
        public void PlayAbilityEffects()
        {
            if (abilitySound != null)
            {
                AudioSource.PlayClipAtPoint(abilitySound, Camera.main.transform.position);
            }
            
            if (heroVoiceLine != null)
            {
                AudioSource.PlayClipAtPoint(heroVoiceLine, Camera.main.transform.position);
            }
        }
    }
    
    public enum HeroType
    {
        Saci,       // Teletransporte + Invisibilidade
        Curupira,   // Velocidade + Confusão de rastros
        IaraMae,    // Cura + Charme/Atração
        Boitata,    // Área de dano de fogo
        MataCavalos // Dash com dano
    }
}
