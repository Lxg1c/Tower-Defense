using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[DisallowMultipleComponent]
public class MainMenu : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "Level 1";
    [SerializeField] private LevelDefinition[] levels;
    [Header("Gameplay lab")]
    [SerializeField] private string gameplayLabSceneName = "GameplayLab";
    [SerializeField] private LevelDefinition gameplayLabLevel;
    private int selectedIndex;
    private int selectedDifficulty = 1;
    private bool canStartSelected;
    public int SelectedIndex => selectedIndex;
    public int SelectedDifficulty => selectedDifficulty;
    public bool CanStartSelected => canStartSelected;
    public int LevelCount => levels?.Length ?? 0;
    public LevelDefinition GetLevel(int index) =>
        levels != null && index >= 0 && index < levels.Length ? levels[index] : null;

    private void Awake()
    {
        Time.timeScale = 1f;
        RunSelection.Clear();
        if (levels == null || levels.Length == 0 || gameplayLabLevel == null ||
            string.IsNullOrWhiteSpace(gameplayLabSceneName))
        {
            Debug.LogError("[MainMenu] Assign campaign levels and the gameplay lab.", this);
            enabled = false;
            return;
        }
        SelectLevel(0);
    }

    public void SelectLevel(int index)
    {
        if (!enabled || index < 0 || index >= levels.Length) return;
        selectedIndex = index;
        selectedDifficulty = 1;
        RefreshSelection();
    }

    public void SelectDifficulty(int difficulty)
    {
        if (!enabled || difficulty < 1 || difficulty > 3) return;
        var level = levels[selectedIndex];
        if (level == null || !PlayerProgression.State.IsDifficultyUnlocked(level.progressionId, difficulty)) return;
        selectedDifficulty = difficulty;
        RefreshSelection();
    }

    private void RefreshSelection()
    {
        int index = selectedIndex;
        canStartSelected = false;
        var level = levels[index];
        if (level == null) return;
        try
        {
            level.Validate(difficulty: selectedDifficulty);
            level.GetWaves(selectedDifficulty);
            canStartSelected = true;
        }
        catch (ArgumentException) { }
    }

    internal void StartGame(PanelSettings panel, VisualTreeAsset loadingScreen, VisualElement menuRoot, string title)
    {
        if (!enabled || !canStartSelected || LevelLoadingScreen.IsLoading) return;
        if (!Application.CanStreamedLevelBeLoaded(gameSceneName))
        {
            Debug.LogError($"[MainMenu] Add {gameSceneName} to the build scene list.", this);
            return;
        }

        RunSelection.Select(levels[selectedIndex], gameSceneName, selectedDifficulty);
        Time.timeScale = 1f;
        LevelLoadingScreen.Begin(panel, loadingScreen, menuRoot, gameSceneName, title, selectedIndex, selectedDifficulty);
    }

    public void StartGameplayLab()
    {
        if (!enabled) return;
        RunSelection.Select(gameplayLabLevel, gameplayLabSceneName);
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameplayLabSceneName);
    }

    public void StartPerformanceBenchmark()
    {
        if (!enabled || levels[0] == null) return;
        RunSelection.SelectPerformanceBenchmark(levels[0], gameSceneName);
        Time.timeScale = 1f;
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
