
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
        
        [Header("Urban Movement System")]
        public float wallRunSpeed = 8f;
        public float wallRunTime = 3f;
        public float wallJumpForce = 400f;
        public float slideSpeed = 12f;
        public float slideDuration = 1.5f;
        public float climbSpeed = 3f;
        public LayerMask wallLayers = -1;
        public LayerMask groundLayers = -1;
        
        private bool isWallRunning;
        private bool isSliding;
        private bool isClimbing;
        private float wallRunTimer;
        private float slideTimer;
        private Vector3 wallNormal;
        private bool wasGrounded;
        
        void HandleMovement()
        {
            // Detecção de solo avançada
            CheckGroundStatus();
            
            // Sistema de movimento base
            Vector3 moveDirection = CalculateMovementDirection();
            
            // Sistemas especiais de movimento urbano
            if (CanWallRun() && !isGrounded && moveDirection.magnitude > 0.1f)
            {
                HandleWallRunning(moveDirection);
            }
            else if (CanSlide() && isSprinting && isGrounded)
            {
                HandleSliding(moveDirection);
            }
            else if (CanClimb())
            {
                HandleClimbing();
            }
            else
            {
                HandleNormalMovement(moveDirection);
            }
            
            // Efeitos visuais de movimento
            HandleMovementEffects();
            
            // Sincronização de rede otimizada
            SyncPositionOptimized();
        }
        
        Vector3 CalculateMovementDirection()
        {
            // Movimento relativo à câmera (estilo Free Fire)
            Vector3 forward = playerCamera.transform.forward;
            Vector3 right = playerCamera.transform.right;
            
            // Normalizar para movimento no plano
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();
            
            return (forward * moveInput.y + right * moveInput.x).normalized;
        }
        
        void HandleWallRunning(Vector3 moveDirection)
        {
            if (!isWallRunning)
            {
                isWallRunning = true;
                wallRunTimer = wallRunTime;
                
                // Efeito sonoro
                PlayMovementSound("wallrun_start");
            }
            
            wallRunTimer -= Time.deltaTime;
            
            if (wallRunTimer <= 0f || moveDirection.magnitude < 0.1f)
            {
                StopWallRunning();
                return;
            }
            
            // Movimento ao longo da parede
            Vector3 wallForward = Vector3.Cross(wallNormal, Vector3.up);
            if (Vector3.Dot(wallForward, moveDirection) < 0)
                wallForward = -wallForward;
            
            rb.velocity = wallForward * wallRunSpeed + Vector3.up * 2f;
            
            // Inclinação visual
            transform.rotation = Quaternion.Lerp(transform.rotation, 
                Quaternion.LookRotation(wallForward, wallNormal), Time.deltaTime * 5f);
        }
        
        void HandleSliding(Vector3 moveDirection)
        {
            if (!isSliding)
            {
                isSliding = true;
                slideTimer = slideDuration;
                
                // Reduzir altura do collider
                var capsule = GetComponent<CapsuleCollider>();
                if (capsule != null)
                {
                    capsule.height *= 0.5f;
                    capsule.center = new Vector3(0, capsule.height * 0.5f, 0);
                }
                
                PlayMovementSound("slide_start");
            }
            
            slideTimer -= Time.deltaTime;
            
            if (slideTimer <= 0f || !isSprinting)
            {
                StopSliding();
                return;
            }
            
            // Movimento de deslizamento
            rb.velocity = new Vector3(moveDirection.x * slideSpeed, rb.velocity.y, 
                moveDirection.z * slideSpeed);
        }
        
        void HandleClimbing()
        {
            if (!isClimbing)
            {
                isClimbing = true;
                PlayMovementSound("climb_start");
            }
            
            // Movimento vertical de escalada
            rb.velocity = new Vector3(0, climbSpeed, 0);
            
            // Verificar se chegou ao topo
            if (!Physics.Raycast(transform.position + Vector3.up * 2f, transform.forward, 1f, wallLayers))
            {
                // Pular para o topo
                rb.velocity = transform.forward * 5f + Vector3.up * 8f;
                isClimbing = false;
            }
        }
        
        void HandleNormalMovement(Vector3 moveDirection)
        {
            // Reset estados especiais
            if (isWallRunning) StopWallRunning();
            if (isSliding) StopSliding();
            if (isClimbing) isClimbing = false;
            
            // Movimento padrão aprimorado
            float currentSpeed = isSprinting ? sprintSpeed : moveSpeed;
            
            // Aceleração suave
            Vector3 targetVelocity = moveDirection * currentSpeed;
            Vector3 velocityChange = targetVelocity - new Vector3(rb.velocity.x, 0, rb.velocity.z);
            velocityChange = Vector3.ClampMagnitude(velocityChange, currentSpeed);
            
            rb.AddForce(velocityChange, ForceMode.VelocityChange);
        }
        
        bool CanWallRun()
        {
            return Physics.Raycast(transform.position, transform.right, out RaycastHit rightHit, 1f, wallLayers) ||
                   Physics.Raycast(transform.position, -transform.right, out RaycastHit leftHit, 1f, wallLayers);
        }
        
        bool CanSlide()
        {
            return isGrounded && !isSliding && moveInput.magnitude > 0.8f;
        }
        
        bool CanClimb()
        {
            return Physics.Raycast(transform.position, transform.forward, 1f, wallLayers) &&
                   Input.GetKey(KeyCode.Space) && !isGrounded;
        }
        
        void StopWallRunning()
        {
            isWallRunning = false;
            transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
        }
        
        void StopSliding()
        {
            isSliding = false;
            
            // Restaurar altura do collider
            var capsule = GetComponent<CapsuleCollider>();
            if (capsule != null)
            {
                capsule.height *= 2f;
                capsule.center = new Vector3(0, capsule.height * 0.5f, 0);
            }
        }
        
        void CheckGroundStatus()
        {
            wasGrounded = isGrounded;
            isGrounded = Physics.Raycast(transform.position, Vector3.down, 1.1f, groundLayers);
            
            // Efeito de pouso
            if (!wasGrounded && isGrounded)
            {
                PlayMovementSound("land");
                
                // Efeito visual de poeira
                SpawnLandingEffect();
            }
        }
        
        void HandleMovementEffects()
        {
            // Efeitos baseados no tipo de movimento
            if (isWallRunning)
            {
                // Partículas da parede
                SpawnWallRunParticles();
            }
            else if (isSliding)
            {
                // Partículas de deslizamento
                SpawnSlideParticles();
            }
            else if (isSprinting && isGrounded)
            {
                // Pegadas de corrida
                SpawnSprintFootsteps();
            }
        }
        
        void SyncPositionOptimized()
        {
            // Sincronização otimizada para reduzir lag
            if (Vector3.Distance(transform.position, networkPosition.Value) > 0.2f ||
                Time.time - lastSyncTime > 0.1f)
            {
                MoveServerRpc(transform.position, rb.velocity, isWallRunning, isSliding);
                lastSyncTime = Time.time;
            }
        }
        
        void PlayMovementSound(string soundName)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(soundName);
            }
        }
        
        void SpawnLandingEffect()
        {
            // TODO: Implementar efeito visual de pouso
        }
        
        void SpawnWallRunParticles()
        {
            // TODO: Implementar partículas de wall run
        }
        
        void SpawnSlideParticles()
        {
            // TODO: Implementar partículas de slide
        }
        
        void SpawnSprintFootsteps()
        {
            // TODO: Implementar pegadas de corrida
        }
        
        private float lastSyncTime;
        
        [Header("Advanced Camera System")]
        public float cameraDistance = 5f;
        public float cameraHeight = 2f;
        public Vector2 pitchMinMax = new Vector2(-40f, 85f);
        public float cameraCollisionRadius = 0.2f;
        public LayerMask cameraCollisionLayers = -1;
        public float aimFOV = 40f;
        public float normalFOV = 60f;
        public float fovTransitionSpeed = 5f;
        
        private float yaw;
        private float pitch;
        private Vector3 cameraOffset;
        private bool isAiming;
        
        void HandleCameraRotation()
        {
            // Input suavizado para mobile
            float mouseX = mouseInput.x * mouseSensitivity * (isMobile ? 0.5f : 1f);
            float mouseY = mouseInput.y * mouseSensitivity * (isMobile ? 0.5f : 1f);
            
            yaw += mouseX;
            pitch -= mouseY;
            pitch = Mathf.Clamp(pitch, pitchMinMax.x, pitchMinMax.y);
            
            // Câmera third-person estilo Free Fire
            UpdateThirdPersonCamera();
            
            // Sistema de mira avançado
            HandleAiming();
        }
        
        void UpdateThirdPersonCamera()
        {
            // Rotação do player (apenas Y)
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            
            // Posição ideal da câmera
            Vector3 targetCameraPosition = transform.position + 
                Quaternion.Euler(pitch, yaw, 0f) * new Vector3(0f, cameraHeight, -cameraDistance);
            
            // Verificar colisões da câmera
            Vector3 direction = (targetCameraPosition - transform.position).normalized;
            float targetDistance = Vector3.Distance(transform.position, targetCameraPosition);
            
            if (Physics.SphereCast(transform.position, cameraCollisionRadius, direction, 
                out RaycastHit hit, targetDistance, cameraCollisionLayers))
            {
                targetCameraPosition = hit.point - direction * cameraCollisionRadius;
            }
            
            // Smooth camera movement
            playerCamera.transform.position = Vector3.Lerp(playerCamera.transform.position, 
                targetCameraPosition, Time.deltaTime * 10f);
            
            // Câmera sempre olha para o player
            Vector3 lookDirection = (transform.position + Vector3.up * 1.7f - playerCamera.transform.position).normalized;
            playerCamera.transform.rotation = Quaternion.LookRotation(lookDirection);
        }
        
        void HandleAiming()
        {
            bool aimInput = Input.GetMouseButton(1) || 
                           (MobileInputSystem.Instance?.IsAimPressed ?? false);
            
            if (aimInput != isAiming)
            {
                isAiming = aimInput;
                
                // Transição de FOV suave
                float targetFOV = isAiming ? aimFOV : normalFOV;
                StartCoroutine(TransitionFOV(targetFOV));
                
                // Ajustar sensibilidade ao mirar
                mouseSensitivity = isAiming ? mouseSensitivity * 0.6f : mouseSensitivity / 0.6f;
            }
        }
        
        System.Collections.IEnumerator TransitionFOV(float targetFOV)
        {
            float startFOV = playerCamera.fieldOfView;
            float elapsed = 0f;
            
            while (elapsed < 1f / fovTransitionSpeed)
            {
                elapsed += Time.deltaTime;
                float t = elapsed * fovTransitionSpeed;
                playerCamera.fieldOfView = Mathf.Lerp(startFOV, targetFOV, t);
                yield return null;
            }
            
            playerCamera.fieldOfView = targetFOV;
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
