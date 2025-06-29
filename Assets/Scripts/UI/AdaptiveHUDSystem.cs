
using UnityEngine;
using UnityEngine.UI;

namespace ArenaBrasil.UI
{
    public class AdaptiveHUDSystem : MonoBehaviour
    {
        public static AdaptiveHUDSystem Instance { get; private set; }
        
        [Header("Screen Adaptation")]
        public bool autoAdaptToScreen = true;
        public Vector2 referenceResolution = new Vector2(1920, 1080);
        public float mobileScaleFactor = 1.2f;
        public float tabletScaleFactor = 1.1f;
        
        [Header("Device Detection")]
        public DeviceType currentDeviceType;
        public ScreenOrientation currentOrientation;
        
        [Header("HUD Layouts")]
        public HUDLayout phonePortraitLayout;
        public HUDLayout phoneLandscapeLayout;
        public HUDLayout tabletLayout;
        public HUDLayout pcLayout;
        
        private CanvasScaler canvasScaler;
        private RectTransform canvasRect;
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeAdaptiveSystem();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void InitializeAdaptiveSystem()
        {
            canvasScaler = GetComponent<CanvasScaler>();
            canvasRect = GetComponent<RectTransform>();
            
            DetectDeviceType();
            AdaptHUDLayout();
            
            // Monitor orientation changes
            InvokeRepeating(nameof(CheckOrientationChange), 0.5f, 0.5f);
        }
        
        void DetectDeviceType()
        {
            float screenDPI = Screen.dpi > 0 ? Screen.dpi : 96f;
            float screenInches = Mathf.Sqrt(Mathf.Pow(Screen.width / screenDPI, 2) + 
                                          Mathf.Pow(Screen.height / screenDPI, 2));
            
            if (Application.isMobilePlatform)
            {
                currentDeviceType = screenInches < 7f ? DeviceType.Phone : DeviceType.Tablet;
            }
            else
            {
                currentDeviceType = DeviceType.Desktop;
            }
            
            currentOrientation = Screen.orientation;
            
            Debug.Log($"📱 Dispositivo detectado: {currentDeviceType} | Orientação: {currentOrientation}");
        }
        
        void AdaptHUDLayout()
        {
            HUDLayout targetLayout = GetTargetLayout();
            ApplyHUDLayout(targetLayout);
            
            // Adjust canvas scaler
            if (canvasScaler != null)
            {
                canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasScaler.referenceResolution = referenceResolution;
                
                switch (currentDeviceType)
                {
                    case DeviceType.Phone:
                        canvasScaler.matchWidthOrHeight = 
                            currentOrientation == ScreenOrientation.Portrait ? 0f : 1f;
                        break;
                    case DeviceType.Tablet:
                        canvasScaler.matchWidthOrHeight = 0.5f;
                        break;
                    case DeviceType.Desktop:
                        canvasScaler.matchWidthOrHeight = 1f;
                        break;
                }
            }
        }
        
        HUDLayout GetTargetLayout()
        {
            switch (currentDeviceType)
            {
                case DeviceType.Phone:
                    return currentOrientation == ScreenOrientation.Portrait ? 
                           phonePortraitLayout : phoneLandscapeLayout;
                case DeviceType.Tablet:
                    return tabletLayout;
                case DeviceType.Desktop:
                    return pcLayout;
                default:
                    return phoneLandscapeLayout;
            }
        }
        
        void ApplyHUDLayout(HUDLayout layout)
        {
            if (layout == null) return;
            
            // Apply button sizes
            ApplyButtonSizes(layout.buttonSizeMultiplier);
            
            // Apply joystick settings
            ApplyJoystickSettings(layout.joystickSize, layout.joystickDeadZone);
            
            // Apply HUD element positions
            ApplyElementPositions(layout.elementPositions);
            
            // Apply safe area adjustments
            ApplySafeAreaAdjustments();
        }
        
        void ApplyButtonSizes(float sizeMultiplier)
        {
            var buttons = FindObjectsOfType<Button>();
            foreach (var button in buttons)
            {
                if (button.gameObject.name.Contains("Mobile"))
                {
                    var rect = button.GetComponent<RectTransform>();
                    rect.localScale = Vector3.one * sizeMultiplier;
                }
            }
        }
        
        void ApplyJoystickSettings(float size, float deadZone)
        {
            if (MobileInputSystem.Instance != null)
            {
                // Apply joystick settings
                var joysticks = FindObjectsOfType<OnScreenStick>();
                foreach (var joystick in joysticks)
                {
                    var rect = joystick.GetComponent<RectTransform>();
                    rect.sizeDelta = Vector2.one * size;
                }
            }
        }
        
        void ApplyElementPositions(UIElementPosition[] positions)
        {
            foreach (var pos in positions)
            {
                var element = GameObject.Find(pos.elementName);
                if (element != null)
                {
                    var rect = element.GetComponent<RectTransform>();
                    rect.anchoredPosition = pos.position;
                    rect.anchorMin = pos.anchorMin;
                    rect.anchorMax = pos.anchorMax;
                }
            }
        }
        
        void ApplySafeAreaAdjustments()
        {
            // Handle notch and safe areas for modern phones
            Rect safeArea = Screen.safeArea;
            Vector2 screenSize = new Vector2(Screen.width, Screen.height);
            
            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;
            
            anchorMin.x /= screenSize.x;
            anchorMin.y /= screenSize.y;
            anchorMax.x /= screenSize.x;
            anchorMax.y /= screenSize.y;
            
            // Apply to main HUD elements
            if (UIManager.Instance?.inGameHUD != null)
            {
                var hudRect = UIManager.Instance.inGameHUD.GetComponent<RectTransform>();
                hudRect.anchorMin = anchorMin;
                hudRect.anchorMax = anchorMax;
            }
        }
        
        void CheckOrientationChange()
        {
            if (Screen.orientation != currentOrientation)
            {
                currentOrientation = Screen.orientation;
                AdaptHUDLayout();
                
                Debug.Log($"📱 Orientação alterada para: {currentOrientation}");
            }
        }
        
        public void SetCustomLayout(DeviceType deviceType, HUDLayout layout)
        {
            switch (deviceType)
            {
                case DeviceType.Phone:
                    if (currentOrientation == ScreenOrientation.Portrait)
                        phonePortraitLayout = layout;
                    else
                        phoneLandscapeLayout = layout;
                    break;
                case DeviceType.Tablet:
                    tabletLayout = layout;
                    break;
                case DeviceType.Desktop:
                    pcLayout = layout;
                    break;
            }
            
            if (currentDeviceType == deviceType)
            {
                AdaptHUDLayout();
            }
        }
    }
    
    [System.Serializable]
    public class HUDLayout
    {
        public string layoutName;
        public float buttonSizeMultiplier = 1f;
        public float joystickSize = 150f;
        public float joystickDeadZone = 0.1f;
        public UIElementPosition[] elementPositions;
    }
    
    [System.Serializable]
    public class UIElementPosition
    {
        public string elementName;
        public Vector2 position;
        public Vector2 anchorMin;
        public Vector2 anchorMax;
    }
    
    public enum DeviceType
    {
        Phone,
        Tablet,
        Desktop
    }
}
