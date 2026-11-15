using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class GamePerformanceSettings
{
    public enum LightingMode { Standard, Simple }
    public enum AndroidQuality { High, Balanced, Low }

    public const string ShowFpsKey = "Display.ShowFPS";
    public const string LightingKey = "Display.Lighting";
    public const string NormalMapsKey = "Display.NormalMaps";
    internal const string AndroidQualityKey = "Display.AndroidQuality";
    internal const string AndroidRenderScaleKey = "Display.AndroidRenderScale";
    internal const int MinRenderScalePercent = 50;
    internal const int MaxRenderScalePercent = 100;
    private const int DefaultRenderScalePercent = 80;
    private static readonly string[] AndroidQualityNames = { "Mobile", "Mobile Balanced", "Mobile Low" };
    private static UniversalRenderPipelineAsset scaledPipeline;
    private static float originalScale;
    public static bool ShowFps { get; private set; }
    public static LightingMode Lighting { get; private set; }
    public static bool NormalMaps { get; private set; } = true;
    internal static AndroidQuality MobileQuality { get; private set; }
    internal static int RenderScalePercent { get; private set; } = DefaultRenderScalePercent;
    internal static bool SupportsAndroidQuality =>
#if UNITY_EDITOR
        UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.Android;
#else
        Application.platform == RuntimePlatform.Android;
#endif
    public static event Action Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        Changed = null;
        Application.quitting -= RestoreRenderScale;
        RestoreRenderScale();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        ShowFps = PlayerPrefs.GetInt(ShowFpsKey, 0) != 0;
        LoadGraphicsPreferences();
        ApplyAndroidQuality();
        Application.quitting += RestoreRenderScale;
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
        int quality = PlayerPrefs.GetInt(AndroidQualityKey, (int)AndroidQuality.High);
        MobileQuality = quality >= 0 && quality < AndroidQualityNames.Length ? (AndroidQuality)quality : AndroidQuality.High;
        RenderScalePercent = Mathf.Clamp(PlayerPrefs.GetInt(AndroidRenderScaleKey, DefaultRenderScalePercent),
            MinRenderScalePercent, MaxRenderScalePercent);
    }

    internal static void SetAndroidQuality(AndroidQuality quality)
    {
        if ((uint)quality >= AndroidQualityNames.Length)
            throw new ArgumentOutOfRangeException(nameof(quality));
        MobileQuality = quality;
        PlayerPrefs.SetInt(AndroidQualityKey, (int)quality);
        PlayerPrefs.Save();
    }

    internal static void ApplyAndroidQuality()
    {
        if (!SupportsAndroidQuality) return;
        int index = Array.IndexOf(QualitySettings.names, AndroidQualityNames[(int)MobileQuality]);
        if (index < 0)
        {
            Debug.LogError($"[GamePerformanceSettings] Missing quality level {MobileQuality}.");
            return;
        }
        if (index != QualitySettings.GetQualityLevel())
        {
            RestoreRenderScale();
            QualitySettings.SetQualityLevel(index, true);
        }
        if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset pipeline) return;
        if (scaledPipeline != pipeline)
        {
            RestoreRenderScale();
            scaledPipeline = pipeline;
            originalScale = pipeline.renderScale;
        }
        pipeline.renderScale = RenderScalePercent / 100f;
    }

    internal static void SetRenderScalePercent(int percent)
    {
        RenderScalePercent = Mathf.Clamp(percent, MinRenderScalePercent, MaxRenderScalePercent);
        PlayerPrefs.SetInt(AndroidRenderScaleKey, RenderScalePercent);
        PlayerPrefs.Save();
    }

    private static void RestoreRenderScale()
    {
        if (scaledPipeline != null) scaledPipeline.renderScale = originalScale;
        scaledPipeline = null;
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
