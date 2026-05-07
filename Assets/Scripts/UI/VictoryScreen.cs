using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Shows a victory panel when WaveSpawner reports that all waves are complete.
/// </summary>
[DisallowMultipleComponent]
public class VictoryScreen : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject root;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;

    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Behaviour")]
    [SerializeField] private bool pauseGameOnShow = true;

    private WaveSpawner spawner;

    private void Awake()
    {
        if (root != null)
            root.SetActive(false);

        if (restartButton != null)
            restartButton.onClick.AddListener(Restart);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(GoToMainMenu);
    }

    private void OnEnable()
    {
        BindSpawner();
        if (spawner == null)
            Invoke(nameof(BindSpawner), 0.1f);
    }

    private void OnDisable()
    {
        if (spawner != null)
            spawner.onAllWavesCompleted.RemoveListener(Show);
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }

    public void Show()
    {
        if (root != null)
            root.SetActive(true);

        if (pauseGameOnShow)
            Time.timeScale = 0f;
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        Scene scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.buildIndex);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void BindSpawner()
    {
        if (spawner != null)
            spawner.onAllWavesCompleted.RemoveListener(Show);

        spawner = WaveSpawner.Instance;
        if (spawner == null)
            return;

        spawner.onAllWavesCompleted.AddListener(Show);

        if (spawner.CurrentPhase == WaveSpawner.Phase.AllCompleted)
            Show();
    }
}
