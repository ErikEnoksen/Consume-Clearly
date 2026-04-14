using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PauseSettings : MonoBehaviour
{
    public TMP_Dropdown resolutionDropdown;

    public Image musicButtonImage;
    public Sprite musicOnSprite;
    public Sprite musicOffSprite;

    public Image fullscreenButtonImage;
    public Sprite fullscreenOnSprite;
    public Sprite fullscreenOffSprite;

    public GameObject keybindsMenu;
    public GameObject settingsContainer;
    public GameObject pauseContainer;
    public GameObject mainMenuButtons;

    void Start()
    {
        InitializeResolutions();
        UpdateVisuals();
    }

    void InitializeResolutions()
    {
        resolutionDropdown.options.Clear();

        Resolution[] resolutions = SettingsManager.Instance.GetAvailableResolutions();

        foreach (UnityEngine.Resolution res in resolutions)
        {
            Debug.Log(res.ToString());
        }

        foreach (Resolution resolution in resolutions)
        {
            var option = new TMP_Dropdown.OptionData($"{resolution.width}x{resolution.height}");
            resolutionDropdown.options.Add(option);
        }

        resolutionDropdown.value = SettingsManager.Instance.resolutionIndex;
        resolutionDropdown.RefreshShownValue();
        resolutionDropdown.onValueChanged.AddListener(SettingsManager.Instance.SetResolution);
    }

    public void ToggleFullscreen()
    {
        SettingsManager.Instance.ToggleFullscreen();
        UpdateVisuals();
    }

    public void ToggleMusic()
    {
        SettingsManager.Instance.ToggleMusic();
        UpdateVisuals();
    }

    void UpdateVisuals()
    {
        var settings = SettingsManager.Instance;

        if (musicButtonImage.sprite != null)
        musicButtonImage.sprite = settings.musicOn ? musicOnSprite : musicOffSprite;

        if (fullscreenButtonImage.sprite != null)
        fullscreenButtonImage.sprite = settings.fullscreen ? fullscreenOnSprite : fullscreenOffSprite;
    }

    public void Keybinds()
    {
        keybindsMenu.SetActive(true);
        settingsContainer.SetActive(false);
    }

    public void Back()
    {
        if (mainMenuButtons == null) 
        { 
            settingsContainer.SetActive(false);
            pauseContainer.SetActive(true);
        } 
        
        if(pauseContainer == null) 
        {
            settingsContainer.SetActive(false);
            mainMenuButtons.SetActive(true);
        }
    }
}