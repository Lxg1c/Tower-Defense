using UnityEngine;
using UnityEngine.SceneManagement;

public abstract class MenuScreenBase : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] protected string mainMenuSceneName = "MainMenu";
    public bool IsVisible { get; private set; }

    protected virtual void Awake()
    {
        Hide();
    }

    protected virtual void OnDestroy()
    {
        Time.timeScale = 1f;
    }

    public virtual void Show() => IsVisible = true;

    public virtual void Hide() => IsVisible = false;

    public virtual void Restart()
    {
        Time.timeScale = 1f;
        Scene scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.buildIndex);
    }

    public virtual void GoToMainMenu()
    {
        GameAudioSettings.Save();
        RunSelection.Clear();
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
