
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
        
        [Header("Brazilian UI Elements")]
        public UnityEngine.UI.Text motivationalText;
        public string[] brazilianPhrases = {
            "Vamos nessa!",
            "Mostra a garra!",
            "É Brasil na veia!",
            "Joga bonito!",
            "Vamo que vamo!"
        };
        
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
}
