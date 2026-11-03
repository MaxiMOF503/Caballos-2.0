using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public sealed class MenuSettings : MonoBehaviour
{
    public Slider masterVolume;
    public Text volumeLabel;
    public Toggle fullscreen;
    public Dropdown resolution;
    Vector2Int[] sizes;

    void Awake()
    {
        masterVolume.onValueChanged.AddListener(SetVolume);
        fullscreen.onValueChanged.AddListener(_ => SaveDisplay());
        resolution.onValueChanged.AddListener(_ => SaveDisplay());
    }

    void OnEnable()
    {
        sizes = MenuPreferences.AvailableSizes();
        masterVolume.SetValueWithoutNotify(MenuPreferences.Volume);
        volumeLabel.text = Mathf.RoundToInt(MenuPreferences.Volume * 100) + "%";
        fullscreen.SetIsOnWithoutNotify(MenuPreferences.Fullscreen);
        resolution.ClearOptions();
        resolution.AddOptions(sizes.Select(s => s.x + " × " + s.y).ToList());
        int selected = Array.IndexOf(sizes, MenuPreferences.Size);
        resolution.SetValueWithoutNotify(Mathf.Max(0, selected));
        resolution.RefreshShownValue();
    }

    void SetVolume(float value)
    {
        MenuPreferences.SetVolume(value);
        volumeLabel.text = Mathf.RoundToInt(value * 100) + "%";
    }
    void SaveDisplay()
    {
        if (sizes != null && sizes.Length > 0)
            MenuPreferences.SetDisplay(sizes[resolution.value], fullscreen.isOn);
    }
}

public static class MenuPreferences
{
    static string Prefix
    {
        get
        {
#if UNITY_EDITOR
            string test = Environment.GetEnvironmentVariable("CABALLOS_VALIDATION_PREFS_PREFIX");
            if (!string.IsNullOrEmpty(test)) return test;
#endif
            return "Caballos.Settings.";
        }
    }
    public static float Volume => Mathf.Clamp01(PlayerPrefs.GetFloat(Prefix + "Volume", 1));
    public static bool Fullscreen => PlayerPrefs.GetInt(Prefix + "Fullscreen", Screen.fullScreen ? 1 : 0) != 0;
    public static Vector2Int Size => new Vector2Int(PlayerPrefs.GetInt(Prefix + "Width", Mathf.Max(1, Screen.width)), PlayerPrefs.GetInt(Prefix + "Height", Mathf.Max(1, Screen.height)));
    public static Vector2Int[] AvailableSizes()
    {
        var sizes = Screen.resolutions.Select(r => new Vector2Int(r.width, r.height)).Distinct().OrderByDescending(s => s.x).ThenByDescending(s => s.y).ToArray();
        return sizes.Length > 0 ? sizes : new[] { new Vector2Int(Mathf.Max(1280, Screen.width), Mathf.Max(720, Screen.height)) };
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void ApplySaved()
    {
        AudioListener.volume = Volume;
        ApplyDisplay(Size, Fullscreen);
    }
    public static void SetVolume(float value)
    {
        PlayerPrefs.SetFloat(Prefix + "Volume", Mathf.Clamp01(value));
        AudioListener.volume = Volume;
        PlayerPrefs.Save();
    }
    public static void SetDisplay(Vector2Int size, bool full)
    {
        if (!AvailableSizes().Contains(size)) return;
        PlayerPrefs.SetInt(Prefix + "Width", size.x);
        PlayerPrefs.SetInt(Prefix + "Height", size.y);
        PlayerPrefs.SetInt(Prefix + "Fullscreen", full ? 1 : 0);
        PlayerPrefs.Save();
        ApplyDisplay(size, full);
    }
    static void ApplyDisplay(Vector2Int size, bool full)
    {
        // Never change the user's display while running editor/headless tests.
        if (!Application.isEditor && !Application.isBatchMode && AvailableSizes().Contains(size))
            Screen.SetResolution(size.x, size.y, full ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
    }
}
