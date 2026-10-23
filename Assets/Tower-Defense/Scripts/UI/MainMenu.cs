using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class MainMenu : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "Level 1";
    [SerializeField] private LevelDefinition[] levels;
    [SerializeField] private UnityEngine.UI.Button levelButtonTemplate;
    [SerializeField] private Transform levelButtonContainer;
    [UnityEngine.Serialization.FormerlySerializedAs("packLabel")]
    [SerializeField] private TMP_Text levelLabel;
    [UnityEngine.Serialization.FormerlySerializedAs("packDescription")]
    [SerializeField] private TMP_Text levelDescription;
    [SerializeField] private TMP_Text wavePreview;
    [SerializeField] private UnityEngine.UI.Button startButton;
    [SerializeField] private UnityEngine.UI.Button[] difficultyButtons;
    private int selectedIndex;
    private int selectedDifficulty = 1;
    private UnityEngine.UI.Button[] buttons;
    public int SelectedIndex => selectedIndex;
    public int LevelCount => levels?.Length ?? 0;

    private void Awake()
    {
        Time.timeScale = 1f;
        RunSelection.Clear();
        if (levels == null || levels.Length == 0 || levelButtonTemplate == null ||
            levelButtonContainer == null || levelLabel == null || levelDescription == null ||
            wavePreview == null || startButton == null || difficultyButtons == null ||
            difficultyButtons.Length != 3 || difficultyButtons.Any(button => button == null))
        {
            Debug.LogError("[MainMenu] Assign levels, level button template/container, labels and Start button.", this);
            if (startButton != null) startButton.interactable = false;
            enabled = false;
            return;
        }
        levelButtonTemplate.gameObject.SetActive(false);
        for (int i = 0; i < difficultyButtons.Length; i++)
        {
            int difficulty = i + 1;
            difficultyButtons[i].onClick.AddListener(() => SelectDifficulty(difficulty));
        }
        buttons = new UnityEngine.UI.Button[levels.Length];
        for (int i = 0; i < levels.Length; i++)
        {
            int index = i;
            var button = Instantiate(levelButtonTemplate, levelButtonContainer);
            button.name = $"Level{i + 1}";
            button.GetComponentInChildren<TMP_Text>(true).text = (i + 1).ToString();
            button.onClick.AddListener(() => SelectLevel(index));
            button.gameObject.SetActive(true);
            buttons[i] = button;
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
        startButton.interactable = false;
        for (int i = 0; i < buttons.Length; i++)
        {
            bool selected = i == index;
            buttons[i].transition = UnityEngine.UI.Selectable.Transition.None;
            buttons[i].targetGraphic.material = null;
            buttons[i].targetGraphic.canvasRenderer.SetColor(Color.white);
            buttons[i].targetGraphic.color = selected ? new Color(.08f, .85f, .87f) : new Color(.045f, .12f, .15f);
            buttons[i].GetComponentInChildren<TMP_Text>(true).color = selected
                ? new Color(.015f, .05f, .07f) : new Color(.7f, .8f, .83f);
        }
        var level = levels[index];
        if (level == null)
        {
            levelLabel.text = $"LEVEL {index + 1}";
            levelDescription.text = "";
            wavePreview.text = "Level is not configured.";
            return;
        }
        levelLabel.text = level.displayName + "  /  " +
            (selectedDifficulty == 1 ? "NORMAL" : selectedDifficulty == 2 ? "HARD" : "NIGHTMARE");
        levelDescription.text = level.description;
        for (int i = 0; i < difficultyButtons.Length; i++)
        {
            bool unlocked = PlayerProgression.State.IsDifficultyUnlocked(level.progressionId, i + 1);
            bool selected = selectedDifficulty == i + 1;
            difficultyButtons[i].interactable = unlocked;
            string name = i == 0 ? "NORMAL" : i == 1 ? "HARD" : "NIGHTMARE";
            var label = difficultyButtons[i].GetComponentInChildren<TMP_Text>(true);
            label.text = unlocked ? name + (selected ? "  ✓" : "") : name + "\nLOCKED";
            label.color = selected ? new Color(.015f, .05f, .07f)
                : unlocked ? new Color(.80f, .92f, .94f) : new Color(.48f, .60f, .63f);
            difficultyButtons[i].targetGraphic.color = selected
                ? new Color(.08f, .85f, .87f) : new Color(.045f, .12f, .15f);
        }
        try
        {
            level.Validate(difficulty: selectedDifficulty);
            var waves = level.GetWaves(selectedDifficulty);
            int enemies = waves.Sum(w => w.spawnGroups.Sum(g => g.entries.Sum(e => e.count)));
            string types = string.Join("  /  ", waves.SelectMany(w => w.spawnGroups)
                .SelectMany(g => g.entries).Select(e => e.enemy.displayName).Distinct());
            wavePreview.text = $"<color=#2EE8E8>{waves.Length} WAVES</color>     {level.GetStartingCoins(selectedDifficulty)} STARTING COINS\n" +
                $"{enemies} enemies  ·  {types}\nWIN: +{level.GetMoneyReward(selectedDifficulty)} MONEY     BALANCE: {PlayerProgression.State.Money}";
            startButton.interactable = true;
        }
        catch (ArgumentException exception) { wavePreview.text = exception.Message; }
    }

    public void StartGame()
    {
        if (!enabled || !startButton.interactable) return;
        RunSelection.Select(levels[selectedIndex], gameSceneName, selectedDifficulty);
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
