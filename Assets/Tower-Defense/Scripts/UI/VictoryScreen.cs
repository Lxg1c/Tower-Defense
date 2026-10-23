using UnityEngine;

[DisallowMultipleComponent]
public class VictoryScreen : MenuScreenBase
{
    [SerializeField] private WaveSpawner spawner;
    [SerializeField] private bool pauseGameOnShow = true;
    [SerializeField] private LevelSetup levelSetup;
    [SerializeField] private UnityEngine.UI.Button nextLevelButton;
    [SerializeField] private UnityEngine.UI.Button skillTreeButton;
    [SerializeField] private TMPro.TMP_Text skillRewardLabel;

    protected override void Awake()
    {
        base.Awake();
        if (skillTreeButton != null) skillTreeButton.onClick.AddListener(OpenSkills);
    }

    protected override void OnDestroy()
    {
        if (skillTreeButton != null) skillTreeButton.onClick.RemoveListener(OpenSkills);
        base.OnDestroy();
    }

    public void OpenSkills()
    {
        PlayerProgression.RequestSkillsScreen();
        GoToMainMenu();
    }

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
        if (spawner != null && spawner.CurrentPhase == WaveSpawner.Phase.AllCompleted &&
            levelSetup != null && levelSetup.CurrentLevel != null)
        {
            levelSetup.TryAwardVictory();
            if (skillRewardLabel != null)
                skillRewardLabel.text = $"+{levelSetup.VictoryMoneyReward} MONEY";
        }
        base.Show();
        if (nextLevelButton != null) nextLevelButton.gameObject.SetActive(CanPlayNextLevel);
        if (pauseGameOnShow) Time.timeScale = 0f;
    }
}
