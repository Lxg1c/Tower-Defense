using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Home, level selection and audio settings within the MainMenu scene.</summary>
public sealed class MainMenuScreens : MonoBehaviour
{
    [SerializeField] private GameObject home;
    [SerializeField] private GameObject levels;
    [SerializeField] private GameObject settings;
    [SerializeField] private Slider music;
    [SerializeField] private Slider effects;
    [SerializeField] private TMP_Text musicValue;
    [SerializeField] private TMP_Text effectsValue;

    private void Awake()
    {
        if (home == null || levels == null || settings == null || music == null || effects == null
            || musicValue == null || effectsValue == null)
        {
            Debug.LogError("[MainMenuScreens] Assign the three screens, audio sliders and value labels.", this);
            enabled = false;
            return;
        }
        FpsInterface.AddSettingsButton((RectTransform)music.transform.parent, musicValue.font);
        FpsInterface.EnsureOverlay(music.GetComponentInParent<Canvas>().rootCanvas, musicValue.font);
    }

    private void OnEnable()
    {
        if (!enabled) return;
        music.onValueChanged.AddListener(ChangeMusic);
        effects.onValueChanged.AddListener(ChangeEffects);
        GameAudioSettings.Changed += RefreshAudio;
        RefreshAudio();
    }

    private void Start() => ShowHome();

    private void OnDisable()
    {
        if (music != null) music.onValueChanged.RemoveListener(ChangeMusic);
        if (effects != null) effects.onValueChanged.RemoveListener(ChangeEffects);
        GameAudioSettings.Changed -= RefreshAudio;
        GameAudioSettings.Save();
    }

    public void ShowHome() => Show(home);
    public void ShowLevels() => Show(levels);
    public void ShowSettings() { RefreshAudio(); Show(settings); }

    private void Show(GameObject screen)
    {
        GameAudioSettings.Save();
        home.SetActive(screen == home);
        levels.SetActive(screen == levels);
        settings.SetActive(screen == settings);
    }

    private void ChangeMusic(float value) => GameAudioSettings.SetMusic(value);
    private void ChangeEffects(float value) => GameAudioSettings.SetEffects(value);
    private void RefreshAudio()
    {
        music.SetValueWithoutNotify(GameAudioSettings.Music);
        effects.SetValueWithoutNotify(GameAudioSettings.Effects);
        musicValue.text = $"{Mathf.RoundToInt(GameAudioSettings.Music * 100)}%";
        effectsValue.text = $"{Mathf.RoundToInt(GameAudioSettings.Effects * 100)}%";
    }
}
