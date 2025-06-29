using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using ArenaBrasil.Core;

namespace ArenaBrasil.UI
{
    public class UIManager : NetworkBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("UI Screens")]
        public GameObject mainMenuScreen;
        public GameObject lobbyScreen;
        public GameObject inGameHUD;
        public GameObject inventoryScreen;
        public GameObject shopScreen;
        public GameObject settingsScreen;
        public GameObject resultsScreen;

        [Header("HUD Elements")]
        public UnityEngine.UI.Slider healthBar;
        public UnityEngine.UI.Text ammoText;
        public UnityEngine.UI.Text playersAliveText;
        public UnityEngine.UI.Text zoneTimerText;
        public Transform killFeedParent;
        public GameObject killFeedItemPrefab;
        
        [Header("Free Fire Style HUD")]
        public RectTransform miniMap;
        public UnityEngine.UI.Image crosshair;
        public GameObject hitMarker;
        public UnityEngine.UI.Slider armorBar;
        public TMPro.TextMeshProUGUI killCountText;
        public GameObject eliminationNotification;
        public UnityEngine.UI.Button[] quickChatButtons;
        public GameObject spectatorUI;
        public UnityEngine.UI.Slider reviveProgressBar;
        
        [Header("Mobile Specific")]
        public CanvasGroup mobileControlsGroup;
        public UnityEngine.UI.Slider sensitivitySlider;
        public Toggle autoShootToggle;
        public Toggle gyroToggle;
        public RectTransform[] adaptiveUIElements;
        
        [Header("Urban Elements")]
        public GameObject parkourIndicator;
        public UnityEngine.UI.Image interactPrompt;
        public TMPro.TextMeshProUGUI interactText;
        public GameObject vehicleUI;
        public UnityEngine.UI.Slider boostMeter;

        [Header("Brazilian UI Elements")]
        public UnityEngine.UI.Text motivationalText;
        public string[] brazilianPhrases = {
            "Vamos nessa!",
            "Mostra a garra!",
            "É Brasil na veia!",
            "Joga bonito!",
            "Vamo que vamo!"
        };

        [Header("Notification System")]
        public GameObject notificationPrefab;
        public Transform notificationParent;
        public float notificationDuration = 3f;

        [Header("Loading System")]
        public GameObject loadingScreen;
        public UnityEngine.UI.Slider loadingBar;
        public TMPro.TextMeshProUGUI loadingText;

        [Header("Achievement System")]
        public GameObject achievementNotificationPrefab;
        public Transform achievementParent;

        [Header("Gacha System")]
        public GameObject gachaResultScreen;
        public GameObject legendaryAnimationScreen;
        public Transform gachaItemParent;

        [Header("Inventory System")]
        public GameObject inventoryItemPrefab;
        public Transform inventoryGrid;
        public TMPro.TextMeshProUGUI inventoryTitle;

        [Header("Shop System")]
        public GameObject shopItemPrefab;
        public Transform shopGrid;
        public TMPro.TextMeshProUGUI playerCoinsText;
        public TMPro.TextMeshProUGUI playerGemsText;

        [Header("Clan System")]
        public GameObject clanMemberPrefab;
        public Transform clanMemberList;
        public TMPro.TextMeshProUGUI clanNameText;
        public TMPro.TextMeshProUGUI clanLevelText;

        [Header("Battle Pass")]
        public GameObject battlePassTierPrefab;
        public Transform battlePassGrid;
        public UnityEngine.UI.Slider battlePassProgress;

        [Header("Ranking System")]
        public GameObject leaderboardItemPrefab;
        public Transform leaderboardList;
        public TMPro.TextMeshProUGUI playerRankText;

        [Header("Easter Eggs")]
        public GameObject easterEggNotificationPrefab;

        private Dictionary<string, GameObject> activeNotifications = new Dictionary<string, GameObject>();
        private Queue<NotificationData> notificationQueue = new Queue<NotificationData>();
        private Dictionary<UIScreen, GameObject> screens;
        private Queue<KillFeedItem> killFeedItems = new Queue<KillFeedItem>();
        private UIScreen currentScreen = UIScreen.MainMenu;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeScreens();
                InitializeUIComponents();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void InitializeScreens()
        {
            screens = new Dictionary<UIScreen, GameObject>
            {
                { UIScreen.MainMenu, mainMenuScreen },
                { UIScreen.Lobby, lobbyScreen },
                { UIScreen.InGame, inGameHUD },
                { UIScreen.Inventory, inventoryScreen },
                { UIScreen.Shop, shopScreen },
                { UIScreen.Settings, settingsScreen },
                { UIScreen.Results, resultsScreen }
            };

            // Hide all screens initially
            foreach (var screen in screens.Values)
            {
                if (screen != null)
                    screen.SetActive(false);
            }

            // Show main menu by default
            ShowScreen(UIScreen.MainMenu);
        }

        void InitializeUIComponents()
        {
            // Setup UI event listeners
            SetupButtonListeners();

            // Initialize notification system
            if (notificationParent == null)
            {
                var notifGO = new GameObject("NotificationParent");
                notificationParent = notifGO.transform;
                notificationParent.SetParent(transform);
            }

            // Start notification processing
            InvokeRepeating(nameof(ProcessNotificationQueue), 0.1f, 0.1f);
        }

        void SetupButtonListeners()
        {
            // Find and setup all UI buttons
            var buttons = FindObjectsOfType<UnityEngine.UI.Button>();
            foreach (var button in buttons)
            {
                switch (button.name.ToLower())
                {
                    case "playbutton":
                        button.onClick.AddListener(OnPlayButtonClicked);
                        break;
                    case "shopbutton":
                        button.onClick.AddListener(OnShopButtonClicked);
                        break;
                    case "inventorybutton":
                        button.onClick.AddListener(OnInventoryButtonClicked);
                        break;
                    case "settingsbutton":
                        button.onClick.AddListener(OnSettingsButtonClicked);
                        break;
                    case "clanbutton":
                        button.onClick.AddListener(OnClanButtonClicked);
                        break;
                        // Add more button mappings as needed
                }
            }
        }

        public void ShowScreen(UIScreen screen)
        {
            // Hide current screen
            if (screens.ContainsKey(currentScreen) && screens[currentScreen] != null)
            {
                screens[currentScreen].SetActive(false);
            }

            // Show new screen
            if (screens.ContainsKey(screen) && screens[screen] != null)
            {
                screens[screen].SetActive(true);
                currentScreen = screen;

                OnScreenChanged(screen);
            }
        }

        void OnScreenChanged(UIScreen screen)
        {
            switch (screen)
            {
                case UIScreen.MainMenu:
                    Cursor.lockState = CursorLockMode.None;
                    break;
                case UIScreen.InGame:
                    Cursor.lockState = CursorLockMode.Locked;
                    ShowMotivationalPhrase();
                    break;
                case UIScreen.Results:
                    Cursor.lockState = CursorLockMode.None;
                    break;
            }
        }

        public void UpdateHealthBar(float health, float maxHealth)
        {
            if (healthBar != null)
            {
                healthBar.value = health / maxHealth;
            }
        }

        public void UpdateAmmoDisplay(int currentAmmo, int maxAmmo)
        {
            if (ammoText != null)
            {
                ammoText.text = $"{currentAmmo}/{maxAmmo}";
            }
        }

        public void UpdatePlayersAlive(int count)
        {
            if (playersAliveText != null)
            {
                playersAliveText.text = $"Jogadores: {count}";
            }
        }

        public void UpdateZoneTimer(float timeRemaining)
        {
            if (zoneTimerText != null)
            {
                int minutes = Mathf.FloorToInt(timeRemaining / 60);
                int seconds = Mathf.FloorToInt(timeRemaining % 60);
                zoneTimerText.text = $"Zona: {minutes:00}:{seconds:00}";
            }
        }

        public void AddKillFeedItem(string killer, string victim, string weapon)
        {
            if (killFeedItemPrefab != null && killFeedParent != null)
            {
                var item = Instantiate(killFeedItemPrefab, killFeedParent);
                var killFeedItem = item.GetComponent<KillFeedItem>();

                if (killFeedItem != null)
                {
                    killFeedItem.Setup(killer, victim, weapon);
                    killFeedItems.Enqueue(killFeedItem);

                    // Remove old items
                    while (killFeedItems.Count > 5)
                    {
                        var oldItem = killFeedItems.Dequeue();
                        if (oldItem != null)
                            Destroy(oldItem.gameObject);
                    }
                }
            }
        }

        void ShowMotivationalPhrase()
        {
            if (motivationalText != null && brazilianPhrases.Length > 0)
            {
                string phrase = brazilianPhrases[Random.Range(0, brazilianPhrases.Length)];
                motivationalText.text = phrase;

                // Hide after 3 seconds
                Invoke(nameof(HideMotivationalText), 3f);
            }
        }

        void HideMotivationalText()
        {
            if (motivationalText != null)
            {
                motivationalText.text = "";
            }
        }

        // Button Events
        public void OnPlayButtonClicked()
        {
            if (ArenaBrasilGameManager.Instance != null)
            {
                ShowScreen(UIScreen.Lobby);
            }
        }

        public void OnShopButtonClicked()
        {
            ShowScreen(UIScreen.Shop);
        }

        public void OnSettingsButtonClicked()
        {
            ShowScreen(UIScreen.Settings);
        }

        public void OnBackButtonClicked()
        {
            ShowScreen(UIScreen.MainMenu);
        }

        public void OnInventoryButtonClicked()
        {
            ShowScreen(UIScreen.Inventory);
        }

        public void OnExitGameButtonClicked()
        {
            Application.Quit();
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                Debug.Log("Arena Brasil - Game has focus");
            }
        }

        // Advanced UI Methods
        public void ShowNotification(string title, string message, NotificationType type = NotificationType.Info)
        {
            var notificationData = new NotificationData
            {
                title = title,
                message = message,
                type = type,
                duration = notificationDuration,
                timestamp = Time.time
            };

            notificationQueue.Enqueue(notificationData);
        }

        void ProcessNotificationQueue()
        {
            if (notificationQueue.Count > 0 && activeNotifications.Count < 3)
            {
                var notification = notificationQueue.Dequeue();
                ShowNotificationImmediate(notification);
            }
        }

        void ShowNotificationImmediate(NotificationData data)
        {
            if (notificationPrefab == null) return;

            var notificationGO = Instantiate(notificationPrefab, notificationParent);
            var notificationUI = notificationGO.GetComponent<NotificationUI>();

            if (notificationUI != null)
            {
                notificationUI.Setup(data);
                string notificationId = $"notification_{Time.time}";
                activeNotifications[notificationId] = notificationGO;

                // Auto-remove after duration
                StartCoroutine(RemoveNotificationAfterDelay(notificationId, data.duration));
            }
        }

        System.Collections.IEnumerator RemoveNotificationAfterDelay(string notificationId, float delay)
        {
            yield return new WaitForSeconds(delay);
            RemoveNotification(notificationId);
        }

        void RemoveNotification(string notificationId)
        {
            if (activeNotifications.TryGetValue(notificationId, out var notification))
            {
                if (notification != null)
                {
                    Destroy(notification);
                }
                activeNotifications.Remove(notificationId);
            }
        }

        // Loading Screen Methods
        public void ShowLoadingScreen(string loadingMessage = "Carregando...")
        {
            if (loadingScreen != null)
            {
                loadingScreen.SetActive(true);
                if (loadingText != null)
                    loadingText.text = loadingMessage;
                if (loadingBar != null)
                    loadingBar.value = 0f;
            }
        }

        public void UpdateLoadingProgress(float progress, string message = "")
        {
            if (loadingBar != null)
                loadingBar.value = progress;
            if (!string.IsNullOrEmpty(message) && loadingText != null)
                loadingText.text = message;
        }

        public void HideLoadingScreen()
        {
            if (loadingScreen != null)
                loadingScreen.SetActive(false);
        }

        // Achievement UI Methods
        public void ShowAchievementNotification(Achievement achievement)
        {
            if (achievementNotificationPrefab == null) return;

            var achievementGO = Instantiate(achievementNotificationPrefab, achievementParent);
            var achievementUI = achievementGO.GetComponent<AchievementNotificationUI>();

            if (achievementUI != null)
            {
                achievementUI.Setup(achievement);

                // Play achievement sound
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySound("achievement_unlock");
                }

                // Auto-remove after 5 seconds
                Destroy(achievementGO, 5f);
            }
        }

        // Gacha UI Methods
        public void ShowGachaResults(List<GachaItem> items)
        {
            if (gachaResultScreen != null)
            {
                gachaResultScreen.SetActive(true);

                // Clear previous items
                foreach (Transform child in gachaItemParent)
                {
                    Destroy(child.gameObject);
                }

                // Show new items
                foreach (var item in items)
                {
                    var itemUI = CreateGachaItemUI(item);
                    itemUI.transform.SetParent(gachaItemParent);
                }
            }
        }

        GameObject CreateGachaItemUI(GachaItem item)
        {
            var itemGO = new GameObject($"GachaItem_{item.itemId}");
            var image = itemGO.AddComponent<UnityEngine.UI.Image>();
            var text = itemGO.AddComponent<TMPro.TextMeshProUGUI>();

            // Setup item display
            if (item.icon != null)
                image.sprite = item.icon;

            text.text = item.itemName;
            text.color = GetRarityColor(item.rarity);

            return itemGO;
        }

        Color GetRarityColor(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Common: return Color.white;
                case ItemRarity.Uncommon: return Color.green;
                case ItemRarity.Rare: return Color.blue;
                case ItemRarity.Epic: return Color.magenta;
                case ItemRarity.Legendary: return Color.yellow;
                default: return Color.white;
            }
        }

        public void ShowLegendaryAnimation()
        {
            if (legendaryAnimationScreen != null)
            {
                legendaryAnimationScreen.SetActive(true);

                // Play legendary animation and sound
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySound("legendary_drop");
                }

                // Hide after animation
                StartCoroutine(HideLegendaryAnimationAfterDelay(3f));
            }
        }

        System.Collections.IEnumerator HideLegendaryAnimationAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (legendaryAnimationScreen != null)
                legendaryAnimationScreen.SetActive(false);
        }

        // Inventory UI Methods
        public void UpdateInventoryDisplay(PlayerInventory inventory)
        {
            if (inventoryGrid == null) return;

            // Clear existing items
            foreach (Transform child in inventoryGrid)
            {
                Destroy(child.gameObject);
            }

            // Display current items
            foreach (var item in inventory.items)
            {
                CreateInventoryItemUI(item);
            }

            // Update inventory title
            if (inventoryTitle != null)
            {
                inventoryTitle.text = $"Inventário ({inventory.items.Count}/{inventory.maxSlots})";
            }
        }

        void CreateInventoryItemUI(InventoryItem item)
        {
            if (inventoryItemPrefab == null) return;

            var itemUI = Instantiate(inventoryItemPrefab, inventoryGrid);
            var itemComponent = itemUI.GetComponent<InventoryItemUI>();

            if (itemComponent != null)
            {
                itemComponent.Setup(item);
            }
        }

        // Shop UI Methods
        public void UpdateShopDisplay(List<ShopItem> shopItems)
        {
            if (shopGrid == null) return;

            // Clear existing items
            foreach (Transform child in shopGrid)
            {
                Destroy(child.gameObject);
            }

            // Display shop items
            foreach (var item in shopItems)
            {
                CreateShopItemUI(item);
            }
        }

        void CreateShopItemUI(ShopItem item)
        {
            if (shopItemPrefab == null) return;

            var itemUI = Instantiate(shopItemPrefab, shopGrid);
            var shopComponent = itemUI.GetComponent<ShopItemUI>();

            if (shopComponent != null)
            {
                shopComponent.Setup(item);
            }
        }

        public void UpdateCurrencyDisplay(long coins, long gems)
        {
            if (playerCoinsText != null)
                playerCoinsText.text = coins.ToString("N0");
            if (playerGemsText != null)
                playerGemsText.text = gems.ToString("N0");
        }

        // Easter Egg UI Methods
        public void ShowEasterEggNotification(CulturalEasterEgg easterEgg)
        {
            if (easterEggNotificationPrefab == null) return;

            var eggUI = Instantiate(easterEggNotificationPrefab, notificationParent);
            var eggComponent = eggUI.GetComponent<EasterEggNotificationUI>();

            if (eggComponent != null)
            {
                eggComponent.Setup(easterEgg);

                // Play discovery sound
                if (AudioManager.Instance != null && !string.IsNullOrEmpty(easterEgg.audioClip))
                {
                    AudioManager.Instance.PlaySound(easterEgg.audioClip);
                }

                // Auto-remove
                Destroy(eggUI, 4f);
            }
        }

        // Additional UI Helper Methods
        public void ShowVictoryScreen(string playerName, int kills, int placement)
        {
            ShowNotification("VITÓRIA ROYALE!", $"{playerName} conquistou a arena com {kills} eliminações!", NotificationType.Victory);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayVictoryMusic();
            }
        }

        public void ShowDefeatScreen(int placement, int kills)
        {
            ShowNotification("Fim de Jogo", $"Posição: #{placement} | Eliminações: {kills}", NotificationType.Info);
        }

        public void ShowKickMessage(string reason)
        {
            ShowNotification("Desconectado", $"Você foi desconectado: {reason}", NotificationType.Error);
        }

        // Button Event Handlers
        void OnPlayButtonClicked()
        {
            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.StartMatchmaking();
            }
        }

        void OnShopButtonClicked()
        {
            ShowScreen(UIScreen.Shop);

            // Load shop data
            if (EconomyManager.Instance != null)
            {
                var shopItems = EconomyManager.Instance.GetShopItems();
                UpdateShopDisplay(shopItems);
            }
        }

        void OnInventoryButtonClicked()
        {
            ShowScreen(UIScreen.Inventory);

            // Load inventory data
            if (InventorySystem.Instance != null && NetworkManager.Singleton != null)
            {
                var inventory = InventorySystem.Instance.GetPlayerInventory(NetworkManager.Singleton.LocalClientId);
                if (inventory != null)
                    UpdateInventoryDisplay(inventory);
            }
        }

        void OnClanButtonClicked()
        {
            ShowScreen(UIScreen.Lobby);
            // Load clan data if needed
        }
    }

    public enum UIScreen
    {
        MainMenu,
        Lobby,
        InGame,
        Inventory,
        Shop,
        Settings,
        Results
    }

    [System.Serializable]
    public class KillFeedItem : MonoBehaviour
    {
        public UnityEngine.UI.Text killerText;
        public UnityEngine.UI.Text victimText;
        public UnityEngine.UI.Text weaponText;

        public void Setup(string killer, string victim, string weapon)
        {
            if (killerText != null) killerText.text = killer;
            if (victimText != null) victimText.text = victim;
            if (weaponText != null) weaponText.text = weapon;

            // Auto destroy after 10 seconds
            Destroy(gameObject, 10f);
        }
    }

    // Data structures
    public enum NotificationType
    {
        Info,
        Warning,
        Error,
        Victory
    }

    [System.Serializable]
    public class NotificationData
    {
        public string title;
        public string message;
        public NotificationType type;
        public float duration;
        public float timestamp;
    }

    [System.Serializable]
    public class Achievement
    {
        public string achievementId;
        public string title;
        public string description;
        public Sprite icon;
    }

    [System.Serializable]
    public class GachaItem
    {
        public string itemId;
        public string itemName;
        public Sprite icon;
        public ItemRarity rarity;
    }

    public enum ItemRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    [System.Serializable]
    public class InventoryItem
    {
        public string itemId;
        public string itemName;
        public Sprite icon;
        public int quantity;
    }

    [System.Serializable]
    public class PlayerInventory
    {
        public List<InventoryItem> items = new List<InventoryItem>();
        public int maxSlots = 20;
    }

    [System.Serializable]
    public class ShopItem
    {
        public string itemId;
        public string itemName;
        public Sprite icon;
        public long priceCoins;
        public long priceGems;
    }

    [System.Serializable]
    public class CulturalEasterEgg
    {
        public string eggId;
        public string title;
        public string description;
        public string audioClip;
        public Sprite image;
    }

    // UI Components
    public class NotificationUI : MonoBehaviour
    {
        public TMPro.TextMeshProUGUI titleText;
        public TMPro.TextMeshProUGUI messageText;
        public UnityEngine.UI.Image icon;

        public void Setup(NotificationData data)
        {
            if (titleText != null)
                titleText.text = data.title;
            if (messageText != null)
                messageText.text = data.message;

            // Setup icon based on type
            switch (data.type)
            {
                case NotificationType.Info:
                    // Set info icon
                    break;
                case NotificationType.Warning:
                    // Set warning icon
                    break;
                case NotificationType.Error:
                    // Set error icon
                    break;
                case NotificationType.Victory:
                    // Set victory icon
                    break;
            }
        }
    }

    public class AchievementNotificationUI : MonoBehaviour
    {
        public TMPro.TextMeshProUGUI titleText;
        public TMPro.TextMeshProUGUI descriptionText;
        public UnityEngine.UI.Image icon;

        public void Setup(Achievement achievement)
        {
            if (titleText != null)
                titleText.text = achievement.title;
            if (descriptionText != null)
                descriptionText.text = achievement.description;
            if (icon != null)
                icon.sprite = achievement.icon;
        }
    }

    public class InventoryItemUI : MonoBehaviour
    {
        public UnityEngine.UI.Image icon;
        public TMPro.TextMeshProUGUI quantityText;

        public void Setup(InventoryItem item)
        {
            if (icon != null)
                icon.sprite = item.icon;
            if (quantityText != null)
                quantityText.text = item.quantity.ToString();
        }
    }

    public class ShopItemUI : MonoBehaviour
    {
        public UnityEngine.UI.Image icon;
        public TMPro.TextMeshProUGUI priceText;
        public UnityEngine.UI.Button buyButton;

        private ShopItem item;

        public void Setup(ShopItem item)
        {
            this.item = item;
            if (icon != null)
                icon.sprite = item.icon;
            if (priceText != null)
                priceText.text = $"{item.priceCoins} Coins / {item.priceGems} Gems";
            if (buyButton != null)
            {
                buyButton.onClick.RemoveAllListeners();
                buyButton.onClick.AddListener(OnBuyButtonClicked);
            }
        }

        void OnBuyButtonClicked()
        {
            // Handle buy logic
            Debug.Log($"Buying item: {item.itemName}");
        }
    }

    public class EasterEggNotificationUI : MonoBehaviour
    {
        public TMPro.TextMeshProUGUI titleText;
        public TMPro.TextMeshProUGUI descriptionText;
        public UnityEngine.UI.Image image;

        public void Setup(CulturalEasterEgg easterEgg)
        {
            if (titleText != null)
                titleText.text = easterEgg.title;
            if (descriptionText != null)
                descriptionText.text = easterEgg.description;
            if (image != null)
                image.sprite = easterEgg.image;
        }
    }
}