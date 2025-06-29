
using UnityEngine;
using ArenaBrasil.Loot;

namespace ArenaBrasil.Gameplay.Items
{
    public class LootPickup : MonoBehaviour
    {
        [Header("Loot Data")]
        public int lootId;
        public string itemId;
        public string itemName;
        public ItemRarity rarity;
        
        [Header("Visual")]
        public Renderer itemRenderer;
        public ParticleSystem rarityEffect;
        public AudioSource pickupSound;
        
        private bool canPickup = true;
        private Color rarityColor;
        
        void Start()
        {
            SetupVisuals();
        }
        
        public void Initialize(int id, string item, string name, ItemRarity itemRarity)
        {
            lootId = id;
            itemId = item;
            itemName = name;
            rarity = itemRarity;
            
            SetupVisuals();
        }
        
        void SetupVisuals()
        {
            // Set rarity color
            switch (rarity)
            {
                case ItemRarity.Common:
                    rarityColor = Color.white;
                    break;
                case ItemRarity.Rare:
                    rarityColor = Color.blue;
                    break;
                case ItemRarity.Epic:
                    rarityColor = Color.magenta;
                    break;
                case ItemRarity.Legendary:
                    rarityColor = Color.yellow;
                    break;
            }
            
            if (itemRenderer != null)
            {
                itemRenderer.material.color = rarityColor;
            }
            
            if (rarityEffect != null)
            {
                var main = rarityEffect.main;
                main.startColor = rarityColor;
                rarityEffect.Play();
            }
        }
        
        void OnTriggerEnter(Collider other)
        {
            if (!canPickup) return;
            
            var player = other.GetComponent<PlayerController>();
            if (player != null && player.IsOwner)
            {
                PickupItem(player);
            }
        }
        
        void PickupItem(PlayerController player)
        {
            canPickup = false;
            
            // Play pickup sound
            if (pickupSound != null)
            {
                pickupSound.Play();
            }
            
            // Notify server
            if (LootSystem.Instance != null)
            {
                LootSystem.Instance.PickupLootServerRpc(lootId, player.OwnerClientId);
            }
            
            // Show pickup UI
            ShowPickupNotification();
        }
        
        void ShowPickupNotification()
        {
            // Create pickup notification UI
            Debug.Log($"Picked up: {itemName} ({rarity})");
        }
        
        void Update()
        {
            // Gentle floating animation
            transform.position += Vector3.up * Mathf.Sin(Time.time * 2f) * 0.01f;
            transform.Rotate(Vector3.up, 30f * Time.deltaTime);
        }
    }
}
