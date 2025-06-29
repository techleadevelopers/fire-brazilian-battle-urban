
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.OnScreen;

namespace ArenaBrasil.Input
{
    public class MobileInputSystem : MonoBehaviour
    {
        public static MobileInputSystem Instance { get; private set; }
        
        [Header("Touch Controls")]
        public OnScreenStick movementStick;
        public OnScreenStick lookStick;
        public OnScreenButton shootButton;
        public OnScreenButton jumpButton;
        public OnScreenButton reloadButton;
        public OnScreenButton aimButton;
        public OnScreenButton crouchButton;
        public OnScreenButton interactButton;
        
        [Header("HUD Elements")]
        public GameObject mobileHUD;
        public GameObject pcHUD;
        
        [Header("Sensitivity Settings")]
        public float touchSensitivity = 2.0f;
        public float gyroSensitivity = 1.0f;
        public bool useGyroscope = false;
        
        [Header("Auto-Shoot Settings")]
        public bool autoShoot = true;
        public float autoShootRange = 50f;
        public LayerMask enemyLayers;
        
        [Header("Touch Feedback")]
        public float hapticFeedbackIntensity = 0.5f;
        public bool useHapticFeedback = true;
        
        // Input states
        public Vector2 MovementInput { get; private set; }
        public Vector2 LookInput { get; private set; }
        public bool IsShootPressed { get; private set; }
        public bool IsJumpPressed { get; private set; }
        public bool IsReloadPressed { get; private set; }
        public bool IsAimPressed { get; private set; }
        public bool IsCrouchPressed { get; private set; }
        public bool IsInteractPressed { get; private set; }
        
        // Events
        public event System.Action OnShootPressed;
        public event System.Action OnShootReleased;
        public event System.Action OnJumpPressed;
        public event System.Action OnReloadPressed;
        public event System.Action OnAimPressed;
        public event System.Action OnAimReleased;
        public event System.Action OnCrouchPressed;
        public event System.Action OnInteractPressed;
        
        private Camera playerCamera;
        private bool isMobile;
        
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
        
        void Start()
        {
            DetectPlatform();
            SetupInputSystem();
            SetupGyroscope();
            SetupAutoShoot();
        }
        
        void DetectPlatform()
        {
            #if UNITY_ANDROID || UNITY_IOS
                isMobile = true;
            #else
                isMobile = SystemInfo.deviceType == DeviceType.Handheld;
            #endif
            
            SetupHUD();
        }
        
        void SetupHUD()
        {
            if (mobileHUD != null)
                mobileHUD.SetActive(isMobile);
            
            if (pcHUD != null)
                pcHUD.SetActive(!isMobile);
            
            // Setup touch controls only on mobile
            if (isMobile)
            {
                SetupTouchControls();
            }
        }
        
        void SetupTouchControls()
        {
            // Movement stick
            if (movementStick != null)
            {
                movementStick.gameObject.SetActive(true);
            }
            
            // Look stick
            if (lookStick != null)
            {
                lookStick.gameObject.SetActive(true);
            }
            
            // Setup all buttons
            SetupButton(shootButton, OnShootButtonPressed, OnShootButtonReleased);
            SetupButton(jumpButton, OnJumpButtonPressed, null);
            SetupButton(reloadButton, OnReloadButtonPressed, null);
            SetupButton(aimButton, OnAimButtonPressed, OnAimButtonReleased);
            SetupButton(crouchButton, OnCrouchButtonPressed, null);
            SetupButton(interactButton, OnInteractButtonPressed, null);
        }
        
        void SetupButton(OnScreenButton button, System.Action onPress, System.Action onRelease)
        {
            if (button == null) return;
            
            button.gameObject.SetActive(true);
            
            // Add button listeners (using Unity's new Input System)
            var buttonControl = button.controlPath;
            
            // This would need proper implementation with InputAction callbacks
        }
        
        void SetupInputSystem()
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                playerCamera = FindObjectOfType<Camera>();
            }
        }
        
        void SetupGyroscope()
        {
            if (isMobile && SystemInfo.supportsGyroscope && useGyroscope)
            {
                Input.gyro.enabled = true;
                Debug.Log("Giroscópio ativado para controle de câmera");
            }
        }
        
        void SetupAutoShoot()
        {
            if (isMobile && autoShoot)
            {
                InvokeRepeating(nameof(CheckAutoShoot), 0.1f, 0.1f);
            }
        }
        
        void Update()
        {
            UpdateInputs();
            HandleGyroscope();
            ProcessAutoAim();
        }
        
        void UpdateInputs()
        {
            if (isMobile)
            {
                UpdateMobileInputs();
            }
            else
            {
                UpdatePCInputs();
            }
        }
        
        void UpdateMobileInputs()
        {
            // Movement from joystick
            if (movementStick != null)
            {
                MovementInput = movementStick.value;
            }
            
            // Look from joystick or touch
            if (lookStick != null)
            {
                LookInput = lookStick.value * touchSensitivity;
            }
            else
            {
                UpdateTouchLook();
            }
        }
        
        void UpdateTouchLook()
        {
            if (Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch touch = Input.GetTouch(i);
                    
                    // Check if touch is on the right side of screen (look area)
                    if (touch.position.x > Screen.width * 0.5f)
                    {
                        if (touch.phase == TouchPhase.Moved)
                        {
                            Vector2 deltaPosition = touch.deltaPosition;
                            LookInput = deltaPosition * touchSensitivity * Time.deltaTime;
                        }
                    }
                }
            }
        }
        
        void UpdatePCInputs()
        {
            // Standard PC inputs
            MovementInput = new Vector2(UnityEngine.Input.GetAxis("Horizontal"), UnityEngine.Input.GetAxis("Vertical"));
            LookInput = new Vector2(UnityEngine.Input.GetAxis("Mouse X"), UnityEngine.Input.GetAxis("Mouse Y"));
            
            IsShootPressed = UnityEngine.Input.GetMouseButton(0);
            IsJumpPressed = UnityEngine.Input.GetKeyDown(KeyCode.Space);
            IsReloadPressed = UnityEngine.Input.GetKeyDown(KeyCode.R);
            IsAimPressed = UnityEngine.Input.GetMouseButton(1);
            IsCrouchPressed = UnityEngine.Input.GetKey(KeyCode.LeftControl);
            IsInteractPressed = UnityEngine.Input.GetKeyDown(KeyCode.E);
        }
        
        void HandleGyroscope()
        {
            if (isMobile && useGyroscope && Input.gyro.enabled)
            {
                Vector3 gyroInput = Input.gyro.rotationRate;
                LookInput += new Vector2(gyroInput.y, -gyroInput.x) * gyroSensitivity;
            }
        }
        
        void ProcessAutoAim()
        {
            if (!isMobile || !autoShoot) return;
            
            // Find nearest enemy for auto-aim assistance
            GameObject nearestEnemy = FindNearestEnemy();
            if (nearestEnemy != null)
            {
                Vector3 directionToEnemy = (nearestEnemy.transform.position - transform.position).normalized;
                
                // Gentle auto-aim assistance
                Vector3 currentForward = playerCamera.transform.forward;
                Vector3 aimDirection = Vector3.Slerp(currentForward, directionToEnemy, 0.1f * Time.deltaTime);
                
                // Apply subtle aim assistance
                LookInput += (Vector2)(aimDirection - currentForward) * 0.5f;
            }
        }
        
        void CheckAutoShoot()
        {
            if (!autoShoot || !isMobile) return;
            
            GameObject target = FindNearestEnemy();
            if (target != null)
            {
                float distance = Vector3.Distance(transform.position, target.transform.position);
                if (distance <= autoShootRange)
                {
                    // Check if enemy is in crosshair
                    Vector3 screenPoint = playerCamera.WorldToScreenPoint(target.transform.position);
                    Vector2 centerScreen = new Vector2(Screen.width / 2, Screen.height / 2);
                    
                    float distanceFromCenter = Vector2.Distance(screenPoint, centerScreen);
                    if (distanceFromCenter < 100f) // Within crosshair range
                    {
                        TriggerAutoShoot();
                    }
                }
            }
        }
        
        GameObject FindNearestEnemy()
        {
            Collider[] enemies = Physics.OverlapSphere(transform.position, autoShootRange, enemyLayers);
            
            GameObject nearest = null;
            float nearestDistance = float.MaxValue;
            
            foreach (Collider enemy in enemies)
            {
                float distance = Vector3.Distance(transform.position, enemy.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = enemy.gameObject;
                }
            }
            
            return nearest;
        }
        
        void TriggerAutoShoot()
        {
            OnShootPressed?.Invoke();
            ProvideTactileFeedback();
        }
        
        void ProvideTactileFeedback()
        {
            if (useHapticFeedback && isMobile)
            {
                #if UNITY_ANDROID || UNITY_IOS
                    Handheld.Vibrate();
                #endif
            }
        }
        
        // Button event handlers
        void OnShootButtonPressed()
        {
            IsShootPressed = true;
            OnShootPressed?.Invoke();
            ProvideTactileFeedback();
        }
        
        void OnShootButtonReleased()
        {
            IsShootPressed = false;
            OnShootReleased?.Invoke();
        }
        
        void OnJumpButtonPressed()
        {
            IsJumpPressed = true;
            OnJumpPressed?.Invoke();
            ProvideTactileFeedback();
        }
        
        void OnReloadButtonPressed()
        {
            IsReloadPressed = true;
            OnReloadPressed?.Invoke();
            ProvideTactileFeedback();
        }
        
        void OnAimButtonPressed()
        {
            IsAimPressed = true;
            OnAimPressed?.Invoke();
        }
        
        void OnAimButtonReleased()
        {
            IsAimPressed = false;
            OnAimReleased?.Invoke();
        }
        
        void OnCrouchButtonPressed()
        {
            IsCrouchPressed = !IsCrouchPressed;
            OnCrouchPressed?.Invoke();
        }
        
        void OnInteractButtonPressed()
        {
            IsInteractPressed = true;
            OnInteractPressed?.Invoke();
            ProvideTactileFeedback();
        }
        
        // Settings
        public void SetTouchSensitivity(float sensitivity)
        {
            touchSensitivity = sensitivity;
        }
        
        public void SetGyroSensitivity(float sensitivity)
        {
            gyroSensitivity = sensitivity;
        }
        
        public void ToggleGyroscope(bool enabled)
        {
            useGyroscope = enabled;
            if (Input.gyro.enabled != enabled)
            {
                Input.gyro.enabled = enabled;
            }
        }
        
        public void ToggleAutoShoot(bool enabled)
        {
            autoShoot = enabled;
        }
        
        public void ToggleHapticFeedback(bool enabled)
        {
            useHapticFeedback = enabled;
        }
        
        void OnDestroy()
        {
            CancelInvoke();
        }
    }
}
