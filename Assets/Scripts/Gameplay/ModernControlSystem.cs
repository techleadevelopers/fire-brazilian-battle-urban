
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using ArenaBrasil.Input;

namespace ArenaBrasil.Controls
{
    public class ModernControlSystem : NetworkBehaviour
    {
        public static ModernControlSystem Instance { get; private set; }
        
        [Header("Control Schemes")]
        public ControlScheme currentScheme = ControlScheme.TouchOptimized;
        public bool adaptiveControls = true;
        public bool predictiveInput = true;
        
        [Header("Free Fire Style Settings")]
        public float touchSensitivity = 2.5f;
        public float aimSensitivity = 1.8f;
        public bool autoShoot = true;
        public bool autoPickup = true;
        public bool autoOpenDoors = true;
        public bool smartMovement = true;
        
        [Header("Advanced Features")]
        public bool gyroscopeAiming = true;
        public float gyroSensitivity = 1.2f;
        public bool hapticFeedback = true;
        public bool voiceCommands = false;
        
        [Header("Customizable Layout")]
        public List<UIControlElement> customizableElements = new List<UIControlElement>();
        public ControlLayout[] presetLayouts;
        public ControlLayout currentLayout;
        
        [Header("Accessibility")]
        public bool colorBlindSupport = true;
        public bool oneHandedMode = false;
        public float buttonSizeMultiplier = 1.0f;
        public bool voiceOver = false;
        
        // Input States
        private Vector2 movementInput;
        private Vector2 lookInput;
        private bool shootInput;
        private bool aimInput;
        private bool jumpInput;
        private bool crouchInput;
        private bool reloadInput;
        private bool interactInput;
        
        // Advanced Input
        private bool isUsingGyro;
        private Vector3 gyroInput;
        private float lastInputTime;
        
        // Events
        public event System.Action<Vector2> OnMovementInput;
        public event System.Action<Vector2> OnLookInput;
        public event System.Action OnShootPressed;
        public event System.Action OnShootReleased;
        public event System.Action OnAimPressed;
        public event System.Action OnAimReleased;
        public event System.Action OnJumpPressed;
        public event System.Action OnCrouchToggled;
        public event System.Action OnReloadPressed;
        public event System.Action OnInteractPressed;
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeControlSystem();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void InitializeControlSystem()
        {
            Debug.Log("🎮 Arena Brasil - Sistema de Controles Moderno Inicializado");
            
            DetectPlatformAndSetup();
            LoadPresetLayouts();
            SetupAdvancedFeatures();
            LoadUserPreferences();
        }
        
        void DetectPlatformAndSetup()
        {
            #if UNITY_ANDROID || UNITY_IOS
                currentScheme = ControlScheme.TouchOptimized;
                SetupMobileControls();
            #else
                currentScheme = ControlScheme.KeyboardMouse;
                SetupPCControls();
            #endif
        }
        
        void SetupMobileControls()
        {
            // Ativar controles touch otimizados
            SetupTouchControls();
            SetupGyroscope();
            SetupHapticFeedback();
            SetupAutoFeatures();
        }
        
        void SetupTouchControls()
        {
            // Joystick virtual de movimento (lado esquerdo)
            var movementJoystick = CreateVirtualJoystick("MovementStick", 
                new Vector2(0.15f, 0.25f), JoystickType.Movement);
            
            // Área de look (lado direito)
            var lookArea = CreateTouchArea("LookArea", 
                new Rect(0.5f, 0f, 0.5f, 1f), TouchAreaType.Look);
            
            // Botões de ação otimizados
            SetupActionButtons();
            
            // Botões contextuais inteligentes
            SetupSmartButtons();
        }
        
        void SetupActionButtons()
        {
            // Layout estilo Free Fire otimizado
            var shootButton = CreateActionButton("ShootButton", 
                new Vector2(0.85f, 0.3f), ButtonType.Shoot, 80f);
            
            var aimButton = CreateActionButton("AimButton", 
                new Vector2(0.75f, 0.45f), ButtonType.Aim, 70f);
            
            var jumpButton = CreateActionButton("JumpButton", 
                new Vector2(0.9f, 0.6f), ButtonType.Jump, 60f);
            
            var crouchButton = CreateActionButton("CrouchButton", 
                new Vector2(0.8f, 0.15f), ButtonType.Crouch, 55f);
            
            var reloadButton = CreateActionButton("ReloadButton", 
                new Vector2(0.65f, 0.3f), ButtonType.Reload, 50f);
        }
        
        void SetupSmartButtons()
        {
            // Botões que aparecem contextualmente
            var interactButton = CreateSmartButton("InteractButton", 
                new Vector2(0.5f, 0.7f), SmartButtonType.Interact);
            
            var pickupButton = CreateSmartButton("PickupButton", 
                new Vector2(0.5f, 0.6f), SmartButtonType.Pickup);
            
            var doorButton = CreateSmartButton("DoorButton", 
                new Vector2(0.5f, 0.65f), SmartButtonType.Door);
            
            var vehicleButton = CreateSmartButton("VehicleButton", 
                new Vector2(0.5f, 0.55f), SmartButtonType.Vehicle);
        }
        
        void SetupGyroscope()
        {
            if (SystemInfo.supportsGyroscope && gyroscopeAiming)
            {
                Input.gyro.enabled = true;
                isUsingGyro = true;
                Debug.Log("📱 Giroscópio ativado para mira");
            }
        }
        
        void SetupHapticFeedback()
        {
            #if UNITY_ANDROID || UNITY_IOS
                if (hapticFeedback)
                {
                    // Configurar diferentes intensidades de vibração
                    SetupHapticPatterns();
                }
            #endif
        }
        
        void SetupAutoFeatures()
        {
            if (autoShoot)
            {
                SetupAutoShootSystem();
            }
            
            if (autoPickup)
            {
                SetupAutoPickupSystem();
            }
            
            if (smartMovement)
            {
                SetupSmartMovementSystem();
            }
        }
        
        void SetupPCControls()
        {
            // Configurar controles de PC otimizados
            LoadKeyboardBindings();
            SetupMouseSensitivity();
        }
        
        void LoadPresetLayouts()
        {
            presetLayouts = new ControlLayout[]
            {
                CreateLayout("Default", "Layout padrão estilo Free Fire"),
                CreateLayout("Pro", "Layout para jogadores profissionais"),
                CreateLayout("Casual", "Layout simplificado para iniciantes"),
                CreateLayout("OnHanded", "Layout para uso com uma mão"),
                CreateLayout("Tablet", "Layout otimizado para tablets")
            };
            
            currentLayout = presetLayouts[0]; // Default
        }
        
        ControlLayout CreateLayout(string name, string description)
        {
            return new ControlLayout
            {
                layoutName = name,
                description = description,
                elements = GenerateLayoutElements(name)
            };
        }
        
        List<UIControlElement> GenerateLayoutElements(string layoutType)
        {
            var elements = new List<UIControlElement>();
            
            switch (layoutType)
            {
                case "Default":
                    elements = GenerateDefaultLayout();
                    break;
                case "Pro":
                    elements = GenerateProLayout();
                    break;
                case "Casual":
                    elements = GenerateCasualLayout();
                    break;
                case "OnHanded":
                    elements = GenerateOneHandedLayout();
                    break;
                case "Tablet":
                    elements = GenerateTabletLayout();
                    break;
            }
            
            return elements;
        }
        
        List<UIControlElement> GenerateDefaultLayout()
        {
            return new List<UIControlElement>
            {
                new UIControlElement { name = "MovementStick", position = new Vector2(0.15f, 0.25f), size = 120f },
                new UIControlElement { name = "ShootButton", position = new Vector2(0.85f, 0.3f), size = 80f },
                new UIControlElement { name = "AimButton", position = new Vector2(0.75f, 0.45f), size = 70f },
                new UIControlElement { name = "JumpButton", position = new Vector2(0.9f, 0.6f), size = 60f },
                new UIControlElement { name = "CrouchButton", position = new Vector2(0.8f, 0.15f), size = 55f },
                new UIControlElement { name = "ReloadButton", position = new Vector2(0.65f, 0.3f), size = 50f }
            };
        }
        
        List<UIControlElement> GenerateProLayout()
        {
            // Layout com controles mais próximos para reação rápida
            return new List<UIControlElement>
            {
                new UIControlElement { name = "MovementStick", position = new Vector2(0.12f, 0.22f), size = 110f },
                new UIControlElement { name = "ShootButton", position = new Vector2(0.88f, 0.25f), size = 85f },
                new UIControlElement { name = "AimButton", position = new Vector2(0.78f, 0.42f), size = 65f },
                new UIControlElement { name = "JumpButton", position = new Vector2(0.92f, 0.55f), size = 55f },
                new UIControlElement { name = "CrouchButton", position = new Vector2(0.82f, 0.12f), size = 50f },
                new UIControlElement { name = "ReloadButton", position = new Vector2(0.68f, 0.28f), size = 45f }
            };
        }
        
        List<UIControlElement> GenerateCasualLayout()
        {
            // Layout com botões maiores e mais espaçados
            return new List<UIControlElement>
            {
                new UIControlElement { name = "MovementStick", position = new Vector2(0.18f, 0.28f), size = 140f },
                new UIControlElement { name = "ShootButton", position = new Vector2(0.82f, 0.35f), size = 100f },
                new UIControlElement { name = "AimButton", position = new Vector2(0.72f, 0.5f), size = 80f },
                new UIControlElement { name = "JumpButton", position = new Vector2(0.88f, 0.65f), size = 70f },
                new UIControlElement { name = "CrouchButton", position = new Vector2(0.78f, 0.18f), size = 65f }
            };
        }
        
        List<UIControlElement> GenerateOneHandedLayout()
        {
            // Layout concentrado em um lado para uso com uma mão
            return new List<UIControlElement>
            {
                new UIControlElement { name = "MovementStick", position = new Vector2(0.2f, 0.3f), size = 100f },
                new UIControlElement { name = "ShootButton", position = new Vector2(0.4f, 0.25f), size = 70f },
                new UIControlElement { name = "AimButton", position = new Vector2(0.35f, 0.45f), size = 60f },
                new UIControlElement { name = "JumpButton", position = new Vector2(0.45f, 0.6f), size = 55f }
            };
        }
        
        List<UIControlElement> GenerateTabletLayout()
        {
            // Layout otimizado para telas maiores
            return new List<UIControlElement>
            {
                new UIControlElement { name = "MovementStick", position = new Vector2(0.1f, 0.2f), size = 140f },
                new UIControlElement { name = "ShootButton", position = new Vector2(0.9f, 0.3f), size = 90f },
                new UIControlElement { name = "AimButton", position = new Vector2(0.8f, 0.45f), size = 75f },
                new UIControlElement { name = "JumpButton", position = new Vector2(0.95f, 0.6f), size = 65f },
                new UIControlElement { name = "CrouchButton", position = new Vector2(0.85f, 0.15f), size = 60f }
            };
        }
        
        void Update()
        {
            ProcessInput();
            HandleGyroscope();
            UpdateSmartFeatures();
            ProcessPredictiveInput();
        }
        
        void ProcessInput()
        {
            switch (currentScheme)
            {
                case ControlScheme.TouchOptimized:
                    ProcessTouchInput();
                    break;
                case ControlScheme.KeyboardMouse:
                    ProcessKeyboardMouseInput();
                    break;
                case ControlScheme.Controller:
                    ProcessControllerInput();
                    break;
            }
            
            // Dispatch input events
            DispatchInputEvents();
        }
        
        void ProcessTouchInput()
        {
            // Processar input do joystick virtual
            movementInput = GetVirtualJoystickInput("MovementStick");
            
            // Processar área de look
            lookInput = GetTouchAreaInput("LookArea");
            
            // Processar botões de ação
            shootInput = GetButtonInput("ShootButton");
            aimInput = GetButtonInput("AimButton");
            jumpInput = GetButtonPressed("JumpButton");
            crouchInput = GetButtonPressed("CrouchButton");
            reloadInput = GetButtonPressed("ReloadButton");
            
            // Aplicar sensibilidade
            lookInput *= touchSensitivity;
            
            if (aimInput)
            {
                lookInput *= (aimSensitivity / touchSensitivity);
            }
        }
        
        void ProcessKeyboardMouseInput()
        {
            // Movimento WASD
            movementInput = new Vector2(
                UnityEngine.Input.GetAxis("Horizontal"),
                UnityEngine.Input.GetAxis("Vertical")
            );
            
            // Look com mouse
            lookInput = new Vector2(
                UnityEngine.Input.GetAxis("Mouse X"),
                UnityEngine.Input.GetAxis("Mouse Y")
            );
            
            // Ações
            shootInput = UnityEngine.Input.GetMouseButton(0);
            aimInput = UnityEngine.Input.GetMouseButton(1);
            jumpInput = UnityEngine.Input.GetKeyDown(KeyCode.Space);
            crouchInput = UnityEngine.Input.GetKeyDown(KeyCode.LeftControl);
            reloadInput = UnityEngine.Input.GetKeyDown(KeyCode.R);
            interactInput = UnityEngine.Input.GetKeyDown(KeyCode.E);
        }
        
        void HandleGyroscope()
        {
            if (!isUsingGyro || !Input.gyro.enabled) return;
            
            // Obter input do giroscópio
            gyroInput = Input.gyro.rotationRate;
            
            // Filtrar e suavizar
            gyroInput = FilterGyroInput(gyroInput);
            
            // Aplicar ao look input se estiver mirando
            if (aimInput)
            {
                lookInput += new Vector2(gyroInput.y, -gyroInput.x) * gyroSensitivity;
            }
        }
        
        Vector3 FilterGyroInput(Vector3 rawInput)
        {
            // Aplicar filtro passa-baixa para reduzir ruído
            float alpha = 0.8f;
            gyroInput = Vector3.Lerp(gyroInput, rawInput, alpha);
            return gyroInput;
        }
        
        void UpdateSmartFeatures()
        {
            if (autoShoot)
            {
                ProcessAutoShoot();
            }
            
            if (autoPickup)
            {
                ProcessAutoPickup();
            }
            
            if (autoOpenDoors)
            {
                ProcessAutoOpenDoors();
            }
            
            UpdateSmartButtons();
        }
        
        void ProcessAutoShoot()
        {
            // Detectar inimigos no crosshair
            var nearbyEnemies = DetectNearbyEnemies();
            
            if (nearbyEnemies.Count > 0 && IsEnemyInCrosshair(nearbyEnemies[0]))
            {
                shootInput = true;
                TriggerHaptic(HapticType.Shoot);
            }
        }
        
        void ProcessAutoPickup()
        {
            // Detectar itens próximos
            var nearbyItems = DetectNearbyItems();
            
            foreach (var item in nearbyItems)
            {
                if (ShouldAutoPickup(item))
                {
                    PickupItem(item);
                    TriggerHaptic(HapticType.Pickup);
                }
            }
        }
        
        void ProcessAutoOpenDoors()
        {
            // Detectar portas próximas
            var nearbyDoors = DetectNearbyDoors();
            
            if (nearbyDoors.Count > 0 && IsMovingTowardsDoor(nearbyDoors[0]))
            {
                OpenDoor(nearbyDoors[0]);
                TriggerHaptic(HapticType.Interact);
            }
        }
        
        void UpdateSmartButtons()
        {
            // Mostrar/esconder botões baseado no contexto
            UpdateInteractButton();
            UpdatePickupButton();
            UpdateVehicleButton();
        }
        
        void UpdateInteractButton()
        {
            bool showInteract = CanInteractWithSomething();
            SetSmartButtonVisibility("InteractButton", showInteract);
        }
        
        void ProcessPredictiveInput()
        {
            if (!predictiveInput) return;
            
            // Predizer próxima ação baseada no padrão de input
            PredictNextAction();
        }
        
        void DispatchInputEvents()
        {
            OnMovementInput?.Invoke(movementInput);
            OnLookInput?.Invoke(lookInput);
            
            if (shootInput) OnShootPressed?.Invoke();
            else OnShootReleased?.Invoke();
            
            if (aimInput) OnAimPressed?.Invoke();
            else OnAimReleased?.Invoke();
            
            if (jumpInput) OnJumpPressed?.Invoke();
            if (crouchInput) OnCrouchToggled?.Invoke();
            if (reloadInput) OnReloadPressed?.Invoke();
            if (interactInput) OnInteractPressed?.Invoke();
        }
        
        // === CUSTOMIZAÇÃO DE LAYOUT ===
        
        public void SetControlLayout(string layoutName)
        {
            var layout = System.Array.Find(presetLayouts, l => l.layoutName == layoutName);
            if (layout != null)
            {
                currentLayout = layout;
                ApplyLayout(layout);
                SaveLayoutPreference(layoutName);
            }
        }
        
        public void CustomizeControlPosition(string controlName, Vector2 newPosition)
        {
            var element = currentLayout.elements.Find(e => e.name == controlName);
            if (element != null)
            {
                element.position = newPosition;
                UpdateControlPosition(controlName, newPosition);
                SaveCustomLayout();
            }
        }
        
        public void CustomizeControlSize(string controlName, float newSize)
        {
            var element = currentLayout.elements.Find(e => e.name == controlName);
            if (element != null)
            {
                element.size = newSize;
                UpdateControlSize(controlName, newSize);
                SaveCustomLayout();
            }
        }
        
        // === CONFIGURAÇÕES ===
        
        public void SetTouchSensitivity(float sensitivity)
        {
            touchSensitivity = Mathf.Clamp(sensitivity, 0.1f, 5.0f);
            SaveSetting("TouchSensitivity", touchSensitivity);
        }
        
        public void SetAimSensitivity(float sensitivity)
        {
            aimSensitivity = Mathf.Clamp(sensitivity, 0.1f, 5.0f);
            SaveSetting("AimSensitivity", aimSensitivity);
        }
        
        public void ToggleAutoShoot(bool enabled)
        {
            autoShoot = enabled;
            SaveSetting("AutoShoot", enabled);
        }
        
        public void ToggleGyroscope(bool enabled)
        {
            gyroscopeAiming = enabled;
            
            if (Input.gyro.enabled != enabled)
            {
                Input.gyro.enabled = enabled;
                isUsingGyro = enabled;
            }
            
            SaveSetting("Gyroscope", enabled);
        }
        
        public void ToggleHapticFeedback(bool enabled)
        {
            hapticFeedback = enabled;
            SaveSetting("HapticFeedback", enabled);
        }
        
        // === HELPERS ABSTRATOS (implementar com sistema real) ===
        
        VirtualJoystick CreateVirtualJoystick(string name, Vector2 position, JoystickType type) { return null; }
        TouchArea CreateTouchArea(string name, Rect area, TouchAreaType type) { return null; }
        ActionButton CreateActionButton(string name, Vector2 position, ButtonType type, float size) { return null; }
        SmartButton CreateSmartButton(string name, Vector2 position, SmartButtonType type) { return null; }
        
        Vector2 GetVirtualJoystickInput(string name) { return Vector2.zero; }
        Vector2 GetTouchAreaInput(string name) { return Vector2.zero; }
        bool GetButtonInput(string name) { return false; }
        bool GetButtonPressed(string name) { return false; }
        
        void TriggerHaptic(HapticType type) { }
        void SetSmartButtonVisibility(string name, bool visible) { }
        
        List<GameObject> DetectNearbyEnemies() { return new List<GameObject>(); }
        List<GameObject> DetectNearbyItems() { return new List<GameObject>(); }
        List<GameObject> DetectNearbyDoors() { return new List<GameObject>(); }
        
        bool IsEnemyInCrosshair(GameObject enemy) { return false; }
        bool ShouldAutoPickup(GameObject item) { return false; }
        bool IsMovingTowardsDoor(GameObject door) { return false; }
        bool CanInteractWithSomething() { return false; }
        
        void PickupItem(GameObject item) { }
        void OpenDoor(GameObject door) { }
        void PredictNextAction() { }
        
        void ApplyLayout(ControlLayout layout) { }
        void UpdateControlPosition(string name, Vector2 position) { }
        void UpdateControlSize(string name, float size) { }
        
        void LoadUserPreferences() { }
        void SaveLayoutPreference(string layoutName) { }
        void SaveCustomLayout() { }
        void SaveSetting(string key, object value) { }
        void SetupHapticPatterns() { }
        void SetupAutoShootSystem() { }
        void SetupAutoPickupSystem() { }
        void SetupSmartMovementSystem() { }
        void LoadKeyboardBindings() { }
        void SetupMouseSensitivity() { }
        void SetupAdvancedFeatures() { }
        
        // === GETTERS PÚBLICOS ===
        
        public Vector2 GetMovementInput() => movementInput;
        public Vector2 GetLookInput() => lookInput;
        public bool IsShootPressed() => shootInput;
        public bool IsAimPressed() => aimInput;
        public ControlLayout GetCurrentLayout() => currentLayout;
        public ControlLayout[] GetPresetLayouts() => presetLayouts;
    }
    
    // === ENUMS E CLASSES ===
    
    public enum ControlScheme
    {
        TouchOptimized,
        KeyboardMouse,
        Controller,
        Custom
    }
    
    public enum JoystickType
    {
        Movement,
        Look
    }
    
    public enum TouchAreaType
    {
        Look,
        Action
    }
    
    public enum ButtonType
    {
        Shoot,
        Aim,
        Jump,
        Crouch,
        Reload,
        Interact
    }
    
    public enum SmartButtonType
    {
        Interact,
        Pickup,
        Door,
        Vehicle
    }
    
    public enum HapticType
    {
        Shoot,
        Hit,
        Pickup,
        Interact,
        Death
    }
    
    [System.Serializable]
    public class ControlLayout
    {
        public string layoutName;
        public string description;
        public List<UIControlElement> elements;
    }
    
    [System.Serializable]
    public class UIControlElement
    {
        public string name;
        public Vector2 position;
        public float size;
        public bool isVisible = true;
        public bool isCustomizable = true;
    }
    
    // Classes abstratas para implementação real
    public class VirtualJoystick { }
    public class TouchArea { }
    public class ActionButton { }
    public class SmartButton { }
}
