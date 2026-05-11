using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public abstract class MenuScreenBase : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] protected GameObject root;
    [SerializeField] protected Button restartButton;
    [SerializeField] protected Button mainMenuButton;

    [Header("Scenes")]
    [SerializeField] protected string mainMenuSceneName = "MainMenu";

    protected virtual void Awake()
    {
        Hide();

        if (restartButton != null)
            restartButton.onClick.AddListener(Restart);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(GoToMainMenu);
    }

    protected virtual void OnDestroy()
    {
        Time.timeScale = 1f;
    }

    public virtual void Show()
    {
        if (root != null)
            root.SetActive(true);
    }

    public virtual void Hide()
    {
        if (root != null)
            root.SetActive(false);
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
}
