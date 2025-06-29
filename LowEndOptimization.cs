
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ArenaBrasil.Optimization
{
    public class LowEndOptimization : MonoBehaviour
    {
        public static LowEndOptimization Instance { get; private set; }
        
        [Header("Device Detection")]
        public DevicePerformanceLevel currentPerformanceLevel;
        public bool autoDetectPerformance = true;
        
        [Header("Performance Presets")]
        public PerformancePreset lowEndPreset;
        public PerformancePreset midRangePreset;
        public PerformancePreset highEndPreset;
        
        [Header("Dynamic Quality")]
        public bool enableDynamicQuality = true;
        public float targetFrameRate = 30f;
        public float frameRateThreshold = 5f;
        
        [Header("Brazilian Optimization")]
        public bool optimizeForBrazilianNetworks = true;
        public int maxNetworkUpdatesPerSecond = 20;
        
        private float currentFPS;
        private Queue<float> fpsHistory = new Queue<float>();
        private int fpsHistorySize = 60; // 2 seconds at 30fps
        private bool isOptimizationActive = false;
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeOptimization();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void Start()
        {
            if (autoDetectPerformance)
            {
                DetectDevicePerformance();
            }
            
            ApplyPerformancePreset();
            StartDynamicOptimization();
        }
        
        void Update()
        {
            if (enableDynamicQuality)
            {
                MonitorPerformance();
                AdjustQualityDynamically();
            }
        }
        
        void InitializeOptimization()
        {
            Debug.Log("Arena Brasil - Inicializando otimização para dispositivos");
            
            SetupPerformancePresets();
            InitializeBrazilianOptimizations();
        }
        
        void SetupPerformancePresets()
        {
            // Low-end preset (devices with < 3GB RAM, weak GPU)
            lowEndPreset = new PerformancePreset
            {
                renderScale = 0.7f,
                shadowQuality = ShadowQuality.Disable,
                shadowDistance = 50f,
                textureQuality = 3, // Quarter resolution
                antiAliasing = 0,
                postProcessing = false,
                particleMaxCount = 100,
                maxPlayers = 40, // Reduced from 60
                audioChannels = 16,
                targetFrameRate = 30,
                vsync = false,
                lodBias = 0.5f,
                maxLODLevel = 2
            };
            
            // Mid-range preset (3-6GB RAM, decent GPU)
            midRangePreset = new PerformancePreset
            {
                renderScale = 0.85f,
                shadowQuality = ShadowQuality.HardOnly,
                shadowDistance = 100f,
                textureQuality = 2, // Half resolution
                antiAliasing = 2,
                postProcessing = true,
                particleMaxCount = 250,
                maxPlayers = 50,
                audioChannels = 24,
                targetFrameRate = 45,
                vsync = false,
                lodBias = 0.7f,
                maxLODLevel = 1
            };
            
            // High-end preset (6GB+ RAM, flagship GPU)
            highEndPreset = new PerformancePreset
            {
                renderScale = 1.0f,
                shadowQuality = ShadowQuality.All,
                shadowDistance = 150f,
                textureQuality = 0, // Full resolution
                antiAliasing = 4,
                postProcessing = true,
                particleMaxCount = 500,
                maxPlayers = 60,
                audioChannels = 32,
                targetFrameRate = 60,
                vsync = true,
                lodBias = 1.0f,
                maxLODLevel = 0
            };
        }
        
        void DetectDevicePerformance()
        {
            int memorySize = SystemInfo.systemMemorySize;
            string gpuName = SystemInfo.graphicsDeviceName.ToLower();
            int cpuCores = SystemInfo.processorCount;
            
            Debug.Log($"Dispositivo detectado - RAM: {memorySize}MB, GPU: {gpuName}, CPU Cores: {cpuCores}");
            
            // Brazilian popular low-end devices detection
            if (IsLowEndDevice(memorySize, gpuName, cpuCores))
            {
                currentPerformanceLevel = DevicePerformanceLevel.LowEnd;
                Debug.Log("Dispositivo classificado como: Low-End (otimizações ativadas)");
            }
            else if (IsMidRangeDevice(memorySize, gpuName))
            {
                currentPerformanceLevel = DevicePerformanceLevel.MidRange;
                Debug.Log("Dispositivo classificado como: Mid-Range");
            }
            else
            {
                currentPerformanceLevel = DevicePerformanceLevel.HighEnd;
                Debug.Log("Dispositivo classificado como: High-End");
            }
        }
        
        bool IsLowEndDevice(int memorySize, string gpuName, int cpuCores)
        {
            // Common low-end devices in Brazil
            string[] lowEndGPUs = {
                "adreno 506", "adreno 505", "adreno 504",
                "mali-g51", "mali-g52", "mali-t830",
                "powervr ge8100", "powervr ge8300"
            };
            
            bool hasLowEndGPU = false;
            foreach (var lowGpu in lowEndGPUs)
            {
                if (gpuName.Contains(lowGpu))
                {
                    hasLowEndGPU = true;
                    break;
                }
            }
            
            return memorySize <= 3072 || hasLowEndGPU || cpuCores <= 4;
        }
        
        bool IsMidRangeDevice(int memorySize, string gpuName)
        {
            return memorySize > 3072 && memorySize <= 6144;
        }
        
        void ApplyPerformancePreset()
        {
            PerformancePreset preset = currentPerformanceLevel switch
            {
                DevicePerformanceLevel.LowEnd => lowEndPreset,
                DevicePerformanceLevel.MidRange => midRangePreset,
                DevicePerformanceLevel.HighEnd => highEndPreset,
                _ => midRangePreset
            };
            
            ApplyGraphicsSettings(preset);
            ApplyAudioSettings(preset);
            ApplyGameplaySettings(preset);
            
            Debug.Log($"Preset aplicado: {currentPerformanceLevel}");
        }
        
        void ApplyGraphicsSettings(PerformancePreset preset)
        {
            // Render Pipeline settings
            var urpAsset = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
            if (urpAsset != null)
            {
                urpAsset.renderScale = preset.renderScale;
                urpAsset.shadowDistance = preset.shadowDistance;
                urpAsset.supportsSoftShadows = preset.shadowQuality != ShadowQuality.Disable;
            }
            
            // Quality settings
            QualitySettings.shadows = preset.shadowQuality;
            QualitySettings.masterTextureLimit = preset.textureQuality;
            QualitySettings.antiAliasing = preset.antiAliasing;
            QualitySettings.lodBias = preset.lodBias;
            QualitySettings.maximumLODLevel = preset.maxLODLevel;
            
            // Frame rate
            Application.targetFrameRate = preset.targetFrameRate;
            QualitySettings.vSyncCount = preset.vsync ? 1 : 0;
            
            // Particles
            var particleSystems = FindObjectsOfType<ParticleSystem>();
            foreach (var ps in particleSystems)
            {
                var main = ps.main;
                main.maxParticles = Mathf.Min(main.maxParticles, preset.particleMaxCount);
            }
        }
        
        void ApplyAudioSettings(PerformancePreset preset)
        {
            AudioSettings.GetConfiguration(out var config);
            config.numVirtualVoices = preset.audioChannels;
            AudioSettings.Reset(config);
        }
        
        void ApplyGameplaySettings(PerformancePreset preset)
        {
            // Adjust max players for low-end devices
            if (ArenaBrasilGameManager.Instance != null)
            {
                // Reduce player count for better performance
                Debug.Log($"Ajustando máximo de jogadores para: {preset.maxPlayers}");
            }
            
            // Network optimization
            if (optimizeForBrazilianNetworks)
            {
                OptimizeForBrazilianNetworks(preset);
            }
        }
        
        void OptimizeForBrazilianNetworks(PerformancePreset preset)
        {
            // Brazilian internet can be unstable, optimize accordingly
            if (NetworkManager.Singleton != null)
            {
                var netConfig = NetworkManager.Singleton.NetworkConfig;
                netConfig.TickRate = Math.Min(netConfig.TickRate, maxNetworkUpdatesPerSecond);
            }
            
            // Reduce network update frequency for low-end devices
            if (currentPerformanceLevel == DevicePerformanceLevel.LowEnd)
            {
                Time.fixedDeltaTime = 1f / 20f; // 20 physics updates per second instead of 50
            }
        }
        
        void StartDynamicOptimization()
        {
            if (enableDynamicQuality)
            {
                InvokeRepeating(nameof(PerformanceCheck), 1f, 1f);
            }
        }
        
        void MonitorPerformance()
        {
            currentFPS = 1f / Time.deltaTime;
            
            fpsHistory.Enqueue(currentFPS);
            if (fpsHistory.Count > fpsHistorySize)
            {
                fpsHistory.Dequeue();
            }
        }
        
        void AdjustQualityDynamically()
        {
            if (fpsHistory.Count < fpsHistorySize) return;
            
            float averageFPS = 0f;
            foreach (float fps in fpsHistory)
            {
                averageFPS += fps;
            }
            averageFPS /= fpsHistory.Count;
            
            if (averageFPS < targetFrameRate - frameRateThreshold)
            {
                // Performance is poor, reduce quality
                ReduceQuality();
            }
            else if (averageFPS > targetFrameRate + frameRateThreshold)
            {
                // Performance is good, try to increase quality
                IncreaseQuality();
            }
        }
        
        void ReduceQuality()
        {
            if (isOptimizationActive) return;
            
            isOptimizationActive = true;
            
            // Reduce render scale
            var urpAsset = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
            if (urpAsset != null && urpAsset.renderScale > 0.5f)
            {
                urpAsset.renderScale = Mathf.Max(0.5f, urpAsset.renderScale - 0.1f);
                Debug.Log($"Qualidade reduzida - Render Scale: {urpAsset.renderScale:F1}");
            }
            
            // Reduce particles
            ReduceParticleEffects();
            
            Invoke(nameof(ResetOptimizationFlag), 2f);
        }
        
        void IncreaseQuality()
        {
            if (isOptimizationActive) return;
            
            isOptimizationActive = true;
            
            // Increase render scale (but don't exceed preset maximum)
            var urpAsset = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
            if (urpAsset != null)
            {
                float maxRenderScale = GetCurrentPreset().renderScale;
                if (urpAsset.renderScale < maxRenderScale)
                {
                    urpAsset.renderScale = Mathf.Min(maxRenderScale, urpAsset.renderScale + 0.05f);
                    Debug.Log($"Qualidade aumentada - Render Scale: {urpAsset.renderScale:F1}");
                }
            }
            
            Invoke(nameof(ResetOptimizationFlag), 2f);
        }
        
        void ReduceParticleEffects()
        {
            var particleSystems = FindObjectsOfType<ParticleSystem>();
            foreach (var ps in particleSystems)
            {
                var main = ps.main;
                main.maxParticles = Mathf.Max(10, main.maxParticles / 2);
            }
        }
        
        void ResetOptimizationFlag()
        {
            isOptimizationActive = false;
        }
        
        void PerformanceCheck()
        {
            float memoryUsage = Profiler.GetTotalAllocatedMemory(Profiler.GetTotalAllocatedMemory(0)) / (1024f * 1024f);
            
            if (memoryUsage > 1024f) // More than 1GB
            {
                System.GC.Collect();
                Resources.UnloadUnusedAssets();
                Debug.Log("Limpeza de memória executada");
            }
        }
        
        PerformancePreset GetCurrentPreset()
        {
            return currentPerformanceLevel switch
            {
                DevicePerformanceLevel.LowEnd => lowEndPreset,
                DevicePerformanceLevel.MidRange => midRangePreset,
                DevicePerformanceLevel.HighEnd => highEndPreset,
                _ => midRangePreset
            };
        }
        
        public void SetPerformanceLevel(DevicePerformanceLevel level)
        {
            currentPerformanceLevel = level;
            ApplyPerformancePreset();
        }
        
        public float GetCurrentFPS() => currentFPS;
        public DevicePerformanceLevel GetPerformanceLevel() => currentPerformanceLevel;
        
        void InitializeBrazilianOptimizations()
        {
            // Specific optimizations for Brazilian market
            
            // Reduce texture streaming for slower connections
            QualitySettings.streamingMipmapsActive = true;
            QualitySettings.streamingMipmapsMemoryBudget = currentPerformanceLevel == DevicePerformanceLevel.LowEnd ? 256 : 512;
            
            // Optimize for common Brazilian device aspect ratios
            OptimizeForCommonAspectRatios();
        }
        
        void OptimizeForCommonAspectRatios()
        {
            float aspectRatio = (float)Screen.width / Screen.height;
            
            // Common ratios in Brazilian market: 16:9, 19:9, 20:9
            if (aspectRatio > 2.0f) // Ultra-wide displays
            {
                // Adjust UI scaling for very wide screens
                Canvas.ForceUpdateCanvases();
            }
        }
    }
    
    [Serializable]
    public class PerformancePreset
    {
        public float renderScale;
        public ShadowQuality shadowQuality;
        public float shadowDistance;
        public int textureQuality;
        public int antiAliasing;
        public bool postProcessing;
        public int particleMaxCount;
        public int maxPlayers;
        public int audioChannels;
        public int targetFrameRate;
        public bool vsync;
        public float lodBias;
        public int maxLODLevel;
    }
    
    public enum DevicePerformanceLevel
    {
        LowEnd,
        MidRange,
        HighEnd
    }
}
