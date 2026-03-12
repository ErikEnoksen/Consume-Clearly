using UnityEngine;
using UnityEngine.UI;

public class PauseSettings : MonoBehaviour
{
    
    public Image resolutionButtonImage;
    public Sprite[] resolutionSprites;
    private int resolutionIndex = 0;

    private int[][] res = new int[][] {
        new int[] {1920, 1080},
        new int[] {1280, 720},
        new int[] {2560, 1440},
        new int[] {3840, 2160}
    };

    public Image musicButtonImage;
    public Sprite musicOnSprite;
    public Sprite musicOffSprite;
    private bool musicOn = true;
   
    public Image fullscreenButtonImage;
    public Sprite fullscreenOnSprite;
    public Sprite fullscreenOffSprite;
    
    public GameObject settingsContainer;
    public GameObject pauseContainer;
    void Start()
    {
        
        UpdateMusicVisual();
        UpdateFullscreenVisual();
        resolutionButtonImage.sprite = resolutionSprites[resolutionIndex];
    }
    
    public void CycleResolution()
    {
        resolutionIndex++;
        if (resolutionIndex >= res.Length)
        {
            resolutionIndex = 0;
        }
        resolutionButtonImage.sprite = resolutionSprites[resolutionIndex];
        Screen.SetResolution(res[resolutionIndex][0], res[resolutionIndex][1], Screen.fullScreen);
    }

    public void ToggleFullscreen()
    {
        Screen.fullScreen = !Screen.fullScreen;
        UpdateFullscreenVisual();
    }

    public void ToggleMusic()
    {
        musicOn = !musicOn;
        if (musicOn)
        {
            AudioManager.Instance.Play("BackgroundMusic");
        }
        else
        {
            AudioManager.Instance.Stop("BackgroundMusic");
        }
        UpdateMusicVisual();
    }
    void UpdateMusicVisual()
    {
        musicButtonImage.sprite = musicOn ? musicOnSprite : musicOffSprite;
    }
    void UpdateFullscreenVisual()
    {
        fullscreenButtonImage.sprite = Screen.fullScreen ? fullscreenOnSprite : fullscreenOffSprite;
    }
    public void Back()
    {
        settingsContainer.SetActive(false);
        pauseContainer.SetActive(true);
    }
}   
