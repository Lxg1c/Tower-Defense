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
    [SerializeField] private AudioSource waveStartAudio;
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
    private VisualElement touchActions;
    private VisualElement joystick;
    private VisualElement joystickZone;
    private int movePointer = -1;
    private Vector2 moveOrigin;
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
    private VisualElement tutorialPanel;
    private Label tutorialTitle;
    private Label tutorialText;
    private FirstLevelTutorial tutorial;
    private TowerPlacementZone[] tutorialZones;
    private Shooter tutorialShooter;
    private bool tutorialHasDefense;
    private Label coreHealthLabel;
    private Label slowEffect;
    private Label shieldEffect;
    private Label turretEffect;
    private Label mobileUltimateCharge;
    private Label desktopUltimateCharge;
    private Label fpsLabel;
    private Button fpsToggle;
    private Button victoryStats;
    private Button defeatStats;
    private Label benchmarkProgress;
    private VisualElement benchmarkHud;
    private Button benchmarkExit;
    private Button performanceBack;
    private PerformanceBenchmark benchmark;
    private bool benchmarkMode;
    private int displayedBenchmarkStage;
    private int displayedBenchmarkSeconds = -1;
    private bool applicationPaused;
    private bool skipPerformanceFrame = true;
    private string runSummary;
    private string runReport;
    private VisualElement performancePanel;
    private Label performanceReport;
    private VisualElement benchmarkStages;
    private string performanceClipboard;
    private PerformanceMeasurement performanceMeasurement;
    private bool performanceReportOpen;
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
    private readonly UltimateReadyCue ultimateReadyCue = new();
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
        public VisualElement Arrow;
        public float PreviewWidth;
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
        joystickZone = root.Q("joystickZone");
        touchActions = root.Q("actionStack");
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
        tutorialPanel = root.Q("tutorialPanel");
        tutorialTitle = root.Q<Label>("tutorialTitle");
        tutorialText = root.Q<Label>("tutorialText");
        slowEffect = root.Q<Label>("slowEffect");
        shieldEffect = root.Q<Label>("shieldEffect");
        turretEffect = root.Q<Label>("turretEffect");
        mobileUltimateCharge = root.Q<Label>("ultimateCharge");
        desktopUltimateCharge = root.Q<Label>("desktopUltimateCharge");
        fpsLabel = root.Q<Label>("fpsLabel");
        fpsToggle = root.Q<Button>("fpsToggle");
        victoryStats = root.Q<Button>("victoryStats");
        defeatStats = root.Q<Button>("defeatStats");
        benchmarkHud = root.Q("benchmarkHud");
        benchmarkProgress = root.Q<Label>("benchmarkProgress");
        benchmarkExit = root.Q<Button>("benchmarkExit");
        performanceBack = root.Q<Button>("performanceBack");
        performancePanel = root.Q("performancePanel");
        performanceReport = root.Q<Label>("performanceReport");
        benchmarkStages = root.Q("benchmarkStages");
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
        if (touchControls == null || desktopActions == null || touchActions == null || joystick == null || joystickZone == null || joystickSector == null ||
            joystickKnob == null || buildPanel == null ||
            orderLayer == null || screenOverlay == null || spawnPreview == null || pausePanel == null ||
            audioPanel == null || exitConfirmPanel == null || victoryPanel == null || defeatPanel == null ||
            exitConfirmTitle == null || exitConfirmDescription == null || exitConfirmButton == null ||
            defeatReward == null ||
            buildCards == null || waveLabel == null || wavePhaseLabel == null ||
            waveTotalLabel == null || waveSteps == null || timerLabel == null ||
            enemiesLabel == null || coinsLabel == null || incomeLabel == null ||
            tutorialPanel == null || tutorialTitle == null || tutorialText == null ||
            coreAlert == null || coreHealthLabel == null ||
            slowEffect == null || shieldEffect == null ||
            turretEffect == null || mobileUltimateCharge == null || desktopUltimateCharge == null ||
            fpsLabel == null || fpsToggle == null || victoryStats == null || defeatStats == null ||
            benchmarkHud == null || benchmarkProgress == null || benchmarkExit == null ||
            performancePanel == null || performanceReport == null || performanceBack == null ||
            benchmarkStages == null ||
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
        victoryStats.clicked += OpenRunReport;
        defeatStats.clicked += OpenRunReport;
        benchmarkExit.clicked += defeat.GoToMainMenu;
        root.Q<Button>("performanceCopy").clicked += CopyPerformanceReport;
        performanceBack.clicked += ClosePerformanceReport;
        joystickZone.RegisterCallback<PointerDownEvent>(BeginMove);
        joystickZone.RegisterCallback<PointerMoveEvent>(Move);
        joystickZone.RegisterCallback<PointerUpEvent>(EndMove);
        joystickZone.RegisterCallback<PointerCancelEvent>(CancelMove);
        joystickZone.RegisterCallback<PointerCaptureOutEvent>(LostMoveCapture);
        if (root.Q("gameRoot") is { } gameRoot)
        {
            gameRoot.RegisterCallback<GeometryChangedEvent>(ResizeJoystick);
            FitJoystick(joystick, gameRoot.layout.size);
        }
        BindUltimate(ultimateButton);
        BindUltimate(desktopUltimate);
        buildModal.onOpened.AddListener(RefreshBuildCards);
        buildModal.onClosed.AddListener(ClearBuildCards);
        GameAudioSettings.Changed += RefreshAudio;
        GamePerformanceSettings.Changed += RefreshFps;
        GameLocalization.Changed += RefreshLanguage;
        BaseUpgrade.OnTownHallChanged += DiscoverGuideBuilding;
        spawner.onCombatPhaseStarted.AddListener(PlayWaveStart);
        healthBars = FindFirstObjectByType<HealthBarManager>();
        benchmarkMode = RunSelection.IsPerformanceBenchmark;
        if (benchmarkMode)
        {
            movement.enabled = false;
            foreach (var child in root.Q("gameRoot").Children())
                if (child != benchmarkHud && child != screenOverlay) child.AddToClassList("benchmark-hidden");
            benchmarkHud.RemoveFromClassList("is-hidden");
        }
        else
        {
            if (healthBars != null) healthBars.SetPresentationRoot(root);
            performanceMeasurement = new PerformanceMeasurement();
            skipPerformanceFrame = true;
        }
        bound = true;
    }

    private void Start()
    {
        if (!bound) return;
        RefreshAudio();
        RefreshFps();
        RefreshLanguage();
        if (!benchmarkMode) DiscoverGuideBuilding();
    }

    private void RefreshLanguage()
    {
        GameLocalization.ApplyTo(root);
        if (benchmarkMode)
            performanceBack.text = GameLocalization.Text(LocalizationKeys.MainMenu);
        else RefreshBuildCards();
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
        BaseUpgrade.OnTownHallChanged -= DiscoverGuideBuilding;
        spawner.onCombatPhaseStarted.RemoveListener(PlayWaveStart);
        victoryStats.clicked -= OpenRunReport;
        defeatStats.clicked -= OpenRunReport;
        benchmarkExit.clicked -= defeat.GoToMainMenu;
        root.Q<Button>("performanceCopy").clicked -= CopyPerformanceReport;
        performanceBack.clicked -= ClosePerformanceReport;
        performanceMeasurement?.Dispose();
        performanceMeasurement = null;
        performanceReportOpen = false;
        runSummary = null;
        runReport = null;
        performanceClipboard = null;
        benchmarkStages.Clear();
        benchmarkStages.AddToClassList("is-hidden");
        benchmark?.Dispose();
        benchmark = null;
        if (benchmarkMode)
        {
            foreach (var child in root.Q("gameRoot").Children()) child.RemoveFromClassList("benchmark-hidden");
            benchmarkHud.AddToClassList("is-hidden");
        }
        root.Q("gameRoot")?.UnregisterCallback<GeometryChangedEvent>(ResizeJoystick);
        joystickZone.UnregisterCallback<PointerDownEvent>(BeginMove);
        joystickZone.UnregisterCallback<PointerMoveEvent>(Move);
        joystickZone.UnregisterCallback<PointerUpEvent>(EndMove);
        joystickZone.UnregisterCallback<PointerCancelEvent>(CancelMove);
        joystickZone.UnregisterCallback<PointerCaptureOutEvent>(LostMoveCapture);
        ResetMove();
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
        if (benchmarkMode)
        {
            UpdateBenchmark();
            return;
        }
        UpdateRunMeasurement();
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
            if (!spawner.IsCombatPhase)
                coreAlertUntil = 0f;
            else if (core.CurrentHealth < previousCoreHealth)
                coreAlertUntil = Time.time + 0.6f;
            previousCoreHealth = core.CurrentHealth;
            SetText(coreHealthLabel, $"{Mathf.CeilToInt(100f * core.CurrentHealth / core.MaxHealth)}%");
        }
        coreAlert.EnableInClassList("is-hidden", !spawner.IsCombatPhase || paused || core == null ||
            (!core.IsUnderAttack && Time.time >= coreAlertUntil));
        fpsLabel.EnableInClassList("is-hidden", !GamePerformanceSettings.ShowFps || modal);
        bool desktop = responsiveHud.IsDesktopLayout;
        root.Q("topBar").EnableInClassList("is-hidden", order || won || lost);
        root.Q("effectRow").EnableInClassList("is-hidden", order || won || lost);
        bool hideTouchControls = desktop || order || paused || won || lost;
        if (hideTouchControls && movePointer >= 0) ResetMove();
        touchControls.EnableInClassList("is-hidden", hideTouchControls);
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
        screenOverlay.EnableInClassList("is-hidden", !paused && !won && !lost && !performanceReportOpen);
        pausePanel.EnableInClassList("is-hidden", !paused || audioOpen || confirmingExit || performanceReportOpen);
        audioPanel.EnableInClassList("is-hidden", !paused || !audioOpen || confirmingExit);
        exitConfirmPanel.EnableInClassList("is-hidden", !confirmingExit);
        victoryPanel.EnableInClassList("is-hidden", !won || performanceReportOpen);
        defeatPanel.EnableInClassList("is-hidden", !lost || performanceReportOpen);
        performancePanel.EnableInClassList("is-hidden", !performanceReportOpen);
        if (confirmingExit)
        {
            bool restarting = pauseMenu.PendingExitAction == PauseMenu.ExitAction.Restart;
            SetText(exitConfirmTitle, restarting
                ? GameLocalization.Text(LocalizationKeys.RestartQuestion)
                : GameLocalization.Text(LocalizationKeys.MenuQuestion));
            SetText(exitConfirmButton, restarting
                ? GameLocalization.Text(LocalizationKeys.Restart)
                : GameLocalization.Text(LocalizationKeys.MainMenu));
        }

        int wave = spawner.DisplayWaveNumber;
        int totalWaves = spawner.WaveCount;
        SetText(wavePhaseLabel, spawner.IsBuildPhase
            ? GameLocalization.Text(LocalizationKeys.BuildPhase)
            : GameLocalization.Text(LocalizationKeys.CombatPhase));
        SetText(waveLabel, GameLocalization.Format(LocalizationKeys.WaveNumber, wave));
        SetText(waveTotalLabel, $"/ {totalWaves:00}");
        UpdateWaveSteps(waveSteps, totalWaves, wave);
        int seconds = Mathf.FloorToInt(timer.ElapsedSeconds);
        SetText(timerLabel, $"{seconds / 60:00}:{seconds % 60:00}");
        SetText(coinsLabel, wallet.Coins.ToString());
        SetText(incomeLabel, $"+{MineIncome.TotalCoinsPerWave}");
        var preview = spawner.IsBuildPhase ? spawner.NextWave :
            spawner.GetWave(Mathf.Clamp(wave - 1, 0, spawner.WaveCount - 1));
        SetText(enemiesLabel, GameLocalization.Format(LocalizationKeys.EnemyCount, CountEnemies(preview)));
        UpdateTutorial(!order && !paused && !won && !lost && !playerHealth.IsGhost, desktop);
        startWaveButton.EnableInClassList("is-hidden", !spawner.IsBuildPhase || Base.Instance == null ||
            modal || order || paused || won || lost);

        SetEffect(slowEffect, slowAmmo.RemainingSeconds > 0f,
            GameLocalization.Format(LocalizationKeys.SlowEffect, Mathf.CeilToInt(slowAmmo.RemainingSeconds)));
        SetEffect(shieldEffect, playerHealth.CurrentShield > 0f && playerHealth.RemainingShieldSeconds > 0f,
            GameLocalization.Format(LocalizationKeys.ShieldEffect, Mathf.CeilToInt(playerHealth.RemainingShieldSeconds)));
        SetEffect(turretEffect, companionTurrets.ActiveCount > 0 && companionTurrets.RemainingSeconds > 0f,
            GameLocalization.Format(LocalizationKeys.TurretEffect,
                companionTurrets.ActiveCount, Mathf.CeilToInt(companionTurrets.RemainingSeconds)));
        bool homeAvailable = teleport.gameObject.activeInHierarchy && spawner.IsBuildPhase && !playerHealth.IsGhost;
        homeButton.SetEnabled(homeAvailable);
        desktopHome.SetEnabled(homeAvailable);
        bool ultimateReady = ultimate.CanReceiveInput && !ultimate.IsCharging;
        ultimateButton.SetEnabled(ultimateReady || ultimate.IsCharging);
        desktopUltimate.SetEnabled(ultimateReady || ultimate.IsCharging);
        bool readyHighlight = ultimateReadyCue.Update(ultimateReady,
            !order && !paused && !won && !lost && !modal, Time.deltaTime);
        ultimateButton.EnableInClassList("ultimate-ready", readyHighlight);
        desktopUltimate.EnableInClassList("ultimate-ready", readyHighlight);
        float energy = ultimate.EnergyProgress;
        mobileUltimateRing.Progress = energy;
        desktopUltimateRing.Progress = energy;
        string chargeText = $"{Mathf.FloorToInt(energy * 100f)}%";
        SetText(mobileUltimateCharge, chargeText);
        SetText(desktopUltimateCharge, chargeText);
        root.Q<Button>("nextLevelButton").EnableInClassList("is-hidden", !victory.CanPlayNextLevel);
        if (won) SetText(root.Q<Label>("victoryReward"), GameLocalization.Format(LocalizationKeys.Reward, levelSetup.VictoryMoneyReward));
        if (lost) SetText(defeatReward, GameLocalization.Format(LocalizationKeys.Reward, levelSetup.DefeatMoneyReward));
        if (buildModal.IsOpen && buildModal.Revision != lastModalRevision) RefreshBuildCards();
        UpdateSpawnPreview(spawner.IsBuildPhase && !order && !paused && !modal && !won && !lost, desktop);
    }

    private void OnApplicationPause(bool paused)
    {
        applicationPaused = paused;
        skipPerformanceFrame = true;
        if (paused && movePointer >= 0) ResetMove();
    }

    private void UpdateRunMeasurement()
    {
        if (performanceMeasurement == null) return;
        var phase = spawner.CurrentPhase;
        if (phase is WaveSpawner.Phase.AllCompleted or WaveSpawner.Phase.Defeated)
        {
            runSummary = performanceMeasurement.Summary();
            runReport = PerformanceMeasurement.DeviceReport() + "\n\n" + performanceMeasurement.Report();
            performanceMeasurement.Dispose();
            performanceMeasurement = null;
            var button = phase == WaveSpawner.Phase.AllCompleted ? victoryStats : defeatStats;
            button.text = GameLocalization.Text(LocalizationKeys.PerfRunDetails) + "\n" + runSummary;
            return;
        }

        if (pauseMenu.IsPaused || applicationPaused)
        {
            skipPerformanceFrame = true;
            return;
        }
        if (skipPerformanceFrame)
        {
            skipPerformanceFrame = false;
            return;
        }
        performanceMeasurement.Tick(Time.unscaledDeltaTime, spawner.DisplayWaveNumber,
            phase == WaveSpawner.Phase.Combat, spawner.AliveMobCount);
    }

    private void UpdateBenchmark()
    {
        if (applicationPaused || performanceReportOpen) return;
        if (benchmark == null)
        {
            benchmark = new PerformanceBenchmark(spawner, Camera.main, movement.transform.position);
            skipPerformanceFrame = true;
            displayedBenchmarkStage = 0;
        }
        if (!skipPerformanceFrame) benchmark.Tick(Time.unscaledDeltaTime);
        skipPerformanceFrame = false;
        if (benchmark.IsComplete)
        {
            performanceClipboard = benchmark.Report;
            for (int i = 0; i < benchmark.StageCount; i++)
            {
                int index = i;
                var button = new Button(() => ShowBenchmarkStage(index)) { text = benchmark.EnemiesAtStage(i).ToString() };
                button.AddToClassList("benchmark-stage-button");
                benchmarkStages.Add(button);
            }
            benchmarkStages.RemoveFromClassList("is-hidden");
            ShowBenchmarkStage(0);
            performanceReportOpen = true;
            benchmarkHud.AddToClassList("is-hidden");
            screenOverlay.RemoveFromClassList("is-hidden");
            performancePanel.RemoveFromClassList("is-hidden");
            return;
        }
        int stage = benchmark.StageNumber;
        int seconds = benchmark.SecondsRemaining;
        if (stage == displayedBenchmarkStage && seconds == displayedBenchmarkSeconds) return;
        displayedBenchmarkStage = stage;
        displayedBenchmarkSeconds = seconds;
        SetText(benchmarkProgress, GameLocalization.Format(LocalizationKeys.PerfBenchmarkProgress, stage, benchmark.StageCount, benchmark.EnemyCount, seconds));
    }

    private void ShowBenchmarkStage(int index)
    {
        performanceReport.text = benchmark.ReportForStage(index);
        for (int i = 0; i < benchmarkStages.childCount; i++)
            benchmarkStages[i].EnableInClassList("benchmark-stage-selected", i == index);
    }

    private void OpenRunReport()
    {
        if (runReport == null) return;
        performanceReport.text = runReport;
        performanceClipboard = runReport;
        performanceReportOpen = true;
    }

    private void CopyPerformanceReport() => GUIUtility.systemCopyBuffer = performanceClipboard;

    private void ClosePerformanceReport()
    {
        if (benchmarkMode) defeat.GoToMainMenu();
        else performanceReportOpen = false;
    }

    private void PlayWaveStart(int waveIndex, int totalWaves, int reward)
    {
        if (waveStartAudio != null) waveStartAudio.Play();
    }

    private void DiscoverGuideBuilding()
    {
        if (levelSetup.IsFirstLocation && BaseUpgrade.Instance != null && BaseUpgrade.Instance.TownHallLevel >= 2)
            BuildingGuide.DiscoverPulse();
    }

    private void UpdateTutorial(bool visible, bool desktop)
    {
        if (!levelSetup.IsFirstLocation) return;

        if (tutorial == null)
        {
            tutorial = new FirstLevelTutorial();
            tutorialZones = FindObjectsByType<TowerPlacementZone>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            tutorialShooter = movement.GetComponentInChildren<Shooter>();
        }

        bool hasCore = Base.Instance != null;
        if (hasCore && !tutorialHasDefense && spawner.IsBuildPhase)
        {
            foreach (var zone in tutorialZones)
                if (zone != null && zone.IsBuilt && zone.BuiltBase == null)
                {
                    tutorialHasDefense = true;
                    break;
                }
        }

        BuildSlotType? offer = null;
        if (buildModal.IsOpen)
        {
            var offers = buildModal.ActiveOffers;
            if (offers.Count == 1 && offers[0].CurrentLevel == 0 && offers[0].Option.displayName != "Pulse")
                offer = offers[0].Option.slotType;
            visible &= offer is BuildSlotType.TownHall or BuildSlotType.Mine or BuildSlotType.Defense;
        }

        bool hasTarget = tutorialShooter != null && tutorialShooter.HasTarget;
        var hint = tutorial.Update(visible, spawner.IsBuildPhase, hasCore, tutorialHasDefense,
            offer, hasTarget, ultimate.IsCharging, ultimate.CanReceiveInput, Time.deltaTime);
        tutorialPanel.EnableInClassList("is-hidden", hint == FirstLevelTutorial.Hint.None);
        if (hint == FirstLevelTutorial.Hint.None) return;
        var copy = hint switch
        {
            FirstLevelTutorial.Hint.Approach => (LocalizationKeys.HintApproachTitle, LocalizationKeys.HintApproachText),
            FirstLevelTutorial.Hint.Core => (LocalizationKeys.HintCoreTitle, LocalizationKeys.HintCoreText),
            FirstLevelTutorial.Hint.Economy => (LocalizationKeys.HintEconomyTitle, LocalizationKeys.HintEconomyText),
            FirstLevelTutorial.Hint.Mine => (LocalizationKeys.HintMineTitle, LocalizationKeys.HintMineText),
            FirstLevelTutorial.Hint.Turret => (LocalizationKeys.HintTurretTitle, LocalizationKeys.HintTurretText),
            FirstLevelTutorial.Hint.StartWave => (LocalizationKeys.HintStartTitle, LocalizationKeys.HintStartText),
            FirstLevelTutorial.Hint.AutoFire => (LocalizationKeys.HintFireTitle, LocalizationKeys.HintFireText),
            _ => (LocalizationKeys.HintUltimateTitle, LocalizationKeys.HintUltimateText)
        };
        SetText(tutorialTitle, GameLocalization.Text(copy.Item1));
        SetText(tutorialText, desktop && hint == FirstLevelTutorial.Hint.Ultimate
            ? GameLocalization.Text(LocalizationKeys.HintUltimateDesktop)
            : GameLocalization.Text(copy.Item2));
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
            gameRoot.RegisterCallback<GeometryChangedEvent>(ResizeJoystick);
            if (gameRoot.Q("joystick") is { } stick) FitJoystick(stick, gameRoot.layout.size);
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
        editorPreviewRoot.UnregisterCallback<GeometryChangedEvent>(ResizeJoystick);
        editorPreviewRoot.RemoveFromClassList("editor-hud-preview");
        editorPreviewRoot.RemoveFromClassList("editor-hud-preview-desktop");
        editorPreviewRoot.Q<RadialProgressElement>("ultimateChargeRing")?.RemoveFromHierarchy();
        editorPreviewRoot.Q<RadialProgressElement>("desktopUltimateChargeRing")?.RemoveFromHierarchy();
        if (editorPreviewRoot.Q<Label>("ultimateCharge") is { } mobileCharge) mobileCharge.text = string.Empty;
        if (editorPreviewRoot.Q<Label>("desktopUltimateCharge") is { } desktopCharge) desktopCharge.text = string.Empty;
        editorPreviewRoot = null;
    }
#endif

    private static void ResizeJoystick(GeometryChangedEvent evt)
    {
        if (evt.currentTarget is VisualElement gameRoot && gameRoot.Q("joystick") is { } stick)
            FitJoystick(stick, evt.newRect.size);
    }

    private static void FitJoystick(VisualElement stick, Vector2 panelSize)
    {
        if (!(panelSize.x > 0f && panelSize.y > 0f)) return;
        float diameter = Mathf.Min(300f, Mathf.Min(panelSize.x * 0.28f, panelSize.y * 0.39f));
        stick.style.width = diameter;
        stick.style.height = diameter;
    }

    private const float SpawnBadgeRadius = 23f;

    private void UpdateSpawnPreview(bool visible, bool desktop)
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
                int number = spawner.GetSpawnPointNumber(point);
                var world = new VisualElement();
                world.AddToClassList("spawn-world-group");
                world.AddToClassList("is-hidden");
                var heading = new Label(number.ToString());
                heading.AddToClassList("spawn-number");
                world.Add(heading);
                var composition = new VisualElement();
                composition.AddToClassList("spawn-composition");
                world.Add(composition);
                int typeCount = 0;
                for (int j = 0; j < spawnEntries.Count; j++)
                {
                    if (spawnEntries[j].spawnPoint != point) continue;
                    typeCount++;
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
                    composition.Add(row);
                }
                float previewWidth = Mathf.Max(SpawnBadgeRadius * 2f, Mathf.Min(typeCount, 4) * 92f + 4f);
                world.style.width = previewWidth;
                var edge = new VisualElement();
                edge.AddToClassList("spawn-edge-badge");
                edge.AddToClassList("is-hidden");
                var arrow = new VisualElement();
                arrow.AddToClassList("spawn-edge-arrow");
                arrow.generateVisualContent += DrawSpawnArrow;
                edge.Add(arrow);
                var index = new Label(number.ToString());
                index.AddToClassList("spawn-number");
                edge.Add(index);
                var distance = new Label();
                distance.AddToClassList("spawn-edge-distance");
                edge.Add(distance);
                IgnorePicking(world);
                IgnorePicking(edge);
                spawnPreview.Add(world);
                spawnPreview.Add(edge);
                spawnMarkers.Add(new SpawnMarker { Point = point, Visible = world, Edge = edge,
                    Distance = distance, Arrow = arrow, PreviewWidth = previewWidth });
            }
        }
        Camera camera = Camera.main;
        float width = spawnPreview.resolvedStyle.width;
        float height = spawnPreview.resolvedStyle.height;
        if (camera == null || width <= 0f || height <= 0f) return;
        Vector2 center = new(width * 0.5f, height * 0.5f);
        Rect actions = (desktop ? desktopActions : touchActions).worldBound;
        Rect stick = desktop ? default : joystick.worldBound;
        foreach (SpawnMarker marker in spawnMarkers)
        {
            if (marker.Point == null)
            {
                marker.Visible.AddToClassList("is-hidden");
                marker.Edge.AddToClassList("is-hidden");
                continue;
            }
            Vector3 viewport = camera.WorldToViewportPoint(marker.Point.position);
            Vector2 target = new(viewport.x * width, (1f - viewport.y) * height);
            bool onScreen = viewport.z > 0f && viewport.x >= 0f && viewport.x <= 1f &&
                viewport.y >= 0f && viewport.y <= 1f;
            marker.Visible.EnableInClassList("is-hidden", !onScreen);
            marker.Edge.EnableInClassList("is-hidden", onScreen);
            if (onScreen)
            {
                float left = Mathf.Clamp(target.x - marker.PreviewWidth * 0.5f,
                    12f, Mathf.Max(12f, width - marker.PreviewWidth - 12f));
                float previewHeight = Mathf.Max(104f, marker.Visible.layout.height);
                if (float.IsNaN(previewHeight)) previewHeight = 104f;
                float limit = height - previewHeight - 124f;
                if (!desktop && left < stick.xMax) limit = Mathf.Min(limit, stick.yMin - previewHeight - 8f);
                if (left + marker.PreviewWidth > actions.xMin) limit = Mathf.Min(limit, actions.yMin - previewHeight - 8f);
                marker.Visible.style.left = left;
                marker.Visible.style.top = Mathf.Clamp(target.y - previewHeight - 12f, 112f, Mathf.Max(112f, limit));
            }
            else
            {
                Vector2 direction = (target - center) * (viewport.z < 0f ? -1f : 1f);
                if (direction.sqrMagnitude < 0.001f) direction = Vector2.down;
                float scaleX = Mathf.Abs(direction.x) > 0.001f ?
                    (width * 0.5f - 50f) / Mathf.Abs(direction.x) : float.PositiveInfinity;
                float scaleY = Mathf.Abs(direction.y) > 0.001f ?
                    (height * 0.5f - (direction.y < 0f ? 142f : 148f)) / Mathf.Abs(direction.y) : float.PositiveInfinity;
                Vector2 edgePosition = center + direction * Mathf.Min(scaleX, scaleY);
                float limit = height - 148f;
                if (!desktop && edgePosition.x - 40f < stick.xMax) limit = Mathf.Min(limit, stick.yMin - 56f);
                if (edgePosition.x + 40f > actions.xMin) limit = Mathf.Min(limit, actions.yMin - 56f);
                marker.Edge.style.left = edgePosition.x - SpawnBadgeRadius;
                marker.Edge.style.top = Mathf.Clamp(edgePosition.y, 142f, Mathf.Max(142f, limit)) - SpawnBadgeRadius;
                Vector3 offset = marker.Point.position - movement.transform.position;
                offset.y = 0f;
                SetText(marker.Distance, GameLocalization.Format(LocalizationKeys.Distance, Mathf.RoundToInt(offset.magnitude)));
                Vector2 arrowOffset = direction.normalized * (SpawnBadgeRadius + 6f);
                marker.Arrow.style.left = SpawnBadgeRadius + arrowOffset.x - 13f;
                marker.Arrow.style.top = SpawnBadgeRadius + arrowOffset.y - 13f;
                marker.Arrow.style.rotate = new Rotate(new Angle(
                    Mathf.Atan2(direction.x, -direction.y) * Mathf.Rad2Deg));
            }
        }
    }

    private static void DrawSpawnArrow(MeshGenerationContext context)
    {
        var painter = context.painter2D;
        painter.fillColor = new Color32(247, 131, 155, 255);
        painter.BeginPath();
        painter.MoveTo(new Vector2(13f, 1f));
        painter.LineTo(new Vector2(4f, 19f));
        painter.LineTo(new Vector2(22f, 19f));
        painter.ClosePath();
        painter.Fill();
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
                string current = GameLocalization.Format(LocalizationKeys.BuildingLevel, source.CurrentLevel);
                string next = nextIndex > currentIndex
                    ? GameLocalization.Text(LocalizationKeys.BuildingNext) + "  ·  " + FormatLevel(levels[nextIndex])
                    : GameLocalization.Text(LocalizationKeys.BuildingMax);
                var progression = new Label($"{current}  ·  {FormatLevel(levels[currentIndex])}\n{next}");
                progression.AddToClassList("build-upgrade-summary");
                card.Add(progression);
            }
            var action = new Button(source.Activate) { tooltip = source.ActionText };
            action.AddToClassList("build-action");
            var actionLabel = new Label(source.ActionText);
            actionLabel.AddToClassList("build-action-label");
            action.Add(actionLabel);
            if (source.Cost.HasValue)
            {
                var credit = new VisualElement();
                credit.AddToClassList("economy-icon");
                credit.AddToClassList("build-credit");
                var center = new VisualElement();
                center.AddToClassList("economy-icon-center");
                credit.Add(center);
                action.Add(credit);
                var price = new Label(source.Cost.Value.ToString());
                price.AddToClassList("build-action-label");
                action.Add(price);
            }
            foreach (var child in action.Children()) IgnorePicking(child);
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
        if (level.Health.HasValue) values.Add(GameLocalization.Format(LocalizationKeys.BuildingHp, level.Health.Value));
        if (level.Damage.HasValue) values.Add(GameLocalization.Format(LocalizationKeys.Damage, level.Damage.Value));
        if (level.Income.HasValue) values.Add(GameLocalization.Format(LocalizationKeys.Income, level.Income.Value));
        return string.Join("  ·  ", values);
    }

    internal static string GetBuildingName(TowerOption option) => option.displayName switch
    {
        "Cannon Turret" => GameLocalization.Text(LocalizationKeys.Cannon),
        "Pulse" => GameLocalization.Text(LocalizationKeys.Pulse),
        "Mine" => GameLocalization.Text(LocalizationKeys.Mine),
        "Ratusha" => GameLocalization.Text(LocalizationKeys.Core),
        "Barracks / Melee" => GameLocalization.Text(LocalizationKeys.Melee),
        "Barracks / Ranged" => GameLocalization.Text(LocalizationKeys.Ranged),
        "Mortar" => GameLocalization.Text(LocalizationKeys.Mortar),
        "Wall" => GameLocalization.Text(LocalizationKeys.Wall),
        _ => option.displayName
    };

    internal static string GetBuildingDescription(TowerOption option)
    {
        if (!string.IsNullOrWhiteSpace(option.description)) return option.description;
        string name = option.displayName?.ToLowerInvariant() ?? "";
        if (name.Contains("pulse")) return GameLocalization.Text(LocalizationKeys.PulseDesc);
        if (name.Contains("mortar")) return GameLocalization.Text(LocalizationKeys.MortarDesc);
        return option.slotType switch
        {
            BuildSlotType.Mine => GameLocalization.Text(LocalizationKeys.MineDesc),
            BuildSlotType.TownHall => GameLocalization.Text(LocalizationKeys.CoreDesc),
            BuildSlotType.Barracks => GameLocalization.Text(LocalizationKeys.BarracksDesc),
            BuildSlotType.Wall => GameLocalization.Text(LocalizationKeys.WallDesc),
            _ => GameLocalization.Text(LocalizationKeys.CannonDesc)
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
        if (responsiveHud.IsDesktopLayout || movePointer >= 0 || evt.button != 0) return;
        Vector2 size = joystick.layout.size;
        if (!(size.x > 0f && size.y > 0f)) return;
        var bounds = root.Q("gameRoot").worldBound;
        moveOrigin = (Vector2)evt.position;
        Vector2 center = new(
            Mathf.Clamp(moveOrigin.x, bounds.xMin + size.x * 0.5f, bounds.xMax - size.x * 0.5f),
            Mathf.Clamp(moveOrigin.y, bounds.yMin + size.y * 0.5f, bounds.yMax - size.y * 0.5f));
        Vector2 restingCenter = touchControls.LocalToWorld(joystick.layout.center);
        Vector2 offset = center - restingCenter;
        joystick.style.translate = new Translate(offset.x, offset.y);
        movePointer = evt.pointerId;
        joystickZone.CapturePointer(movePointer);
        evt.StopPropagation();
    }

    private void Move(PointerMoveEvent evt)
    {
        if (evt.pointerId == movePointer) UpdateMove((Vector2)evt.position);
    }

    private void EndMove(PointerUpEvent evt)
    {
        if (evt.pointerId == movePointer && evt.button == 0) ResetMove();
    }

    private void CancelMove(PointerCancelEvent evt)
    {
        if (evt.pointerId == movePointer) ResetMove();
    }

    private void LostMoveCapture(PointerCaptureOutEvent evt)
    {
        if (evt.pointerId == movePointer) ResetMove();
    }

    private void UpdateMove(Vector2 worldPosition)
    {
        Vector2 direction = Vector2.ClampMagnitude((worldPosition - moveOrigin) / (joystick.layout.width * 0.38f), 1f);
        direction.y = -direction.y;
        movement.SetToolkitMoveInput(direction);
        float knobTravel = joystick.layout.width * (46f / 178f);
        joystickKnob.transform.position = new Vector3(direction.x * knobTravel, -direction.y * knobTravel, 0f);
        joystickSector.EnableInClassList("is-active", direction.sqrMagnitude > 0.0025f);
        joystickSector.transform.rotation = Quaternion.Euler(0f, 0f,
            45f - Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
    }
    private void ResetMove()
    {
        int pointer = movePointer;
        movePointer = -1;
        if (pointer >= 0 && joystickZone.HasPointerCapture(pointer))
            joystickZone.ReleasePointer(pointer);
        movement.SetToolkitMoveInput(Vector2.zero);
        joystick.style.translate = StyleKeyword.Null;
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
