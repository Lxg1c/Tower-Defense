using UnityEngine;

[DisallowMultipleComponent]
public class PauseMenu : MenuScreenBase
{
    public enum ExitAction { None, Restart, MainMenu }

    [SerializeField] private WaveSpawner spawner;

    public bool IsPaused { get; private set; }
    public bool IsSettingsOpen { get; private set; }
    public ExitAction PendingExitAction { get; private set; }
    public bool IsConfirmingExit => PendingExitAction != ExitAction.None;

    public void Toggle()
    {
        if (IsPaused) Resume();
        else          Pause();
    }

    public void Pause()
    {
        if (spawner != null && (spawner.CurrentPhase == WaveSpawner.Phase.AllCompleted ||
            spawner.CurrentPhase == WaveSpawner.Phase.Defeated)) return;
        PendingExitAction = ExitAction.None;
        IsPaused = true;
        ShowPauseOptions();
        Show();
        Time.timeScale = 0f;
    }

    public void Resume()
    {
        PendingExitAction = ExitAction.None;
        IsPaused = false;
        IsSettingsOpen = false;
        Hide();
        Time.timeScale = 1f;
    }

    public void ShowSettings()
    {
        if (!IsPaused) return;
        PendingExitAction = ExitAction.None;
        IsSettingsOpen = true;
    }

    public void ShowPauseOptions()
    {
        IsSettingsOpen = false;
        GameAudioSettings.Save();
    }

    public override void Restart() => RequestExit(ExitAction.Restart);

    public override void GoToMainMenu() => RequestExit(ExitAction.MainMenu);

    public void CancelExit() => PendingExitAction = ExitAction.None;

    public void ConfirmExit()
    {
        ExitAction action = PendingExitAction;
        PendingExitAction = ExitAction.None;
        if (action == ExitAction.Restart) base.Restart();
        else if (action == ExitAction.MainMenu) base.GoToMainMenu();
    }

    private void RequestExit(ExitAction action)
    {
        if (spawner == null || spawner.CurrentPhase is WaveSpawner.Phase.AllCompleted or WaveSpawner.Phase.Defeated)
        {
            if (action == ExitAction.Restart) base.Restart();
            else base.GoToMainMenu();
            return;
        }

        if (!IsPaused) Pause();
        PendingExitAction = action;
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused && Application.platform == RuntimePlatform.Android && spawner != null &&
            spawner.CurrentPhase is WaveSpawner.Phase.Build or WaveSpawner.Phase.Combat)
            Pause();
    }
}
