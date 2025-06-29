
using UnityEngine;
using Unity.Netcode;

namespace ArenaBrasil.Gameplay.Weapons
{
    public class WeaponController : NetworkBehaviour
    {
        [Header("Weapon Settings")]
        public WeaponData currentWeapon;
        public Transform shootPoint;
        public LayerMask hitLayers;
        
        [Header("Audio")]
        public AudioSource audioSource;
        
        [Header("Effects")]
        public ParticleSystem muzzleFlash;
        public GameObject impactEffect;
        
        // Weapon state
        private float lastShootTime;
        private int currentAmmo;
        private bool isReloading;
        
        void Start()
        {
            if (currentWeapon != null)
            {
                currentAmmo = currentWeapon.magazineSize;
            }
        }
        
        public void Shoot(Vector3 origin, Vector3 direction)
        {
            if (!CanShoot()) return;
            
            ShootServerRpc(origin, direction);
        }
        
        [ServerRpc]
        void ShootServerRpc(Vector3 origin, Vector3 direction)
        {
            if (!CanShoot()) return;
            
            lastShootTime = Time.time;
            currentAmmo--;
            
            // Perform raycast for hit detection
            RaycastHit hit;
            bool didHit = Physics.Raycast(origin, direction, out hit, currentWeapon.range, hitLayers);
            
            if (didHit)
            {
                // Check if hit a player
                var hitPlayer = hit.collider.GetComponent<PlayerController>();
                if (hitPlayer != null)
                {
                    hitPlayer.TakeDamage(currentWeapon.damage);
                }
                
                // Spawn impact effect
                ShootEffectsClientRpc(origin, direction, hit.point, hit.normal, true);
            }
            else
            {
                Vector3 endPoint = origin + direction * currentWeapon.range;
                ShootEffectsClientRpc(origin, direction, endPoint, Vector3.up, false);
            }
        }
        
        [ClientRpc]
        void ShootEffectsClientRpc(Vector3 origin, Vector3 direction, Vector3 hitPoint, Vector3 hitNormal, bool didHit)
        {
            // Play muzzle flash
            if (muzzleFlash != null)
            {
                muzzleFlash.Play();
            }
            
            // Play shoot sound
            if (audioSource != null && currentWeapon.shootSound != null)
            {
                audioSource.PlayOneShot(currentWeapon.shootSound);
            }
            
            // Spawn impact effect
            if (didHit && impactEffect != null)
            {
                GameObject impact = Instantiate(impactEffect, hitPoint, Quaternion.LookRotation(hitNormal));
                Destroy(impact, 2f);
            }
            
            // Draw tracer line (if weapon has tracers)
            if (currentWeapon.hasTracers)
            {
                DrawTracer(origin, hitPoint);
            }
        }
        
        void DrawTracer(Vector3 start, Vector3 end)
        {
            // Implement tracer line rendering
            LineRenderer tracer = GetComponent<LineRenderer>();
            if (tracer != null)
            {
                tracer.SetPosition(0, start);
                tracer.SetPosition(1, end);
                tracer.enabled = true;
                
                // Hide tracer after short time
                Invoke("HideTracer", 0.1f);
            }
        }
        
        void HideTracer()
        {
            LineRenderer tracer = GetComponent<LineRenderer>();
            if (tracer != null)
            {
                tracer.enabled = false;
            }
        }
        
        public void Reload()
        {
            if (isReloading || currentAmmo >= currentWeapon.magazineSize) return;
            
            ReloadServerRpc();
        }
        
        [ServerRpc]
        void ReloadServerRpc()
        {
            if (isReloading) return;
            
            isReloading = true;
            ReloadClientRpc();
            
            Invoke("CompleteReload", currentWeapon.reloadTime);
        }
        
        [ClientRpc]
        void ReloadClientRpc()
        {
            // Play reload animation and sound
            if (audioSource != null && currentWeapon.reloadSound != null)
            {
                audioSource.PlayOneShot(currentWeapon.reloadSound);
            }
        }
        
        void CompleteReload()
        {
            currentAmmo = currentWeapon.magazineSize;
            isReloading = false;
        }
        
        bool CanShoot()
        {
            return currentWeapon != null && 
                   currentAmmo > 0 && 
                   !isReloading && 
                   Time.time >= lastShootTime + (1f / currentWeapon.fireRate);
        }
        
        public void EquipWeapon(WeaponData newWeapon)
        {
            currentWeapon = newWeapon;
            currentAmmo = newWeapon.magazineSize;
            isReloading = false;
        }
        
        public int GetCurrentAmmo()
        {
            return currentAmmo;
        }
        
        public bool IsReloading()
        {
            return isReloading;
        }
        
        public float GetReloadProgress()
        {
            if (!isReloading) return 1f;
            
            float elapsed = Time.time - (lastShootTime + (1f / currentWeapon.fireRate));
            return Mathf.Clamp01(elapsed / currentWeapon.reloadTime);
        }
    }
    
    [CreateAssetMenu(fileName = "New Weapon", menuName = "Arena Brasil/Weapon Data")]
    public class WeaponData : ScriptableObject
    {
        [Header("Basic Info")]
        public string weaponName;
        public WeaponType weaponType;
        public Sprite weaponIcon;
        public GameObject weaponModel;
        
        [Header("Stats")]
        public float damage = 25f;
        public float range = 100f;
        public float fireRate = 10f; // shots per second
        public int magazineSize = 30;
        public float reloadTime = 2f;
        public float accuracy = 0.95f;
        
        [Header("Visual & Audio")]
        public AudioClip shootSound;
        public AudioClip reloadSound;
        public bool hasTracers = true;
        public ParticleSystem muzzleFlashPrefab;
        
        [Header("Brazilian Weapon Names")]
        [TextArea(2, 3)]
        public string description = "Arma desenvolvida no Brasil";
    }
    
    public enum WeaponType
    {
        AssaultRifle,   // Fuzil FAL brasileiro
        SMG,           // Submetralhadora INA
        Shotgun,       // Espingarda Boito
        SniperRifle,   // Rifle de precisão
        Pistol,        // Pistola Taurus
        LMG            // Metralhadora leve
    }
}

namespace ArenaBrasil.Gameplay.Weapons
{
    public class WeaponController : MonoBehaviour
    {
        [Header("Current Weapon")]
        public BrazilianWeapon currentWeapon;
        
        [Header("Weapon Slots")]
        public BrazilianWeapon[] weaponSlots = new BrazilianWeapon[3];
        public int currentSlot = 0;
        
        [Header("Shooting")]
        public Transform firePoint;
        public LayerMask targetLayers;
        public float maxRange = 500f;
        
        [Header("Ammo")]
        public int currentAmmo;
        public int reserveAmmo;
        
        private float lastShotTime;
        private bool isReloading;
        private PlayerController playerController;
        
        void Start()
        {
            playerController = GetComponent<PlayerController>();
            
            // Initialize with default weapon if none equipped
            if (currentWeapon == null && weaponSlots[0] != null)
            {
                EquipWeapon(0);
            }
        }
        
        void Update()
        {
            HandleWeaponSwitching();
            HandleReloading();
        }
        
        void HandleWeaponSwitching()
        {
            if (isReloading) return;
            
            if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchToWeapon(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchToWeapon(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchToWeapon(2);
            
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll > 0) SwitchToNextWeapon();
            if (scroll < 0) SwitchToPreviousWeapon();
        }
        
        void HandleReloading()
        {
            if (Input.GetKeyDown(KeyCode.R) && !isReloading)
            {
                Reload();
            }
        }
        
        public void Shoot(Vector3 origin, Vector3 direction)
        {
            if (currentWeapon == null || isReloading) return;
            if (Time.time < lastShotTime + currentWeapon.fireRate) return;
            if (currentAmmo <= 0)
            {
                // Auto reload if no ammo
                Reload();
                return;
            }
            
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
            if (ArenaBrasil.UI.UIManager.Instance != null)
            {
                ArenaBrasil.UI.UIManager.Instance.UpdateAmmoDisplay(currentAmmo, currentWeapon.magazineSize);
            }
        }
        
        void FireHitscan(Vector3 origin, Vector3 direction)
        {
            // Apply weapon spread
            Vector3 spread = CalculateSpread(direction);
            
            if (Physics.Raycast(origin, spread, out RaycastHit hit, maxRange, targetLayers))
            {
                // Handle hit
                ProcessHit(hit);
            }
        }
        
        void FireShotgun(Vector3 origin, Vector3 direction)
        {
            int pellets = 8; // Shotgun pellets
            for (int i = 0; i < pellets; i++)
            {
                Vector3 spread = CalculateSpread(direction, 0.3f); // Higher spread for shotgun
                
                if (Physics.Raycast(origin, spread, out RaycastHit hit, maxRange / 2, targetLayers))
                {
                    ProcessHit(hit, currentWeapon.damage / pellets);
                }
            }
        }
        
        Vector3 CalculateSpread(Vector3 direction, float spreadMultiplier = 1f)
        {
            float spread = (1f - currentWeapon.accuracy) * spreadMultiplier;
            
            Vector3 randomSpread = new Vector3(
                Random.Range(-spread, spread),
                Random.Range(-spread, spread),
                Random.Range(-spread, spread)
            );
            
            return (direction + randomSpread).normalized;
        }
        
        void ProcessHit(RaycastHit hit, float? customDamage = null)
        {
            float damage = customDamage ?? currentWeapon.damage;
            
            // Check if hit a player
            var targetPlayer = hit.collider.GetComponent<PlayerController>();
            if (targetPlayer != null && targetPlayer != playerController)
            {
                // Damage will be processed by CombatSystem
                return;
            }
            
            // Handle environmental hits
            PlayImpactEffects(hit.point, hit.normal);
        }
        
        void PlayShootEffects()
        {
            // Muzzle flash
            if (currentWeapon.muzzleFlashPrefab != null && firePoint != null)
            {
                var flash = Instantiate(currentWeapon.muzzleFlashPrefab, firePoint.position, firePoint.rotation);
                Destroy(flash.gameObject, 0.5f);
            }
            
            // Sound
            if (ArenaBrasil.Audio.AudioManager.Instance != null)
            {
                ArenaBrasil.Audio.AudioManager.Instance.PlayWeaponSound(currentWeapon.weaponName);
            }
        }
        
        void PlayImpactEffects(Vector3 position, Vector3 normal)
        {
            // Play impact sound
            if (ArenaBrasil.Audio.AudioManager.Instance != null)
            {
                ArenaBrasil.Audio.AudioManager.Instance.PlayImpactSound();
            }
            
            // TODO: Add visual impact effects
        }
        
        public void Reload()
        {
            if (isReloading || reserveAmmo <= 0 || currentAmmo >= currentWeapon.magazineSize) return;
            
            StartCoroutine(ReloadCoroutine());
        }
        
        System.Collections.IEnumerator ReloadCoroutine()
        {
            isReloading = true;
            
            // Play reload sound
            if (ArenaBrasil.Audio.AudioManager.Instance != null)
            {
                ArenaBrasil.Audio.AudioManager.Instance.PlayReloadSound();
            }
            
            yield return new WaitForSeconds(currentWeapon.reloadTime);
            
            // Calculate ammo to reload
            int ammoNeeded = currentWeapon.magazineSize - currentAmmo;
            int ammoToReload = Mathf.Min(ammoNeeded, reserveAmmo);
            
            currentAmmo += ammoToReload;
            reserveAmmo -= ammoToReload;
            
            isReloading = false;
            
            // Update UI
            if (ArenaBrasil.UI.UIManager.Instance != null)
            {
                ArenaBrasil.UI.UIManager.Instance.UpdateAmmoDisplay(currentAmmo, currentWeapon.magazineSize);
            }
        }
        
        public void EquipWeapon(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= weaponSlots.Length) return;
            if (weaponSlots[slotIndex] == null) return;
            
            currentWeapon = weaponSlots[slotIndex];
            currentSlot = slotIndex;
            currentAmmo = currentWeapon.magazineSize;
            reserveAmmo = currentWeapon.magazineSize * 3; // Default reserve ammo
            
            // Update UI
            if (ArenaBrasil.UI.UIManager.Instance != null)
            {
                ArenaBrasil.UI.UIManager.Instance.UpdateAmmoDisplay(currentAmmo, currentWeapon.magazineSize);
            }
        }
        
        public void SwitchToWeapon(int slotIndex)
        {
            if (weaponSlots[slotIndex] != null)
            {
                EquipWeapon(slotIndex);
            }
        }
        
        public void SwitchToNextWeapon()
        {
            int nextSlot = (currentSlot + 1) % weaponSlots.Length;
            for (int i = 0; i < weaponSlots.Length; i++)
            {
                if (weaponSlots[nextSlot] != null)
                {
                    EquipWeapon(nextSlot);
                    return;
                }
                nextSlot = (nextSlot + 1) % weaponSlots.Length;
            }
        }
        
        public void SwitchToPreviousWeapon()
        {
            int prevSlot = currentSlot - 1;
            if (prevSlot < 0) prevSlot = weaponSlots.Length - 1;
            
            for (int i = 0; i < weaponSlots.Length; i++)
            {
                if (weaponSlots[prevSlot] != null)
                {
                    EquipWeapon(prevSlot);
                    return;
                }
                prevSlot--;
                if (prevSlot < 0) prevSlot = weaponSlots.Length - 1;
            }
        }
        
        public void AddWeapon(BrazilianWeapon weapon)
        {
            // Find empty slot or replace weapon of same type
            for (int i = 0; i < weaponSlots.Length; i++)
            {
                if (weaponSlots[i] == null || weaponSlots[i].weaponType == weapon.weaponType)
                {
                    weaponSlots[i] = weapon;
                    if (currentWeapon == null)
                    {
                        EquipWeapon(i);
                    }
                    return;
                }
            }
        }
        
        public void AddAmmo(string weaponType, int amount)
        {
            if (currentWeapon != null && currentWeapon.weaponType.ToString() == weaponType)
            {
                reserveAmmo += amount;
            }
        }
        
        public bool CanShoot()
        {
            return currentWeapon != null && !isReloading && currentAmmo > 0 && 
                   Time.time >= lastShotTime + currentWeapon.fireRate;
        }
}
