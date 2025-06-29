
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArenaBrasil.Environment
{
    public class MapManager : MonoBehaviour
    {
        public static MapManager Instance { get; private set; }
        
        [Header("Brazilian Maps Configuration")]
        public List<BrazilianMapData> availableMaps = new List<BrazilianMapData>();
        public BrazilianMapData currentMap;
        
        [Header("Safe Zone Settings")]
        public Transform safeZoneCenter;
        public float initialSafeZoneRadius = 500f;
        public float finalSafeZoneRadius = 50f;
        public int totalSafeZonePhases = 8;
        public float timeBetweenPhases = 120f; // 2 minutes
        
        [Header("Environmental Effects")]
        public bool enableDynamicWeather = true;
        public bool enableDayNightCycle = false;
        public float weatherChangeInterval = 300f; // 5 minutes
        
        // Safe zone state
        private int currentSafeZonePhase = 0;
        private float phaseTimer = 0f;
        private bool safeZoneActive = false;
        
        // Events
        public event Action<BrazilianMapData> OnMapLoaded;
        public event Action<int, float> OnSafeZonePhaseChanged;
        public event Action<Vector3, float> OnSafeZoneUpdated;
        public event Action<WeatherType> OnWeatherChanged;
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeMapManager();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void Update()
        {
            if (safeZoneActive)
            {
                UpdateSafeZone();
            }
        }
        
        void InitializeMapManager()
        {
            Debug.Log("Arena Brasil - Initializing Map Manager");
            
            // Initialize Brazilian maps
            InitializeBrazilianMaps();
        }
        
        void InitializeBrazilianMaps()
        {
            if (availableMaps.Count == 0)
            {
                // Create default Brazilian maps
                availableMaps.Add(new BrazilianMapData
                {
                    mapName = "Favela da Vitória",
                    sceneName = "Map_Favela",
                    culturalTheme = CulturalTheme.Favela,
                    description = "Uma vibrante favela carioca com becos estreitos e vistas panorâmicas da cidade",
                    maxPlayers = 60,
                    weatherTypes = new List<WeatherType> { WeatherType.Sunny, WeatherType.Rain, WeatherType.Storm }
                });
                
                availableMaps.Add(new BrazilianMapData
                {
                    mapName = "Coração da Amazônia",
                    sceneName = "Map_Amazonia",
                    culturalTheme = CulturalTheme.Amazon,
                    description = "Densa floresta amazônica com rios, árvores gigantes e vida selvagem",
                    maxPlayers = 60,
                    weatherTypes = new List<WeatherType> { WeatherType.Fog, WeatherType.Rain, WeatherType.Humid }
                });
                
                availableMaps.Add(new BrazilianMapData
                {
                    mapName = "Metrópole Paulista",
                    sceneName = "Map_Metropole",
                    culturalTheme = CulturalTheme.Metropolis,
                    description = "Moderna metrópole com arranha-céus, shopping centers e avenidas movimentadas",
                    maxPlayers = 60,
                    weatherTypes = new List<WeatherType> { WeatherType.Sunny, WeatherType.Cloudy, WeatherType.Smog }
                });
                
                availableMaps.Add(new BrazilianMapData
                {
                    mapName = "Sertão Nordestino",
                    sceneName = "Map_Sertao",
                    culturalTheme = CulturalTheme.Sertao,
                    description = "Árido sertão nordestino com cidades históricas e paisagens únicas",
                    maxPlayers = 60,
                    weatherTypes = new List<WeatherType> { WeatherType.Sunny, WeatherType.Hot, WeatherType.Drought }
                });
                
                availableMaps.Add(new BrazilianMapData
                {
                    mapName = "Pantanal Selvagem",
                    sceneName = "Map_Pantanal",
                    culturalTheme = CulturalTheme.Pantanal,
                    description = "Vasta planície alagável com rica biodiversidade e paisagens deslumbrantes",
                    maxPlayers = 60,
                    weatherTypes = new List<WeatherType> { WeatherType.Humid, WeatherType.Rain, WeatherType.Fog }
                });
            }
        }
        
        public void LoadRandomMap()
        {
            if (availableMaps.Count > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, availableMaps.Count);
                LoadMap(availableMaps[randomIndex]);
            }
            else
            {
                Debug.LogError("No maps available to load!");
            }
        }
        
        public void LoadMap(BrazilianMapData mapData)
        {
            Debug.Log($"Arena Brasil - Loading map: {mapData.mapName}");
            
            currentMap = mapData;
            
            // Load the scene
            SceneManager.LoadScene(mapData.sceneName);
            
            // Initialize map-specific settings
            InitializeMapSettings(mapData);
            
            OnMapLoaded?.Invoke(mapData);
        }
        
        void InitializeMapSettings(BrazilianMapData mapData)
        {
            // Set safe zone center based on map
            if (safeZoneCenter == null)
            {
                safeZoneCenter = new GameObject("SafeZoneCenter").transform;
            }
            
            // Position safe zone center randomly within map bounds
            Vector3 randomCenter = GetRandomSafeZoneCenter(mapData);
            safeZoneCenter.position = randomCenter;
            
            // Initialize weather for the map
            if (enableDynamicWeather && mapData.weatherTypes.Count > 0)
            {
                ChangeWeather(mapData.weatherTypes[0]);
            }
        }
        
        Vector3 GetRandomSafeZoneCenter(BrazilianMapData mapData)
        {
            // Different center logic based on cultural theme
            switch (mapData.culturalTheme)
            {
                case CulturalTheme.Favela:
                    return new Vector3(
                        UnityEngine.Random.Range(-200f, 200f),
                        50f,
                        UnityEngine.Random.Range(-200f, 200f)
                    );
                
                case CulturalTheme.Amazon:
                    return new Vector3(
                        UnityEngine.Random.Range(-300f, 300f),
                        10f,
                        UnityEngine.Random.Range(-300f, 300f)
                    );
                
                case CulturalTheme.Metropolis:
                    return new Vector3(
                        UnityEngine.Random.Range(-250f, 250f),
                        0f,
                        UnityEngine.Random.Range(-250f, 250f)
                    );
                
                default:
                    return Vector3.zero;
            }
        }
        
        public void StartSafeZone()
        {
            Debug.Log("Arena Brasil - Starting safe zone phases");
            
            safeZoneActive = true;
            currentSafeZonePhase = 0;
            phaseTimer = timeBetweenPhases;
            
            OnSafeZoneUpdated?.Invoke(safeZoneCenter.position, initialSafeZoneRadius);
        }
        
        void UpdateSafeZone()
        {
            phaseTimer -= Time.deltaTime;
            
            if (phaseTimer <= 0f && currentSafeZonePhase < totalSafeZonePhases)
            {
                AdvanceSafeZonePhase();
            }
        }
        
        void AdvanceSafeZonePhase()
        {
            currentSafeZonePhase++;
            phaseTimer = timeBetweenPhases;
            
            // Calculate new safe zone radius
            float progress = (float)currentSafeZonePhase / totalSafeZonePhases;
            float currentRadius = Mathf.Lerp(initialSafeZoneRadius, finalSafeZoneRadius, progress);
            
            Debug.Log($"Safe Zone Phase {currentSafeZonePhase}: Radius {currentRadius}m");
            
            OnSafeZonePhaseChanged?.Invoke(currentSafeZonePhase, currentRadius);
            OnSafeZoneUpdated?.Invoke(safeZoneCenter.position, currentRadius);
            
            // Final phase - very small safe zone
            if (currentSafeZonePhase >= totalSafeZonePhases)
            {
                safeZoneActive = false;
                Debug.Log("Safe zone final phase reached!");
            }
        }
        
        public void ChangeWeather(WeatherType newWeather)
        {
            Debug.Log($"Arena Brasil - Weather changed to: {newWeather}");
            OnWeatherChanged?.Invoke(newWeather);
            
            // Apply weather effects based on type
            ApplyWeatherEffects(newWeather);
        }
        
        void ApplyWeatherEffects(WeatherType weather)
        {
            switch (weather)
            {
                case WeatherType.Rain:
                    // Reduce visibility, add rain particles
                    RenderSettings.fogDensity = 0.02f;
                    break;
                
                case WeatherType.Fog:
                    // Heavy fog reduces long-range visibility
                    RenderSettings.fogDensity = 0.05f;
                    break;
                
                case WeatherType.Storm:
                    // Dramatic weather with lightning
                    RenderSettings.fogDensity = 0.03f;
                    break;
                
                case WeatherType.Sunny:
                    // Clear visibility
                    RenderSettings.fogDensity = 0.001f;
                    break;
                
                default:
                    RenderSettings.fogDensity = 0.01f;
                    break;
            }
        }
        
        public float GetDistanceToSafeZone(Vector3 position)
        {
            if (safeZoneCenter == null) return 0f;
            
            float distance = Vector3.Distance(position, safeZoneCenter.position);
            float currentRadius = GetCurrentSafeZoneRadius();
            
            return Mathf.Max(0f, distance - currentRadius);
        }
        
        public float GetCurrentSafeZoneRadius()
        {
            if (!safeZoneActive) return initialSafeZoneRadius;
            
            float progress = (float)currentSafeZonePhase / totalSafeZonePhases;
            return Mathf.Lerp(initialSafeZoneRadius, finalSafeZoneRadius, progress);
        }
        
        public bool IsPositionInSafeZone(Vector3 position)
        {
            return GetDistanceToSafeZone(position) <= 0f;
        }
        
        public BrazilianMapData GetMapByName(string mapName)
        {
            return availableMaps.Find(map => map.mapName == mapName);
        }
        
        public List<BrazilianMapData> GetMapsByTheme(CulturalTheme theme)
        {
            return availableMaps.FindAll(map => map.culturalTheme == theme);
        }
    }
    
    [System.Serializable]
    public class BrazilianMapData
    {
        public string mapName;
        public string sceneName;
        public CulturalTheme culturalTheme;
        public string description;
        public int maxPlayers = 60;
        public List<WeatherType> weatherTypes = new List<WeatherType>();
        public Sprite mapPreview;
        public AudioClip ambientMusic;
    }
    
    public enum CulturalTheme
    {
        Favela,      // Rio de Janeiro favelas
        Amazon,      // Amazon rainforest
        Metropolis,  // São Paulo/Rio metropolis
        Sertao,      // Northeast semi-arid region
        Pantanal,    // Pantanal wetlands
        Coast,       // Brazilian coast/beaches
        Historic     // Historic cities like Ouro Preto
    }
    
    public enum WeatherType
    {
        Sunny,
        Cloudy,
        Rain,
        Storm,
        Fog,
        Hot,
        Humid,
        Drought,
        Smog
    }
}
