using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Audio controls that also work while gameplay time is paused.</summary>
public sealed class AudioSettingsPanel : MonoBehaviour
{
    [SerializeField] private Slider music;
    [SerializeField] private Slider effects;
    [SerializeField] private TMP_Text musicValue;
    [SerializeField] private TMP_Text effectsValue;

    private void OnEnable()
    {
        music.onValueChanged.AddListener(GameAudioSettings.SetMusic);
        effects.onValueChanged.AddListener(GameAudioSettings.SetEffects);
        GameAudioSettings.Changed += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        music.onValueChanged.RemoveListener(GameAudioSettings.SetMusic);
        effects.onValueChanged.RemoveListener(GameAudioSettings.SetEffects);
        GameAudioSettings.Changed -= Refresh;
        GameAudioSettings.Save();
    }

    private void Refresh()
    {
        music.SetValueWithoutNotify(GameAudioSettings.Music);
        effects.SetValueWithoutNotify(GameAudioSettings.Effects);
        musicValue.text = $"{Mathf.RoundToInt(GameAudioSettings.Music * 100)}%";
        effectsValue.text = $"{Mathf.RoundToInt(GameAudioSettings.Effects * 100)}%";
    }
}
