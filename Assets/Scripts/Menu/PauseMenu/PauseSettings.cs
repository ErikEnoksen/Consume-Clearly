using System;
using System.Collections;
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
        resolutionDropdown.SetValueWithoutNotify(FindCurrentDedupedIndex());
        resolutionDropdown.RefreshShownValue();
        resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
    }

    void InitializeDisplayMode()
    {
        if (displayModeDropdown == null) return;

        displayModeDropdown.options.Clear();
        displayModeDropdown.options.Add(new TMP_Dropdown.OptionData("Windowed"));
        displayModeDropdown.options.Add(new TMP_Dropdown.OptionData("Borderless"));
        displayModeDropdown.options.Add(new TMP_Dropdown.OptionData("Fullscreen"));

        displayModeDropdown.onValueChanged.RemoveAllListeners();
        displayModeDropdown.SetValueWithoutNotify(SettingsManager.Instance.displayMode);
        displayModeDropdown.RefreshShownValue();
        displayModeDropdown.onValueChanged.AddListener(OnDisplayModeChanged);
    }

    private static readonly (int width, int height)[] StandardResolutions =
    {
        (800,  600),
        (1280, 720),
        (1366, 768),
        (1600, 900),
        (1920, 1080),
        (2560, 1440),
        (3840, 2160),
    };

    private Resolution[] GetUniqueResolutions()
    {
        var supported = new Dictionary<(int, int), Resolution>();
        foreach (var res in Screen.resolutions)
        {
            var key = (res.width, res.height);
            if (!supported.ContainsKey(key))
                supported[key] = res;
        }

        var result = new List<Resolution>();
        foreach (var (w, h) in StandardResolutions)
        {
            if (supported.TryGetValue((w, h), out var res))
                result.Add(res);
        }

        // Fallback: always include current resolution if nothing matched
        if (result.Count == 0)
        {
            var cur = Screen.currentResolution;
            if (supported.TryGetValue((cur.width, cur.height), out var curRes))
                result.Add(curRes);
        }

        return result.ToArray();
    }

    private int FindCurrentDedupedIndex()
    {
        // Try saved resolution first
        var all = Screen.resolutions;
        int savedIdx = SettingsManager.Instance.resolutionIndex;
        if (savedIdx >= 0 && savedIdx < all.Length)
        {
            var saved = all[savedIdx];
            for (int i = 0; i < _dedupedResolutions.Length; i++)
            {
                if (_dedupedResolutions[i].width == saved.width && _dedupedResolutions[i].height == saved.height)
                    return i;
            }
        }

        // Fall back to actual current screen resolution
        var current = Screen.currentResolution;
        for (int i = 0; i < _dedupedResolutions.Length; i++)
        {
            if (_dedupedResolutions[i].width == current.width && _dedupedResolutions[i].height == current.height)
                return i;
        }

        return _dedupedResolutions.Length - 1;
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
        SettingsManager.Instance.SetDisplayMode(index);
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
            displayModeDropdown.SetValueWithoutNotify(s.displayMode);
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