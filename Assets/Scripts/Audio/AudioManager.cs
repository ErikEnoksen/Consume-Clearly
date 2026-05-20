using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class Sound
{
    public string name;
    public AudioClip clip;

    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.5f, 2f)] public float pitch = 1f;
    
    public bool loop = false;
    
    
    [HideInInspector] public AudioSource source;
}
public class AudioManager : MonoBehaviour
{
    //Making a one Global Instance of AudioManager that any script can access
    public static AudioManager Instance { get; private set; }

    private void Awake()
    {
       //Checking for duplicate Instance if so destroy it

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
    
    //Sound List would look like this in the inspector:
    // name: "BackgroundMusic" clip: music.mp3 loop: true volume: 0.4
    public Sound[] sounds;
    
    //Quick check so Play() dosent go through the whole array every call
    private Dictionary<string, Sound> _soundMap;
    private float _sfxVolume = 1f;

    private void Start()
    {
        AudioListener.volume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        _soundMap = new Dictionary<string, Sound>();

        if (sounds == null) return;
        foreach (Sound s in sounds)
        {
            _soundMap.Add(s.name, s);
        }
        
        //Auto-play anything that has been marked with Loop on start
        foreach (Sound s in sounds)
        {
            if (s.loop && s.source != null)
            {
                s.source.Play();
            }
        }
        
    }

    public void SetMasterVolume(float value)
    {
        AudioListener.volume = value;
    }

    public void SetMusicVolume(float value)
    {
        if (sounds == null) return;
        foreach (var s in sounds)
        {
            if (s.loop && s.source != null)
                s.source.volume = s.volume * value;
        }
    }

    public void SetSfxVolume(float value)
    {
        _sfxVolume = value;
        if (sounds == null) return;
        foreach (var s in sounds)
        {
            if (!s.loop && s.source != null)
                s.source.volume = s.volume * value;
        }
    }

    /*
     * Public API
     */

    public void Play(string soundName)
    {
        if (!TryGet(soundName, out Sound s)) return;
        s.source.volume = s.volume * _sfxVolume;
        s.source.Play();
    }
    
    //Stop a looping Sound
    public void Stop(string soundName)
    {
        if (!TryGet(soundName, out Sound s))
        {
            return;
        }
        s.source.Stop();
    }

    public bool IsPlaying(string soundName)
    {
        if (!TryGet(soundName, out Sound s))
        {
            return false;
        }
        return s.source.isPlaying;
    }
    
    //Fade sound volume while transistioning scenes. (Background Music)
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
        {
            s.source.Stop();
        }
    }
    
    /*
     * Helpers:
     */
    private bool TryGet(string soundName, out Sound s)
    {
        if (_soundMap != null && _soundMap.TryGetValue(soundName, out s))
        {
            return true;
        }
        
        if (sounds == null)
        {
            s = null;
            return false;
        }

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
