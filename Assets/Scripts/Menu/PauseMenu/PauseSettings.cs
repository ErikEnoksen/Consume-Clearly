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
    public GameObject backButton;
    public GameObject keybindsButton;
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

        Resolution[] standardResolutions = new[]
        {
            new Resolution { width = 1280, height = 720 },
            new Resolution { width = 1366, height = 768 },
            new Resolution { width = 1600, height = 900 },
            new Resolution { width = 1920, height = 1080 },
            new Resolution { width = 2560, height = 1440 },
            new Resolution { width = 3840, height = 2160 }
        };

        int selectedIndex = 0;
        int currentResolutionIndex = SettingsManager.Instance.resolutionIndex;

        foreach (Resolution resolution in standardResolutions)
        {
            var option = new TMP_Dropdown.OptionData($"{resolution.width}x{resolution.height}");
            resolutionDropdown.options.Add(option);
            
            // Track the index matching the current resolution
            if (currentResolutionIndex == resolutionDropdown.options.Count - 1)
                selectedIndex = resolutionDropdown.options.Count - 1;
        }

        resolutionDropdown.onValueChanged.RemoveAllListeners();
        resolutionDropdown.SetValueWithoutNotify(selectedIndex);
        resolutionDropdown.RefreshShownValue();
        resolutionDropdown.onValueChanged.AddListener(SettingsManager.Instance.SetResolution);
    }

    public void ToggleFullscreen()
    {
        if (SettingsManager.Instance == null) return;
        SettingsManager.Instance.ToggleFullscreen();
        UpdateVisuals();
    }

    public void ToggleMusic()
    {
        if (SettingsManager.Instance == null) return;
        SettingsManager.Instance.ToggleMusic();
        UpdateVisuals();
    }

    void UpdateVisuals()
    {
        if (SettingsManager.Instance == null) return;
        var settings = SettingsManager.Instance;

        if (musicButtonImage != null)
            musicButtonImage.sprite = settings.musicOn ? musicOnSprite : musicOffSprite;

        if (fullscreenButtonImage != null)
            fullscreenButtonImage.sprite = settings.fullscreen ? fullscreenOnSprite : fullscreenOffSprite;
    }

    public void Keybinds()
    {
        KeybindManager.Instance.SetBackContext(keybindsMenu, settingsContainer);
        keybindsMenu.SetActive(true);
        settingsContainer.SetActive(false);
        if (backButton != null) backButton.SetActive(false);
        if (keybindsButton != null) keybindsButton.SetActive(false);
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