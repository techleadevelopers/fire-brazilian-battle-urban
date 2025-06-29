
using UnityEngine;
using Unity.Netcode;
using System.Collections;
using ArenaBrasil.Environment;

namespace ArenaBrasil.Gameplay.SafeZone
{
    public class SafeZoneController : NetworkBehaviour
    {
        public static SafeZoneController Instance { get; private set; }
        
        [Header("Safe Zone Configuration")]
        public Transform safeZoneCenter;
        public GameObject safeZoneVisualPrefab;
        public Material safeZoneWallMaterial;
        
        [Header("Zone Settings")]
        public float initialRadius = 1000f;
        public float finalRadius = 50f;
        public int totalPhases = 8;
        public float[] phaseDurations = { 300f, 240f, 180f, 120f, 90f, 60f, 45f, 30f }; // 5min, 4min, 3min, 2min, 1.5min, 1min, 45s, 30s
        
        [Header("Damage Settings")]
        public float[] phaseDamage = { 1f, 2f, 5f, 8f, 12f, 15f, 20f, 25f };
        public float damageInterval = 1f;
        
        // Network Variables
        private NetworkVariable<Vector3> networkZoneCenter = new NetworkVariable<Vector3>();
        private NetworkVariable<float> networkCurrentRadius = new NetworkVariable<float>();
        private NetworkVariable<int> networkCurrentPhase = new NetworkVariable<int>();
        private NetworkVariable<float> networkPhaseTimeRemaining = new NetworkVariable<float>();
        
        // Local state
        private GameObject safeZoneVisual;
        private bool isZoneActive = false;
        private Coroutine phaseCoroutine;
        private Coroutine damageCoroutine;
        
        // Events
        public event System.Action<int, float> OnPhaseChanged;
        public event System.Action<Vector3, float> OnZoneUpdated;
        public event System.Action<float> OnZoneDamage;
        
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
        
        public override void OnNetworkSpawn()
        {
            // Subscribe to network variable changes
            networkZoneCenter.OnValueChanged += OnZoneCenterChanged;
            networkCurrentRadius.OnValueChanged += OnRadiusChanged;
            networkCurrentPhase.OnValueChanged += OnPhaseChanged_Network;
            
            if (IsServer)
            {
                InitializeZone();
            }
            
            // Create visual representation
            CreateZoneVisual();
        }
        
        void InitializeZone()
        {
            // Set random zone center
            Vector3 randomCenter = new Vector3(
                Random.Range(-200f, 200f),
                0f,
                Random.Range(-200f, 200f)
            );
            
            networkZoneCenter.Value = randomCenter;
            networkCurrentRadius.Value = initialRadius;
            networkCurrentPhase.Value = 0;
            networkPhaseTimeRemaining.Value = phaseDurations[0];
        }
        
        public void StartSafeZone()
        {
            if (!IsServer) return;
            
            isZoneActive = true;
            phaseCoroutine = StartCoroutine(PhaseSequence());
            damageCoroutine = StartCoroutine(DamageLoop());
            
            Debug.Log("Arena Brasil - Safe Zone activated!");
        }
        
        IEnumerator PhaseSequence()
        {
            for (int phase = 0; phase < totalPhases; phase++)
            {
                networkCurrentPhase.Value = phase;
                float phaseDuration = phaseDurations[phase];
                networkPhaseTimeRemaining.Value = phaseDuration;
                
                Debug.Log($"Safe Zone Phase {phase + 1} - Duration: {phaseDuration}s");
                
                // Wait for phase duration with countdown
                float timer = phaseDuration;
                while (timer > 0)
                {
                    networkPhaseTimeRemaining.Value = timer;
                    yield return new WaitForSeconds(1f);
                    timer -= 1f;
                }
                
                // Shrink zone
                if (phase < totalPhases - 1)
                {
                    yield return StartCoroutine(ShrinkZone(phase + 1));
                }
            }
            
            // Final phase - zone closed
            networkCurrentRadius.Value = 0f;
            Debug.Log("Safe Zone fully closed!");
        }
        
        IEnumerator ShrinkZone(int newPhase)
        {
            float startRadius = networkCurrentRadius.Value;
            float targetRadius = CalculateRadiusForPhase(newPhase);
            float shrinkDuration = 30f; // 30 seconds to shrink
            
            Debug.Log($"Shrinking zone from {startRadius}m to {targetRadius}m");
            
            float elapsed = 0f;
            while (elapsed < shrinkDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / shrinkDuration;
                networkCurrentRadius.Value = Mathf.Lerp(startRadius, targetRadius, progress);
                yield return null;
            }
            
            networkCurrentRadius.Value = targetRadius;
        }
        
        float CalculateRadiusForPhase(int phase)
        {
            float progress = (float)phase / (totalPhases - 1);
            return Mathf.Lerp(initialRadius, finalRadius, progress);
        }
        
        IEnumerator DamageLoop()
        {
            while (isZoneActive)
            {
                yield return new WaitForSeconds(damageInterval);
                
                if (networkCurrentPhase.Value > 0) // No damage in first phase
                {
                    ApplyZoneDamage();
                }
            }
        }
        
        void ApplyZoneDamage()
        {
            float currentDamage = phaseDamage[Mathf.Min(networkCurrentPhase.Value, phaseDamage.Length - 1)];
            
            // Find all players outside safe zone
            foreach (var client in NetworkManager.Singleton.ConnectedClients)
            {
                var player = client.Value.PlayerObject?.GetComponent<PlayerController>();
                if (player != null)
                {
                    float distanceFromCenter = Vector3.Distance(player.transform.position, networkZoneCenter.Value);
                    if (distanceFromCenter > networkCurrentRadius.Value)
                    {
                        // Player is outside safe zone - apply damage
                        player.TakeDamage(currentDamage);
                        NotifyZoneDamageClientRpc(client.Key, currentDamage);
                    }
                }
            }
        }
        
        [ClientRpc]
        void NotifyZoneDamageClientRpc(ulong playerId, float damage)
        {
            if (playerId == NetworkManager.Singleton.LocalClientId)
            {
                OnZoneDamage?.Invoke(damage);
                ShowZoneDamageEffect();
            }
        }
        
        void ShowZoneDamageEffect()
        {
            // Create screen edge red effect
            Debug.Log("Taking zone damage!");
        }
        
        void CreateZoneVisual()
        {
            if (safeZoneVisualPrefab != null)
            {
                safeZoneVisual = Instantiate(safeZoneVisualPrefab);
                UpdateZoneVisual();
            }
        }
        
        void UpdateZoneVisual()
        {
            if (safeZoneVisual != null)
            {
                safeZoneVisual.transform.position = networkZoneCenter.Value;
                safeZoneVisual.transform.localScale = Vector3.one * networkCurrentRadius.Value * 2f;
            }
        }
        
        // Network event handlers
        void OnZoneCenterChanged(Vector3 previousValue, Vector3 newValue)
        {
            UpdateZoneVisual();
            OnZoneUpdated?.Invoke(newValue, networkCurrentRadius.Value);
        }
        
        void OnRadiusChanged(float previousValue, float newValue)
        {
            UpdateZoneVisual();
            OnZoneUpdated?.Invoke(networkZoneCenter.Value, newValue);
        }
        
        void OnPhaseChanged_Network(int previousValue, int newValue)
        {
            OnPhaseChanged?.Invoke(newValue, networkPhaseTimeRemaining.Value);
        }
        
        // Public getters
        public Vector3 GetZoneCenter() => networkZoneCenter.Value;
        public float GetCurrentRadius() => networkCurrentRadius.Value;
        public int GetCurrentPhase() => networkCurrentPhase.Value;
        public float GetPhaseTimeRemaining() => networkPhaseTimeRemaining.Value;
        
        public bool IsPositionInZone(Vector3 position)
        {
            float distance = Vector3.Distance(position, networkZoneCenter.Value);
            return distance <= networkCurrentRadius.Value;
        }
        
        public float GetDistanceToZone(Vector3 position)
        {
            float distance = Vector3.Distance(position, networkZoneCenter.Value);
            return Mathf.Max(0f, distance - networkCurrentRadius.Value);
        }
        
        void OnDestroy()
        {
            if (phaseCoroutine != null)
                StopCoroutine(phaseCoroutine);
            
            if (damageCoroutine != null)
                StopCoroutine(damageCoroutine);
        }
    }
}
