using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Main menu controller. Hook button OnClick events to <see cref="StartGame"/>
/// and <see cref="ExitGame"/>.
/// </summary>
[DisallowMultipleComponent]
public class MainMenu : MonoBehaviour
{
    [Tooltip("Name of the gameplay scene (must be added to Build Settings).")]
    [SerializeField] private string gameSceneName = "SampleScene";

    public void StartGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
