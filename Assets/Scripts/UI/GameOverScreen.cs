using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Shown when the Base dies. Freezes time, displays a panel with a Restart button.
///
/// Wiring:
///   - Place this on a GameObject under your main Canvas with a panel child (root).
///   - Assign <see cref="root"/> to that panel (kept inactive at start).
///   - Assign <see cref="restartButton"/>.
///   - Hook Base.onDied → GameOverScreen.Show in the inspector.
/// </summary>
[DisallowMultipleComponent]
public class GameOverScreen : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject root;
    [SerializeField] private Button restartButton;

    private void Awake()
    {
        if (root != null) root.SetActive(false);
        if (restartButton != null) restartButton.onClick.AddListener(Restart);
    }

    private void OnDestroy()
    {
        // Defensive: if scene is unloading mid-pause, restore time scale.
        Time.timeScale = 1f;
    }

    /// <summary>Hook this to Base.onDied (UnityEvent).</summary>
    public void Show()
    {
        if (root != null) root.SetActive(true);
        Time.timeScale = 0f;
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        Scene s = SceneManager.GetActiveScene();
        SceneManager.LoadScene(s.buildIndex);
    }
}
