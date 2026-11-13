using System;
using UnityEngine;
using UnityEngine.Rendering;

public static class GamePerformanceSettings
{
    public enum LightingMode { Standard, Simple }

    public const string ShowFpsKey = "Display.ShowFPS";
    public const string LightingKey = "Display.Lighting";
    public const string NormalMapsKey = "Display.NormalMaps";
    public static bool ShowFps { get; private set; }
    public static LightingMode Lighting { get; private set; }
    public static bool NormalMaps { get; private set; } = true;
    public static event Action Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() => Changed = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        ShowFps = PlayerPrefs.GetInt(ShowFpsKey, 0) != 0;
        LoadGraphicsPreferences();
        QualitySettings.vSyncCount = 0;
        OnDemandRendering.renderFrameInterval = 1;
        // Mobile's default (-1) is 30 FPS, not unlimited. Request the display rate.
        Application.targetFrameRate = 240;
    }

    internal static void LoadGraphicsPreferences()
    {
        Lighting = PlayerPrefs.GetInt(LightingKey) == (int)LightingMode.Simple
            ? LightingMode.Simple : LightingMode.Standard;
        NormalMaps = PlayerPrefs.GetInt(NormalMapsKey, 1) != 0;
    }

    public static void SetLighting(LightingMode mode)
    {
        if (mode != LightingMode.Standard && mode != LightingMode.Simple)
            throw new ArgumentOutOfRangeException(nameof(mode));
        Lighting = mode;
        PlayerPrefs.SetInt(LightingKey, (int)mode);
        PlayerPrefs.Save();
    }

    public static void ToggleNormalMaps()
    {
        NormalMaps = !NormalMaps;
        PlayerPrefs.SetInt(NormalMapsKey, NormalMaps ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static void ToggleFps()
    {
        ShowFps = !ShowFps;
        PlayerPrefs.SetInt(ShowFpsKey, ShowFps ? 1 : 0);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }
}
