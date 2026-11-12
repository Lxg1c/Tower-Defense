using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>UI Toolkit presentation for the existing campaign and progression rules.</summary>
[ExecuteAlways]
[RequireComponent(typeof(UIDocument))]
public sealed class MenuToolkitView : MonoBehaviour
{
    [SerializeField] private MainMenu menu;
    [UnityEngine.Serialization.FormerlySerializedAs("legacyScreens")]
    [SerializeField] private MainMenuScreens screens;

    [SerializeField] private TowerLoadout guideLoadout;

    private enum PreviewPage { Campaign, Equipment, Settings, Talents, BuildingGuide }
    [SerializeField, Tooltip("Screen displayed in Game view outside Play Mode.")]
    private PreviewPage editorPreview = PreviewPage.Campaign;

    private Button[] skillButtons;
    private Button[] armorPreviews;
    private Button[] leftWeaponPreviews;
    private Button[] rightWeaponPreviews;
    private Button upgradeSkill;
    private VisualElement guidePage;
    private string selectedGuideBuilding = "Ratusha";
    private int selectedSkill;
    private int previewArmor;
    private int previewLeftWeapon;
    private int previewRightWeapon;
#if UNITY_EDITOR
    private PreviewPage shownEditorPreview;
    private readonly SkillTreeState editorProgress = new();
#endif

    private SkillTreeState Progress
    {
        get
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) return editorProgress;
#endif
            return PlayerProgression.State;
        }
    }
    private VisualElement root;
    private VisualElement homePage;
    private VisualElement settingsPage;
    private VisualElement skillsPage;
    private VisualElement loadoutPage;
    private Label levelTitle;
    private Label levelDescription;
    private Label wavePreview;
    private Label levelIndex;
    private Label levelAvailability;
    private Label homeMoney;
    private VisualElement levelArt;
    private VisualElement featuredLevelCard;
    private Button previousLevelButton;
    private Button nextLevelButton;
    private IVisualElementScheduledItem levelSlide;
    private int pendingLevelIndex;
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
    private Button standardModule;
    private Button overdriveModule;
    private const string RussianOption = "Русский";
    private const string EnglishOption = "English";
    private DropdownField languageDropdown;
    private Slider masterSlider;
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
    private static readonly string[] SkillKeys = { "vitality", "firepower", "rapid-fire" };
    private static readonly string[] SkillNames = { "Живучесть", "Огневая мощь", "Скорострельность" };
    private static readonly string[] SkillIcons = { "talent-vitality", "talent-firepower", "talent-rapid-fire" };
    private static readonly string[] SkillBonusKeys = { "vitality-bonus", "firepower-bonus", "rapid-bonus" };
    private static readonly string[] SkillBonuses =
        { "+15% к максимальному здоровью за ранг", "+10% к урону за ранг", "+8% к скорострельности за ранг" };

    // Equipment mockups are for menu comparison until equipment affects combat.
    private static readonly (string Key, string Name, float Armor, float Speed)[] ArmorExamples =
        { ("base-name", "Базовый", 20f, 3f), ("assault-name", "Штурмовой", 35f, 2.6f), ("scout-name", "Разведкорпус", 12f, 3.6f) };
    private static readonly (string Key, string Name, float Damage, float Rate)[] WeaponExamples =
        { ("standard-name", "Стандарт", 5f, 2f), ("heavy-name", "Тяжёлая", 8f, 1.4f), ("rapid-name", "Скорострельная", 4f, 2.6f) };

    private void OnEnable()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            RefreshEditorPreview();
            return;
        }
#endif
        if (bound || !FindElements()) return;
        BindActions();
        bound = true;
    }

    private bool FindElements()
    {
        var document = GetComponent<UIDocument>();
        if (menu == null || screens == null ||
            document.panelSettings == null || document.visualTreeAsset == null)
        {
            if (Application.isPlaying)
                Debug.LogError("[MenuToolkitView] Assign the menu, screens and UI Toolkit assets.", this);
            return false;
        }

        root = document.rootVisualElement;
        if (root?.Q("menuRoot") == null) return false;
        root.AddToClassList("menu-document-root");
        homePage = root.Q("homePage");
        settingsPage = root.Q("settingsPage");
        skillsPage = root.Q("skillsPage");
        loadoutPage = root.Q("loadoutPage");
        guidePage = root.Q("guidePage");
        levelTitle = root.Q<Label>("levelTitle");
        levelDescription = root.Q<Label>("levelDescription");
        wavePreview = root.Q<Label>("wavePreview");
        levelIndex = root.Q<Label>("levelIndex");
        levelAvailability = root.Q<Label>("levelAvailability");
        homeMoney = root.Q<Label>("homeMoney");
        levelArt = root.Q("levelArt");
        featuredLevelCard = root.Q(className: "campaign-featured");
        previousLevelButton = root.Q<Button>("previousLevelButton");
        nextLevelButton = root.Q<Button>("nextLevelButton");
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
        armorPreviews = FindButtons("baseArmorPreview", "assaultArmorPreview", "scoutArmorPreview");
        leftWeaponPreviews = FindButtons("leftStandardPreview", "leftHeavyPreview", "leftRapidPreview");
        rightWeaponPreviews = FindButtons("rightStandardPreview", "rightHeavyPreview", "rightRapidPreview");
        skillButtons = FindButtons("vitalitySkill", "firepowerSkill", "rapidFireSkill");
        upgradeSkill = root.Q<Button>("upgradeSkill");
        standardModule = root.Q<Button>("standardModule");
        overdriveModule = root.Q<Button>("overdriveModule");
        languageDropdown = root.Q<DropdownField>("languageDropdown");
        masterSlider = root.Q<Slider>("masterSlider");
        fpsLabel = root.Q<Label>("fpsLabel");
        fpsToggle = root.Q<Button>("fpsToggle");
        musicSlider = root.Q<Slider>("musicSlider");
        effectsSlider = root.Q<Slider>("effectsSlider");
        startButton = root.Q<Button>("startButton");
        difficultyButtons = new[] { root.Q<Button>("normalButton"), root.Q<Button>("hardButton"),
            root.Q<Button>("nightmareButton") };
        if (homePage == null || settingsPage == null || skillsPage == null || loadoutPage == null || guidePage == null ||
            levelTitle == null || levelDescription == null || wavePreview == null || moneyLabel == null ||
            levelIndex == null || levelAvailability == null || homeMoney == null || levelArt == null ||
            featuredLevelCard == null ||
            previousLevelButton == null || nextLevelButton == null ||
            loadoutMoney == null || loadoutPortrait == null || armorCatalog == null || weaponCatalog == null ||
            moduleCatalog == null || armorTab == null || weaponTab == null || moduleTab == null ||
            armorSlot == null || leftWeaponSlot == null || rightWeaponSlot == null || moduleSlot == null ||
            armorPreviews.Any(button => button == null) || leftWeaponPreviews.Any(button => button == null) ||
            rightWeaponPreviews.Any(button => button == null) || skillButtons.Any(button => button == null) ||
            upgradeSkill == null || standardModule == null || overdriveModule == null ||
            fpsLabel == null || fpsToggle == null || languageDropdown == null ||
            masterSlider == null || musicSlider == null ||
            effectsSlider == null || startButton == null || difficultyButtons.Any(button => button == null))
        {
            if (Application.isPlaying)
                Debug.LogError("[MenuToolkitView] Menu.uxml is missing a required named element.", this);
            return false;
        }
        languageDropdown.choices = new System.Collections.Generic.List<string> { RussianOption, EnglishOption };
        root.UnregisterCallback<GeometryChangedEvent>(RefreshLayout);
        root.RegisterCallback<GeometryChangedEvent>(RefreshLayout);
        return true;
    }

    private Button[] FindButtons(params string[] names) => names.Select(name => root.Q<Button>(name)).ToArray();

    private void RefreshLayout(GeometryChangedEvent evt)
    {
        var menuRoot = ((VisualElement)evt.currentTarget).Q("menuRoot");
        if (menuRoot == null) return;
        menuRoot.EnableInClassList("loadout-narrow", evt.newRect.width < 1180f);
        menuRoot.EnableInClassList("campaign-narrow", evt.newRect.width < 1100f);
        menuRoot.EnableInClassList("subpage-short", evt.newRect.height < 580f);
        ApplySafeArea(menuRoot, evt.newRect.size);
    }

    private static void ApplySafeArea(VisualElement menuRoot, Vector2 panelSize)
    {
        float width = UnityEngine.Device.Screen.width;
        float height = UnityEngine.Device.Screen.height;
        if (width <= 0f || height <= 0f) return;
        Rect safe = UnityEngine.Device.Screen.safeArea;
        menuRoot.style.paddingLeft = Mathf.Max(0f, safe.xMin) * panelSize.x / width;
        menuRoot.style.paddingRight = Mathf.Max(0f, width - safe.xMax) * panelSize.x / width;
        menuRoot.style.paddingTop = Mathf.Max(0f, height - safe.yMax) * panelSize.y / height;
        menuRoot.style.paddingBottom = Mathf.Max(0f, safe.yMin) * panelSize.y / height;
    }

    private void BindActions()
    {
        previousLevelButton.clicked += () => SelectAdjacentLevel(-1);
        nextLevelButton.clicked += () => SelectAdjacentLevel(1);
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
        for (int i = 0; i < armorPreviews.Length; i++)
        {
            int option = i;
            armorPreviews[i].clicked += () => SelectArmorPreview(option);
            leftWeaponPreviews[i].clicked += () => SelectWeaponPreview(true, option);
            rightWeaponPreviews[i].clicked += () => SelectWeaponPreview(false, option);
        }
        for (int i = 0; i < skillButtons.Length; i++)
        {
            int option = i;
            skillButtons[i].clicked += () =>
            {
                selectedSkill = option;
                RefreshSkills();
            };
        }
        upgradeSkill.clicked += () => PlayerProgression.TryUpgrade(Skills[selectedSkill]);
        standardModule.clicked += () => PlayerProgression.TryEquipWeaponModule(PlayerWeaponModule.Standard);
        overdriveModule.clicked += SelectOverdrive;
        root.Q<Button>("settingsButton").clicked += ShowSettings;
        root.Q<Button>("labButton").clicked += menu.StartGameplayLab;
        root.Q<Button>("guideButton").clicked += ShowGuide;
        root.Q<Button>("guideBack").clicked += ShowHome;
        root.Q<Button>("repeatHints").clicked += () =>
        {
            FirstLevelTutorial.QueueReplay();
            RefreshTutorialSettings();
        };
        root.Q<Button>("exitButton").clicked += menu.ExitGame;
        root.Q<Button>("settingsBack").clicked += ShowHome;
        root.Q<Button>("skillsBack").clicked += ShowHome;
        startButton.clicked += () =>
        {
            CompleteLevelSlide();
            menu.StartGame();
        };
        for (int i = 0; i < difficultyButtons.Length; i++)
        {
            int difficulty = i + 1;
            difficultyButtons[i].clicked += () =>
            {
                if (levelSlide != null) return;
                menu.SelectDifficulty(difficulty);
                RefreshLevel();
            };
        }
        masterSlider.RegisterValueChangedCallback(evt => GameAudioSettings.SetMaster(evt.newValue));
        musicSlider.RegisterValueChangedCallback(evt => GameAudioSettings.SetMusic(evt.newValue));
        effectsSlider.RegisterValueChangedCallback(evt => GameAudioSettings.SetEffects(evt.newValue));
        fpsToggle.clicked += GamePerformanceSettings.ToggleFps;
        languageDropdown.RegisterValueChangedCallback(evt =>
            GameLocalization.SelectLanguage(evt.newValue == EnglishOption ? "en" : "ru"));
        GameLocalization.Changed += RefreshLanguage;
        PlayerProgression.Changed += RefreshAll;
        GameAudioSettings.Changed += RefreshAudio;
        GamePerformanceSettings.Changed += RefreshFps;
    }

    private void Start()
    {
        if (!bound) return;
        RefreshLanguage();
        switch (screens.CurrentScreen)
        {
            case MainMenuScreens.Screen.Skills: ShowSkills(); break;
            case MainMenuScreens.Screen.Settings: ShowSettings(); break;
            default: ShowHome(); break;
        }
    }

    private void OnDisable()
    {
        if (!bound) return;
        StopLevelSlide();
        PlayerProgression.Changed -= RefreshAll;
        GameAudioSettings.Changed -= RefreshAudio;
        GamePerformanceSettings.Changed -= RefreshFps;
        GameLocalization.Changed -= RefreshLanguage;
        bound = false;
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            RefreshEditorPreview();
            return;
        }
#endif
        if (!bound || !GamePerformanceSettings.ShowFps) return;
        fpsElapsed += Time.unscaledDeltaTime;
        fpsFrames++;
        if (fpsElapsed < 0.25f) return;
        fpsLabel.text = $"FPS {Mathf.RoundToInt(fpsFrames / fpsElapsed)}";
        fpsElapsed = 0f;
        fpsFrames = 0;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying) UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
    }

    private void RefreshEditorPreview()
    {
        var documentRoot = GetComponent<UIDocument>().rootVisualElement;
        if (documentRoot?.Q("menuRoot") == null) return;
        bool needsInitialization = root != documentRoot || levelTitle != documentRoot.Q<Label>("levelTitle");
        if (!needsInitialization && shownEditorPreview == editorPreview) return;
        if (needsInitialization && !FindElements()) return;

        shownEditorPreview = editorPreview;
        if (editorProgress.Money == 0) editorProgress.RecordDefeatReward(1250);
        RefreshLevel();
        RefreshSkills();
        RefreshLoadout();
        RefreshGuide();
        RefreshTutorialSettings();
        RefreshVolumeControl(masterSlider, "master", 1f);
        RefreshVolumeControl(musicSlider, "music", .7f);
        RefreshVolumeControl(effectsSlider, "effects", .85f);
        languageDropdown.SetValueWithoutNotify(RussianOption);
        fpsLabel.AddToClassList("is-hidden");
        Show(editorPreview switch
        {
            PreviewPage.Equipment => loadoutPage,
            PreviewPage.Settings => settingsPage,
            PreviewPage.Talents => skillsPage,
            PreviewPage.BuildingGuide => guidePage,
            _ => homePage
        });
    }
#endif

    private void RefreshLanguage()
    {
        GameLocalization.ApplyTo(root);
        languageDropdown.SetEnabled(GameLocalization.IsReady);
        languageDropdown.SetValueWithoutNotify(GameLocalization.Language == "en" ? EnglishOption : RussianOption);
        RefreshAll();
    }

    private void RefreshAll()
    {
        RefreshLevel();
        RefreshSkills();
        RefreshLoadout();
        RefreshGuide();
        RefreshTutorialSettings();
        RefreshAudio();
        RefreshFps();
    }

    private void RefreshLevel()
    {
        int index = menu.SelectedIndex;
        var level = menu.GetLevel(index);
        homeMoney.text = Progress.Money.ToString();
        bool canStart = Application.isPlaying ? menu.CanStartSelected : level != null;
        levelTitle.text = LevelTitle(level, index);
        levelDescription.text = LevelDescription(level);
        levelIndex.text = $"{index + 1:00} / {menu.LevelCount:00}";
        levelAvailability.text = canStart ? GameLocalization.Text("available", "ДОСТУПЕН") : GameLocalization.Text("unavailable", "НЕДОСТУПЕН");
        SetLevelArt(levelArt, index);
        RefreshAdjacentLevel(previousLevelButton, index - 1);
        RefreshAdjacentLevel(nextLevelButton, index + 1);
        for (int i = 0; i < difficultyButtons.Length; i++)
        {
            bool unlocked = level != null &&
                Progress.IsDifficultyUnlocked(level.progressionId, i + 1);
            difficultyButtons[i].SetEnabled(unlocked);
            difficultyButtons[i].EnableInClassList("is-selected", menu.SelectedDifficulty == i + 1);
            difficultyButtons[i].tooltip = unlocked ? "" : GameLocalization.Text("difficulty-locked", "Пройдите предыдущую сложность");
        }
        startButton.SetEnabled(canStart);
        wavePreview.EnableInClassList("is-hidden", !canStart);
        wavePreview.text = canStart ? FormatWaves(level.GetWaves(menu.SelectedDifficulty).Length) : "";
    }

    private void SelectAdjacentLevel(int offset)
    {
        int targetIndex = menu.SelectedIndex + offset;
        if (levelSlide != null || !IsCampaignIndex(targetIndex)) return;

        const float exitDuration = 0.12f;
        const float enterDuration = 0.18f;
        float travel = Mathf.Clamp(featuredLevelCard.resolvedStyle.width * 0.14f, 36f, 90f);
        float startedAt = Time.unscaledTime;
        pendingLevelIndex = targetIndex;

        levelSlide = featuredLevelCard.schedule.Execute(() =>
        {
            float elapsed = Time.unscaledTime - startedAt;
            if (elapsed < exitDuration)
            {
                float progress = elapsed / exitDuration;
                ApplyLevelSlide(-offset * travel * progress * progress, 1f - progress);
                return;
            }

            ApplyPendingLevel();
            float enter = Mathf.Clamp01((elapsed - exitDuration) / enterDuration);
            float remaining = 1f - enter;
            ApplyLevelSlide(offset * travel * remaining * remaining * remaining, 1f - remaining * remaining);
            if (enter >= 1f) StopLevelSlide();
        }).Every(16);
    }

    private void ApplyLevelSlide(float x, float opacity)
    {
        featuredLevelCard.style.translate = new Translate(x, 0);
        featuredLevelCard.style.opacity = opacity;
        float sideOffset = x * 0.28f;
        previousLevelButton.style.translate = new Translate(sideOffset, 0);
        nextLevelButton.style.translate = new Translate(sideOffset, 0);
    }

    private void ApplyPendingLevel()
    {
        if (menu.SelectedIndex == pendingLevelIndex) return;
        menu.SelectLevel(pendingLevelIndex);
        RefreshLevel();
    }

    private void CompleteLevelSlide()
    {
        if (levelSlide == null) return;
        ApplyPendingLevel();
        StopLevelSlide();
    }

    private void StopLevelSlide()
    {
        if (levelSlide == null) return;
        levelSlide.Pause();
        levelSlide = null;
        ApplyLevelSlide(0f, 1f);
    }

    private bool IsCampaignIndex(int index) => index >= 0 && index < menu.LevelCount;

    private void RefreshAdjacentLevel(Button button, int index)
    {
        bool visible = IsCampaignIndex(index);
        button.EnableInClassList("is-empty", !visible);
        button.SetEnabled(visible);
        if (!visible) return;

        var level = menu.GetLevel(index);
        button.Q<Label>("cardTitle").text = LevelTitle(level, index);
        button.Q<Label>("cardDescription").text = LevelDescription(level);
        button.Q<Label>("cardIndex").text = $"{index + 1:00} / {menu.LevelCount:00}";
        SetLevelArt(button.Q("cardArt"), index);
    }

    private static void SetLevelArt(VisualElement art, int index)
    {
        art.EnableInClassList("campaign-art-1", index == 0 || index > 2);
        art.EnableInClassList("campaign-art-2", index == 1);
        art.EnableInClassList("campaign-art-3", index == 2);
    }

    private static string LevelTitle(LevelDefinition level, int index) =>
        string.IsNullOrWhiteSpace(level?.displayName) ||
        string.Equals(level.displayName, $"Level {index + 1}", StringComparison.OrdinalIgnoreCase)
            ? GameLocalization.Format("level-number", "УРОВЕНЬ {0:00}", index + 1) : level.displayName;

    private static string LevelDescription(LevelDefinition level) => level?.progressionId switch
    {
        "level-1" => GameLocalization.Text("level-one", "Один выход врагов"),
        "level-2" => GameLocalization.Text("level-two", "Несколько подходов. Летающие враги"),
        "level-3" => GameLocalization.Text("level-three", "Бронированные враги"),
        _ => level?.description ?? GameLocalization.Text("level-missing", "Уровень ещё не настроен")
    };

    private static string FormatWaves(int count)
    {
        int lastDigit = count % 10;
        if (GameLocalization.Language == "en")
            return count == 1 ? GameLocalization.Format("wave-count-one", "{0} WAVE", count) :
                GameLocalization.Format("wave-count-many", "{0} WAVES", count);

        if (count % 100 is >= 11 and <= 14) return GameLocalization.Format("wave-count-many", "{0} ВОЛН", count);
        return lastDigit switch
        {
            1 => GameLocalization.Format("wave-count-one", "{0} ВОЛНА", count),
            >= 2 and <= 4 => GameLocalization.Format("wave-count-few", "{0} ВОЛНЫ", count),
            _ => GameLocalization.Format("wave-count-many", "{0} ВОЛН", count)
        };
    }

    private void RefreshSkills()
    {
        SkillTreeState state = Progress;
        moneyLabel.text = state.Money.ToString();
        for (int i = 0; i < Skills.Length; i++)
        {
            int rank = state.Rank(Skills[i]);
            bool unlocked = state.IsUnlocked(Skills[i]);
            skillButtons[i].Q<Label>("rank").text = unlocked ? GameLocalization.Format("rank", "Ранг {0} / {1}", rank, SkillTreeState.MaxRank) :
                GameLocalization.Text("locked", "Закрыто");
            skillButtons[i].EnableInClassList("is-locked", !unlocked);
            skillButtons[i].EnableInClassList("is-selected", i == selectedSkill);
        }

        PlayerSkill skill = Skills[selectedSkill];
        int currentRank = state.Rank(skill);
        int cost = state.NextCost(skill);
        bool available = state.IsUnlocked(skill);
        var detailIcon = root.Q("skillDetailIcon");
        for (int i = 0; i < SkillIcons.Length; i++)
            detailIcon.EnableInClassList(SkillIcons[i], i == selectedSkill);
        root.Q<Label>("skillDetailTitle").text = GameLocalization.Text(SkillKeys[selectedSkill], SkillNames[selectedSkill]);
        root.Q<Label>("skillDetailRank").text = GameLocalization.Format("rank-detail", "Ранг {0} из {1}", currentRank, SkillTreeState.MaxRank);
        root.Q<Label>("skillDetailBonus").text = GameLocalization.Text(SkillBonusKeys[selectedSkill], SkillBonuses[selectedSkill]);
        root.Q<Label>("skillDetailRequirement").text = selectedSkill == 0
            ? GameLocalization.Text("unlock-branches", "Первый ранг открывает обе ветки") :
            available ? "" : GameLocalization.Text("requires-vitality", "Требуется Живучесть I");

        var steps = root.Q("skillRankSteps");
        for (int i = 0; i < steps.childCount; i++)
            steps[i].EnableInClassList("is-earned", i < currentRank);

        root.Q<Label>("upgradeSkillText").text = !available ? GameLocalization.Text("locked", "Закрыто") :
            cost == 0 ? GameLocalization.Text("max-rank", "Максимальный ранг") : GameLocalization.Text("upgrade", "Улучшить");
        root.Q("upgradeSkillPrice").EnableInClassList("is-hidden", !available || cost == 0);
        root.Q<Label>("upgradeSkillCost").text = cost.ToString();
        upgradeSkill.SetEnabled(available && cost > 0 && state.Money >= cost);
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
        SkillTreeState state = Progress;
        loadoutMoney.text = state.Money.ToString();
        RefreshLoadoutStats();
        RefreshEquipmentLabels();
        bool overdrive = state.EquippedWeaponModule == PlayerWeaponModule.Overdrive;
        moduleSlot.text = overdrive ? GameLocalization.Text("overdrive-slot", "БОЕВОЙ МОДУЛЬ  ·  ФОРСАЖ") :
            GameLocalization.Text("standard-slot", "БОЕВОЙ МОДУЛЬ  ·  СТАНДАРТ");
        standardModule.text = overdrive ? GameLocalization.Text("equip", "ВЫБРАТЬ") : GameLocalization.Text("equipped", "НАДЕТО");
        standardModule.SetEnabled(overdrive);
        if (!state.IsWeaponModuleUnlocked(PlayerWeaponModule.Overdrive))
        {
            overdriveModule.text = GameLocalization.Format("buy", "КУПИТЬ  {0}", SkillTreeState.OverdriveCost);
            overdriveModule.SetEnabled(state.Money >= SkillTreeState.OverdriveCost);
        }
        else
        {
            overdriveModule.text = overdrive ? GameLocalization.Text("equipped", "НАДЕТО") : GameLocalization.Text("equip", "ВЫБРАТЬ");
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

    private void SelectArmorPreview(int option)
    {
        previewArmor = option;
        for (int i = 0; i < armorPreviews.Length; i++)
        {
            armorPreviews[i].EnableInClassList("is-selected", i == option);
        }
        RefreshEquipmentLabels();
        RefreshAppearancePreview();
        RefreshLoadoutStats();
    }

    private void SelectWeaponPreview(bool left, int option)
    {
        if (left) previewLeftWeapon = option;
        else previewRightWeapon = option;
        Button[] options = left ? leftWeaponPreviews : rightWeaponPreviews;
        for (int i = 0; i < options.Length; i++)
            options[i].EnableInClassList("is-selected", i == option);
        RefreshEquipmentLabels();
        ShowLoadoutCatalog("weapon", left);
        RefreshAppearancePreview();
        RefreshLoadoutStats();
    }

    private void RefreshEquipmentLabels()
    {
        var armor = ArmorExamples[previewArmor];
        var left = WeaponExamples[previewLeftWeapon];
        var right = WeaponExamples[previewRightWeapon];
        armorSlot.text = GameLocalization.Text("armor-slot", "КОРПУС") + "\n" + GameLocalization.Text(armor.Key, armor.Name);
        leftWeaponSlot.text = GameLocalization.Text("left-weapon", "ЛЕВАЯ ПУШКА") + "\n" + GameLocalization.Text(left.Key, left.Name);
        rightWeaponSlot.text = GameLocalization.Text("right-weapon", "ПРАВАЯ ПУШКА") + "\n" + GameLocalization.Text(right.Key, right.Name);
        for (int i = 0; i < armorPreviews.Length; i++)
            armorPreviews[i].Q<Label>(className: "preview-status").text = i == previewArmor
                ? GameLocalization.Text("shown", "Показано") : GameLocalization.Text("try-on", "Примерить");
    }

    private void RefreshAppearancePreview()
    {
        bool assault = previewArmor == 1;
        bool leftHeavy = previewLeftWeapon == 1;
        bool rightHeavy = previewRightWeapon == 1;
        string selected = assault ? "preview-armor" : "preview-base";
        if (leftHeavy && rightHeavy) selected = assault ? "preview-armor-both" : "preview-both";
        else if (leftHeavy) selected = assault ? "preview-armor-left" : "preview-left";
        else if (rightHeavy) selected = assault ? "preview-armor-right" : "preview-right";
        string[] variants = { "preview-base", "preview-armor", "preview-left", "preview-right", "preview-both",
            "preview-armor-left", "preview-armor-right", "preview-armor-both" };
        foreach (string variant in variants) loadoutPortrait.EnableInClassList(variant, variant == selected);
    }

    private void RefreshLoadoutStats()
    {
        SkillTreeState state = Progress;
        var armor = ArmorExamples[previewArmor];
        var baseArmor = ArmorExamples[0];
        var left = WeaponExamples[previewLeftWeapon];
        var right = WeaponExamples[previewRightWeapon];
        var standard = WeaponExamples[0];
        float health = 100f * state.HealthMultiplier;
        float damageBonus = state.DamageMultiplier * state.WeaponDamageMultiplier;
        float rateBonus = state.FireRateMultiplier * state.WeaponFireRateMultiplier;

        SetStat(0, health, health);
        SetStat(1, baseArmor.Armor, armor.Armor);
        SetStat(2, baseArmor.Speed, armor.Speed);
        SetStat(3, standard.Damage * damageBonus, (left.Damage + right.Damage) * .5f * damageBonus);
        SetStat(4, standard.Rate * rateBonus, (left.Rate + right.Rate) * .5f * rateBonus);
    }

    private void SetStat(int index, float current, float preview)
    {
        var delta = root.Q<Label>($"statDelta{index}");
        float difference = Mathf.Round((preview - current) * 100f) / 100f;
        root.Q<Label>($"statCurrent{index}").text = current.ToString("0.##");
        root.Q<Label>($"statPreview{index}").text = preview.ToString("0.##");
        delta.text = difference == 0f ? "—" : difference.ToString("+0.##;−0.##");
        delta.EnableInClassList("is-positive", difference > 0f);
        delta.EnableInClassList("is-negative", difference < 0f);
    }

    private void RefreshAudio()
    {
        RefreshVolumeControl(masterSlider, "master", GameAudioSettings.Master);
        RefreshVolumeControl(musicSlider, "music", GameAudioSettings.Music);
        RefreshVolumeControl(effectsSlider, "effects", GameAudioSettings.Effects);
    }

    private void RefreshVolumeControl(Slider slider, string id, float value)
    {
        slider.SetValueWithoutNotify(value);
        root.Q<Label>(id + "Value").text = $"{Mathf.RoundToInt(value * 100)}%";
        root.Q(id + "Fill").style.width = Length.Percent(value * 100f);
    }

    private void RefreshFps()
    {
        fpsToggle.EnableInClassList("is-on", GamePerformanceSettings.ShowFps);
        fpsLabel.EnableInClassList("is-hidden", !GamePerformanceSettings.ShowFps);
        fpsElapsed = 0f;
        fpsFrames = 0;
    }

    private void ShowHome() { screens.ShowHome(); Show(homePage); }
    private void ShowSettings() { screens.ShowSettings(); RefreshAudio(); Show(settingsPage); }
    private void ShowSkills() { screens.ShowSkills(); RefreshSkills(); Show(skillsPage); }
    private void ShowLoadout() { screens.ShowHome(); RefreshLoadout(); ShowLoadoutCatalog("armor"); Show(loadoutPage); }
    private void ShowGuide() { screens.ShowHome(); RefreshGuide(); Show(guidePage); }

    private void RefreshTutorialSettings()
    {
        bool queued = Application.isPlaying && FirstLevelTutorial.ReplayQueued;
        root.Q<Button>("repeatHints").SetEnabled(!queued);
        root.Q<Label>("repeatHintsStatus").EnableInClassList("is-hidden", !queued);
    }

    private void RefreshGuide()
    {
        bool preview = !Application.isPlaying;
        bool pulseNew = preview || BuildingGuide.PulseIsNew;
        root.Q("guideNewBadge").EnableInClassList("is-hidden", !pulseNew);

        var entries = root.Q<ScrollView>("guideEntries");
        entries.Clear();
        if (guideLoadout == null || guideLoadout.options == null) return;
        bool pulseAvailable = preview || BuildingGuide.PulseDiscovered;
        foreach (string name in new[] { "Ratusha", "Mine", "Cannon Turret", "Pulse" })
        {
            if (name == "Pulse" && !pulseAvailable) continue;
            var option = Array.Find(guideLoadout.options, item => item != null && item.displayName == name);
            if (option == null) continue;
            var row = new Button(() =>
            {
                selectedGuideBuilding = name;
                if (!preview && name == "Pulse") BuildingGuide.ReadPulse();
                RefreshGuide();
                entries.Q<Button>(name)?.Focus();
            }) { name = name, tooltip = GetBuildingName(option) };
            row.AddToClassList("guide-entry");
            row.EnableInClassList("is-selected", name == selectedGuideBuilding);
            var icon = new Image { sprite = option.icon, pickingMode = PickingMode.Ignore };
            icon.AddToClassList("guide-entry-icon");
            row.Add(icon);
            var labels = new VisualElement { pickingMode = PickingMode.Ignore };
            labels.AddToClassList("guide-entry-labels");
            var title = new Label(GetBuildingName(option)) { pickingMode = PickingMode.Ignore };
            title.AddToClassList("guide-entry-title");
            labels.Add(title);
            var role = new Label(GuideRole(name)) { pickingMode = PickingMode.Ignore };
            role.AddToClassList("guide-role");
            labels.Add(role);
            row.Add(labels);
            if (name == "Pulse" && pulseNew)
            {
                var badge = new Label(GameLocalization.Text("guide-new", "Новое")) { pickingMode = PickingMode.Ignore };
                badge.AddToClassList("guide-badge");
                row.Add(badge);
            }
            entries.Add(row);
            if (name != selectedGuideBuilding) continue;
            root.Q<Label>("guideTitle").text = GetBuildingName(option);
            root.Q<Image>("guideArt").sprite = option.icon;
            root.Q<Label>("guideRole").text = GuideRole(name);
            root.Q<Label>("guideDescription").text = name switch
            {
                "Ratusha" => GameLocalization.Text("guide-core-text", "Главная цель обороны. Если узел разрушат, забег закончится."),
                "Mine" => GameLocalization.Text("guide-mine-text", "Приносит кредиты после каждой пережитой волны. Построй заранее, чтобы усилить оборону позже."),
                "Cannon Turret" => GameLocalization.Text("guide-cannon-text", "Автоматически атакует наземных и летающих врагов в радиусе."),
                _ => GameplayToolkitView.GetBuildingDescription(option)
            };
            root.Q<Label>("guideCost").text = option.cost.ToString();
            root.Q<Label>("guideRequirement").text = option.requiredTownHallLevel switch
            {
                0 => GameLocalization.Text("guide-immediate", "Доступен сразу"),
                1 => GameLocalization.Text("guide-after-core", "После постройки узла"),
                _ => GameLocalization.Format("guide-core-level", "Узел {0} уровня", option.requiredTownHallLevel)
            };
        }
    }

    private static string GetBuildingName(TowerOption option) => GameplayToolkitView.GetBuildingName(option);

    private static string GuideRole(string name) => name switch
    {
        "Ratusha" => GameLocalization.Text("guide-role-core", "Защита"),
        "Mine" => GameLocalization.Text("guide-role-mine", "Доход"),
        "Cannon Turret" => GameLocalization.Text("guide-role-cannon", "Прямая защита"),
        _ => GameLocalization.Text("guide-role-pulse", "Контроль группы")
    };

    private void Show(VisualElement page)
    {
        if (page != homePage) CompleteLevelSlide();
        root.Q("menuRoot").EnableInClassList("subpage-open", page != homePage);
        root.Q("menuRoot").EnableInClassList("campaign-open", page == homePage);
        root.Q("menuRoot").EnableInClassList("loadout-open", page == loadoutPage);
        homePage.EnableInClassList("is-hidden", page != homePage);
        settingsPage.EnableInClassList("is-hidden", page != settingsPage);
        skillsPage.EnableInClassList("is-hidden", page != skillsPage);
        loadoutPage.EnableInClassList("is-hidden", page != loadoutPage);
        guidePage.EnableInClassList("is-hidden", page != guidePage);
    }
}
