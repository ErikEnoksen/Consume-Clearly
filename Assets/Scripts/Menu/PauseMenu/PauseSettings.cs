using UnityEngine;
using UnityEngine.UI;

public class PauseSettings : MonoBehaviour
{
    public Image resolutionButtonImage;
    public Sprite[] resolutionSprites;

    public Image musicButtonImage;
    public Sprite musicOnSprite;
    public Sprite musicOffSprite;

    public Image fullscreenButtonImage;
    public Sprite fullscreenOnSprite;
    public Sprite fullscreenOffSprite;

    public GameObject settingsContainer;
    public GameObject pauseContainer;

    void Start()
    {
        UpdateVisuals();
    }

    public void CycleResolution()
    {
        SettingsManager.Instance.CycleResolution();
        UpdateVisuals();
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

        resolutionButtonImage.sprite = resolutionSprites[settings.resolutionIndex];

        musicButtonImage.sprite = settings.musicOn ? musicOnSprite : musicOffSprite;

        fullscreenButtonImage.sprite = settings.fullscreen ? fullscreenOnSprite : fullscreenOffSprite;
    }

    public void Back()
    {
        settingsContainer.SetActive(false);
        pauseContainer.SetActive(true);
    }
}