using UnityEngine;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance;

    public int resolutionIndex;
    public bool fullscreen;
    public bool musicOn;
    public float masterVolume;

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
        resolutionIndex = PlayerPrefs.GetInt("ResolutionIndex", 0);
        fullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
        musicOn = PlayerPrefs.GetInt("MusicOn", 1) == 1;
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
    }

    public void SaveSettings()
    {
        PlayerPrefs.SetInt("ResolutionIndex", resolutionIndex);
        PlayerPrefs.SetInt("Fullscreen", fullscreen ? 1 : 0);
        PlayerPrefs.SetInt("MusicOn", musicOn ? 1 : 0);
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
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
            Screen.SetResolution(r.width, r.height, fullscreen);
        }

        // Audio
        AudioListener.volume = masterVolume;

        if (musicOn)
            AudioManager.Instance?.Play("BackgroundMusic");
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

    public void ToggleFullscreen()
    {
        fullscreen = !fullscreen;
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
        ApplySettings();
        SaveSettings();
    }
}