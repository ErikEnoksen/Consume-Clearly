using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PauseSettings : MonoBehaviour
{
    [Header("Display")]
    public TMP_Dropdown resolutionDropdown;
    public TMP_Dropdown displayModeDropdown;

    [Header("Volume")]
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;

    [Header("Volume Labels")]
    public TMP_Text masterValueText;
    public TMP_Text musicValueText;
    public TMP_Text sfxValueText;

    [Header("Navigation")]
    public GameObject settingsContainer;
    public GameObject pauseContainer;
    public GameObject mainMenuButtons;

    private Resolution[] _dedupedResolutions;

    void Start()
    {
        InitializeResolutions();
        InitializeDisplayMode();
        SyncUI();
    }

    private void OnEnable()
    {
        SyncUI();
    }

    void InitializeResolutions()
    {
        if (resolutionDropdown == null) return;

        _dedupedResolutions = GetUniqueResolutions();

        resolutionDropdown.options.Clear();
        foreach (var res in _dedupedResolutions)
            resolutionDropdown.options.Add(new TMP_Dropdown.OptionData($"{res.width}x{res.height}"));

        resolutionDropdown.onValueChanged.RemoveAllListeners();
        resolutionDropdown.SetValueWithoutNotify(FindDedupedIndex(SettingsManager.Instance.resolutionIndex));
        resolutionDropdown.RefreshShownValue();
        resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
    }

    void InitializeDisplayMode()
    {
        if (displayModeDropdown == null) return;

        displayModeDropdown.options.Clear();
        displayModeDropdown.options.Add(new TMP_Dropdown.OptionData("Windowed"));
        displayModeDropdown.options.Add(new TMP_Dropdown.OptionData("Fullscreen"));

        displayModeDropdown.onValueChanged.RemoveAllListeners();
        displayModeDropdown.SetValueWithoutNotify(SettingsManager.Instance.fullscreen ? 1 : 0);
        displayModeDropdown.RefreshShownValue();
        displayModeDropdown.onValueChanged.AddListener(OnDisplayModeChanged);
    }

    private Resolution[] GetUniqueResolutions()
    {
        var seen = new HashSet<string>();
        var result = new List<Resolution>();
        foreach (var res in Screen.resolutions)
        {
            if (seen.Add($"{res.width}x{res.height}"))
                result.Add(res);
        }
        return result.ToArray();
    }

    private int FindDedupedIndex(int savedScreenIndex)
    {
        var all = Screen.resolutions;
        if (savedScreenIndex < 0 || savedScreenIndex >= all.Length) return 0;
        var saved = all[savedScreenIndex];
        for (int i = 0; i < _dedupedResolutions.Length; i++)
        {
            if (_dedupedResolutions[i].width == saved.width && _dedupedResolutions[i].height == saved.height)
                return i;
        }
        return 0;
    }

    private void OnResolutionChanged(int dedupedIndex)
    {
        if (_dedupedResolutions == null || dedupedIndex < 0 || dedupedIndex >= _dedupedResolutions.Length) return;
        var target = _dedupedResolutions[dedupedIndex];
        var all = Screen.resolutions;
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].width == target.width && all[i].height == target.height)
            {
                SettingsManager.Instance.SetResolution(i);
                return;
            }
        }
    }

    private void OnDisplayModeChanged(int index)
    {
        SettingsManager.Instance.SetFullscreen(index == 1);
    }

    private void SyncUI()
    {
        if (SettingsManager.Instance == null) return;
        var s = SettingsManager.Instance;

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.RemoveAllListeners();
            masterVolumeSlider.value = s.masterVolume;
            SetVolumeLabel(masterValueText, s.masterVolume);
            masterVolumeSlider.onValueChanged.AddListener(v => { s.SetVolume(v); SetVolumeLabel(masterValueText, v); });
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.RemoveAllListeners();
            musicVolumeSlider.value = s.musicVolume;
            SetVolumeLabel(musicValueText, s.musicVolume);
            musicVolumeSlider.onValueChanged.AddListener(v => { s.SetMusicVolume(v); SetVolumeLabel(musicValueText, v); });
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.RemoveAllListeners();
            sfxVolumeSlider.value = s.sfxVolume;
            SetVolumeLabel(sfxValueText, s.sfxVolume);
            sfxVolumeSlider.onValueChanged.AddListener(v => { s.SetSfxVolume(v); SetVolumeLabel(sfxValueText, v); });
        }

        if (displayModeDropdown != null)
            displayModeDropdown.SetValueWithoutNotify(s.fullscreen ? 1 : 0);
    }

    private void SetVolumeLabel(TMP_Text label, float value)
    {
        if (label != null)
            label.text = Mathf.RoundToInt(value * 100).ToString();
    }

    public void Back()
    {
        settingsContainer.SetActive(false);
        if (pauseContainer != null)
            pauseContainer.SetActive(true);
        else if (mainMenuButtons != null)
            mainMenuButtons.SetActive(true);
    }
}