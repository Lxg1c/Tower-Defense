using System;
using UnityEngine;
using UnityEngine.Rendering;

public static class GamePerformanceSettings
{
    public const string ShowFpsKey = "Display.ShowFPS";
    public static bool ShowFps { get; private set; }
    public static event Action Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() => Changed = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        ShowFps = PlayerPrefs.GetInt(ShowFpsKey, 0) != 0;
        QualitySettings.vSyncCount = 0;
        OnDemandRendering.renderFrameInterval = 1;
        // Mobile's default (-1) is 30 FPS, not unlimited. Request the display rate.
        Application.targetFrameRate = 240;
    }

    public static void ToggleFps()
    {
        ShowFps = !ShowFps;
        PlayerPrefs.SetInt(ShowFpsKey, ShowFps ? 1 : 0);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }
}
