
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Collections;

namespace ArenaBrasil.Environment
{
    public class WeatherSystem : NetworkBehaviour
    {
        public static WeatherSystem Instance { get; private set; }
        
        [Header("Weather Configuration")]
        public bool enableDynamicWeather = true;
        public float weatherChangeInterval = 300f; // 5 minutes
        public float transitionDuration = 30f; // 30 seconds
        
        [Header("Visual Effects")]
        public ParticleSystem rainParticles;
        public ParticleSystem stormParticles;
        public ParticleSystem fogParticles;
        public ParticleSystem sandstormParticles;
        
        [Header("Audio Effects")]
        public AudioSource weatherAudioSource;
        public AudioClip rainSound;
        public AudioClip stormSound;
        public AudioClip windSound;
        
        [Header("Lighting")]
        public Light sunLight;
        public Gradient sunnyColor;
        public Gradient cloudyColor;
        public Gradient stormColor;
        
        // Current weather state
        private WeatherType currentWeather = WeatherType.Sunny;
        private WeatherType targetWeather = WeatherType.Sunny;
        private float weatherTransitionTime = 0f;
        private bool isTransitioning = false;
        
        // Regional weather patterns
        private Dictionary<CulturalTheme, List<WeatherPattern>> regionalWeatherPatterns;
        
        // Events
        public event System.Action<WeatherType, WeatherType> OnWeatherChanged;
        public event System.Action<WeatherEffect> OnWeatherEffect;
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeWeatherSystem();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void InitializeWeatherSystem()
        {
            Debug.Log("Arena Brasil - Initializing Weather System");
            SetupRegionalWeatherPatterns();
            
            if (enableDynamicWeather)
            {
                StartCoroutine(WeatherCycle());
            }
        }
        
        void SetupRegionalWeatherPatterns()
        {
            regionalWeatherPatterns = new Dictionary<CulturalTheme, List<WeatherPattern>>
            {
                {
                    CulturalTheme.Amazon,
                    new List<WeatherPattern>
                    {
                        new WeatherPattern { weather = WeatherType.Humid, probability = 0.4f, duration = 600f },
                        new WeatherPattern { weather = WeatherType.Rain, probability = 0.3f, duration = 180f },
                        new WeatherPattern { weather = WeatherType.Fog, probability = 0.2f, duration = 300f },
                        new WeatherPattern { weather = WeatherType.Storm, probability = 0.1f, duration = 120f }
                    }
                },
                {
                    CulturalTheme.Sertao,
                    new List<WeatherPattern>
                    {
                        new WeatherPattern { weather = WeatherType.Hot, probability = 0.5f, duration = 900f },
                        new WeatherPattern { weather = WeatherType.Drought, probability = 0.3f, duration = 1200f },
                        new WeatherPattern { weather = WeatherType.Sunny, probability = 0.15f, duration = 600f },
                        new WeatherPattern { weather = WeatherType.Sandstorm, probability = 0.05f, duration = 150f }
                    }
                },
                {
                    CulturalTheme.Coast,
                    new List<WeatherPattern>
                    {
                        new WeatherPattern { weather = WeatherType.Sunny, probability = 0.4f, duration = 800f },
                        new WeatherPattern { weather = WeatherType.Cloudy, probability = 0.25f, duration = 400f },
                        new WeatherPattern { weather = WeatherType.Rain, probability = 0.2f, duration = 200f },
                        new WeatherPattern { weather = WeatherType.Storm, probability = 0.15f, duration = 180f }
                    }
                },
                {
                    CulturalTheme.Pantanal,
                    new List<WeatherPattern>
                    {
                        new WeatherPattern { weather = WeatherType.Humid, probability = 0.35f, duration = 700f },
                        new WeatherPattern { weather = WeatherType.Rain, probability = 0.3f, duration = 250f },
                        new WeatherPattern { weather = WeatherType.Fog, probability = 0.25f, duration = 400f },
                        new WeatherPattern { weather = WeatherType.Sunny, probability = 0.1f, duration = 600f }
                    }
                },
                {
                    CulturalTheme.Metropolis,
                    new List<WeatherPattern>
                    {
                        new WeatherPattern { weather = WeatherType.Sunny, probability = 0.3f, duration = 600f },
                        new WeatherPattern { weather = WeatherType.Cloudy, probability = 0.3f, duration = 500f },
                        new WeatherPattern { weather = WeatherType.Smog, probability = 0.25f, duration = 400f },
                        new WeatherPattern { weather = WeatherType.Rain, probability = 0.15f, duration = 200f }
                    }
                }
            };
        }
        
        IEnumerator WeatherCycle()
        {
            while (true)
            {
                yield return new WaitForSeconds(weatherChangeInterval);
                
                if (!isTransitioning)
                {
                    ChangeWeatherRandomly();
                }
            }
        }
        
        void ChangeWeatherRandomly()
        {
            if (!IsServer) return;
            
            WeatherType newWeather = SelectRandomWeather();
            
            if (newWeather != currentWeather)
            {
                ChangeWeather(newWeather);
            }
        }
        
        WeatherType SelectRandomWeather()
        {
            // Get current map theme
            var mapManager = MapManager.Instance;
            if (mapManager?.currentMap?.culturalTheme == null)
            {
                return GetRandomWeatherType();
            }
            
            var theme = mapManager.currentMap.culturalTheme;
            
            if (regionalWeatherPatterns.ContainsKey(theme))
            {
                var patterns = regionalWeatherPatterns[theme];
                float totalProbability = 0f;
                
                foreach (var pattern in patterns)
                {
                    totalProbability += pattern.probability;
                }
                
                float randomValue = Random.Range(0f, totalProbability);
                float currentProbability = 0f;
                
                foreach (var pattern in patterns)
                {
                    currentProbability += pattern.probability;
                    if (randomValue <= currentProbability)
                    {
                        return pattern.weather;
                    }
                }
            }
            
            return GetRandomWeatherType();
        }
        
        WeatherType GetRandomWeatherType()
        {
            var allWeatherTypes = System.Enum.GetValues(typeof(WeatherType));
            return (WeatherType)allWeatherTypes.GetValue(Random.Range(0, allWeatherTypes.Length));
        }
        
        public void ChangeWeather(WeatherType newWeather)
        {
            if (!IsServer) return;
            
            if (newWeather == currentWeather) return;
            
            WeatherType oldWeather = currentWeather;
            targetWeather = newWeather;
            
            StartCoroutine(TransitionWeather(oldWeather, newWeather));
            
            OnWeatherChanged?.Invoke(oldWeather, newWeather);
            WeatherChangedClientRpc(oldWeather, newWeather);
            
            Debug.Log($"Weather changing from {oldWeather} to {newWeather}");
        }
        
        IEnumerator TransitionWeather(WeatherType from, WeatherType to)
        {
            isTransitioning = true;
            weatherTransitionTime = 0f;
            
            while (weatherTransitionTime < transitionDuration)
            {
                weatherTransitionTime += Time.deltaTime;
                float progress = weatherTransitionTime / transitionDuration;
                
                ApplyWeatherTransition(from, to, progress);
                
                yield return null;
            }
            
            currentWeather = to;
            ApplyWeatherEffects(to, 1f);
            isTransitioning = false;
            
            // Apply gameplay effects
            ApplyGameplayEffects(to);
        }
        
        void ApplyWeatherTransition(WeatherType from, WeatherType to, float progress)
        {
            // Interpolate lighting
            Color fromLightColor = GetWeatherLightColor(from);
            Color toLightColor = GetWeatherLightColor(to);
            
            if (sunLight != null)
            {
                sunLight.color = Color.Lerp(fromLightColor, toLightColor, progress);
                sunLight.intensity = Mathf.Lerp(GetWeatherLightIntensity(from), GetWeatherLightIntensity(to), progress);
            }
            
            // Interpolate fog
            float fromFogDensity = GetWeatherFogDensity(from);
            float toFogDensity = GetWeatherFogDensity(to);
            RenderSettings.fogDensity = Mathf.Lerp(fromFogDensity, toFogDensity, progress);
            
            // Transition particle effects
            TransitionParticleEffects(from, to, progress);
        }
        
        void ApplyWeatherEffects(WeatherType weather, float intensity = 1f)
        {
            // Stop all weather particles first
            StopAllWeatherParticles();
            
            // Apply specific weather effects
            switch (weather)
            {
                case WeatherType.Rain:
                    if (rainParticles != null)
                    {
                        rainParticles.gameObject.SetActive(true);
                        var main = rainParticles.main;
                        main.maxParticles = Mathf.RoundToInt(1000 * intensity);
                    }
                    PlayWeatherSound(rainSound);
                    break;
                    
                case WeatherType.Storm:
                    if (stormParticles != null)
                    {
                        stormParticles.gameObject.SetActive(true);
                        var main = stormParticles.main;
                        main.maxParticles = Mathf.RoundToInt(1500 * intensity);
                    }
                    PlayWeatherSound(stormSound);
                    StartCoroutine(LightningEffect());
                    break;
                    
                case WeatherType.Fog:
                    if (fogParticles != null)
                    {
                        fogParticles.gameObject.SetActive(true);
                    }
                    break;
                    
                case WeatherType.Sandstorm:
                    if (sandstormParticles != null)
                    {
                        sandstormParticles.gameObject.SetActive(true);
                        var main = sandstormParticles.main;
                        main.maxParticles = Mathf.RoundToInt(2000 * intensity);
                    }
                    PlayWeatherSound(windSound);
                    break;
                    
                default:
                    StopWeatherSound();
                    break;
            }
            
            // Update environment lighting
            if (sunLight != null)
            {
                sunLight.color = GetWeatherLightColor(weather);
                sunLight.intensity = GetWeatherLightIntensity(weather);
            }
            
            // Update fog
            RenderSettings.fogDensity = GetWeatherFogDensity(weather);
            RenderSettings.fogColor = GetWeatherFogColor(weather);
        }
        
        void ApplyGameplayEffects(WeatherType weather)
        {
            var effect = new WeatherEffect
            {
                weatherType = weather,
                visibilityMultiplier = GetVisibilityMultiplier(weather),
                movementSpeedMultiplier = GetMovementSpeedMultiplier(weather),
                weaponAccuracyMultiplier = GetWeaponAccuracyMultiplier(weather),
                damageMultiplier = GetDamageMultiplier(weather)
            };
            
            OnWeatherEffect?.Invoke(effect);
            WeatherEffectClientRpc(effect);
        }
        
        float GetVisibilityMultiplier(WeatherType weather)
        {
            switch (weather)
            {
                case WeatherType.Fog: return 0.3f;
                case WeatherType.Storm: return 0.5f;
                case WeatherType.Rain: return 0.7f;
                case WeatherType.Sandstorm: return 0.2f;
                case WeatherType.Smog: return 0.6f;
                default: return 1f;
            }
        }
        
        float GetMovementSpeedMultiplier(WeatherType weather)
        {
            switch (weather)
            {
                case WeatherType.Storm: return 0.9f;
                case WeatherType.Sandstorm: return 0.8f;
                case WeatherType.Humid: return 0.95f;
                default: return 1f;
            }
        }
        
        float GetWeaponAccuracyMultiplier(WeatherType weather)
        {
            switch (weather)
            {
                case WeatherType.Storm: return 0.85f;
                case WeatherType.Rain: return 0.9f;
                case WeatherType.Sandstorm: return 0.7f;
                default: return 1f;
            }
        }
        
        float GetDamageMultiplier(WeatherType weather)
        {
            switch (weather)
            {
                case WeatherType.Storm: return 1.1f; // Lightning empowers attacks
                default: return 1f;
            }
        }
        
        Color GetWeatherLightColor(WeatherType weather)
        {
            switch (weather)
            {
                case WeatherType.Sunny: return Color.white;
                case WeatherType.Cloudy: return new Color(0.8f, 0.8f, 0.9f);
                case WeatherType.Rain: return new Color(0.6f, 0.6f, 0.8f);
                case WeatherType.Storm: return new Color(0.4f, 0.4f, 0.6f);
                case WeatherType.Fog: return new Color(0.7f, 0.7f, 0.7f);
                case WeatherType.Hot: return new Color(1f, 0.9f, 0.7f);
                case WeatherType.Sandstorm: return new Color(0.8f, 0.7f, 0.5f);
                case WeatherType.Smog: return new Color(0.7f, 0.7f, 0.6f);
                default: return Color.white;
            }
        }
        
        float GetWeatherLightIntensity(WeatherType weather)
        {
            switch (weather)
            {
                case WeatherType.Sunny: return 1.2f;
                case WeatherType.Cloudy: return 0.8f;
                case WeatherType.Rain: return 0.6f;
                case WeatherType.Storm: return 0.4f;
                case WeatherType.Fog: return 0.5f;
                case WeatherType.Hot: return 1.4f;
                case WeatherType.Sandstorm: return 0.3f;
                case WeatherType.Smog: return 0.7f;
                default: return 1f;
            }
        }
        
        float GetWeatherFogDensity(WeatherType weather)
        {
            switch (weather)
            {
                case WeatherType.Fog: return 0.05f;
                case WeatherType.Storm: return 0.03f;
                case WeatherType.Rain: return 0.02f;
                case WeatherType.Sandstorm: return 0.08f;
                case WeatherType.Smog: return 0.04f;
                default: return 0.001f;
            }
        }
        
        Color GetWeatherFogColor(WeatherType weather)
        {
            switch (weather)
            {
                case WeatherType.Fog: return Color.gray;
                case WeatherType.Storm: return new Color(0.3f, 0.3f, 0.4f);
                case WeatherType.Sandstorm: return new Color(0.8f, 0.7f, 0.5f);
                case WeatherType.Smog: return new Color(0.6f, 0.6f, 0.5f);
                default: return RenderSettings.fogColor;
            }
        }
        
        void TransitionParticleEffects(WeatherType from, WeatherType to, float progress)
        {
            // Fade out old weather particles
            FadeWeatherParticles(from, 1f - progress);
            
            // Fade in new weather particles
            FadeWeatherParticles(to, progress);
        }
        
        void FadeWeatherParticles(WeatherType weather, float alpha)
        {
            ParticleSystem particles = GetWeatherParticles(weather);
            if (particles != null)
            {
                var main = particles.main;
                var startColor = main.startColor;
                Color color = startColor.color;
                color.a = alpha;
                main.startColor = color;
                
                if (alpha > 0 && !particles.gameObject.activeInHierarchy)
                {
                    particles.gameObject.SetActive(true);
                }
                else if (alpha <= 0 && particles.gameObject.activeInHierarchy)
                {
                    particles.gameObject.SetActive(false);
                }
            }
        }
        
        ParticleSystem GetWeatherParticles(WeatherType weather)
        {
            switch (weather)
            {
                case WeatherType.Rain: return rainParticles;
                case WeatherType.Storm: return stormParticles;
                case WeatherType.Fog: return fogParticles;
                case WeatherType.Sandstorm: return sandstormParticles;
                default: return null;
            }
        }
        
        void StopAllWeatherParticles()
        {
            if (rainParticles != null) rainParticles.gameObject.SetActive(false);
            if (stormParticles != null) stormParticles.gameObject.SetActive(false);
            if (fogParticles != null) fogParticles.gameObject.SetActive(false);
            if (sandstormParticles != null) sandstormParticles.gameObject.SetActive(false);
        }
        
        void PlayWeatherSound(AudioClip clip)
        {
            if (weatherAudioSource != null && clip != null)
            {
                weatherAudioSource.clip = clip;
                weatherAudioSource.loop = true;
                weatherAudioSource.Play();
            }
        }
        
        void StopWeatherSound()
        {
            if (weatherAudioSource != null)
            {
                weatherAudioSource.Stop();
            }
        }
        
        IEnumerator LightningEffect()
        {
            while (currentWeather == WeatherType.Storm)
            {
                yield return new WaitForSeconds(Random.Range(5f, 15f));
                
                if (sunLight != null)
                {
                    float originalIntensity = sunLight.intensity;
                    sunLight.intensity = 2f;
                    yield return new WaitForSeconds(0.1f);
                    sunLight.intensity = originalIntensity;
                }
            }
        }
        
        // Client RPCs
        [ClientRpc]
        void WeatherChangedClientRpc(WeatherType from, WeatherType to)
        {
            Debug.Log($"🌤️ Clima mudando: {from} → {to}");
            
            // This would update UI weather indicator
            // UIManager.Instance?.UpdateWeatherIndicator(to);
        }
        
        [ClientRpc]
        void WeatherEffectClientRpc(WeatherEffect effect)
        {
            Debug.Log($"🌩️ Efeito climático ativo: {effect.weatherType}");
            
            // Apply local effects to player
            ApplyLocalWeatherEffects(effect);
        }
        
        void ApplyLocalWeatherEffects(WeatherEffect effect)
        {
            // This would affect local player's movement, visibility, etc.
            // Could integrate with PlayerController and other systems
        }
        
        // Public getters
        public WeatherType GetCurrentWeather() => currentWeather;
        public bool IsWeatherTransitioning() => isTransitioning;
        public float GetWeatherTransitionProgress() => weatherTransitionTime / transitionDuration;
    }
    
    [System.Serializable]
    public class WeatherPattern
    {
        public WeatherType weather;
        public float probability;
        public float duration;
    }
    
    [System.Serializable]
    public class WeatherEffect
    {
        public WeatherType weatherType;
        public float visibilityMultiplier = 1f;
        public float movementSpeedMultiplier = 1f;
        public float weaponAccuracyMultiplier = 1f;
        public float damageMultiplier = 1f;
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
        Smog,
        Sandstorm
    }
}
