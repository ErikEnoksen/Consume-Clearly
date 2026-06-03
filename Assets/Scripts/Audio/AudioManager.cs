// =============================================================================
// AudioManager.cs — Global Sound Playback Manager
// 
//
// PURPOSE:
//   Singleton that owns every AudioSource in the game. All sounds are defined
//   in the Inspector as Sound entries in the sounds array. Any script can call
//   AudioManager.Instance.Play("SoundName") without needing a reference.
//
// HOW SOUNDS WORK:
//   Each Sound entry holds a clip, volume, pitch, and loop flag.
//   On Awake, one AudioSource component is added to the AudioManager GameObject
//   per Sound entry. On Start, looping sounds (e.g. background music) auto-play.
//
// VOLUME TIERS:
//   • Master volume — AudioListener.volume, scales all audio globally
//   • Music volume  — applied to all loop=true sources (background tracks)
//   • SFX volume    — applied to all loop=false sources (effects, footsteps, etc.)
//   Settings are loaded from SettingsManager on Start, falling back to PlayerPrefs.
//
// DICTIONARY CACHE (_soundMap):
//   Sounds are indexed by name into a dictionary at Start so Play/Stop/IsPlaying
//   look up in O(1) instead of iterating the array every call.
//   TryGet falls back to the array if the map isn't built yet (e.g. very early calls).
//
// FADE:
//   Fade() coroutine lerps a sound's volume to a target over a duration.
//   Stops the source if fading to zero — used for crossfading music between scenes.
// =============================================================================

using UnityEngine;
using System.Collections.Generic;

// --- Sound Entry ---
// One entry per sound effect or music track. Assign in the AudioManager Inspector.
// loop = true → background music / ambient; loop = false → one-shot SFX.
[System.Serializable]
public class Sound
{
    public string name;
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.5f, 2f)] public float pitch = 1f;
    public bool loop = false;
    [HideInInspector] public AudioSource source; // created at runtime, not set in Inspector
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    // All sounds in the game — defined here, one AudioSource per entry is added at Awake.
    // Example entry: name:"Footstep" clip:footstep.wav loop:false volume:0.6
    public Sound[] sounds;

    // Dictionary for O(1) name lookup — built in Start from the sounds array.
    private Dictionary<string, Sound> _soundMap;

    // Stored separately so SetSfxVolume can scale without touching the base Sound.volume.
    private float _sfxVolume = 1f;

    // --- Awake: Source Creation ---
    // Each Sound gets its own AudioSource so sounds can play simultaneously without interrupting each other.
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (sounds == null) return;
        foreach (Sound s in sounds)
        {
            s.source = gameObject.AddComponent<AudioSource>();
            s.source.clip = s.clip;
            s.source.volume = s.volume;
            s.source.pitch = s.pitch;
            s.source.loop = s.loop;
        }
    }

    // --- Start: Map Build + Volume Init + Auto-Play ---
    // Volume settings are read from SettingsManager if available, otherwise from PlayerPrefs.
    // Looping sounds auto-play here so music begins without any external call.
    private void Start()
    {
        _soundMap = new Dictionary<string, Sound>();
        if (sounds != null)
        {
            foreach (Sound s in sounds)
                _soundMap.Add(s.name, s);
        }

        // Apply saved volume settings — prefer SettingsManager so values stay in sync.
        if (SettingsManager.Instance != null)
        {
            AudioListener.volume = SettingsManager.Instance.masterVolume;
            SetMusicVolume(SettingsManager.Instance.musicOn ? SettingsManager.Instance.musicVolume : 0f);
            SetSfxVolume(SettingsManager.Instance.sfxVolume);
        }
        else
        {
            AudioListener.volume = PlayerPrefs.GetFloat("MasterVolume", 1f);
            bool musicOn = PlayerPrefs.GetInt("MusicOn", 1) == 1;
            SetMusicVolume(musicOn ? PlayerPrefs.GetFloat("MusicVolume", 1f) : 0f);
            SetSfxVolume(PlayerPrefs.GetFloat("SfxVolume", 1f));
        }

        if (sounds == null) return;

        // Auto-play looping sounds (music/ambient) on scene start.
        foreach (Sound s in sounds)
        {
            if (s.loop && s.source != null)
                s.source.Play();
        }
    }

    // --- Volume Control ---
    // Master scales everything via Unity's AudioListener.
    // Music and SFX are distinguished by the loop flag on each Sound entry.
    public void SetMasterVolume(float value)
    {
        AudioListener.volume = value;
    }

    public void SetMusicVolume(float value)
    {
        if (sounds == null) return;
        foreach (var s in sounds)
            if (s.loop && s.source != null)
                s.source.volume = s.volume * value;
    }

    public void SetSfxVolume(float value)
    {
        _sfxVolume = value;
        if (sounds == null) return;
        foreach (var s in sounds)
            if (!s.loop && s.source != null)
                s.source.volume = s.volume * value;
    }

    // --- Public Playback API ---

    // Plays a sound by name. SFX volume is applied at play time so volume changes take effect immediately.
    public void Play(string soundName)
    {
        if (!TryGet(soundName, out Sound s)) return;
        s.source.volume = s.volume * _sfxVolume;
        s.source.Play();
    }

    // Stops a sound — primarily used for looping sounds like footsteps.
    public void Stop(string soundName)
    {
        if (!TryGet(soundName, out Sound s)) return;
        s.source.Stop();
    }

    public bool IsPlaying(string soundName)
    {
        if (!TryGet(soundName, out Sound s)) return false;
        return s.source.isPlaying;
    }

    // Smoothly transitions a sound's volume to targetVolume over duration seconds.
    // Stops the source when fading to zero — used for scene music crossfades.
    public System.Collections.IEnumerator Fade(string soundName, float targetVolume, float duration)
    {
        if (!TryGet(soundName, out Sound s)) yield break;

        float startVolume = s.source.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            s.source.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
            yield return null;
        }

        s.source.volume = targetVolume;
        if (targetVolume <= 0f)
            s.source.Stop();
    }

    // --- Helper ---
    // Tries the dictionary first (O(1)), falls back to linear array search if the map
    // isn't ready. Logs a warning and returns false if the name isn't found.
    private bool TryGet(string soundName, out Sound s)
    {
        if (_soundMap != null && _soundMap.TryGetValue(soundName, out s))
            return true;

        if (sounds == null) { s = null; return false; }

        foreach (Sound sound in sounds)
        {
            if (sound.name == soundName)
            {
                s = sound;
                return true;
            }
        }

        Debug.LogWarning($"AudioManager: sound '{soundName}' not found.");
        s = null;
        return false;
    }
}
