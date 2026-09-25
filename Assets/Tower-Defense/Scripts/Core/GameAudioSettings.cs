using System;
using UnityEngine;

/// <summary>Music bypasses listener volume; every other sound uses the effects level.</summary>
public static class GameAudioSettings
{
    public const string MusicKey = "Audio.MusicVolume";
    public const string EffectsKey = "Audio.EffectsVolume";
    public static float Music { get; private set; } = 1f;
    public static float Effects { get; private set; } = 1f;
    public static event Action Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() => Changed = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Load()
    {
        Music = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicKey, 1f));
        Effects = Mathf.Clamp01(PlayerPrefs.GetFloat(EffectsKey, 1f));
        AudioListener.volume = Effects;
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
        AudioListener.volume = Effects;
        PlayerPrefs.SetFloat(EffectsKey, Effects);
        Changed?.Invoke();
    }

    public static void Save() => PlayerPrefs.Save();
}
