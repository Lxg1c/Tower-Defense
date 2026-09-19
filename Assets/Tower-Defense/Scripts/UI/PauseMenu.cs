using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PauseMenu : MenuScreenBase
{
    [Header("Pause Buttons")]
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button resumeButton;

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
        IsPaused = true;
        Show();
        Time.timeScale = 0f;
    }

    public void Resume()
    {
        IsPaused = false;
        Hide();
        Time.timeScale = 1f;
    }
}
