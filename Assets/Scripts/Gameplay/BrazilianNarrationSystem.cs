
using UnityEngine;
using System.Collections.Generic;
using System;
using ArenaBrasil.Core;

namespace ArenaBrasil.Audio
{
    public class BrazilianNarrationSystem : MonoBehaviour
    {
        public static BrazilianNarrationSystem Instance { get; private set; }
        
        [Header("Narration Configuration")]
        public bool enableRegionalAccents = true;
        public BrazilianRegion currentRegion = BrazilianRegion.Sudeste;
        public float narratorVolume = 0.8f;
        public bool enableDynamicNarration = true;
        
        [Header("Regional Narrators")]
        public RegionalNarrator[] regionalNarrators;
        
        private AudioSource narratorAudioSource;
        private Dictionary<string, List<AudioClip>> narratorClips = new Dictionary<string, List<AudioClip>>();
        private Dictionary<BrazilianRegion, RegionalNarrator> narratorMap = new Dictionary<BrazilianRegion, RegionalNarrator>();
        
        // Events
        public event Action<string, BrazilianRegion> OnNarrationPlayed;
        
        [System.Serializable]
        public class RegionalNarrator
        {
            public BrazilianRegion region;
            public string narratorName;
            public NarrationClips clips;
            public RegionalPhrases phrases;
            public float accentStrength = 1.0f;
            public bool isAvailable = true;
        }
        
        [System.Serializable]
        public class NarrationClips
        {
            [Header("Game Events")]
            public AudioClip[] matchStart;
            public AudioClip[] playerElimination;
            public AudioClip[] lastPlayersAlive;
            public AudioClip[] victory;
            public AudioClip[] defeat;
            
            [Header("Combat")]
            public AudioClip[] firstBlood;
            public AudioClip[] killStreak;
            public AudioClip[] headshot;
            public AudioClip[] assist;
            
            [Header("Zone")]
            public AudioClip[] zoneWarning;
            public AudioClip[] zoneClosing;
            public AudioClip[] zoneDamage;
            
            [Header("Items")]
            public AudioClip[] rareItemFound;
            public AudioClip[] weaponUpgrade;
            public AudioClip[] healing;
            
            [Header("Brazilian Specific")]
            public AudioClip[] carnivalMode;
            public AudioClip[] soccerGoal;
            public AudioClip[] capoeiraMoves;
            public AudioClip[] festasJuninas;
        }
        
        [System.Serializable]
        public class RegionalPhrases
        {
            [Header("Nordeste")]
            public string[] nordesteGreetings = {
                "Oxente, vai começar a briga!",
                "Vixe, tá pegando fogo!",
                "Eita lasca, que pancadaria!",
                "Rapaz, tá doido!",
                "Arretado demais!"
            };
            
            [Header("Sul")]
            public string[] sulGreetings = {
                "Bah, tchê, vamo que vamo!",
                "Capaz que dê bom!",
                "Tri legal essa batalha!",
                "Barbaridade, que luta!",
                "Massa demais, guri!"
            };
            
            [Header("Sudeste")]
            public string[] sudesteGreetings = {
                "Vai que é sua, mermão!",
                "Tá ligado, véi!",
                "Massa, cara!",
                "Mandou bem!",
                "Show de bola!"
            };
            
            [Header("Norte")]
            public string[] norteGreetings = {
                "Rapá, que coisa boa!",
                "Valeu, mano!",
                "Tá maneiro!",
                "Que legal, brother!",
                "Firmeza total!"
            };
            
            [Header("Centro-Oeste")]
            public string[] centroOesteGreetings = {
                "Sô, tá bom demais!",
                "Uai, que massa!",
                "Trem bão!",
                "Oxe, que legal!",
                "Bacana demais!"
            };
        }
        
        public enum BrazilianRegion
        {
            Norte,
            Nordeste,
            CentroOeste,
            Sudeste,
            Sul
        }
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeNarrationSystem();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void InitializeNarrationSystem()
        {
            Debug.Log("Arena Brasil - Initializing Brazilian Narration System");
            
            // Setup audio source
            narratorAudioSource = gameObject.GetComponent<AudioSource>();
            if (narratorAudioSource == null)
            {
                narratorAudioSource = gameObject.AddComponent<AudioSource>();
            }
            
            narratorAudioSource.volume = narratorVolume;
            narratorAudioSource.spatialBlend = 0f; // 2D audio
            
            // Setup regional narrator map
            InitializeRegionalNarrators();
            
            // Load user's preferred region
            LoadUserRegionalPreference();
            
            // Subscribe to game events
            SubscribeToGameEvents();
        }
        
        void InitializeRegionalNarrators()
        {
            foreach (var narrator in regionalNarrators)
            {
                narratorMap[narrator.region] = narrator;
            }
            
            Debug.Log($"Initialized {narratorMap.Count} regional narrators");
        }
        
        void LoadUserRegionalPreference()
        {
            string savedRegion = PlayerPrefs.GetString("PreferredRegion", "Sudeste");
            if (Enum.TryParse<BrazilianRegion>(savedRegion, out BrazilianRegion region))
            {
                currentRegion = region;
            }
            
            Debug.Log($"Loaded user preferred region: {currentRegion}");
        }
        
        void SubscribeToGameEvents()
        {
            // Subscribe to various game events for narration
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnMatchStarted += OnMatchStarted;
                GameManager.Instance.OnMatchEnded += OnMatchEnded;
            }
            
            if (CombatSystem.Instance != null)
            {
                CombatSystem.Instance.OnPlayerEliminated += OnPlayerEliminated;
                CombatSystem.Instance.OnPlayersAliveChanged += OnPlayersAliveChanged;
            }
        }
        
        void OnMatchStarted()
        {
            PlayRandomNarration("match_start");
            
            // Play regional greeting
            PlayRegionalGreeting();
        }
        
        void OnMatchEnded()
        {
            // Determine if player won or lost
            bool playerWon = DeterminePlayerVictory();
            
            if (playerWon)
            {
                PlayRandomNarration("victory");
                PlayRegionalVictoryPhrase();
            }
            else
            {
                PlayRandomNarration("defeat");
                PlayRegionalDefeatPhrase();
            }
        }
        
        void OnPlayerEliminated(ulong killerId, ulong victimId, string weaponId)
        {
            if (IsLocalPlayer(killerId))
            {
                PlayRandomNarration("player_elimination");
                PlayRegionalKillPhrase(weaponId);
            }
        }
        
        void OnPlayersAliveChanged(int playersAlive)
        {
            if (playersAlive <= 10 && playersAlive > 1)
            {
                PlayRandomNarration("last_players_alive");
                PlayRegionalTensionPhrase(playersAlive);
            }
        }
        
        public void PlayRandomNarration(string eventType)
        {
            var narrator = GetCurrentNarrator();
            if (narrator == null) return;
            
            AudioClip[] clips = GetClipsForEvent(narrator.clips, eventType);
            if (clips != null && clips.Length > 0)
            {
                AudioClip selectedClip = clips[UnityEngine.Random.Range(0, clips.Length)];
                PlayNarrationClip(selectedClip, eventType);
            }
        }
        
        AudioClip[] GetClipsForEvent(NarrationClips clips, string eventType)
        {
            switch (eventType)
            {
                case "match_start": return clips.matchStart;
                case "player_elimination": return clips.playerElimination;
                case "last_players_alive": return clips.lastPlayersAlive;
                case "victory": return clips.victory;
                case "defeat": return clips.defeat;
                case "first_blood": return clips.firstBlood;
                case "kill_streak": return clips.killStreak;
                case "headshot": return clips.headshot;
                case "zone_warning": return clips.zoneWarning;
                case "zone_closing": return clips.zoneClosing;
                case "rare_item": return clips.rareItemFound;
                case "carnival_mode": return clips.carnivalMode;
                case "soccer_goal": return clips.soccerGoal;
                case "capoeira_move": return clips.capoeiraMoves;
                default: return null;
            }
        }
        
        void PlayNarrationClip(AudioClip clip, string eventType)
        {
            if (narratorAudioSource != null && clip != null)
            {
                narratorAudioSource.clip = clip;
                narratorAudioSource.Play();
                
                OnNarrationPlayed?.Invoke(eventType, currentRegion);
                
                Debug.Log($"Playing narration: {eventType} in {currentRegion} accent");
            }
        }
        
        void PlayRegionalGreeting()
        {
            var narrator = GetCurrentNarrator();
            if (narrator == null) return;
            
            string[] greetings = GetRegionalGreetings(narrator.phrases, currentRegion);
            if (greetings.Length > 0)
            {
                string greeting = greetings[UnityEngine.Random.Range(0, greetings.Length)];
                PlayTextToSpeech(greeting);
            }
        }
        
        void PlayRegionalVictoryPhrase()
        {
            var phrases = new string[]
            {
                "Arrasou, campeão brasileiro!",
                "Essa é a garra tupiniquim!",
                "Viva o Brasil, viva a vitória!",
                "Mandou ver, guerreiro da selva!",
                "Brasil acima de tudo!"
            };
            
            string phrase = phrases[UnityEngine.Random.Range(0, phrases.Length)];
            PlayTextToSpeech(phrase);
        }
        
        void PlayRegionalDefeatPhrase()
        {
            var phrases = new string[]
            {
                "Não foi dessa vez, mas vai na fé!",
                "Levanta a cabeça, brasileiro!",
                "Na próxima é nossa!",
                "Faz parte, guerreiro!",
                "Bora treinar mais!"
            };
            
            string phrase = phrases[UnityEngine.Random.Range(0, phrases.Length)];
            PlayTextToSpeech(phrase);
        }
        
        void PlayRegionalKillPhrase(string weaponId)
        {
            var killPhrases = new Dictionary<string, string[]>
            {
                ["AK47"] = new string[] { "Metralhou geral!", "Rajada certeira!", "AK na veia!" },
                ["Sniper"] = new string[] { "Tiro de elite!", "Sniper raiz!", "Headshot brasileiro!" },
                ["Shotgun"] = new string[] { "Escopetada!", "No peito e na raça!", "Tiro de doze!" },
                ["Pistol"] = new string[] { "Pistolada!", "Tiro certeiro!", "Na mira brasileira!" }
            };
            
            if (killPhrases.ContainsKey(weaponId))
            {
                var phrases = killPhrases[weaponId];
                string phrase = phrases[UnityEngine.Random.Range(0, phrases.Length)];
                PlayTextToSpeech(phrase);
            }
        }
        
        void PlayRegionalTensionPhrase(int playersAlive)
        {
            var tensionPhrases = new string[]
            {
                $"Só restam {playersAlive} guerreiros na arena!",
                $"A tensão tá no ar com {playersAlive} sobreviventes!",
                $"Final brasileiro com {playersAlive} lutadores!",
                $"Agora é que a coisa fica séria - {playersAlive} restantes!",
                "O bicho tá pegando!"
            };
            
            string phrase = tensionPhrases[UnityEngine.Random.Range(0, tensionPhrases.Length)];
            PlayTextToSpeech(phrase);
        }
        
        string[] GetRegionalGreetings(RegionalPhrases phrases, BrazilianRegion region)
        {
            switch (region)
            {
                case BrazilianRegion.Nordeste: return phrases.nordesteGreetings;
                case BrazilianRegion.Sul: return phrases.sulGreetings;
                case BrazilianRegion.Sudeste: return phrases.sudesteGreetings;
                case BrazilianRegion.Norte: return phrases.norteGreetings;
                case BrazilianRegion.CentroOeste: return phrases.centroOesteGreetings;
                default: return phrases.sudesteGreetings;
            }
        }
        
        void PlayTextToSpeech(string text)
        {
            // In a real implementation, this would use a TTS system
            // For now, we'll just log the text that would be spoken
            Debug.Log($"TTS ({currentRegion}): {text}");
        }
        
        RegionalNarrator GetCurrentNarrator()
        {
            return narratorMap.ContainsKey(currentRegion) ? narratorMap[currentRegion] : null;
        }
        
        bool DeterminePlayerVictory()
        {
            // Check if local player won the match
            // This would integrate with your player/match system
            return false; // Placeholder
        }
        
        bool IsLocalPlayer(ulong playerId)
        {
            // Check if the player ID belongs to the local player
            return true; // Placeholder
        }
        
        // Public API
        public void SetRegion(BrazilianRegion region)
        {
            currentRegion = region;
            PlayerPrefs.SetString("PreferredRegion", region.ToString());
            PlayerPrefs.Save();
            
            Debug.Log($"Narration region changed to: {region}");
        }
        
        public void SetNarratorVolume(float volume)
        {
            narratorVolume = Mathf.Clamp01(volume);
            if (narratorAudioSource != null)
            {
                narratorAudioSource.volume = narratorVolume;
            }
        }
        
        public void PlaySpecialEvent(string eventType)
        {
            switch (eventType)
            {
                case "carnival":
                    PlayRandomNarration("carnival_mode");
                    PlayTextToSpeech("É carnaval na arena! Vamo sambar e brigar!");
                    break;
                case "festa_junina":
                    PlayTextToSpeech("Arraiá do Arena Brasil! Quadrilha de guerra!");
                    break;
                case "copa_do_mundo":
                    PlayTextToSpeech("Que nem na Copa - Brasil sempre no coração!");
                    break;
                case "independencia":
                    PlayTextToSpeech("Independência ou morte na arena brasileira!");
                    break;
            }
        }
        
        public void PlayCustomPhrase(string phrase, BrazilianRegion? region = null)
        {
            BrazilianRegion oldRegion = currentRegion;
            
            if (region.HasValue)
            {
                currentRegion = region.Value;
            }
            
            PlayTextToSpeech(phrase);
            
            // Restore original region
            currentRegion = oldRegion;
        }
        
        public List<string> GetAvailableRegions()
        {
            var regions = new List<string>();
            foreach (BrazilianRegion region in Enum.GetValues(typeof(BrazilianRegion)))
            {
                regions.Add(region.ToString());
            }
            return regions;
        }
        
        public void TestNarration(string eventType)
        {
            Debug.Log($"Testing narration for event: {eventType}");
            PlayRandomNarration(eventType);
        }
        
        void OnDestroy()
        {
            // Unsubscribe from events
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnMatchStarted -= OnMatchStarted;
                GameManager.Instance.OnMatchEnded -= OnMatchEnded;
            }
            
            if (CombatSystem.Instance != null)
            {
                CombatSystem.Instance.OnPlayerEliminated -= OnPlayerEliminated;
                CombatSystem.Instance.OnPlayersAliveChanged -= OnPlayersAliveChanged;
            }
        }
    }
}
