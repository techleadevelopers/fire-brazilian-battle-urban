
using UnityEngine;
using System.Collections.Generic;
using ArenaBrasil.Core;

namespace ArenaBrasil.Audio
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }
        
        [Header("Audio Sources")]
        public AudioSource musicSource;
        public AudioSource sfxSource;
        public AudioSource voiceSource;
        public AudioSource ambientSource;
        
        [Header("Brazilian Music")]
        public AudioClip[] menuMusic;
        public AudioClip[] combatMusicLow;
        public AudioClip[] combatMusicHigh;
        public AudioClip[] victoryMusic;
        
        [Header("Brazilian Voice Lines")]
        public AudioClip[] saciVoiceLines;
        public AudioClip[] curupiraVoiceLines;
        public AudioClip[] iaraVoiceLines;
        public AudioClip[] boitataVoiceLines;
        public AudioClip[] mataCavalosVoiceLines;
        
        [Header("Combat SFX")]
        public AudioClip[] weaponSounds;
        public AudioClip[] impactSounds;
        public AudioClip[] reloadSounds;
        
        [Header("Ambient Sounds")]
        public AudioClip[] jungleAmbient;
        public AudioClip[] cityAmbient;
        public AudioClip[] favelaAmbient;
        
        [Header("Audio Settings")]
        [Range(0f, 1f)] public float masterVolume = 1f;
        [Range(0f, 1f)] public float musicVolume = 0.7f;
        [Range(0f, 1f)] public float sfxVolume = 0.8f;
        [Range(0f, 1f)] public float voiceVolume = 1f;
        
        private MusicState currentMusicState = MusicState.Menu;
        private Dictionary<string, AudioClip[]> heroVoiceLines;
        
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeAudio();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void InitializeAudio()
        {
            heroVoiceLines = new Dictionary<string, AudioClip[]>
            {
                { "Saci", saciVoiceLines },
                { "Curupira", curupiraVoiceLines },
                { "IaraMae", iaraVoiceLines },
                { "Boitata", boitataVoiceLines },
                { "MataCavalos", mataCavalosVoiceLines }
            };
            
            // Set initial volumes
            UpdateVolumes();
            
            // Start menu music
            PlayMenuMusic();
        }
        
        public void UpdateVolumes()
        {
            if (musicSource != null)
                musicSource.volume = masterVolume * musicVolume;
            
            if (sfxSource != null)
                sfxSource.volume = masterVolume * sfxVolume;
            
            if (voiceSource != null)
                voiceSource.volume = masterVolume * voiceVolume;
        }
        
        public void PlayMenuMusic()
        {
            if (menuMusic.Length > 0 && musicSource != null)
            {
                currentMusicState = MusicState.Menu;
                PlayRandomMusic(menuMusic);
            }
        }
        
        public void PlayCombatMusic(bool highIntensity = false)
        {
            var musicArray = highIntensity ? combatMusicHigh : combatMusicLow;
            if (musicArray.Length > 0 && musicSource != null)
            {
                currentMusicState = highIntensity ? MusicState.CombatHigh : MusicState.CombatLow;
                PlayRandomMusic(musicArray);
            }
        }
        
        public void PlayVictoryMusic()
        {
            if (victoryMusic.Length > 0 && musicSource != null)
            {
                currentMusicState = MusicState.Victory;
                PlayRandomMusic(victoryMusic);
            }
        }
        
        void PlayRandomMusic(AudioClip[] musicArray)
        {
            if (musicArray.Length > 0 && musicSource != null)
            {
                var clip = musicArray[Random.Range(0, musicArray.Length)];
                musicSource.clip = clip;
                musicSource.Play();
            }
        }
        
        public void PlayHeroVoiceLine(string heroName, VoiceLineType type = VoiceLineType.Combat)
        {
            if (heroVoiceLines.ContainsKey(heroName) && voiceSource != null)
            {
                var voiceLines = heroVoiceLines[heroName];
                if (voiceLines.Length > 0)
                {
                    var clip = voiceLines[Random.Range(0, voiceLines.Length)];
                    voiceSource.PlayOneShot(clip);
                }
            }
        }
        
        public void PlayWeaponSound(string weaponId)
        {
            if (weaponSounds.Length > 0 && sfxSource != null)
            {
                // Simple implementation - could be expanded to specific weapon sounds
                var clip = weaponSounds[Random.Range(0, weaponSounds.Length)];
                sfxSource.PlayOneShot(clip);
            }
        }
        
        public void PlayImpactSound()
        {
            if (impactSounds.Length > 0 && sfxSource != null)
            {
                var clip = impactSounds[Random.Range(0, impactSounds.Length)];
                sfxSource.PlayOneShot(clip);
            }
        }
        
        public void PlayReloadSound()
        {
            if (reloadSounds.Length > 0 && sfxSource != null)
            {
                var clip = reloadSounds[Random.Range(0, reloadSounds.Length)];
                sfxSource.PlayOneShot(clip);
            }
        }
        
        public void PlayAmbientSound(MapType mapType)
        {
            AudioClip[] ambientClips = null;
            
            switch (mapType)
            {
                case MapType.Jungle:
                    ambientClips = jungleAmbient;
                    break;
                case MapType.City:
                    ambientClips = cityAmbient;
                    break;
                case MapType.Favela:
                    ambientClips = favelaAmbient;
                    break;
            }
            
            if (ambientClips != null && ambientClips.Length > 0 && ambientSource != null)
            {
                var clip = ambientClips[Random.Range(0, ambientClips.Length)];
                ambientSource.clip = clip;
                ambientSource.loop = true;
                ambientSource.Play();
            }
        }
        
        public void StopAmbientSound()
        {
            if (ambientSource != null)
            {
                ambientSource.Stop();
            }
        }
        
        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            UpdateVolumes();
        }
        
        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            UpdateVolumes();
        }
        
        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            UpdateVolumes();
        }
        
        public void SetVoiceVolume(float volume)
        {
            voiceVolume = Mathf.Clamp01(volume);
            UpdateVolumes();
        }
    }
    
    public enum MusicState
    {
        Menu,
        CombatLow,
        CombatHigh,
        Victory
    }
    
    public enum VoiceLineType
    {
        Combat,
        Ability,
        Death,
        Victory
    }
    
    public enum MapType
    {
        Jungle,
        City,
        Favela
    }
}
