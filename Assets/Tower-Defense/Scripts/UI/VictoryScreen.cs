using UnityEngine;

[DisallowMultipleComponent]
public class VictoryScreen : MenuScreenBase
{
    [SerializeField] private WaveSpawner spawner;
    [SerializeField] private bool pauseGameOnShow = true;
    [SerializeField] private LevelSetup levelSetup;
    [SerializeField] private UnityEngine.UI.Button nextLevelButton;

    public bool CanPlayNextLevel => spawner != null &&
        spawner.CurrentPhase == WaveSpawner.Phase.AllCompleted &&
        levelSetup != null && levelSetup.CurrentLevel != null &&
        levelSetup.CurrentLevel.nextLevel != null;

    public void PlayNextLevel()
    {
        if (!CanPlayNextLevel) return;
        string scene = gameObject.scene.name;
        if (!RunSelection.TrySelectNext(levelSetup.CurrentLevel, scene)) return;
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(scene);
    }

    private void OnEnable()
    {
        if (spawner == null)
        {
            Debug.LogError("[VictoryScreen] Assign the session spawner.", this);
            return;
        }
        spawner.onAllWavesCompleted.AddListener(Show);
        if (spawner.CurrentPhase == WaveSpawner.Phase.AllCompleted) Show();
    }

    private void OnDisable()
    {
        if (spawner != null) spawner.onAllWavesCompleted.RemoveListener(Show);
    }

    public override void Show()
    {
        base.Show();
        if (nextLevelButton != null) nextLevelButton.gameObject.SetActive(CanPlayNextLevel);
        if (pauseGameOnShow) Time.timeScale = 0f;
    }
}
