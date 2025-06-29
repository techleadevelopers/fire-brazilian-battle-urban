
using System;
using System.Threading.Tasks;
using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Functions;

namespace ArenaBrasil.Backend
{
    public class FirebaseBackendService : MonoBehaviour
    {
        public static FirebaseBackendService Instance { get; private set; }
        
        [Header("Firebase Configuration")]
        public bool initializeOnStart = true;
        
        // Firebase services
        private FirebaseApp firebaseApp;
        private FirebaseAuth firebaseAuth;
        private FirebaseFirestore firestore;
        private FirebaseFunctions functions;
        
        // Events
        public event Action OnFirebaseInitialized;
        public event Action<FirebaseUser> OnUserSignedIn;
        public event Action OnUserSignedOut;
        
        // Current user data
        public FirebaseUser CurrentUser => firebaseAuth?.CurrentUser;
        public bool IsSignedIn => CurrentUser != null;
        
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
            if (initializeOnStart)
            {
                InitializeFirebase();
            }
        }
        
        public async void InitializeFirebase()
        {
            try
            {
                Debug.Log("Arena Brasil - Initializing Firebase");
                
                var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
                
                if (dependencyStatus == DependencyStatus.Available)
                {
                    firebaseApp = FirebaseApp.DefaultInstance;
                    firebaseAuth = FirebaseAuth.DefaultInstance;
                    firestore = FirebaseFirestore.DefaultInstance;
                    functions = FirebaseFunctions.DefaultInstance;
                    
                    // Configure auth state listener
                    firebaseAuth.StateChanged += OnAuthStateChanged;
                    
                    Debug.Log("Firebase initialized successfully");
                    OnFirebaseInitialized?.Invoke();
                }
                else
                {
                    Debug.LogError($"Firebase dependency error: {dependencyStatus}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Firebase initialization failed: {e.Message}");
            }
        }
        
        // Authentication Methods
        public async Task<bool> SignInWithEmailAndPassword(string email, string password)
        {
            try
            {
                Debug.Log($"Signing in user: {email}");
                
                var authResult = await firebaseAuth.SignInWithEmailAndPasswordAsync(email, password);
                Debug.Log($"User signed in successfully: {authResult.User.UserId}");
                
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Sign in failed: {e.Message}");
                return false;
            }
        }
        
        public async Task<bool> CreateUserWithEmailAndPassword(string email, string password, string displayName)
        {
            try
            {
                Debug.Log($"Creating user: {email}");
                
                var authResult = await firebaseAuth.CreateUserWithEmailAndPasswordAsync(email, password);
                
                // Update display name
                var profile = new UserProfile { DisplayName = displayName };
                await authResult.User.UpdateUserProfileAsync(profile);
                
                // Create player profile in Firestore
                await CreatePlayerProfile(authResult.User.UserId, displayName);
                
                Debug.Log($"User created successfully: {authResult.User.UserId}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"User creation failed: {e.Message}");
                return false;
            }
        }
        
        public void SignOut()
        {
            try
            {
                firebaseAuth.SignOut();
                Debug.Log("User signed out");
            }
            catch (Exception e)
            {
                Debug.LogError($"Sign out failed: {e.Message}");
            }
        }
        
        // Player Data Methods
        public async Task CreatePlayerProfile(string userId, string displayName)
        {
            try
            {
                var playerData = new PlayerProfile
                {
                    UserId = userId,
                    DisplayName = displayName,
                    Level = 1,
                    XP = 0,
                    Coins = 100, // Starting coins
                    Matches = 0,
                    Wins = 0,
                    Kills = 0,
                    CreatedAt = DateTime.UtcNow,
                    LastLogin = DateTime.UtcNow,
                    SelectedHero = HeroType.Saci.ToString()
                };
                
                await firestore.Collection("players").Document(userId).SetAsync(playerData);
                Debug.Log($"Player profile created for {displayName}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to create player profile: {e.Message}");
            }
        }
        
        public async Task<PlayerProfile> GetPlayerProfile(string userId)
        {
            try
            {
                var snapshot = await firestore.Collection("players").Document(userId).GetSnapshotAsync();
                
                if (snapshot.Exists)
                {
                    return snapshot.ConvertTo<PlayerProfile>();
                }
                
                return null;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to get player profile: {e.Message}");
                return null;
            }
        }
        
        public async Task UpdatePlayerStats(string userId, int xpGained, int coinsGained, bool won, int kills)
        {
            try
            {
                var playerRef = firestore.Collection("players").Document(userId);
                
                await firestore.RunTransactionAsync(async transaction =>
                {
                    var snapshot = await transaction.GetSnapshotAsync(playerRef);
                    var playerData = snapshot.ConvertTo<PlayerProfile>();
                    
                    playerData.XP += xpGained;
                    playerData.Coins += coinsGained;
                    playerData.Matches++;
                    playerData.Kills += kills;
                    playerData.LastLogin = DateTime.UtcNow;
                    
                    if (won)
                    {
                        playerData.Wins++;
                    }
                    
                    // Level up check
                    int newLevel = CalculateLevel(playerData.XP);
                    if (newLevel > playerData.Level)
                    {
                        playerData.Level = newLevel;
                        // Give level up rewards
                        playerData.Coins += newLevel * 10;
                    }
                    
                    transaction.Set(playerRef, playerData);
                });
                
                Debug.Log($"Player stats updated for {userId}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to update player stats: {e.Message}");
            }
        }
        
        // Shop and Economy
        public async Task<bool> PurchaseItem(string itemId, int cost)
        {
            try
            {
                var purchaseData = new { itemId = itemId, cost = cost };
                var callable = functions.GetHttpsCallable("purchaseItem");
                var result = await callable.CallAsync(purchaseData);
                
                return result.Data != null;
            }
            catch (Exception e)
            {
                Debug.LogError($"Purchase failed: {e.Message}");
                return false;
            }
        }
        
        // Leaderboards
        public async Task<System.Collections.Generic.List<PlayerProfile>> GetLeaderboard(int limit = 100)
        {
            try
            {
                var query = firestore.Collection("players")
                    .OrderByDescending("XP")
                    .Limit(limit);
                
                var snapshot = await query.GetSnapshotAsync();
                var leaderboard = new System.Collections.Generic.List<PlayerProfile>();
                
                foreach (var doc in snapshot.Documents)
                {
                    leaderboard.Add(doc.ConvertTo<PlayerProfile>());
                }
                
                return leaderboard;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to get leaderboard: {e.Message}");
                return new System.Collections.Generic.List<PlayerProfile>();
            }
        }
        
        // Utility Methods
        int CalculateLevel(int xp)
        {
            return Mathf.FloorToInt(xp / 1000f) + 1;
        }
        
        void OnAuthStateChanged(object sender, EventArgs eventArgs)
        {
            if (firebaseAuth.CurrentUser != null)
            {
                Debug.Log($"User signed in: {firebaseAuth.CurrentUser.UserId}");
                OnUserSignedIn?.Invoke(firebaseAuth.CurrentUser);
            }
            else
            {
                Debug.Log("User signed out");
                OnUserSignedOut?.Invoke();
            }
        }
        
        void OnDestroy()
        {
            if (firebaseAuth != null)
            {
                firebaseAuth.StateChanged -= OnAuthStateChanged;
            }
        }
    }
    
    [Serializable]
    public class PlayerProfile
    {
        public string UserId;
        public string DisplayName;
        public int Level;
        public int XP;
        public int Coins;
        public int Matches;
        public int Wins;
        public int Kills;
        public DateTime CreatedAt;
        public DateTime LastLogin;
        public string SelectedHero;
    }
}
