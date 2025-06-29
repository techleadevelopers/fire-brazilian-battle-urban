
using UnityEngine;
using Unity.Netcode;
using ArenaBrasil.Gameplay.Characters;
using ArenaBrasil.Gameplay.Weapons;
using ArenaBrasil.Combat;

namespace ArenaBrasil.Gameplay.Characters
{
    public class PlayerController : NetworkBehaviour
    {
        [Header("Movement Settings")]
        public float moveSpeed = 5.0f;
        public float sprintSpeed = 8.0f;
        public float jumpForce = 500.0f;
        public float mouseSensitivity = 2.0f;
        
        [Header("Combat Settings")]
        public Transform shootPoint;
        public LayerMask enemyLayer;
        public float maxHealth = 100f;
        
        [Header("Brazilian Flair")]
        public AudioClip[] combatPhrases;
        public string playerName = "Guerreiro";
        
        // Network Variables
        private NetworkVariable<Vector3> networkPosition = new NetworkVariable<Vector3>();
        private NetworkVariable<float> networkHealth = new NetworkVariable<float>();
        private NetworkVariable<int> networkKills = new NetworkVariable<int>();
        private NetworkVariable<float> networkDamageDealt = new NetworkVariable<float>();
        
        // Components
        private Rigidbody rb;
        private Camera playerCamera;
        private WeaponController weaponController;
        
        // Input
        private Vector2 moveInput;
        private Vector2 mouseInput;
        private bool isGrounded;
        private bool isSprinting;
        
        // Hero Lenda atual
        public HeroLenda currentHero;
        private float lastAbilityUse = 0f;
        
        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            playerCamera = GetComponentInChildren<Camera>();
            weaponController = GetComponent<WeaponController>();
            
            networkHealth.Value = maxHealth;
        }
        
        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                playerCamera.gameObject.SetActive(true);
                Cursor.lockState = CursorLockMode.Locked;
                
                // Set random hero if none assigned
                if (currentHero == null)
                {
                    AssignRandomHero();
                }
            }
            else
            {
                playerCamera.gameObject.SetActive(false);
            }
            
            // Subscribe to health changes
            networkHealth.OnValueChanged += OnHealthChanged;
        }
        
        void AssignRandomHero()
        {
            // Load random Brazilian hero
            HeroType[] heroes = { HeroType.Saci, HeroType.Curupira, HeroType.IaraMae, HeroType.Boitata, HeroType.MataCavalos };
            HeroType selectedHero = heroes[Random.Range(0, heroes.Length)];
            
            // Create hero data
            currentHero = ScriptableObject.CreateInstance<HeroLenda>();
            currentHero.heroType = selectedHero;
            currentHero.heroName = selectedHero.ToString();
            
            switch (selectedHero)
            {
                case HeroType.Saci:
                    currentHero.baseHealth = 90f;
                    currentHero.moveSpeedMultiplier = 1.1f;
                    currentHero.abilityName = "Redemoinho Mágico";
                    break;
                case HeroType.Curupira:
                    currentHero.baseHealth = 100f;
                    currentHero.moveSpeedMultiplier = 1.3f;
                    currentHero.abilityName = "Pés Virados";
                    break;
                case HeroType.IaraMae:
                    currentHero.baseHealth = 110f;
                    currentHero.moveSpeedMultiplier = 0.9f;
                    currentHero.abilityName = "Canto da Sereia";
                    break;
                case HeroType.Boitata:
                    currentHero.baseHealth = 100f;
                    currentHero.damageMultiplier = 1.2f;
                    currentHero.abilityName = "Chamas Protetoras";
                    break;
                case HeroType.MataCavalos:
                    currentHero.baseHealth = 120f;
                    currentHero.moveSpeedMultiplier = 1.0f;
                    currentHero.abilityName = "Galope Sombrio";
                    break;
            }
            
            // Apply hero stats
            maxHealth = currentHero.baseHealth;
            networkHealth.Value = maxHealth;
            moveSpeed *= currentHero.moveSpeedMultiplier;
            sprintSpeed *= currentHero.moveSpeedMultiplier;
            
            Debug.Log($"Hero selected: {currentHero.heroName}");
        }
        
        void Update()
        {
            if (!IsOwner) return;
            
            HandleInput();
            HandleMovement();
            HandleCameraRotation();
            HandleCombat();
        }
        
        void HandleInput()
        {
            moveInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
            mouseInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            
            isSprinting = Input.GetKey(KeyCode.LeftShift);
            
            if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
            {
                Jump();
            }
            
            if (Input.GetMouseButtonDown(0))
            {
                Shoot();
            }
            
            if (Input.GetKeyDown(KeyCode.R))
            {
                Reload();
            }
            
            // Habilidade especial do Herói Lenda
            if (Input.GetKeyDown(KeyCode.Q) && currentHero != null)
            {
                UseHeroAbility();
            }
        }
        
        void HandleMovement()
        {
            Vector3 forward = transform.forward;
            Vector3 right = transform.right;
            
            Vector3 moveDirection = (forward * moveInput.y + right * moveInput.x).normalized;
            float currentSpeed = isSprinting ? sprintSpeed : moveSpeed;
            
            Vector3 velocity = moveDirection * currentSpeed;
            velocity.y = rb.velocity.y;
            
            rb.velocity = velocity;
            
            // Sincronizar posição via network
            if (Vector3.Distance(transform.position, networkPosition.Value) > 0.1f)
            {
                MoveServerRpc(transform.position);
            }
        }
        
        void HandleCameraRotation()
        {
            float mouseX = mouseInput.x * mouseSensitivity;
            float mouseY = mouseInput.y * mouseSensitivity;
            
            transform.Rotate(Vector3.up * mouseX);
            
            Vector3 currentRotation = playerCamera.transform.localEulerAngles;
            currentRotation.x -= mouseY;
            currentRotation.x = Mathf.Clamp(currentRotation.x, -90f, 90f);
            playerCamera.transform.localEulerAngles = currentRotation;
        }
        
        void HandleCombat()
        {
            // Combat logic handled by WeaponController and CombatSystem
        }
        
        void Jump()
        {
            rb.AddForce(Vector3.up * jumpForce);
            isGrounded = false;
        }
        
        void Shoot()
        {
            if (weaponController != null)
            {
                weaponController.Shoot(shootPoint.position, shootPoint.forward);
                
                // Use combat system for authoritative hit detection
                if (CombatSystem.Instance != null)
                {
                    float damage = weaponController.currentWeapon?.damage ?? 25f;
                    string weaponId = weaponController.currentWeapon?.weaponName ?? "Unknown";
                    
                    CombatSystem.Instance.ProcessDamageServerRpc(
                        OwnerClientId, 
                        shootPoint.position, 
                        shootPoint.forward, 
                        damage * currentHero.damageMultiplier,
                        weaponId
                    );
                }
            }
        }
        
        void Reload()
        {
            if (weaponController != null)
            {
                weaponController.Reload();
            }
        }
        
        void UseHeroAbility()
        {
            if (currentHero != null && Time.time >= lastAbilityUse + currentHero.abilityCooldown)
            {
                UseHeroAbilityServerRpc();
                lastAbilityUse = Time.time;
                
                // Play Brazilian combat phrase
                PlayCombatPhrase();
            }
        }
        
        void PlayCombatPhrase()
        {
            if (combatPhrases.Length > 0)
            {
                var audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
                
                audioSource.PlayOneShot(combatPhrases[Random.Range(0, combatPhrases.Length)]);
            }
        }
        
        // Network RPCs
        [ServerRpc]
        void MoveServerRpc(Vector3 newPosition)
        {
            networkPosition.Value = newPosition;
            MoveClientRpc(newPosition);
        }
        
        [ClientRpc]
        void MoveClientRpc(Vector3 newPosition)
        {
            if (!IsOwner)
            {
                transform.position = newPosition;
            }
        }
        
        [ServerRpc]
        void UseHeroAbilityServerRpc()
        {
            if (currentHero != null && currentHero.CanUseAbility())
            {
                currentHero.UseAbility(this);
                UseHeroAbilityClientRpc(currentHero.heroType);
            }
        }
        
        [ClientRpc]
        void UseHeroAbilityClientRpc(HeroType heroType)
        {
            // Executar efeitos visuais da habilidade
            if (currentHero != null && currentHero.heroType == heroType)
            {
                currentHero.PlayAbilityEffects();
                Debug.Log($"{currentHero.heroName} usou {currentHero.abilityName}!");
            }
        }
        
        public void TakeDamage(float damage)
        {
            if (IsServer)
            {
                networkHealth.Value = Mathf.Max(0, networkHealth.Value - damage);
                
                if (networkHealth.Value <= 0)
                {
                    Die();
                }
            }
        }
        
        public void AddKill()
        {
            if (IsServer)
            {
                networkKills.Value++;
            }
        }
        
        public void AddDamage(float damage)
        {
            if (IsServer)
            {
                networkDamageDealt.Value += damage;
            }
        }
        
        void Die()
        {
            // Lógica de morte do jogador
            Debug.Log($"Player {OwnerClientId} foi eliminado!");
            DieClientRpc();
        }
        
        [ClientRpc]
        void DieClientRpc()
        {
            // Efeitos visuais de morte
            if (IsOwner)
            {
                // Show death screen
                Debug.Log("Você foi eliminado!");
            }
            gameObject.SetActive(false);
        }
        
        void OnHealthChanged(float previousValue, float newValue)
        {
            if (IsOwner)
            {
                // Update health UI
                Debug.Log($"Health: {newValue}/{maxHealth}");
            }
        }
        
        void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Ground"))
            {
                isGrounded = true;
            }
        }
        
        public void SetHeroLenda(HeroLenda hero)
        {
            currentHero = hero;
            maxHealth = hero.baseHealth;
            networkHealth.Value = maxHealth;
        }
        
        // Public getters
        public float GetCurrentHealth() => networkHealth.Value;
        public int GetKills() => networkKills.Value;
        public float GetDamageDealt() => networkDamageDealt.Value;
        public string GetPlayerName() => playerName;
    }
}
