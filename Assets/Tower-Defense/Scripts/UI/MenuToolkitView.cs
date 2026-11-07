using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>UI Toolkit presentation for the existing campaign and progression rules.</summary>
[RequireComponent(typeof(UIDocument))]
public sealed class MenuToolkitView : MonoBehaviour
{
    [SerializeField] private Canvas legacyCanvas;
    [SerializeField] private MainMenu menu;
    [SerializeField] private MainMenuScreens legacyScreens;

    private readonly List<Button> levelButtons = new();
    private readonly List<Button> skillButtons = new();
    private VisualElement root;
    private VisualElement homePage;
    private VisualElement levelsPage;
    private VisualElement settingsPage;
    private VisualElement skillsPage;
    private VisualElement loadoutPage;
    private Label levelTitle;
    private Label levelDescription;
    private Label wavePreview;
    private Label moneyLabel;
    private Label loadoutMoney;
    private Label equippedModule;
    private Label moduleDamage;
    private Label moduleFireRate;
    private Button standardModule;
    private Button overdriveModule;
    private Label musicValue;
    private Label effectsValue;
    private Label fpsLabel;
    private Button fpsToggle;
    private Slider musicSlider;
    private Slider effectsSlider;
    private Button startButton;
    private Button[] difficultyButtons;
    private bool bound;
    private float fpsElapsed;
    private int fpsFrames;

    private static readonly PlayerSkill[] Skills =
        { PlayerSkill.Vitality, PlayerSkill.Firepower, PlayerSkill.RapidFire };
    private static readonly string[] SkillNames = { "VITALITY", "FIREPOWER", "RAPID FIRE" };
    private static readonly string[] SkillBonuses =
        { "+15% MAX HP", "+10% SHOT DAMAGE", "+8% FIRE RATE" };

    private void OnEnable()
    {
        if (bound) return;
        var document = GetComponent<UIDocument>();
        if (legacyCanvas == null || menu == null || legacyScreens == null ||
            document.panelSettings == null || document.visualTreeAsset == null)
        {
            Debug.LogError("[MenuToolkitView] Assign the legacy canvas, menu, screens and UI Toolkit assets.", this);
            enabled = false;
            return;
        }

        root = document.rootVisualElement;
        root.AddToClassList("menu-document-root");
        homePage = root.Q("homePage");
        levelsPage = root.Q("levelsPage");
        settingsPage = root.Q("settingsPage");
        skillsPage = root.Q("skillsPage");
        loadoutPage = root.Q("loadoutPage");
        levelTitle = root.Q<Label>("levelTitle");
        levelDescription = root.Q<Label>("levelDescription");
        wavePreview = root.Q<Label>("wavePreview");
        moneyLabel = root.Q<Label>("moneyLabel");
        loadoutMoney = root.Q<Label>("loadoutMoney");
        equippedModule = root.Q<Label>("equippedModule");
        moduleDamage = root.Q<Label>("moduleDamage");
        moduleFireRate = root.Q<Label>("moduleFireRate");
        standardModule = root.Q<Button>("standardModule");
        overdriveModule = root.Q<Button>("overdriveModule");
        musicValue = root.Q<Label>("musicValue");
        effectsValue = root.Q<Label>("effectsValue");
        fpsLabel = root.Q<Label>("fpsLabel");
        fpsToggle = root.Q<Button>("fpsToggle");
        musicSlider = root.Q<Slider>("musicSlider");
        effectsSlider = root.Q<Slider>("effectsSlider");
        startButton = root.Q<Button>("startButton");
        difficultyButtons = new[] { root.Q<Button>("normalButton"), root.Q<Button>("hardButton"),
            root.Q<Button>("nightmareButton") };
        if (homePage == null || levelsPage == null || settingsPage == null || skillsPage == null || loadoutPage == null ||
            levelTitle == null || levelDescription == null || wavePreview == null || moneyLabel == null ||
            loadoutMoney == null || equippedModule == null || moduleDamage == null || moduleFireRate == null ||
            standardModule == null || overdriveModule == null || root.Q<Button>("loadoutButton") == null ||
            root.Q<Button>("loadoutBack") == null ||
            musicValue == null || effectsValue == null || fpsLabel == null || fpsToggle == null ||
            musicSlider == null || effectsSlider == null ||
            startButton == null || difficultyButtons.Any(button => button == null) ||
            root.Q<VisualElement>("levelList") == null || root.Q<VisualElement>("skillList") == null)
        {
            Debug.LogError("[MenuToolkitView] Menu.uxml is missing a required named element.", this);
            enabled = false;
            return;
        }

        root.Q<Button>("playButton").clicked += ShowLevels;
        root.Q<Button>("skillsButton").clicked += ShowSkills;
        root.Q<Button>("loadoutButton").clicked += ShowLoadout;
        root.Q<Button>("loadoutBack").clicked += ShowHome;
        standardModule.clicked += () => PlayerProgression.TryEquipWeaponModule(PlayerWeaponModule.Standard);
        overdriveModule.clicked += SelectOverdrive;
        root.Q<Button>("settingsButton").clicked += ShowSettings;
        root.Q<Button>("labButton").clicked += menu.StartGameplayLab;
        root.Q<Button>("exitButton").clicked += menu.ExitGame;
        root.Q<Button>("levelsBack").clicked += ShowHome;
        root.Q<Button>("settingsBack").clicked += ShowHome;
        root.Q<Button>("skillsBack").clicked += ShowHome;
        startButton.clicked += menu.StartGame;
        for (int i = 0; i < difficultyButtons.Length; i++)
        {
            int difficulty = i + 1;
            difficultyButtons[i].clicked += () => { menu.SelectDifficulty(difficulty); RefreshLevel(); };
        }
        musicSlider.RegisterValueChangedCallback(evt => GameAudioSettings.SetMusic(evt.newValue));
        effectsSlider.RegisterValueChangedCallback(evt => GameAudioSettings.SetEffects(evt.newValue));
        fpsToggle.clicked += GamePerformanceSettings.ToggleFps;
        BuildLevels();
        BuildSkills();
        PlayerProgression.Changed += RefreshAll;
        GameAudioSettings.Changed += RefreshAudio;
        GamePerformanceSettings.Changed += RefreshFps;
        bound = true;
    }

    private void Start()
    {
        if (!bound) return;
        RefreshAll();
        switch (legacyScreens.CurrentScreen)
        {
            case MainMenuScreens.Screen.Skills: ShowSkills(); break;
            case MainMenuScreens.Screen.Levels: ShowLevels(); break;
            case MainMenuScreens.Screen.Settings: ShowSettings(); break;
            default: ShowHome(); break;
        }
        legacyCanvas.enabled = false;
    }

    private void OnDisable()
    {
        if (!bound) return;
        PlayerProgression.Changed -= RefreshAll;
        GameAudioSettings.Changed -= RefreshAudio;
        GamePerformanceSettings.Changed -= RefreshFps;
        bound = false;
    }

    private void Update()
    {
        if (!bound || !GamePerformanceSettings.ShowFps) return;
        fpsElapsed += Time.unscaledDeltaTime;
        fpsFrames++;
        if (fpsElapsed < 0.25f) return;
        fpsLabel.text = $"FPS {Mathf.RoundToInt(fpsFrames / fpsElapsed)}";
        fpsElapsed = 0f;
        fpsFrames = 0;
    }

    private void BuildLevels()
    {
        var list = root.Q<VisualElement>("levelList");
        for (int i = 0; i < menu.LevelCount; i++)
        {
            int index = i;
            var button = new Button(() => { menu.SelectLevel(index); RefreshLevel(); })
            {
                text = $"{i + 1:00}"
            };
            button.AddToClassList("level-button");
            list.Add(button);
            levelButtons.Add(button);
        }
    }

    private void BuildSkills()
    {
        var list = root.Q<VisualElement>("skillList");
        for (int i = 0; i < Skills.Length; i++)
        {
            int index = i;
            var card = new VisualElement();
            card.AddToClassList("skill-card");
            var art = new VisualElement();
            art.AddToClassList("skill-art");
            art.AddToClassList(i == 0 ? "skill-art-vitality" :
                i == 1 ? "skill-art-firepower" : "skill-art-rapidfire");
            card.Add(art);
            var name = new Label(SkillNames[i]);
            name.AddToClassList("skill-name");
            card.Add(name);
            var copy = new Label(SkillBonuses[i] + " PER RANK");
            copy.AddToClassList("skill-copy");
            card.Add(copy);
            var button = new Button(() => PlayerProgression.TryUpgrade(Skills[index]));
            button.AddToClassList("skill-button");
            card.Add(button);
            list.Add(card);
            skillButtons.Add(button);
        }
    }

    private void RefreshAll()
    {
        RefreshLevel();
        RefreshSkills();
        RefreshLoadout();
        RefreshAudio();
        RefreshFps();
    }

    private void RefreshLevel()
    {
        for (int i = 0; i < levelButtons.Count; i++)
            levelButtons[i].EnableInClassList("is-selected", i == menu.SelectedIndex);
        var level = menu.GetLevel(menu.SelectedIndex);
        if (level == null)
        {
            levelTitle.text = $"LEVEL {menu.SelectedIndex + 1:00}";
            levelDescription.text = "This operation is not configured yet.";
            wavePreview.text = "";
            startButton.SetEnabled(false);
            return;
        }
        levelTitle.text = level.displayName.ToUpperInvariant();
        levelDescription.text = level.description;
        for (int i = 0; i < difficultyButtons.Length; i++)
        {
            bool unlocked = PlayerProgression.State.IsDifficultyUnlocked(level.progressionId, i + 1);
            difficultyButtons[i].SetEnabled(unlocked);
            difficultyButtons[i].EnableInClassList("is-selected", menu.SelectedDifficulty == i + 1);
            difficultyButtons[i].text = (i == 0 ? "NORMAL" : i == 1 ? "HARD" : "NIGHTMARE") +
                (unlocked ? "" : "  LOCKED");
        }
        startButton.SetEnabled(menu.CanStartSelected);
        try
        {
            var waves = level.GetWaves(menu.SelectedDifficulty);
            int enemies = waves.Sum(w => w.spawnGroups.Sum(g => g.entries.Sum(e => e.count)));
            wavePreview.text = $"{waves.Length} WAVES   •   {enemies} ENEMIES\n" +
                $"{level.GetStartingCoins(menu.SelectedDifficulty)} STARTING COINS\n" +
                $"WIN REWARD   +{level.GetMoneyReward(menu.SelectedDifficulty)} MONEY";
        }
        catch (ArgumentException exception) { wavePreview.text = exception.Message; }
    }

    private void RefreshSkills()
    {
        SkillTreeState state = PlayerProgression.State;
        moneyLabel.text = $"AVAILABLE MONEY   {state.Money}";
        for (int i = 0; i < Skills.Length; i++)
        {
            int rank = state.Rank(Skills[i]);
            int cost = state.NextCost(Skills[i]);
            bool unlocked = state.IsUnlocked(Skills[i]);
            skillButtons[i].text = cost == 0 ? $"MAX RANK  {rank}" :
                unlocked ? $"RANK {rank}/{SkillTreeState.MaxRank}   •   {cost}" : "LOCKED";
            skillButtons[i].SetEnabled(unlocked && cost > 0 && state.Money >= cost);
        }
    }

    private void SelectOverdrive()
    {
        if (!PlayerProgression.State.IsWeaponModuleUnlocked(PlayerWeaponModule.Overdrive))
        {
            if (!PlayerProgression.TryPurchaseOverdrive()) return;
        }
        PlayerProgression.TryEquipWeaponModule(PlayerWeaponModule.Overdrive);
    }

    private void RefreshLoadout()
    {
        SkillTreeState state = PlayerProgression.State;
        loadoutMoney.text = $"◆  {state.Money}";
        bool overdrive = state.EquippedWeaponModule == PlayerWeaponModule.Overdrive;
        equippedModule.text = overdrive ? "OVERDRIVE" : "STANDARD";
        moduleDamage.text = $"×{state.WeaponDamageMultiplier:0.##}";
        moduleFireRate.text = $"×{state.WeaponFireRateMultiplier:0.##}";
        standardModule.text = overdrive ? "EQUIP" : "EQUIPPED";
        standardModule.SetEnabled(overdrive);
        if (!state.IsWeaponModuleUnlocked(PlayerWeaponModule.Overdrive))
        {
            overdriveModule.text = $"BUY  {SkillTreeState.OverdriveCost}";
            overdriveModule.SetEnabled(state.Money >= SkillTreeState.OverdriveCost);
        }
        else
        {
            overdriveModule.text = overdrive ? "EQUIPPED" : "EQUIP";
            overdriveModule.SetEnabled(!overdrive);
        }
    }

    private void RefreshAudio()
    {
        musicSlider.SetValueWithoutNotify(GameAudioSettings.Music);
        effectsSlider.SetValueWithoutNotify(GameAudioSettings.Effects);
        musicValue.text = $"{Mathf.RoundToInt(GameAudioSettings.Music * 100)}%";
        effectsValue.text = $"{Mathf.RoundToInt(GameAudioSettings.Effects * 100)}%";
    }

    private void RefreshFps()
    {
        fpsToggle.text = GamePerformanceSettings.ShowFps ? "FPS DISPLAY: ON" : "FPS DISPLAY: OFF";
        fpsLabel.EnableInClassList("is-hidden", !GamePerformanceSettings.ShowFps);
        fpsElapsed = 0f;
        fpsFrames = 0;
    }

    private void ShowHome() { legacyScreens.ShowHome(); Show(homePage); }
    private void ShowLevels() { legacyScreens.ShowLevels(); RefreshLevel(); Show(levelsPage); }
    private void ShowSettings() { legacyScreens.ShowSettings(); RefreshAudio(); Show(settingsPage); }
    private void ShowSkills() { legacyScreens.ShowSkills(); RefreshSkills(); Show(skillsPage); }
    private void ShowLoadout() { legacyScreens.ShowHome(); RefreshLoadout(); Show(loadoutPage); }

    private void Show(VisualElement page)
    {
        root.Q("menuRoot").EnableInClassList("compact-header", page != homePage);
        homePage.EnableInClassList("is-hidden", page != homePage);
        levelsPage.EnableInClassList("is-hidden", page != levelsPage);
        settingsPage.EnableInClassList("is-hidden", page != settingsPage);
        skillsPage.EnableInClassList("is-hidden", page != skillsPage);
        loadoutPage.EnableInClassList("is-hidden", page != loadoutPage);
    }
}
