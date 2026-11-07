using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Screen-space UI Toolkit view over the existing gameplay controllers.</summary>
[RequireComponent(typeof(UIDocument))]
public sealed class GameplayToolkitView : MonoBehaviour
{
    [SerializeField] private GameObject[] legacyScreenGroups;
    [SerializeField] private Canvas legacyOrderCanvas;
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

    private readonly List<CanvasGroup> hiddenGroups = new();
    private VisualElement root;
    private VisualElement touchControls;
    private VisualElement desktopActions;
    private VisualElement joystick;
    private VisualElement joystickKnob;
    private VisualElement buildPanel;
    private VisualElement orderLayer;
    private VisualElement screenOverlay;
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
    private Label timerLabel;
    private Label enemiesLabel;
    private Label coinsLabel;
    private Label incomeLabel;
    private Label guideLabel;
    private Label slowEffect;
    private Label shieldEffect;
    private Label turretEffect;
    private Label cooldownLabel;
    private Label fpsLabel;
    private Button fpsToggle;
    private float fpsElapsed;
    private int fpsFrames;
    private ProgressBar ultimateProgress;
    private Button startWaveButton;
    private Button homeButton;
    private Button desktopHome;
    private Button rallyButton;
    private Button desktopRally;
    private Button ultimateButton;
    private Button desktopUltimate;
    private Slider musicSlider;
    private Slider effectsSlider;
    private bool audioOpen;
    private bool ultimatePointerHeld;
    private int lastModalRevision = -1;
    private bool bound;

    private void OnEnable()
    {
        if (bound) return;
        var document = GetComponent<UIDocument>();
        if (document.panelSettings == null || document.visualTreeAsset == null ||
            legacyScreenGroups == null || legacyScreenGroups.Length != 4 ||
            legacyOrderCanvas == null || responsiveHud == null || spawner == null || wallet == null ||
            timer == null || movement == null || ultimate == null || teleport == null ||
            pauseMenu == null || victory == null || defeat == null || levelSetup == null ||
            buildModal == null || orders == null ||
            slowAmmo == null || playerHealth == null || companionTurrets == null)
        {
            Debug.LogError("[GameplayToolkitView] Assign all scene controllers, legacy screen groups and UI Toolkit assets.", this);
            enabled = false;
            return;
        }
        foreach (GameObject group in legacyScreenGroups)
            if (group == null)
            {
                Debug.LogError("[GameplayToolkitView] A legacy screen group is missing.", this);
                enabled = false;
                return;
            }

        root = document.rootVisualElement;
        root.AddToClassList("game-document-root");
        touchControls = root.Q("touchControls");
        desktopActions = root.Q("desktopActions");
        joystick = root.Q("joystick");
        joystickKnob = root.Q("joystickKnob");
        buildPanel = root.Q("buildPanel");
        orderLayer = root.Q("orderLayer");
        screenOverlay = root.Q("screenOverlay");
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
        timerLabel = root.Q<Label>("timerLabel");
        enemiesLabel = root.Q<Label>("enemiesLabel");
        coinsLabel = root.Q<Label>("coinsLabel");
        incomeLabel = root.Q<Label>("incomeLabel");
        guideLabel = root.Q<Label>("guideLabel");
        slowEffect = root.Q<Label>("slowEffect");
        shieldEffect = root.Q<Label>("shieldEffect");
        turretEffect = root.Q<Label>("turretEffect");
        cooldownLabel = root.Q<Label>("ultimateCooldown");
        fpsLabel = root.Q<Label>("fpsLabel");
        fpsToggle = root.Q<Button>("fpsToggle");
        ultimateProgress = root.Q<ProgressBar>("ultimateProgress");
        startWaveButton = root.Q<Button>("startWaveButton");
        homeButton = root.Q<Button>("homeButton");
        desktopHome = root.Q<Button>("desktopHome");
        rallyButton = root.Q<Button>("rallyButton");
        desktopRally = root.Q<Button>("desktopRally");
        ultimateButton = root.Q<Button>("ultimateButton");
        desktopUltimate = root.Q<Button>("desktopUltimate");
        musicSlider = root.Q<Slider>("musicSlider");
        effectsSlider = root.Q<Slider>("effectsSlider");
        if (touchControls == null || desktopActions == null || joystick == null ||
            joystickKnob == null || buildPanel == null ||
            orderLayer == null || screenOverlay == null || pausePanel == null ||
            audioPanel == null || exitConfirmPanel == null || victoryPanel == null || defeatPanel == null ||
            exitConfirmTitle == null || exitConfirmDescription == null || exitConfirmButton == null ||
            defeatReward == null ||
            buildCards == null || waveLabel == null || timerLabel == null ||
            enemiesLabel == null || coinsLabel == null || incomeLabel == null ||
            guideLabel == null || slowEffect == null || shieldEffect == null ||
            turretEffect == null || cooldownLabel == null || fpsLabel == null || fpsToggle == null || ultimateProgress == null ||
            startWaveButton == null || homeButton == null || desktopHome == null ||
            rallyButton == null || desktopRally == null ||
            ultimateButton == null || desktopUltimate == null || musicSlider == null || effectsSlider == null)
        {
            Debug.LogError("[GameplayToolkitView] Gameplay.uxml is missing a required named element.", this);
            enabled = false;
            return;
        }

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
        bound = true;
    }

    private void Start()
    {
        if (!bound) return;
        foreach (GameObject screen in legacyScreenGroups)
        {
            CanvasGroup group = screen.GetComponent<CanvasGroup>();
            if (group == null)
            {
                Debug.LogError("[GameplayToolkitView] Each legacy screen group needs a CanvasGroup.", screen);
                enabled = false;
                return;
            }
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
            hiddenGroups.Add(group);
        }
        var fps = legacyScreenGroups[0].transform.parent.Find("FpsOverlay");
        if (fps != null && fps.TryGetComponent(out Canvas fpsCanvas)) fpsCanvas.enabled = false;
        var orderGroup = legacyOrderCanvas.GetComponent<CanvasGroup>();
        if (orderGroup == null) orderGroup = legacyOrderCanvas.gameObject.AddComponent<CanvasGroup>();
        orderGroup.alpha = 0f;
        orderGroup.blocksRaycasts = false;
        orderGroup.interactable = false;
        hiddenGroups.Add(orderGroup);
        RefreshAudio();
        RefreshFps();
        RefreshBuildCards();
    }

    private void OnDisable()
    {
        if (!bound) return;
        buildModal.onOpened.RemoveListener(RefreshBuildCards);
        buildModal.onClosed.RemoveListener(ClearBuildCards);
        GameAudioSettings.Changed -= RefreshAudio;
        GamePerformanceSettings.Changed -= RefreshFps;
        movement.ClearToolkitMoveInput();
        if (ultimatePointerHeld) ultimate.Release();
        hiddenGroups.Clear();
        bound = false;
    }

    private void Update()
    {
        if (!bound || hiddenGroups.Count == 0) return;
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
        fpsLabel.EnableInClassList("is-hidden", !GamePerformanceSettings.ShowFps || modal);
        bool desktop = responsiveHud.IsDesktopLayout;
        root.Q("topBar").EnableInClassList("is-hidden", order || won || lost);
        root.Q("effectRow").EnableInClassList("is-hidden", order || won || lost);
        touchControls.EnableInClassList("is-hidden", desktop || order || paused || won || lost);
        root.Q("actionStack").EnableInClassList("is-hidden", modal);
        desktopActions.EnableInClassList("is-hidden", !desktop || order || paused || won || lost || modal);
        rallyButton.EnableInClassList("is-hidden", levelSetup.IsFirstLocation);
        desktopRally.EnableInClassList("is-hidden", levelSetup.IsFirstLocation);
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
            SetText(exitConfirmTitle, restarting ? "RESTART RUN?" : "RETURN TO MENU?");
            SetText(exitConfirmDescription, "Current wave progress will be lost. No reward is granted.");
            SetText(exitConfirmButton, restarting ? "RESTART" : "MAIN MENU");
        }

        int wave = spawner.IsBuildPhase ? spawner.NextWaveIndex + 1 :
            Mathf.Max(1, spawner.NextWaveIndex);
        SetText(waveLabel, $"WAVE {wave} / {spawner.WaveCount}");
        int seconds = Mathf.FloorToInt(timer.ElapsedSeconds);
        SetText(timerLabel, $"{seconds / 60:00}:{seconds % 60:00}");
        SetText(coinsLabel, $"◆  {wallet.Coins}");
        SetText(incomeLabel, $"+{MineIncome.TotalCoinsPerWave} / WAVE");
        var preview = spawner.IsBuildPhase ? spawner.NextWave :
            spawner.GetWave(Mathf.Clamp(wave - 1, 0, spawner.WaveCount - 1));
        SetText(enemiesLabel, $"{CountEnemies(preview)} ENEMIES");
        SetText(guideLabel, spawner.IsBuildPhase ? Base.Instance == null
            ? "STAND ON THE HOME MARKER TO BUILD YOUR BASE"
            : "PREPARE YOUR DEFENSES" : "DEFEND THE BASE");
        guideLabel.EnableInClassList("is-hidden", !spawner.IsBuildPhase || order || paused || modal || won || lost);
        startWaveButton.EnableInClassList("is-hidden", !spawner.IsBuildPhase || Base.Instance == null ||
            modal || order || paused || won || lost);
        if (spawner.IsBuildPhase && spawner.NextWave != null)
            SetText(startWaveButton, $"START WAVE  ·  +{spawner.NextWave.coinReward}");

        SetEffect(slowEffect, slowAmmo.RemainingSeconds > 0f,
            $"❄  SLOW  {Mathf.CeilToInt(slowAmmo.RemainingSeconds)}s");
        SetEffect(shieldEffect, playerHealth.CurrentShield > 0f && playerHealth.RemainingShieldSeconds > 0f,
            $"◇  SHIELD  {Mathf.CeilToInt(playerHealth.RemainingShieldSeconds)}s");
        SetEffect(turretEffect, companionTurrets.ActiveCount > 0 && companionTurrets.RemainingSeconds > 0f,
            $"✦  TURRET ×{companionTurrets.ActiveCount}  {Mathf.CeilToInt(companionTurrets.RemainingSeconds)}s");
        bool homeAvailable = teleport.gameObject.activeInHierarchy && spawner.IsBuildPhase && !playerHealth.IsGhost;
        homeButton.SetEnabled(homeAvailable);
        desktopHome.SetEnabled(homeAvailable);
        ultimateButton.SetEnabled(ultimate.CanReceiveInput || ultimate.IsCharging);
        desktopUltimate.SetEnabled(ultimate.CanReceiveInput || ultimate.IsCharging);
        ultimateProgress.value = (ultimate.IsCharging ? ultimate.ChargeProgress : ultimate.CooldownProgress) * 100f;
        cooldownLabel.EnableInClassList("is-hidden", !ultimate.IsOnCooldown);
        if (ultimate.IsOnCooldown) SetText(cooldownLabel, Mathf.CeilToInt(ultimate.CooldownRemaining).ToString());
        root.Q<Button>("nextLevelButton").EnableInClassList("is-hidden", !victory.CanPlayNextLevel);
        if (won) SetText(root.Q<Label>("victoryReward"), $"+{levelSetup.VictoryMoneyReward} MONEY EARNED");
        if (lost) SetText(defeatReward, $"+{levelSetup.DefeatMoneyReward} MONEY EARNED");
        if (buildModal.IsOpen && buildModal.Revision != lastModalRevision) RefreshBuildCards();
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
        foreach (TowerCard source in buildModal.ActiveCards)
        {
            displayedCards++;
            var levels = BuildingLevelList.Read(source.Option);
            var card = new VisualElement();
            card.AddToClassList("build-card");
            var header = new VisualElement();
            header.AddToClassList("build-card-header");
            if (source.Option.icon != null)
            {
                var icon = new Image { sprite = source.Option.icon };
                icon.AddToClassList("build-card-icon");
                header.Add(icon);
            }
            var title = new Label(source.Option.displayName.ToUpperInvariant());
            title.AddToClassList("build-card-title");
            header.Add(title);
            card.Add(header);
            var progression = new VisualElement();
            progression.AddToClassList("build-levels");
            int targetLevel = Mathf.Max(1, source.CurrentLevel);
            for (int i = 0; i < levels.Count; i++)
            {
                var level = levels[i];
                var row = new VisualElement();
                row.AddToClassList("build-level-row");
                if (i + 1 == targetLevel) row.AddToClassList("build-level-target");
                var number = new Label($"LV {i + 1:00}");
                number.AddToClassList("build-level-number");
                row.Add(number);
                var values = new System.Text.StringBuilder();
                if (level.Health.HasValue) values.Append($"HP {level.Health.Value:0}");
                if (level.Damage.HasValue) values.Append($"   DMG {level.Damage.Value:0}");
                if (level.Income.HasValue) values.Append($"   INCOME {level.Income.Value}");
                var detail = new Label(values.ToString());
                detail.AddToClassList("build-level-values");
                row.Add(detail);
                progression.Add(row);
            }
            card.Add(progression);
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
    }

    private void ClearBuildCards()
    {
        buildCards.Clear();
        buildPanel.RemoveFromClassList("single-card-panel");
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
        if (musicSlider == null) return;
        musicSlider.SetValueWithoutNotify(GameAudioSettings.Music);
        effectsSlider.SetValueWithoutNotify(GameAudioSettings.Effects);
        SetText(root.Q<Label>("musicValue"), $"{Mathf.RoundToInt(GameAudioSettings.Music * 100)}%");
        SetText(root.Q<Label>("effectsValue"), $"{Mathf.RoundToInt(GameAudioSettings.Effects * 100)}%");
    }

    private void RefreshFps()
    {
        fpsToggle.text = GamePerformanceSettings.ShowFps ? "FPS DISPLAY: ON" : "FPS DISPLAY: OFF";
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
        joystickKnob.transform.position = new Vector3(direction.x * 48f, -direction.y * 48f, 0f);
    }
    private void ResetMove()
    {
        movement.SetToolkitMoveInput(Vector2.zero);
        joystickKnob.transform.position = Vector3.zero;
    }

    private void BindUltimate(Button button)
    {
        button.RegisterCallback<PointerDownEvent>(evt =>
        {
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
