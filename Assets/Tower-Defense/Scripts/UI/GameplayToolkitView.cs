using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Screen-space UI Toolkit view over the existing gameplay controllers.</summary>
[ExecuteAlways]
[RequireComponent(typeof(UIDocument))]
public sealed class GameplayToolkitView : MonoBehaviour
{
    [SerializeField] private ResponsiveGameplayHud responsiveHud;
    [SerializeField] private WaveSpawner spawner;
    [SerializeField] private PlayerWallet wallet;
    [SerializeField] private LevelTimer timer;
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private PlayerUltimate ultimate;
    [SerializeField] private PlayerHomeTeleportButton teleport;
    [SerializeField] private PauseMenu pauseMenu;
    [SerializeField] private VictoryScreen victory;
    [SerializeField] private GameOverScreen defeat;
    [SerializeField] private LevelSetup levelSetup;
    [SerializeField] private TowerSelectionModal buildModal;
    [SerializeField] private SquadOrderController orders;
    [SerializeField] private SlowAmmoInventory slowAmmo;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private CompanionTurretController companionTurrets;
#if UNITY_EDITOR
    private VisualElement editorPreviewRoot;
#endif

    private VisualElement root;
    private VisualElement touchControls;
    private VisualElement desktopActions;
    private VisualElement joystick;
    private VisualElement joystickSector;
    private VisualElement joystickKnob;
    private VisualElement buildPanel;
    private VisualElement orderLayer;
    private VisualElement screenOverlay;
    private VisualElement spawnPreview;
    private VisualElement coreAlert;
    private VisualElement pausePanel;
    private VisualElement audioPanel;
    private VisualElement exitConfirmPanel;
    private VisualElement victoryPanel;
    private VisualElement defeatPanel;
    private Label exitConfirmTitle;
    private Label exitConfirmDescription;
    private Button exitConfirmButton;
    private Label defeatReward;
    private ScrollView buildCards;
    private Label waveLabel;
    private Label wavePhaseLabel;
    private Label waveTotalLabel;
    private VisualElement waveSteps;
    private Label timerLabel;
    private Label enemiesLabel;
    private Label coinsLabel;
    private Label incomeLabel;
    private Label guideLabel;
    private Label coreHealthLabel;
    private Label slowEffect;
    private Label shieldEffect;
    private Label turretEffect;
    private Label mobileUltimateCharge;
    private Label desktopUltimateCharge;
    private Label fpsLabel;
    private Button fpsToggle;
    private float fpsElapsed;
    private int fpsFrames;
    private RadialProgressElement mobileUltimateRing;
    private RadialProgressElement desktopUltimateRing;
    private Button startWaveButton;
    private Button homeButton;
    private Button desktopHome;
    private Button rallyButton;
    private Button desktopRally;
    private Button ultimateButton;
    private Button desktopUltimate;
    private Slider masterSlider;
    private Slider musicSlider;
    private Slider effectsSlider;
    private bool audioOpen;
    private bool ultimatePointerHeld;
    private int lastModalRevision = -1;
    private bool bound;
    private Base observedCore;
    private float previousCoreHealth;
    private float coreAlertUntil;
    private readonly List<WaveSpawner.WavePreviewEntry> spawnEntries = new();
    private readonly List<SpawnMarker> spawnMarkers = new();
    private HealthBarManager healthBars;
    private int previewWaveIndex = -1;

    private sealed class SpawnMarker
    {
        public Transform Point;
        public VisualElement Visible;
        public VisualElement Edge;
        public Label Distance;
        public Label Arrow;
    }

    private void OnEnable()
    {
#if UNITY_EDITOR
        if (!Application.IsPlaying(gameObject))
        {
            RefreshEditorHudPreview();
            return;
        }
        ClearEditorHudPreview();
#endif
        if (bound) return;
        var document = GetComponent<UIDocument>();
        if (document.panelSettings == null || document.visualTreeAsset == null ||
            responsiveHud == null || spawner == null || wallet == null ||
            timer == null || movement == null || ultimate == null || teleport == null ||
            pauseMenu == null || victory == null || defeat == null || levelSetup == null ||
            buildModal == null || orders == null ||
            slowAmmo == null || playerHealth == null || companionTurrets == null)
        {
            Debug.LogError("[GameplayToolkitView] Assign all scene controllers and UI Toolkit assets.", this);
            enabled = false;
            return;
        }

        root = document.rootVisualElement;
        root.AddToClassList("game-document-root");
        coreAlert = root.Q("coreAlert");
        coreHealthLabel = root.Q<Label>("coreHealthLabel");
        touchControls = root.Q("touchControls");
        desktopActions = root.Q("desktopActions");
        joystick = root.Q("joystick");
        joystickSector = root.Q("joystickSector");
        joystickKnob = root.Q("joystickKnob");
        buildPanel = root.Q("buildPanel");
        orderLayer = root.Q("orderLayer");
        screenOverlay = root.Q("screenOverlay");
        spawnPreview = root.Q("spawnPreview");
        pausePanel = root.Q("pausePanel");
        audioPanel = root.Q("audioPanel");
        exitConfirmPanel = root.Q("exitConfirmPanel");
        victoryPanel = root.Q("victoryPanel");
        defeatPanel = root.Q("defeatPanel");
        exitConfirmTitle = root.Q<Label>("exitConfirmTitle");
        exitConfirmDescription = root.Q<Label>("exitConfirmDescription");
        exitConfirmButton = root.Q<Button>("exitConfirmButton");
        defeatReward = root.Q<Label>("defeatReward");
        buildCards = root.Q<ScrollView>("buildCards");
        waveLabel = root.Q<Label>("waveLabel");
        wavePhaseLabel = root.Q<Label>("wavePhaseLabel");
        waveTotalLabel = root.Q<Label>("waveTotalLabel");
        waveSteps = root.Q("waveSteps");
        timerLabel = root.Q<Label>("timerLabel");
        enemiesLabel = root.Q<Label>("enemiesLabel");
        coinsLabel = root.Q<Label>("coinsLabel");
        incomeLabel = root.Q<Label>("incomeLabel");
        guideLabel = root.Q<Label>("guideLabel");
        slowEffect = root.Q<Label>("slowEffect");
        shieldEffect = root.Q<Label>("shieldEffect");
        turretEffect = root.Q<Label>("turretEffect");
        mobileUltimateCharge = root.Q<Label>("ultimateCharge");
        desktopUltimateCharge = root.Q<Label>("desktopUltimateCharge");
        fpsLabel = root.Q<Label>("fpsLabel");
        fpsToggle = root.Q<Button>("fpsToggle");
        startWaveButton = root.Q<Button>("startWaveButton");
        homeButton = root.Q<Button>("homeButton");
        desktopHome = root.Q<Button>("desktopHome");
        rallyButton = root.Q<Button>("rallyButton");
        desktopRally = root.Q<Button>("desktopRally");
        ultimateButton = root.Q<Button>("ultimateButton");
        desktopUltimate = root.Q<Button>("desktopUltimate");
        masterSlider = root.Q<Slider>("masterSlider");
        musicSlider = root.Q<Slider>("musicSlider");
        effectsSlider = root.Q<Slider>("effectsSlider");
        if (touchControls == null || desktopActions == null || joystick == null || joystickSector == null ||
            joystickKnob == null || buildPanel == null ||
            orderLayer == null || screenOverlay == null || spawnPreview == null || pausePanel == null ||
            audioPanel == null || exitConfirmPanel == null || victoryPanel == null || defeatPanel == null ||
            exitConfirmTitle == null || exitConfirmDescription == null || exitConfirmButton == null ||
            defeatReward == null ||
            buildCards == null || waveLabel == null || wavePhaseLabel == null ||
            waveTotalLabel == null || waveSteps == null || timerLabel == null ||
            enemiesLabel == null || coinsLabel == null || incomeLabel == null ||
            guideLabel == null || coreAlert == null || coreHealthLabel == null ||
            slowEffect == null || shieldEffect == null ||
            turretEffect == null || mobileUltimateCharge == null || desktopUltimateCharge == null ||
            fpsLabel == null || fpsToggle == null ||
            startWaveButton == null || homeButton == null || desktopHome == null ||
            rallyButton == null || desktopRally == null ||
            ultimateButton == null || desktopUltimate == null || masterSlider == null ||
            musicSlider == null || effectsSlider == null)
        {
            Debug.LogError("[GameplayToolkitView] Gameplay.uxml is missing a required named element.", this);
            enabled = false;
            return;
        }

        ultimateButton.Q<RadialProgressElement>("ultimateChargeRing")?.RemoveFromHierarchy();
        desktopUltimate.Q<RadialProgressElement>("desktopUltimateChargeRing")?.RemoveFromHierarchy();
        mobileUltimateRing = CreateUltimateRing("ultimateChargeRing");
        desktopUltimateRing = CreateUltimateRing("desktopUltimateChargeRing");
        ultimateButton.Insert(1, mobileUltimateRing);
        desktopUltimate.Insert(1, desktopUltimateRing);

        root.Q<Button>("pauseButton").clicked += pauseMenu.Pause;
        root.Q<Button>("resumeButton").clicked += pauseMenu.Resume;
        root.Q<Button>("pauseSettings").clicked += OpenAudio;
        root.Q<Button>("audioBack").clicked += CloseAudio;
        root.Q<Button>("restartButton").clicked += pauseMenu.Restart;
        root.Q<Button>("pauseMenuButton").clicked += pauseMenu.GoToMainMenu;
        root.Q<Button>("exitCancelButton").clicked += pauseMenu.CancelExit;
        exitConfirmButton.clicked += pauseMenu.ConfirmExit;
        root.Q<Button>("nextLevelButton").clicked += victory.PlayNextLevel;
        root.Q<Button>("replayButton").clicked += victory.Restart;
        root.Q<Button>("victorySkills").clicked += victory.OpenSkills;
        root.Q<Button>("victoryMenu").clicked += victory.GoToMainMenu;
        root.Q<Button>("defeatMenu").clicked += defeat.GoToMainMenu;
        root.Q<Button>("defeatReplay").clicked += defeat.Restart;
        root.Q<Button>("orderCancel").clicked += orders.CancelOrder;
        startWaveButton.clicked += spawner.StartNextWave;
        homeButton.clicked += teleport.TeleportToHome;
        desktopHome.clicked += teleport.TeleportToHome;
        rallyButton.clicked += AllyCommand.ToggleFollowPlayer;
        desktopRally.clicked += AllyCommand.ToggleFollowPlayer;
        masterSlider.RegisterValueChangedCallback(evt => GameAudioSettings.SetMaster(evt.newValue));
        musicSlider.RegisterValueChangedCallback(evt => GameAudioSettings.SetMusic(evt.newValue));
        effectsSlider.RegisterValueChangedCallback(evt => GameAudioSettings.SetEffects(evt.newValue));
        fpsToggle.clicked += GamePerformanceSettings.ToggleFps;
        joystick.RegisterCallback<PointerDownEvent>(BeginMove);
        joystick.RegisterCallback<PointerMoveEvent>(Move);
        joystick.RegisterCallback<PointerUpEvent>(EndMove);
        joystick.RegisterCallback<PointerCaptureOutEvent>(_ => ResetMove());
        BindUltimate(ultimateButton);
        BindUltimate(desktopUltimate);
        buildModal.onOpened.AddListener(RefreshBuildCards);
        buildModal.onClosed.AddListener(ClearBuildCards);
        GameAudioSettings.Changed += RefreshAudio;
        GamePerformanceSettings.Changed += RefreshFps;
        GameLocalization.Changed += RefreshLanguage;
        healthBars = FindFirstObjectByType<HealthBarManager>();
        if (healthBars != null) healthBars.SetPresentationRoot(root);
        bound = true;
    }

    private void Start()
    {
        if (!bound) return;
        RefreshAudio();
        RefreshFps();
        RefreshLanguage();
    }

    private void RefreshLanguage()
    {
        GameLocalization.ApplyTo(root);
        RefreshBuildCards();
    }

    private void OnDisable()
    {
#if UNITY_EDITOR
        ClearEditorHudPreview();
#endif
        if (!bound) return;
        buildModal.onOpened.RemoveListener(RefreshBuildCards);
        buildModal.onClosed.RemoveListener(ClearBuildCards);
        GameAudioSettings.Changed -= RefreshAudio;
        GamePerformanceSettings.Changed -= RefreshFps;
        GameLocalization.Changed -= RefreshLanguage;
        if (healthBars != null) healthBars.SetPresentationRoot(null);
        movement.ClearToolkitMoveInput();
        if (ultimatePointerHeld) ultimate.Release();
        bound = false;
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (!Application.IsPlaying(gameObject))
        {
            RefreshEditorHudPreview();
            return;
        }
#endif
        if (!bound) return;
        if (GamePerformanceSettings.ShowFps)
        {
            fpsElapsed += Time.unscaledDeltaTime;
            fpsFrames++;
            if (fpsElapsed >= 0.25f)
            {
                SetText(fpsLabel, $"FPS {Mathf.RoundToInt(fpsFrames / fpsElapsed)}");
                fpsElapsed = 0f;
                fpsFrames = 0;
            }
        }
        bool order = orders.IsOrderModeActive;
        bool won = spawner.CurrentPhase == WaveSpawner.Phase.AllCompleted;
        bool lost = spawner.CurrentPhase == WaveSpawner.Phase.Defeated;
        bool paused = pauseMenu.IsPaused && !won && !lost;
        bool confirmingExit = paused && pauseMenu.IsConfirmingExit;
        bool modal = buildModal.IsOpen;
        if (healthBars != null) healthBars.SetVisible(!order && !paused && !won && !lost);
        Base core = Base.Instance;
        if (core != observedCore)
        {
            observedCore = core;
            previousCoreHealth = core != null ? core.CurrentHealth : 0f;
            coreAlertUntil = 0f;
        }
        if (core != null)
        {
            if (core.CurrentHealth < previousCoreHealth)
                coreAlertUntil = Time.unscaledTime + 2f;
            previousCoreHealth = core.CurrentHealth;
            SetText(coreHealthLabel, $"{Mathf.CeilToInt(100f * core.CurrentHealth / core.MaxHealth)}%");
        }
        coreAlert.EnableInClassList("is-hidden", core == null || won || lost ||
            Time.unscaledTime >= coreAlertUntil);
        fpsLabel.EnableInClassList("is-hidden", !GamePerformanceSettings.ShowFps || modal);
        bool desktop = responsiveHud.IsDesktopLayout;
        root.Q("topBar").EnableInClassList("is-hidden", order || won || lost);
        root.Q("effectRow").EnableInClassList("is-hidden", order || won || lost);
        touchControls.EnableInClassList("is-hidden", desktop || order || paused || won || lost);
        root.Q("actionStack").EnableInClassList("is-hidden", modal);
        desktopActions.EnableInClassList("is-hidden", !desktop || order || paused || won || lost || modal);
        bool hideRally = levelSetup.IsFirstLocation;
#if UNITY_EDITOR
        // Temporary placement preview on Level 1.
        hideRally = false;
#endif
        rallyButton.EnableInClassList("is-hidden", hideRally);
        desktopRally.EnableInClassList("is-hidden", hideRally);
        orderLayer.EnableInClassList("is-hidden", !order);
        buildPanel.EnableInClassList("is-hidden", !buildModal.IsOpen || order || paused || won || lost);
        screenOverlay.EnableInClassList("is-hidden", !paused && !won && !lost);
        pausePanel.EnableInClassList("is-hidden", !paused || audioOpen || confirmingExit);
        audioPanel.EnableInClassList("is-hidden", !paused || !audioOpen || confirmingExit);
        exitConfirmPanel.EnableInClassList("is-hidden", !confirmingExit);
        victoryPanel.EnableInClassList("is-hidden", !won);
        defeatPanel.EnableInClassList("is-hidden", !lost);
        if (confirmingExit)
        {
            bool restarting = pauseMenu.PendingExitAction == PauseMenu.ExitAction.Restart;
            SetText(exitConfirmTitle, restarting
                ? GameLocalization.Text("restart-question", "НАЧАТЬ ЗАБЕГ ЗАНОВО?")
                : GameLocalization.Text("menu-question", "ВЕРНУТЬСЯ В МЕНЮ?"));
            SetText(exitConfirmButton, restarting
                ? GameLocalization.Text("restart-button", "НАЧАТЬ ЗАНОВО")
                : GameLocalization.Text("main-menu-button", "ГЛАВНОЕ МЕНЮ"));
        }

        int wave = spawner.IsBuildPhase ? spawner.NextWaveIndex + 1 :
            Mathf.Max(1, spawner.NextWaveIndex);
        int totalWaves = spawner.WaveCount;
        wave = Mathf.Clamp(wave, 0, totalWaves);
        SetText(wavePhaseLabel, spawner.IsBuildPhase
            ? GameLocalization.Text("build-phase", "ПОДГОТОВКА К ВОЛНЕ")
            : GameLocalization.Text("combat-phase", "ВОЛНА ИДЁТ"));
        SetText(waveLabel, GameLocalization.Format("wave-number", "ВОЛНА {0:00}", wave));
        SetText(waveTotalLabel, $"/ {totalWaves:00}");
        UpdateWaveSteps(waveSteps, totalWaves, wave);
        int seconds = Mathf.FloorToInt(timer.ElapsedSeconds);
        SetText(timerLabel, $"{seconds / 60:00}:{seconds % 60:00}");
        SetText(coinsLabel, wallet.Coins.ToString());
        SetText(incomeLabel, $"+{MineIncome.TotalCoinsPerWave}");
        var preview = spawner.IsBuildPhase ? spawner.NextWave :
            spawner.GetWave(Mathf.Clamp(wave - 1, 0, spawner.WaveCount - 1));
        SetText(enemiesLabel, GameLocalization.Format("enemy-count", "ВРАГИ: {0}", CountEnemies(preview)));
        SetText(guideLabel, spawner.IsBuildPhase ? Base.Instance == null
            ? GameLocalization.Text("place-core", "ВСТАНЬТЕ НА ТОЧКУ УЗЛА, ЧТОБЫ ПОСТРОИТЬ ЕГО")
            : GameLocalization.Text("prepare-defense", "ПОДГОТОВЬТЕ ОБОРОНУ")
            : GameLocalization.Text("defend-core", "ЗАЩИЩАЙТЕ СЕТЕВОЙ УЗЕЛ"));
        guideLabel.EnableInClassList("is-hidden", !spawner.IsBuildPhase || order || paused || modal || won || lost);
        startWaveButton.EnableInClassList("is-hidden", !spawner.IsBuildPhase || Base.Instance == null ||
            modal || order || paused || won || lost);

        SetEffect(slowEffect, slowAmmo.RemainingSeconds > 0f,
            GameLocalization.Format("slow-effect", "❄  ЗАМЕДЛЕНИЕ  {0} с", Mathf.CeilToInt(slowAmmo.RemainingSeconds)));
        SetEffect(shieldEffect, playerHealth.CurrentShield > 0f && playerHealth.RemainingShieldSeconds > 0f,
            GameLocalization.Format("shield-effect", "◇  ЩИТ  {0} с", Mathf.CeilToInt(playerHealth.RemainingShieldSeconds)));
        SetEffect(turretEffect, companionTurrets.ActiveCount > 0 && companionTurrets.RemainingSeconds > 0f,
            GameLocalization.Format("turret-effect", "✦  ТУРЕЛЬ ×{0}  {1} с",
                companionTurrets.ActiveCount, Mathf.CeilToInt(companionTurrets.RemainingSeconds)));
        bool homeAvailable = teleport.gameObject.activeInHierarchy && spawner.IsBuildPhase && !playerHealth.IsGhost;
        homeButton.SetEnabled(homeAvailable);
        desktopHome.SetEnabled(homeAvailable);
        ultimateButton.SetEnabled(ultimate.CanReceiveInput || ultimate.IsCharging);
        desktopUltimate.SetEnabled(ultimate.CanReceiveInput || ultimate.IsCharging);
        float energy = ultimate.EnergyProgress;
        mobileUltimateRing.Progress = energy;
        desktopUltimateRing.Progress = energy;
        string chargeText = $"{Mathf.FloorToInt(energy * 100f)}%";
        SetText(mobileUltimateCharge, chargeText);
        SetText(desktopUltimateCharge, chargeText);
        root.Q<Button>("nextLevelButton").EnableInClassList("is-hidden", !victory.CanPlayNextLevel);
        if (won) SetText(root.Q<Label>("victoryReward"), GameLocalization.Format("reward", "+{0} КРЕДИТОВ", levelSetup.VictoryMoneyReward));
        if (lost) SetText(defeatReward, GameLocalization.Format("reward", "+{0} КРЕДИТОВ", levelSetup.DefeatMoneyReward));
        if (buildModal.IsOpen && buildModal.Revision != lastModalRevision) RefreshBuildCards();
        UpdateSpawnPreview(spawner.IsBuildPhase && !order && !paused && !modal && !won && !lost);
    }

    private static void UpdateWaveSteps(VisualElement steps, int totalWaves, int currentWave)
    {
        if (steps.childCount != totalWaves)
        {
            steps.Clear();
            for (int i = 0; i < totalWaves; i++)
            {
                var step = new VisualElement { pickingMode = PickingMode.Ignore };
                step.AddToClassList("wave-step");
                steps.Add(step);
            }
        }

        for (int i = 0; i < steps.childCount; i++)
            steps[i].EnableInClassList("is-current-or-past", i < currentWave);
    }

#if UNITY_EDITOR
    private void RefreshEditorHudPreview()
    {
        var documentRoot = GetComponent<UIDocument>().rootVisualElement;
        var gameRoot = documentRoot?.Q("gameRoot");
        if (gameRoot != editorPreviewRoot)
        {
            ClearEditorHudPreview();
            if (gameRoot == null) return;

            editorPreviewRoot = gameRoot;
            documentRoot.AddToClassList("game-document-root");
            gameRoot.AddToClassList("editor-hud-preview");
            if (gameRoot.Q("waveSteps") is { } steps) UpdateWaveSteps(steps, 5, 2);
            AddEditorUltimatePreview(gameRoot.Q<Button>("ultimateButton"), "ultimateChargeRing", "ultimateCharge");
            AddEditorUltimatePreview(gameRoot.Q<Button>("desktopUltimate"), "desktopUltimateChargeRing",
                "desktopUltimateCharge");
        }

        gameRoot?.EnableInClassList("editor-hud-preview-desktop",
            responsiveHud == null || responsiveHud.UseDesktopLayout());
    }

    private static void AddEditorUltimatePreview(Button button, string ringName, string labelName)
    {
        if (button == null) return;
        var charge = button.Q<Label>(labelName);
        if (charge == null) return;
        var ring = CreateUltimateRing(ringName);
        ring.Progress = 1f;
        button.Insert(button.IndexOf(charge), ring);
        charge.text = "100%";
    }

    private void ClearEditorHudPreview()
    {
        if (editorPreviewRoot == null) return;
        editorPreviewRoot.RemoveFromClassList("editor-hud-preview");
        editorPreviewRoot.RemoveFromClassList("editor-hud-preview-desktop");
        editorPreviewRoot.Q<RadialProgressElement>("ultimateChargeRing")?.RemoveFromHierarchy();
        editorPreviewRoot.Q<RadialProgressElement>("desktopUltimateChargeRing")?.RemoveFromHierarchy();
        if (editorPreviewRoot.Q<Label>("ultimateCharge") is { } mobileCharge) mobileCharge.text = string.Empty;
        if (editorPreviewRoot.Q<Label>("desktopUltimateCharge") is { } desktopCharge) desktopCharge.text = string.Empty;
        editorPreviewRoot = null;
    }
#endif

    private void UpdateSpawnPreview(bool visible)
    {
        spawnPreview.EnableInClassList("is-hidden", !visible);
        if (!visible) return;
        if (previewWaveIndex != spawner.NextWaveIndex)
        {
            previewWaveIndex = spawner.NextWaveIndex;
            spawnEntries.Clear();
            spawnMarkers.Clear();
            spawnPreview.Clear();
            spawner.GetWavePreviewEntries(previewWaveIndex, spawnEntries);
            for (int i = 0; i < spawnEntries.Count; i++)
            {
                Transform point = spawnEntries[i].spawnPoint;
                if (point == null || spawnMarkers.Exists(marker => marker.Point == point)) continue;
                int number = spawnMarkers.Count + 1;
                var world = new VisualElement();
                world.AddToClassList("spawn-world-group");
                world.AddToClassList("is-hidden");
                var heading = new Label(number.ToString());
                heading.AddToClassList("spawn-world-number");
                world.Add(heading);
                for (int j = 0; j < spawnEntries.Count; j++)
                {
                    if (spawnEntries[j].spawnPoint != point) continue;
                    var row = new VisualElement();
                    row.AddToClassList("spawn-type");
                    if (spawnEntries[j].icon != null)
                    {
                        var icon = new Image { sprite = spawnEntries[j].icon };
                        icon.AddToClassList("spawn-type-icon");
                        row.Add(icon);
                    }
                    var count = new Label(spawnEntries[j].count.ToString());
                    count.AddToClassList("spawn-type-count");
                    row.Add(count);
                    world.Add(row);
                }
                var edge = new VisualElement();
                edge.AddToClassList("spawn-edge-badge");
                edge.AddToClassList("is-hidden");
                var arrow = new Label("▲");
                arrow.AddToClassList("spawn-edge-arrow");
                edge.Add(arrow);
                var index = new Label(number.ToString());
                index.AddToClassList("spawn-edge-number");
                edge.Add(index);
                var distance = new Label();
                distance.AddToClassList("spawn-edge-distance");
                edge.Add(distance);
                IgnorePicking(world);
                IgnorePicking(edge);
                spawnPreview.Add(world);
                spawnPreview.Add(edge);
                spawnMarkers.Add(new SpawnMarker { Point = point, Visible = world, Edge = edge,
                    Distance = distance, Arrow = arrow });
            }
        }
        Camera camera = Camera.main;
        float width = spawnPreview.resolvedStyle.width;
        float height = spawnPreview.resolvedStyle.height;
        if (camera == null || width <= 0f || height <= 0f) return;
        foreach (SpawnMarker marker in spawnMarkers)
        {
            if (marker.Point == null) continue;
            Vector3 screen = camera.WorldToScreenPoint(marker.Point.position);
            Vector2 target = new(screen.x * width / Screen.width, screen.y * height / Screen.height);
            Vector2 center = new(width * 0.5f, height * 0.5f);
            if (screen.z < 0f) target = center - (target - center);
            bool onScreen = screen.z > 0f && target.x >= 0f && target.x <= width &&
                target.y >= 0f && target.y <= height;
            marker.Visible.EnableInClassList("is-hidden", !onScreen);
            marker.Edge.EnableInClassList("is-hidden", onScreen);
            if (onScreen)
            {
                marker.Visible.style.left = Mathf.Clamp(target.x - marker.Visible.resolvedStyle.width * 0.5f,
                    12f, width - marker.Visible.resolvedStyle.width - 12f);
                marker.Visible.style.top = Mathf.Clamp(height - target.y - 84f, 90f, height - 210f);
            }
            else
            {
                Vector2 direction = target - center;
                if (direction.sqrMagnitude < 0.001f) direction = Vector2.up;
                float scaleX = Mathf.Abs(direction.x) > 0.001f ?
                    (width * 0.5f - 58f) / Mathf.Abs(direction.x) : float.PositiveInfinity;
                float scaleY = Mathf.Abs(direction.y) > 0.001f ?
                    (height * 0.5f - 100f) / Mathf.Abs(direction.y) : float.PositiveInfinity;
                Vector2 edgePosition = center + direction * Mathf.Min(scaleX, scaleY);
                // Keep the badge clear of the corner pause button and the top HUD.
                marker.Edge.style.left = Mathf.Clamp(edgePosition.x - 33f, 96f, width - 96f);
                marker.Edge.style.top = Mathf.Clamp(height - edgePosition.y - 33f, 108f, height - 88f);
                float meters = Vector3.Distance(camera.transform.position, marker.Point.position);
                marker.Distance.text = GameLocalization.Format("distance", "{0} м", Mathf.RoundToInt(meters));
                marker.Arrow.style.rotate = new Rotate(new Angle(
                    Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f));
            }
        }
    }

    private static RadialProgressElement CreateUltimateRing(string name)
    {
        var ring = new RadialProgressElement
        {
            name = name,
            TrackColor = new Color(0.49f, 0.38f, 0.56f, 1f),
            FillColor = new Color(0.95f, 0.80f, 1f, 1f),
            StrokeWidth = 9f
        };
        ring.AddToClassList("ultimate-charge-ring");
        return ring;
    }

    private static void IgnorePicking(VisualElement element)
    {
        element.pickingMode = PickingMode.Ignore;
        foreach (VisualElement child in element.Children()) IgnorePicking(child);
    }

    private static int CountEnemies(WaveSpawner.Wave wave)
    {
        if (wave == null) return 0;
        int total = 0;
        if (wave.spawnGroups != null && wave.spawnGroups.Length > 0)
            foreach (var group in wave.spawnGroups)
                if (group?.entries != null)
                    foreach (var entry in group.entries)
                        if (entry != null) total += Mathf.Max(0, entry.count);
        else if (wave.entries != null)
            foreach (var fallbackEntry in wave.entries)
                if (fallbackEntry != null) total += Mathf.Max(0, fallbackEntry.count);
        return total;
    }

    private void RefreshBuildCards()
    {
        if (!bound) return;
        buildCards.Clear();
        if (!buildModal.IsOpen) return;
        lastModalRevision = buildModal.Revision;
        if (buildModal.Owner != null && Camera.main != null)
        {
            Vector3 screen = Camera.main.WorldToScreenPoint(buildModal.Owner.transform.position);
            buildPanel.EnableInClassList("panel-left", screen.z > 0f && screen.x > Screen.width * 0.6f);
        }
        else buildPanel.RemoveFromClassList("panel-left");
        SetText(root.Q<Label>("buildHeading"), buildModal.Heading);
        int displayedCards = 0;
        foreach (TowerSelectionModal.BuildOffer source in buildModal.ActiveOffers)
        {
            displayedCards++;
            var levels = BuildingLevelList.Read(source.Option);
            var card = new VisualElement();
            card.AddToClassList("build-card");
            if (source.CurrentLevel > 0) card.AddToClassList("build-card-upgrade");
            var header = new VisualElement();
            header.AddToClassList("build-card-header");
            if (source.Option.icon != null)
            {
                var icon = new Image { sprite = source.Option.icon };
                icon.AddToClassList("build-card-icon");
                header.Add(icon);
            }
            var summary = new VisualElement();
            summary.AddToClassList("build-card-summary");
            var title = new Label(GetBuildingName(source.Option).ToUpperInvariant());
            title.AddToClassList("build-card-title");
            summary.Add(title);
            var description = new Label(GetBuildingDescription(source.Option));
            description.AddToClassList("build-card-description");
            summary.Add(description);
            header.Add(summary);
            card.Add(header);
            if (source.CurrentLevel > 0 && levels.Count > 0)
            {
                int currentIndex = Mathf.Clamp(source.CurrentLevel - 1, 0, levels.Count - 1);
                int nextIndex = Mathf.Min(currentIndex + 1, levels.Count - 1);
                string current = GameLocalization.Format("building-level", "УРОВЕНЬ {0}", source.CurrentLevel);
                string next = nextIndex > currentIndex
                    ? GameLocalization.Text("building-next", "СЛЕДУЮЩИЙ") + "  ·  " + FormatLevel(levels[nextIndex])
                    : GameLocalization.Text("building-max", "МАКС. УРОВЕНЬ");
                var progression = new Label($"{current}  ·  {FormatLevel(levels[currentIndex])}\n{next}");
                progression.AddToClassList("build-upgrade-summary");
                card.Add(progression);
            }
            var action = new Button(source.Activate)
            {
                text = source.Cost.HasValue ? $"{source.ActionText}   •   {source.Cost.Value} ◆" : source.ActionText
            };
            action.AddToClassList("build-action");
            action.SetEnabled(source.Available);
            card.Add(action);
            buildCards.Add(card);
        }
        buildPanel.EnableInClassList("single-card-panel", displayedCards == 1);
        buildPanel.EnableInClassList("upgrade-panel", displayedCards == 1 &&
            buildModal.ActiveOffers.Count == 1 && buildModal.ActiveOffers[0].CurrentLevel > 0);
    }

    private static string FormatLevel(BuildingLevelList.Level level)
    {
        var values = new List<string>();
        if (level.Health.HasValue) values.Add(GameLocalization.Format("building-hp", "ЗД {0:0}", level.Health.Value));
        if (level.Damage.HasValue) values.Add(GameLocalization.Format("damage", "УРОН {0:0}", level.Damage.Value));
        if (level.Income.HasValue) values.Add(GameLocalization.Format("income", "+{0:0} / ВОЛНА", level.Income.Value));
        return string.Join("  ·  ", values);
    }

    private static string GetBuildingName(TowerOption option) => option.displayName switch
    {
        "Cannon Turret" => GameLocalization.Text("cannon", "Пушечная турель"),
        "Pulse" => GameLocalization.Text("pulse", "Импульсная башня"),
        "Mine" => GameLocalization.Text("mine", "Шахта"),
        "Ratusha" => GameLocalization.Text("core", "Сетевой узел"),
        "Barracks / Melee" => GameLocalization.Text("melee", "Казарма ближнего боя"),
        "Barracks / Ranged" => GameLocalization.Text("ranged", "Казарма дальнего боя"),
        "Mortar" => GameLocalization.Text("mortar", "Мортира"),
        "Wall" => GameLocalization.Text("wall", "Стена"),
        _ => option.displayName
    };

    private static string GetBuildingDescription(TowerOption option)
    {
        if (!string.IsNullOrWhiteSpace(option.description)) return option.description;
        string name = option.displayName?.ToLowerInvariant() ?? "";
        if (name.Contains("pulse")) return GameLocalization.Text("pulse-desc", "Наносит урон и замедляет наземных врагов рядом.");
        if (name.Contains("mortar")) return GameLocalization.Text("mortar-desc", "Поражает группы врагов с большого расстояния.");
        return option.slotType switch
        {
            BuildSlotType.Mine => GameLocalization.Text("mine-desc", "Даёт ресурсы после каждой волны."),
            BuildSlotType.TownHall => GameLocalization.Text("core-desc", "Защищайте этот узел до конца забега."),
            BuildSlotType.Barracks => GameLocalization.Text("barracks-desc", "Выпускает союзников для удержания позиции."),
            BuildSlotType.Wall => GameLocalization.Text("wall-desc", "Перекрывает путь и задерживает врагов."),
            _ => GameLocalization.Text("cannon-desc", "Стреляет по наземным и летающим целям.")
        };
    }

    private void ClearBuildCards()
    {
        buildCards.Clear();
        buildPanel.RemoveFromClassList("single-card-panel");
        buildPanel.RemoveFromClassList("upgrade-panel");
        lastModalRevision = -1;
    }

    private static void SetText(TextElement element, string text)
    {
        if (element.text != text) element.text = text;
    }
    private static void SetEffect(Label label, bool visible, string text)
    {
        label.EnableInClassList("is-hidden", !visible);
        if (visible) SetText(label, text);
    }

    private void OpenAudio() { pauseMenu.ShowSettings(); audioOpen = true; RefreshAudio(); }
    private void CloseAudio() { pauseMenu.ShowPauseOptions(); audioOpen = false; }
    private void RefreshAudio()
    {
        if (masterSlider == null) return;
        RefreshVolumeControl(masterSlider, "masterValue", "masterFill", GameAudioSettings.Master);
        RefreshVolumeControl(musicSlider, "musicValue", "musicFill", GameAudioSettings.Music);
        RefreshVolumeControl(effectsSlider, "effectsValue", "effectsFill", GameAudioSettings.Effects);
    }

    private void RefreshVolumeControl(Slider slider, string valueName, string fillName, float volume)
    {
        slider.SetValueWithoutNotify(volume);
        SetText(root.Q<Label>(valueName), $"{Mathf.RoundToInt(volume * 100f)}%");
        root.Q(fillName).style.width = Length.Percent(volume * 100f);
    }

    private void RefreshFps()
    {
        fpsToggle.EnableInClassList("is-on", GamePerformanceSettings.ShowFps);
        fpsLabel.EnableInClassList("is-hidden", !GamePerformanceSettings.ShowFps);
        fpsElapsed = 0f;
        fpsFrames = 0;
    }

    private void BeginMove(PointerDownEvent evt)
    {
        if (responsiveHud.IsDesktopLayout) return;
        joystick.CapturePointer(evt.pointerId);
        UpdateMove((Vector2)evt.position);
    }
    private void Move(PointerMoveEvent evt)
    {
        if (joystick.HasPointerCapture(evt.pointerId)) UpdateMove((Vector2)evt.position);
    }
    private void EndMove(PointerUpEvent evt)
    {
        if (joystick.HasPointerCapture(evt.pointerId)) joystick.ReleasePointer(evt.pointerId);
        ResetMove();
    }
    private void UpdateMove(Vector2 worldPosition)
    {
        Vector2 local = joystick.WorldToLocal(worldPosition);
        Vector2 center = joystick.layout.size * 0.5f;
        Vector2 direction = Vector2.ClampMagnitude((local - center) / (joystick.layout.width * 0.38f), 1f);
        direction.y = -direction.y;
        movement.SetToolkitMoveInput(direction);
        joystickKnob.transform.position = new Vector3(direction.x * 46f, -direction.y * 46f, 0f);
        joystickSector.EnableInClassList("is-active", direction.sqrMagnitude > 0.0025f);
        joystickSector.transform.rotation = Quaternion.Euler(0f, 0f,
            45f - Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
    }
    private void ResetMove()
    {
        movement.SetToolkitMoveInput(Vector2.zero);
        joystickKnob.transform.position = Vector3.zero;
        joystickSector.RemoveFromClassList("is-active");
    }

    private void BindUltimate(Button button)
    {
        button.clicked += () =>
        {
            if (ultimate.IsAutomaticCharge && ultimate.CanReceiveInput)
                ultimate.BeginCharge();
        };
        button.RegisterCallback<PointerDownEvent>(evt =>
        {
            if (ultimate.IsAutomaticCharge) return;
            if (!ultimate.CanReceiveInput || ultimate.IsCharging) return;
            ultimatePointerHeld = !ultimate.IsAutomaticCharge;
            ultimate.BeginCharge();
            if (ultimatePointerHeld) button.CapturePointer(evt.pointerId);
        });
        button.RegisterCallback<PointerUpEvent>(evt =>
        {
            if (!ultimatePointerHeld) return;
            ultimatePointerHeld = false;
            ultimate.Release();
            if (button.HasPointerCapture(evt.pointerId)) button.ReleasePointer(evt.pointerId);
        });
        button.RegisterCallback<PointerCaptureOutEvent>(_ =>
        {
            if (!ultimatePointerHeld) return;
            ultimatePointerHeld = false;
            ultimate.Release();
        });
    }
}
