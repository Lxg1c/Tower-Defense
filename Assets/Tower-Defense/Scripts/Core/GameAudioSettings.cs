using System;
using UnityEngine;

/// <summary>Shared volume preferences for the music and effects channels.</summary>
public static class GameAudioSettings
{
    public const string MusicKey = "Audio.MusicVolume";
    public const string EffectsKey = "Audio.EffectsVolume";
    public const string MasterKey = "Audio.MasterVolume";
    public static float Master { get; private set; } = 1f;
    public static float Music { get; private set; } = 1f;
    public static float Effects { get; private set; } = 1f;
    public static event Action Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() => Changed = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Load()
    {
        Master = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterKey, 1f));
        Music = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicKey, 1f));
        Effects = Mathf.Clamp01(PlayerPrefs.GetFloat(EffectsKey, 1f));
        ApplyEffectsVolume();
        Changed?.Invoke();
    }

    public static void SetMaster(float value)
    {
        Master = Mathf.Clamp01(value);
        ApplyEffectsVolume();
        PlayerPrefs.SetFloat(MasterKey, Master);
        Changed?.Invoke();
    }

    public static void SetMusic(float value)
    {
        Music = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MusicKey, Music);
        Changed?.Invoke();
    }

    public static void SetEffects(float value)
    {
        Effects = Mathf.Clamp01(value);
        ApplyEffectsVolume();
        PlayerPrefs.SetFloat(EffectsKey, Effects);
        Changed?.Invoke();
    }

    public static void Save() => PlayerPrefs.Save();

    private static void ApplyEffectsVolume() => AudioListener.volume = Master * Effects;
}
