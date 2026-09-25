using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PauseMenu : MenuScreenBase
{
    [Header("Pause Buttons")]
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button resumeButton;
    [SerializeField] private GameObject menuContent;
    [SerializeField] private GameObject settingsContent;
    [SerializeField] private WaveSpawner spawner;

    public bool IsPaused { get; private set; }

    protected override void Awake()
    {
        base.Awake();

        if (pauseButton != null)
            pauseButton.onClick.AddListener(Pause);

        if (resumeButton != null)
            resumeButton.onClick.AddListener(Resume);
    }

    public void Toggle()
    {
        if (IsPaused) Resume();
        else          Pause();
    }

    public void Pause()
    {
        if (spawner != null && (spawner.CurrentPhase == WaveSpawner.Phase.AllCompleted ||
            spawner.CurrentPhase == WaveSpawner.Phase.Defeated)) return;
        IsPaused = true;
        ShowPauseOptions();
        Show();
        Time.timeScale = 0f;
    }

    public void Resume()
    {
        IsPaused = false;
        Hide();
        Time.timeScale = 1f;
    }

    public void ShowSettings()
    {
        if (!IsPaused) return;
        menuContent.SetActive(false);
        settingsContent.SetActive(true);
    }

    public void ShowPauseOptions()
    {
        settingsContent.SetActive(false);
        menuContent.SetActive(true);
        GameAudioSettings.Save();
    }
}
