using UnityEngine;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance;

    public int resolutionIndex;
    public int displayMode; // 0=Windowed, 1=Borderless, 2=Fullscreen
    public bool musicOn;
    public float masterVolume;
    public float musicVolume;
    public float sfxVolume;

    private Resolution[] resolutions;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        resolutions = Screen.resolutions;

        LoadSettings();
        ApplySettings();
    }

    void LoadSettings()
    {
        resolutionIndex = PlayerPrefs.GetInt("ResolutionIndex", GetDefaultResolutionIndex());
        displayMode = PlayerPrefs.GetInt("DisplayMode", 2);
        musicOn = PlayerPrefs.GetInt("MusicOn", 1) == 1;
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
        sfxVolume = PlayerPrefs.GetFloat("SfxVolume", 1f);
    }

    private int GetDefaultResolutionIndex()
    {
        Resolution current = Screen.currentResolution;
        for (int i = 0; i < resolutions.Length; i++)
        {
            if (resolutions[i].width == current.width && resolutions[i].height == current.height)
                return i;
        }
        for (int i = 0; i < resolutions.Length; i++)
        {
            if (resolutions[i].width == 1920 && resolutions[i].height == 1080)
                return i;
        }
        return resolutions.Length - 1;
    }

    public void SaveSettings()
    {
        PlayerPrefs.SetInt("ResolutionIndex", resolutionIndex);
        PlayerPrefs.SetInt("DisplayMode", displayMode);
        PlayerPrefs.SetInt("MusicOn", musicOn ? 1 : 0);
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        PlayerPrefs.SetFloat("SfxVolume", sfxVolume);
    }

    private void EnsureResolutions()
    {
        if (resolutions == null || resolutions.Length == 0)
            resolutions = Screen.resolutions;
    }

    public void ApplySettings()
    {
        EnsureResolutions();
        // Resolution
        if (resolutionIndex < resolutions.Length)
        {
            var r = resolutions[resolutionIndex];
            Screen.SetResolution(r.width, r.height, GetFullScreenMode());
        }

        // Audio
        AudioListener.volume = masterVolume;
        AudioManager.Instance?.SetMusicVolume(musicOn ? musicVolume : 0f);
        AudioManager.Instance?.SetSfxVolume(sfxVolume);

        if (musicOn)
        {
            if (AudioManager.Instance != null && !AudioManager.Instance.IsPlaying("BackgroundMusic"))
                AudioManager.Instance.Play("BackgroundMusic");
        }
        else
            AudioManager.Instance?.Stop("BackgroundMusic");
    }

    public Resolution[] GetAvailableResolutions()
    {
        EnsureResolutions();
        return resolutions;
    }

    public void SetResolution(int index)
    {
        EnsureResolutions();
        if (index >= 0 && index < resolutions.Length)
        {
            resolutionIndex = index;
            ApplySettings();
            SaveSettings();
        }
    }

    private FullScreenMode GetFullScreenMode()
    {
        return displayMode switch
        {
            1 => FullScreenMode.FullScreenWindow,
            2 => FullScreenMode.ExclusiveFullScreen,
            _ => FullScreenMode.Windowed,
        };
    }

    public void SetDisplayMode(int mode)
    {
        displayMode = Mathf.Clamp(mode, 0, 2);
        ApplySettings();
        SaveSettings();
    }

    public void ToggleMusic()
    {
        musicOn = !musicOn;
        ApplySettings();
        SaveSettings();
    }

    public void SetVolume(float value)
    {
        masterVolume = value;
        AudioListener.volume = value;
        SaveSettings();
    }

    public void SetMusicVolume(float value)
    {
        musicVolume = value;
        AudioManager.Instance?.SetMusicVolume(musicOn ? musicVolume : 0f);
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
    }

    public void SetSfxVolume(float value)
    {
        sfxVolume = value;
        AudioManager.Instance?.SetSfxVolume(sfxVolume);
        PlayerPrefs.SetFloat("SfxVolume", sfxVolume);
    }
}