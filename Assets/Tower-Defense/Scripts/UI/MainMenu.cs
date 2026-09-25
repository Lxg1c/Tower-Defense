using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class MainMenu : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "Level";
    [SerializeField] private LevelDefinition[] levels;
    [SerializeField] private UnityEngine.UI.Button levelButtonTemplate;
    [SerializeField] private Transform levelButtonContainer;
    [UnityEngine.Serialization.FormerlySerializedAs("packLabel")]
    [SerializeField] private TMP_Text levelLabel;
    [UnityEngine.Serialization.FormerlySerializedAs("packDescription")]
    [SerializeField] private TMP_Text levelDescription;
    [SerializeField] private TMP_Text wavePreview;
    [SerializeField] private UnityEngine.UI.Button startButton;
    private int selectedIndex;
    private UnityEngine.UI.Button[] buttons;
    public int SelectedIndex => selectedIndex;
    public int LevelCount => levels?.Length ?? 0;

    private void Awake()
    {
        Time.timeScale = 1f;
        RunSelection.Clear();
        if (levels == null || levels.Length == 0 || levelButtonTemplate == null ||
            levelButtonContainer == null || levelLabel == null || levelDescription == null ||
            wavePreview == null || startButton == null)
        {
            Debug.LogError("[MainMenu] Assign levels, level button template/container, labels and Start button.", this);
            if (startButton != null) startButton.interactable = false;
            enabled = false;
            return;
        }
        levelButtonTemplate.gameObject.SetActive(false);
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
        levelLabel.text = level.displayName + "  /  " + level.difficulty;
        levelDescription.text = level.description;
        try
        {
            level.Validate();
            int enemies = level.waves.Sum(w => w.spawnGroups.Sum(g => g.entries.Sum(e => e.count)));
            string types = string.Join("  /  ", level.waves.SelectMany(w => w.spawnGroups)
                .SelectMany(g => g.entries).Select(e => e.enemy.displayName).Distinct());
            wavePreview.text = $"<color=#2EE8E8>{level.waves.Length} WAVES</color>     {level.startingCoins} STARTING COINS\n\n" +
                $"{enemies} enemies\n{types}\n\nBuild your defenses. Survive every wave.";
            startButton.interactable = true;
        }
        catch (ArgumentException exception) { wavePreview.text = exception.Message; }
    }

    public void StartGame()
    {
        if (!enabled || !startButton.interactable) return;
        RunSelection.Select(levels[selectedIndex], gameSceneName);
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
