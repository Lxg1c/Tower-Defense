using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>UI Toolkit presentation for the existing campaign and progression rules.</summary>
[RequireComponent(typeof(UIDocument))]
public sealed class MenuToolkitView : MonoBehaviour
{
    [SerializeField] private MainMenu menu;
    [UnityEngine.Serialization.FormerlySerializedAs("legacyScreens")]
    [SerializeField] private MainMenuScreens screens;

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
    private VisualElement loadoutPortrait;
    private VisualElement armorCatalog;
    private VisualElement weaponCatalog;
    private VisualElement moduleCatalog;
    private Button armorTab;
    private Button weaponTab;
    private Button moduleTab;
    private Button armorSlot;
    private Button leftWeaponSlot;
    private Button rightWeaponSlot;
    private Button moduleSlot;
    private Button baseArmorPreview;
    private Button assaultArmorPreview;
    private Button leftStandardPreview;
    private Button leftHeavyPreview;
    private Button rightStandardPreview;
    private Button rightHeavyPreview;
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
    private bool previewArmor;
    private bool previewLeftHeavy;
    private bool previewRightHeavy;
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
        if (menu == null || screens == null ||
            document.panelSettings == null || document.visualTreeAsset == null)
        {
            Debug.LogError("[MenuToolkitView] Assign the menu, screens and UI Toolkit assets.", this);
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
        loadoutPortrait = root.Q("loadoutPortrait");
        armorCatalog = root.Q("armorCatalog");
        weaponCatalog = root.Q("weaponCatalog");
        moduleCatalog = root.Q("moduleCatalog");
        armorTab = root.Q<Button>("armorTab");
        weaponTab = root.Q<Button>("weaponTab");
        moduleTab = root.Q<Button>("moduleTab");
        armorSlot = root.Q<Button>("armorSlot");
        leftWeaponSlot = root.Q<Button>("leftWeaponSlot");
        rightWeaponSlot = root.Q<Button>("rightWeaponSlot");
        moduleSlot = root.Q<Button>("moduleSlot");
        baseArmorPreview = root.Q<Button>("baseArmorPreview");
        assaultArmorPreview = root.Q<Button>("assaultArmorPreview");
        leftStandardPreview = root.Q<Button>("leftStandardPreview");
        leftHeavyPreview = root.Q<Button>("leftHeavyPreview");
        rightStandardPreview = root.Q<Button>("rightStandardPreview");
        rightHeavyPreview = root.Q<Button>("rightHeavyPreview");
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
            loadoutMoney == null || loadoutPortrait == null || armorCatalog == null || weaponCatalog == null ||
            moduleCatalog == null || armorTab == null || weaponTab == null || moduleTab == null ||
            armorSlot == null || leftWeaponSlot == null || rightWeaponSlot == null || moduleSlot == null ||
            baseArmorPreview == null || assaultArmorPreview == null || leftStandardPreview == null ||
            leftHeavyPreview == null || rightStandardPreview == null || rightHeavyPreview == null ||
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
        armorTab.clicked += () => ShowLoadoutCatalog("armor");
        weaponTab.clicked += () => ShowLoadoutCatalog("weapon");
        moduleTab.clicked += () => ShowLoadoutCatalog("module");
        armorSlot.clicked += () => ShowLoadoutCatalog("armor");
        leftWeaponSlot.clicked += () => ShowLoadoutCatalog("weapon", true);
        rightWeaponSlot.clicked += () => ShowLoadoutCatalog("weapon", false);
        moduleSlot.clicked += () => ShowLoadoutCatalog("module");
        baseArmorPreview.clicked += () => SelectArmorPreview(false);
        assaultArmorPreview.clicked += () => SelectArmorPreview(true);
        leftStandardPreview.clicked += () => SelectWeaponPreview(true, false);
        leftHeavyPreview.clicked += () => SelectWeaponPreview(true, true);
        rightStandardPreview.clicked += () => SelectWeaponPreview(false, false);
        rightHeavyPreview.clicked += () => SelectWeaponPreview(false, true);
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
        root.RegisterCallback<GeometryChangedEvent>(evt =>
            root.Q("menuRoot").EnableInClassList("loadout-narrow", evt.newRect.width < 1180f));
        bound = true;
    }

    private void Start()
    {
        if (!bound) return;
        RefreshAll();
        switch (screens.CurrentScreen)
        {
            case MainMenuScreens.Screen.Skills: ShowSkills(); break;
            case MainMenuScreens.Screen.Levels: ShowLevels(); break;
            case MainMenuScreens.Screen.Settings: ShowSettings(); break;
            default: ShowHome(); break;
        }
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
        loadoutMoney.text = $"◆  КРЕДИТЫ  {state.Money}";
        bool overdrive = state.EquippedWeaponModule == PlayerWeaponModule.Overdrive;
        moduleSlot.text = overdrive ? "БОЕВОЙ МОДУЛЬ  ·  ФОРСАЖ" : "БОЕВОЙ МОДУЛЬ  ·  СТАНДАРТ";
        standardModule.text = overdrive ? "ВЫБРАТЬ" : "НАДЕТО";
        standardModule.SetEnabled(overdrive);
        if (!state.IsWeaponModuleUnlocked(PlayerWeaponModule.Overdrive))
        {
            overdriveModule.text = $"КУПИТЬ  {SkillTreeState.OverdriveCost}";
            overdriveModule.SetEnabled(state.Money >= SkillTreeState.OverdriveCost);
        }
        else
        {
            overdriveModule.text = overdrive ? "НАДЕТО" : "ВЫБРАТЬ";
            overdriveModule.SetEnabled(!overdrive);
        }
    }

    private void ShowLoadoutCatalog(string category, bool leftWeapon = true)
    {
        armorCatalog.EnableInClassList("is-hidden", category != "armor");
        weaponCatalog.EnableInClassList("is-hidden", category != "weapon");
        moduleCatalog.EnableInClassList("is-hidden", category != "module");
        armorTab.EnableInClassList("is-selected", category == "armor");
        weaponTab.EnableInClassList("is-selected", category == "weapon");
        moduleTab.EnableInClassList("is-selected", category == "module");
        armorSlot.EnableInClassList("is-focused", category == "armor");
        leftWeaponSlot.EnableInClassList("is-focused", category == "weapon" && leftWeapon);
        rightWeaponSlot.EnableInClassList("is-focused", category == "weapon" && !leftWeapon);
        moduleSlot.EnableInClassList("is-focused", category == "module");
        root.Q("leftWeaponColumn").EnableInClassList("is-focused", leftWeapon);
        root.Q("rightWeaponColumn").EnableInClassList("is-focused", !leftWeapon);
    }

    private void SelectArmorPreview(bool assault)
    {
        previewArmor = assault;
        armorSlot.text = assault ? "КОРПУС  ·  ШТУРМОВОЙ" : "КОРПУС  ·  БАЗОВЫЙ";
        baseArmorPreview.EnableInClassList("is-selected", !assault);
        assaultArmorPreview.EnableInClassList("is-selected", assault);
        root.Q<Label>("baseArmorStatus").text = assault ? "ПРИМЕРИТЬ" : "ПОКАЗАНО";
        root.Q<Label>("assaultArmorStatus").text = assault ? "ПОКАЗАНО" : "ПРИМЕРИТЬ";
        RefreshAppearancePreview();
    }

    private void SelectWeaponPreview(bool left, bool heavy)
    {
        if (left) previewLeftHeavy = heavy;
        else previewRightHeavy = heavy;
        Button standard = left ? leftStandardPreview : rightStandardPreview;
        Button heavyButton = left ? leftHeavyPreview : rightHeavyPreview;
        standard.EnableInClassList("is-selected", !heavy);
        heavyButton.EnableInClassList("is-selected", heavy);
        (left ? leftWeaponSlot : rightWeaponSlot).text =
            (left ? "ЛЕВАЯ ПУШКА" : "ПРАВАЯ ПУШКА") + (heavy ? "  ·  ТЯЖЁЛАЯ" : "  ·  СТАНДАРТ");
        ShowLoadoutCatalog("weapon", left);
        RefreshAppearancePreview();
    }

    private void RefreshAppearancePreview()
    {
        string selected = previewArmor ? "preview-armor" : "preview-base";
        if (previewLeftHeavy && previewRightHeavy) selected = previewArmor ? "preview-armor-both" : "preview-both";
        else if (previewLeftHeavy) selected = previewArmor ? "preview-armor-left" : "preview-left";
        else if (previewRightHeavy) selected = previewArmor ? "preview-armor-right" : "preview-right";
        string[] variants = { "preview-base", "preview-armor", "preview-left", "preview-right", "preview-both",
            "preview-armor-left", "preview-armor-right", "preview-armor-both" };
        foreach (string variant in variants) loadoutPortrait.EnableInClassList(variant, variant == selected);
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

    private void ShowHome() { screens.ShowHome(); Show(homePage); }
    private void ShowLevels() { screens.ShowLevels(); RefreshLevel(); Show(levelsPage); }
    private void ShowSettings() { screens.ShowSettings(); RefreshAudio(); Show(settingsPage); }
    private void ShowSkills() { screens.ShowSkills(); RefreshSkills(); Show(skillsPage); }
    private void ShowLoadout() { screens.ShowHome(); RefreshLoadout(); ShowLoadoutCatalog("armor"); Show(loadoutPage); }

    private void Show(VisualElement page)
    {
        root.Q("menuRoot").EnableInClassList("compact-header", page != homePage);
        root.Q("menuRoot").EnableInClassList("loadout-open", page == loadoutPage);
        homePage.EnableInClassList("is-hidden", page != homePage);
        levelsPage.EnableInClassList("is-hidden", page != levelsPage);
        settingsPage.EnableInClassList("is-hidden", page != settingsPage);
        skillsPage.EnableInClassList("is-hidden", page != skillsPage);
        loadoutPage.EnableInClassList("is-hidden", page != loadoutPage);
    }
}
