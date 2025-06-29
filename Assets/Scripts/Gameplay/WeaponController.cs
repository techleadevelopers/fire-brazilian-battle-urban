
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using ArenaBrasil.Gameplay.Weapons;

namespace ArenaBrasil.Gameplay.Weapons
{
    public class WeaponController : NetworkBehaviour
    {
        [Header("Current Weapon")]
        public BrazilianWeapon currentWeapon;
        
        [Header("Weapon Slots - Free Fire Style")]
        public BrazilianWeapon primaryWeapon;     // Fuzil/SMG
        public BrazilianWeapon secondaryWeapon;   // Pistola
        public BrazilianWeapon meleeWeapon;       // Taco/Machado/Faca
        public int currentSlot = 0;
        
        [Header("Urban Combat Settings")]
        public Transform firePoint;
        public LayerMask targetLayers;
        public float maxRange = 500f;
        public bool autoAim = false; // Free Fire style auto-aim
        
        [Header("Ammo System")]
        public int currentAmmo;
        public int reserveAmmo;
        public Dictionary<string, int> ammoInventory = new Dictionary<string, int>();
        
        [Header("Brazilian Street Weapons")]
        public List<BrazilianWeapon> availableWeapons = new List<BrazilianWeapon>();
        
        private float lastShotTime;
        private bool isReloading;
        private PlayerController playerController;
        private Vector3 recoilPattern = Vector3.zero;
        
        void Start()
        {
            playerController = GetComponent<PlayerController>();
            InitializeBrazilianWeapons();
            
            // Start with default weapons
            if (primaryWeapon == null) EquipDefaultWeapons();
        }
        
        void InitializeBrazilianWeapons()
        {
            // Pistolas brasileiras
            availableWeapons.Add(new BrazilianWeapon
            {
                weaponId = "taurus_pt92",
                weaponName = "Taurus PT92",
                weaponType = WeaponType.Pistol,
                damage = 45f,
                fireRate = 0.3f,
                range = 80f,
                accuracy = 0.85f,
                magazineSize = 15,
                reloadTime = 1.8f,
                isAutomatic = false,
                rarity = WeaponRarity.Common,
                skinPrice = 0,
                description = "Pistola brasileira padrão da polícia"
            });
            
            // Fuzis de assalto
            availableWeapons.Add(new BrazilianWeapon
            {
                weaponId = "imbel_ia2",
                weaponName = "IMBEL IA2",
                weaponType = WeaponType.AssaultRifle,
                damage = 35f,
                fireRate = 0.12f,
                range = 200f,
                accuracy = 0.78f,
                magazineSize = 30,
                reloadTime = 2.5f,
                isAutomatic = true,
                rarity = WeaponRarity.Rare,
                skinPrice = 100,
                description = "Fuzil de assalto brasileiro do Exército"
            });
            
            // Submetralhadoras
            availableWeapons.Add(new BrazilianWeapon
            {
                weaponId = "uru_smg",
                weaponName = "URU SMG",
                weaponType = WeaponType.SMG,
                damage = 28f,
                fireRate = 0.08f,
                range = 120f,
                accuracy = 0.72f,
                magazineSize = 40,
                reloadTime = 2.0f,
                isAutomatic = true,
                rarity = WeaponRarity.Common,
                skinPrice = 50,
                description = "Submetralhadora nacional"
            });
            
            // Espingarda brasileira
            availableWeapons.Add(new BrazilianWeapon
            {
                weaponId = "boito_shotgun",
                weaponName = "Boito 12",
                weaponType = WeaponType.Shotgun,
                damage = 80f,
                fireRate = 0.8f,
                range = 40f,
                accuracy = 0.65f,
                magazineSize = 8,
                reloadTime = 3.0f,
                isAutomatic = false,
                rarity = WeaponRarity.Common,
                skinPrice = 75,
                description = "Espingarda brasileira de caça"
            });
            
            // Rifle de precisão
            availableWeapons.Add(new BrazilianWeapon
            {
                weaponId = "sniper_nacional",
                weaponName = "Mosquefal",
                weaponType = WeaponType.SniperRifle,
                damage = 120f,
                fireRate = 1.5f,
                range = 400f,
                accuracy = 0.95f,
                magazineSize = 5,
                reloadTime = 3.5f,
                isAutomatic = false,
                rarity = WeaponRarity.Epic,
                skinPrice = 300,
                description = "Rifle de precisão das forças especiais"
            });
            
            // ARMAS BRANCAS URBANAS
            
            // Taco de baseball
            availableWeapons.Add(new BrazilianWeapon
            {
                weaponId = "taco_baseball",
                weaponName = "Taco de Baseball",
                weaponType = WeaponType.Melee,
                damage = 65f,
                fireRate = 0.6f,
                range = 2f,
                accuracy = 1.0f,
                magazineSize = 1,
                reloadTime = 0f,
                isAutomatic = false,
                rarity = WeaponRarity.Common,
                skinPrice = 25,
                description = "Clássico taco de baseball urbano"
            });
            
            // Machado
            availableWeapons.Add(new BrazilianWeapon
            {
                weaponId = "machado_lenhador",
                weaponName = "Machado do Curupira",
                weaponType = WeaponType.Melee,
                damage = 85f,
                fireRate = 1.0f,
                range = 2.5f,
                accuracy = 1.0f,
                magazineSize = 1,
                reloadTime = 0f,
                isAutomatic = false,
                rarity = WeaponRarity.Rare,
                skinPrice = 150,
                description = "Machado lendário do protetor da floresta"
            });
            
            // Facão
            availableWeapons.Add(new BrazilianWeapon
            {
                weaponId = "facao_sertao",
                weaponName = "Facão Nordestino",
                weaponType = WeaponType.Melee,
                damage = 70f,
                fireRate = 0.4f,
                range = 1.8f,
                accuracy = 1.0f,
                magazineSize = 1,
                reloadTime = 0f,
                isAutomatic = false,
                rarity = WeaponRarity.Common,
                skinPrice = 40,
                description = "Facão tradicional do sertão brasileiro"
            });
            
            // Katana (influência japonesa no Brasil)
            availableWeapons.Add(new BrazilianWeapon
            {
                weaponId = "katana_liberdade",
                weaponName = "Katana da Liberdade",
                weaponType = WeaponType.Melee,
                damage = 95f,
                fireRate = 0.8f,
                range = 3f,
                accuracy = 1.0f,
                magazineSize = 1,
                reloadTime = 0f,
                isAutomatic = false,
                rarity = WeaponRarity.Legendary,
                skinPrice = 500,
                description = "Katana forjada no bairro da Liberdade"
            });
        }
        
        void EquipDefaultWeapons()
        {
            primaryWeapon = GetWeapon("uru_smg");
            secondaryWeapon = GetWeapon("taurus_pt92");
            meleeWeapon = GetWeapon("facao_sertao");
            currentWeapon = primaryWeapon;
            
            // Initialize ammo
            currentAmmo = currentWeapon.magazineSize;
            InitializeAmmoInventory();
        }
        
        void InitializeAmmoInventory()
        {
            ammoInventory["pistol"] = 120;
            ammoInventory["rifle"] = 180;
            ammoInventory["shotgun"] = 60;
            ammoInventory["sniper"] = 20;
        }
        
        void Update()
        {
            HandleWeaponSwitching();
            HandleShooting();
            HandleReloading();
            HandleMeleeAttack();
        }
        
        void HandleWeaponSwitching()
        {
            if (isReloading) return;
            
            // Weapon switching like Free Fire
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Q))
                SwitchToPrimary();
            if (Input.GetKeyDown(KeyCode.Alpha2))
                SwitchToSecondary();
            if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.V))
                SwitchToMelee();
                
            // Mouse wheel switching
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll > 0) SwitchToNextWeapon();
            if (scroll < 0) SwitchToPreviousWeapon();
        }
        
        void HandleShooting()
        {
            if (currentWeapon.weaponType == WeaponType.Melee) return;
            
            bool shouldShoot = currentWeapon.isAutomatic ? 
                Input.GetMouseButton(0) : Input.GetMouseButtonDown(0);
                
            if (shouldShoot && CanShoot())
            {
                Vector3 shootDirection = GetShootDirection();
                Shoot(firePoint.position, shootDirection);
            }
        }
        
        void HandleMeleeAttack()
        {
            if (currentWeapon.weaponType != WeaponType.Melee) return;
            
            if (Input.GetMouseButtonDown(0) && CanMeleeAttack())
            {
                PerformMeleeAttack();
            }
        }
        
        Vector3 GetShootDirection()
        {
            Vector3 direction = Camera.main.transform.forward;
            
            // Add auto-aim assistance (Free Fire style)
            if (autoAim)
            {
                direction = GetAutoAimDirection(direction);
            }
            
            // Add recoil pattern
            direction += CalculateRecoil();
            
            return direction.normalized;
        }
        
        Vector3 GetAutoAimDirection(Vector3 baseDirection)
        {
            Collider[] nearbyTargets = Physics.OverlapSphere(transform.position, 50f, targetLayers);
            
            foreach (var target in nearbyTargets)
            {
                if (target.gameObject != gameObject)
                {
                    Vector3 targetDirection = (target.transform.position - firePoint.position).normalized;
                    float angle = Vector3.Angle(baseDirection, targetDirection);
                    
                    if (angle < 15f) // Auto-aim cone
                    {
                        return Vector3.Lerp(baseDirection, targetDirection, 0.3f);
                    }
                }
            }
            
            return baseDirection;
        }
        
        Vector3 CalculateRecoil()
        {
            // Different recoil patterns for each weapon type
            Vector3 recoil = Vector3.zero;
            
            switch (currentWeapon.weaponType)
            {
                case WeaponType.AssaultRifle:
                    recoil = new Vector3(
                        Random.Range(-0.05f, 0.05f),
                        Random.Range(0.02f, 0.08f),
                        0f
                    );
                    break;
                case WeaponType.SMG:
                    recoil = new Vector3(
                        Random.Range(-0.08f, 0.08f),
                        Random.Range(0.01f, 0.04f),
                        0f
                    );
                    break;
                case WeaponType.Shotgun:
                    recoil = new Vector3(
                        Random.Range(-0.1f, 0.1f),
                        Random.Range(0.05f, 0.15f),
                        0f
                    );
                    break;
            }
            
            recoil *= (1f - currentWeapon.accuracy);
            return recoil;
        }
        
        public void Shoot(Vector3 origin, Vector3 direction)
        {
            if (!CanShoot()) return;
            
            lastShotTime = Time.time;
            currentAmmo--;
            
            // Play effects
            PlayShootEffects();
            
            // Handle different weapon types
            switch (currentWeapon.weaponType)
            {
                case WeaponType.Shotgun:
                    FireShotgun(origin, direction);
                    break;
                default:
                    FireHitscan(origin, direction);
                    break;
            }
            
            // Update UI
            UpdateAmmoUI();
            
            // Auto reload when empty
            if (currentAmmo <= 0)
            {
                Reload();
            }
        }
        
        void FireShotgun(Vector3 origin, Vector3 direction)
        {
            int pellets = 8;
            float spreadAngle = 0.3f;
            
            for (int i = 0; i < pellets; i++)
            {
                Vector3 spread = CalculateSpread(direction, spreadAngle);
                
                if (Physics.Raycast(origin, spread, out RaycastHit hit, currentWeapon.range, targetLayers))
                {
                    float damage = currentWeapon.damage / pellets;
                    ProcessHit(hit, damage);
                }
            }
        }
        
        void FireHitscan(Vector3 origin, Vector3 direction)
        {
            if (Physics.Raycast(origin, direction, out RaycastHit hit, currentWeapon.range, targetLayers))
            {
                ProcessHit(hit, currentWeapon.damage);
            }
        }
        
        void ProcessHit(RaycastHit hit, float damage)
        {
            var targetPlayer = hit.collider.GetComponent<PlayerController>();
            if (targetPlayer != null && targetPlayer != playerController)
            {
                // Calculate damage multiplier based on hit location
                float multiplier = GetDamageMultiplier(hit.collider.tag);
                float finalDamage = damage * multiplier;
                
                // Send damage to combat system
                if (ArenaBrasil.Combat.CombatSystem.Instance != null)
                {
                    ArenaBrasil.Combat.CombatSystem.Instance.ProcessDamageServerRpc(
                        playerController.OwnerClientId,
                        firePoint.position,
                        hit.point - firePoint.position,
                        finalDamage,
                        currentWeapon.weaponId
                    );
                }
            }
            
            PlayImpactEffects(hit.point, hit.normal);
        }
        
        float GetDamageMultiplier(string hitTag)
        {
            switch (hitTag)
            {
                case "Head": return 2.0f;
                case "Body": return 1.0f;
                case "Limb": return 0.8f;
                default: return 1.0f;
            }
        }
        
        void PerformMeleeAttack()
        {
            lastShotTime = Time.time;
            
            // Play melee animation and sound
            PlayMeleeEffects();
            
            // Check for hits in melee range
            Collider[] hits = Physics.OverlapSphere(firePoint.position, currentWeapon.range, targetLayers);
            
            foreach (var hit in hits)
            {
                var targetPlayer = hit.GetComponent<PlayerController>();
                if (targetPlayer != null && targetPlayer != playerController)
                {
                    // Send melee damage
                    if (ArenaBrasil.Combat.CombatSystem.Instance != null)
                    {
                        ArenaBrasil.Combat.CombatSystem.Instance.ProcessDamageServerRpc(
                            playerController.OwnerClientId,
                            firePoint.position,
                            (hit.transform.position - firePoint.position).normalized,
                            currentWeapon.damage,
                            currentWeapon.weaponId
                        );
                    }
                    break; // Only hit one target
                }
            }
        }
        
        public void Reload()
        {
            if (isReloading || currentWeapon.weaponType == WeaponType.Melee) return;
            if (currentAmmo >= currentWeapon.magazineSize) return;
            
            string ammoType = GetAmmoType(currentWeapon.weaponType);
            if (!ammoInventory.ContainsKey(ammoType) || ammoInventory[ammoType] <= 0) return;
            
            StartCoroutine(ReloadCoroutine(ammoType));
        }
        
        System.Collections.IEnumerator ReloadCoroutine(string ammoType)
        {
            isReloading = true;
            
            // Play reload effects
            PlayReloadEffects();
            
            yield return new WaitForSeconds(currentWeapon.reloadTime);
            
            // Calculate reload amount
            int ammoNeeded = currentWeapon.magazineSize - currentAmmo;
            int ammoAvailable = ammoInventory[ammoType];
            int ammoToReload = Mathf.Min(ammoNeeded, ammoAvailable);
            
            currentAmmo += ammoToReload;
            ammoInventory[ammoType] -= ammoToReload;
            
            isReloading = false;
            UpdateAmmoUI();
        }
        
        string GetAmmoType(WeaponType weaponType)
        {
            switch (weaponType)
            {
                case WeaponType.Pistol: return "pistol";
                case WeaponType.AssaultRifle: 
                case WeaponType.SMG: return "rifle";
                case WeaponType.Shotgun: return "shotgun";
                case WeaponType.SniperRifle: return "sniper";
                default: return "rifle";
            }
        }
        
        // Weapon switching methods
        void SwitchToPrimary()
        {
            if (primaryWeapon != null) EquipWeapon(primaryWeapon);
        }
        
        void SwitchToSecondary()
        {
            if (secondaryWeapon != null) EquipWeapon(secondaryWeapon);
        }
        
        void SwitchToMelee()
        {
            if (meleeWeapon != null) EquipWeapon(meleeWeapon);
        }
        
        void SwitchToNextWeapon()
        {
            if (currentWeapon == primaryWeapon && secondaryWeapon != null)
                EquipWeapon(secondaryWeapon);
            else if (currentWeapon == secondaryWeapon && meleeWeapon != null)
                EquipWeapon(meleeWeapon);
            else if (primaryWeapon != null)
                EquipWeapon(primaryWeapon);
        }
        
        void SwitchToPreviousWeapon()
        {
            if (currentWeapon == meleeWeapon && secondaryWeapon != null)
                EquipWeapon(secondaryWeapon);
            else if (currentWeapon == secondaryWeapon && primaryWeapon != null)
                EquipWeapon(primaryWeapon);
            else if (meleeWeapon != null)
                EquipWeapon(meleeWeapon);
        }
        
        void EquipWeapon(BrazilianWeapon weapon)
        {
            currentWeapon = weapon;
            currentAmmo = weapon.weaponType == WeaponType.Melee ? 1 : weapon.magazineSize;
            UpdateAmmoUI();
        }
        
        public void PickupWeapon(BrazilianWeapon weapon)
        {
            // Replace weapon based on type
            switch (weapon.weaponType)
            {
                case WeaponType.Pistol:
                    secondaryWeapon = weapon;
                    break;
                case WeaponType.AssaultRifle:
                case WeaponType.SMG:
                case WeaponType.SniperRifle:
                case WeaponType.Shotgun:
                    if (primaryWeapon == null || weapon.rarity > primaryWeapon.rarity)
                        primaryWeapon = weapon;
                    break;
                case WeaponType.Melee:
                    if (meleeWeapon == null || weapon.rarity > meleeWeapon.rarity)
                        meleeWeapon = weapon;
                    break;
            }
            
            // Auto-equip if no current weapon
            if (currentWeapon == null)
            {
                EquipWeapon(weapon);
            }
        }
        
        public void AddAmmo(string ammoType, int amount)
        {
            if (ammoInventory.ContainsKey(ammoType))
            {
                ammoInventory[ammoType] += amount;
            }
            else
            {
                ammoInventory[ammoType] = amount;
            }
            
            UpdateAmmoUI();
        }
        
        BrazilianWeapon GetWeapon(string weaponId)
        {
            return availableWeapons.Find(w => w.weaponId == weaponId);
        }
        
        Vector3 CalculateSpread(Vector3 direction, float spreadAmount)
        {
            Vector3 spread = new Vector3(
                Random.Range(-spreadAmount, spreadAmount),
                Random.Range(-spreadAmount, spreadAmount),
                Random.Range(-spreadAmount, spreadAmount)
            );
            
            return (direction + spread).normalized;
        }
        
        bool CanShoot()
        {
            return currentWeapon != null && 
                   currentWeapon.weaponType != WeaponType.Melee &&
                   currentAmmo > 0 && 
                   !isReloading && 
                   Time.time >= lastShotTime + currentWeapon.fireRate;
        }
        
        bool CanMeleeAttack()
        {
            return currentWeapon != null && 
                   currentWeapon.weaponType == WeaponType.Melee &&
                   Time.time >= lastShotTime + currentWeapon.fireRate;
        }
        
        void PlayShootEffects()
        {
            if (ArenaBrasil.Audio.AudioManager.Instance != null)
            {
                ArenaBrasil.Audio.AudioManager.Instance.PlayWeaponSound(currentWeapon.weaponName);
            }
        }
        
        void PlayMeleeEffects()
        {
            if (ArenaBrasil.Audio.AudioManager.Instance != null)
            {
                ArenaBrasil.Audio.AudioManager.Instance.PlayMeleeSound(currentWeapon.weaponName);
            }
        }
        
        void PlayReloadEffects()
        {
            if (ArenaBrasil.Audio.AudioManager.Instance != null)
            {
                ArenaBrasil.Audio.AudioManager.Instance.PlayReloadSound();
            }
        }
        
        void PlayImpactEffects(Vector3 position, Vector3 normal)
        {
            if (ArenaBrasil.Audio.AudioManager.Instance != null)
            {
                ArenaBrasil.Audio.AudioManager.Instance.PlayImpactSound();
            }
        }
        
        void UpdateAmmoUI()
        {
            if (ArenaBrasil.UI.UIManager.Instance != null)
            {
                ArenaBrasil.UI.UIManager.Instance.UpdateAmmoDisplay(currentAmmo, currentWeapon.magazineSize);
            }
        }
    }
    
    [System.Serializable]
    public class BrazilianWeapon
    {
        public string weaponId;
        public string weaponName;
        public WeaponType weaponType;
        public float damage;
        public float fireRate; // Time between shots
        public float range;
        public float accuracy; // 0.0 to 1.0
        public int magazineSize;
        public float reloadTime;
        public bool isAutomatic;
        public WeaponRarity rarity;
        public int skinPrice;
        public string description;
        public Sprite weaponIcon;
        public GameObject weaponModel;
        public List<WeaponSkin> availableSkins = new List<WeaponSkin>();
    }
    
    [System.Serializable]
    public class WeaponSkin
    {
        public string skinId;
        public string skinName;
        public WeaponRarity rarity;
        public int price;
        public Material skinMaterial;
        public bool isLimited;
        public string description;
    }
    
    public enum WeaponType
    {
        Pistol,
        AssaultRifle,
        SMG,
        Shotgun,
        SniperRifle,
        Melee
    }
    
    public enum WeaponRarity
    {
        Common,
        Rare,
        Epic,
        Legendary
    }
}
