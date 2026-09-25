# Tower Defense

A Unity 3D tower-defense prototype with a directly controlled hero, automatic
shooting, build/upgrade phases, enemy waves and allied squads.

Contributor and agent conventions are in [Skills.md](Skills.md).
See [Docs/VisualDirection.md](Docs/VisualDirection.md) for the first UI polish pass
and the remaining visual priorities.
See [Docs/Balance.md](Docs/Balance.md) for the economy review, measured wave
results and the next gameplay balance experiments.

## Open and play

1. Install **Unity 6000.0.72f1** through Unity Hub. This is the version recorded in
   `ProjectSettings/ProjectVersion.txt`.
2. Add this repository folder as a project in Unity Hub and open it. Let package
   resolution, asset import and script compilation finish.
3. Open `Assets/Tower-Defense/Scenes/MainMenu.unity` and press Play, then Start.
   Open `Level.unity` directly when iterating on gameplay.

The build scenes are **MainMenu**, then **Level**. The obsolete `GameScene.unity`
has been removed. Keep unused scenes out of Build Settings: the installed Test
Framework includes disabled entries when building a test Player.

Use the on-screen joystick to move (drag it with the mouse in the Editor).
The hero aims and shoots automatically. Hold the ultimate button to charge an
orb and release to fire it. Stand in a building zone until its progress circle
fills to choose a building or upgrade. Use Start Wave to begin combat after the
town hall exists. The ally command button switches soldiers to following the hero.

Building panels leave movement available and do not darken the world. Walk out
of the building zone to dismiss its panel; there is no Close button. A successful
build or upgrade also closes the panel and consumes that zone's action for the phase.
Construction and upgrades share `BuildingModal.prefab` and `TowerCard.prefab`.
Each card lists every authored level (starting at 1), with health and damage icons;
mines show income per wave instead of damage. The current level is highlighted,
and only buildings with an unlocked next upgrade show an interaction point.
Upgrade the Town Hall to reveal newly eligible points. Maximum-level buildings
have no upgrade point. Hold progress appears above each point on the HUD, so
scenery cannot hide the ring. It stays full while the panel is open and clears
when walking away or completing the purchase.

The loop is: build/upgrade, fight a wave, receive rewards and repairs, then repeat.
Player death enters a ghost/respawn state. Destroyed towers recover after a wave.
Base destruction ends the run; clearing the final wave wins.

### Authored levels on one map

Main Menu shows one level card at a time. Swipe horizontally, use the side arrows,
or choose a page number to select **Level 1**, **Level 2**, or **Level 3**, and
press **Play Level**. All choices load the same Level scene with different
authored waves, spawn directions and starting coins. Restart retains the selected
level; returning to the menu clears it. Direct Editor play uses the explicitly
assigned **Direct Play Level** on the scene's LevelSetup (currently Level 1).

| Level | Focus | Waves | Starting coins |
|---|---|---|---|
| 1 | Ground enemies; learn building and economy | 3 | 12 |
| 2 | Introduce flight and different approaches | 4 | 10 |
| 3 | Mixed enemies with armored threats | 4 | 10 |

These are starting balance presets, not fully playtested difficulty guarantees.
Difficulty text is a label; enemy counts, intervals, composition, directions,
starting coins and wave rewards define the actual challenge.

To create or tune a level:

1. Select an asset in Assets/Tower-Defense/Levels, duplicate one, or use
   **Create > Tower Defense > Level**.
2. Edit its title, description, difficulty label and starting coins.
3. Expand **Waves**. Each wave has a coin reward and ordered **Spawn Groups**.
   Each group has a spawn-point index and enemy entries with counts and intervals.
4. Indices 0–3 refer to the four transforms in the scene WaveSpawner's
   **Spawn Points** array, in Inspector order. Groups execute sequentially.
5. Add the asset to the Main Menu Canvas component's **Levels** list.
   Its selection button is created automatically. No additional scene is needed.
6. For direct iteration, assign that asset to **LevelSetup > Direct Play Level**
   in Level. Run the EditMode and PlayMode suites after changing code or settings.

Invalid level data is rejected. Runtime waves are copied, so playing or restarting
does not mutate authored assets. LevelDefinition assets are the sole authored
wave source; the earlier seeded-wave prototype and inline scene waves have been removed.


The new rock material uses `TowerDefense/Mobile/Environment`. It preserves the
original texture and adds soft directional shading and a restrained edge tint.
See [Docs/VisualDirection.md](Docs/VisualDirection.md) for its rendering scope and
the remaining physical-device performance checks.

## Dependencies

Unity Package Manager resolves the versions pinned in `Packages/manifest.json`
and `Packages/packages-lock.json`. Key packages include URP 17.0.4, Input System
1.19.0, AI Navigation 2.0.12, Cinemachine 3.1.6 and Unity Test Framework 1.6.0.
The project also contains Joystick Pack, DOTween and art/audio assets.

Unity Pipeline and the Unity CLI support automation against an open Editor.
They are not required to run the game or use the Test Runner window.

## Code map

Gameplay source is under `Assets/Tower-Defense/Scripts`.

| Area | Entry points | Responsibility |
|---|---|---|
| Session | `GameSession` | Legal phase transitions, victory/defeat and one reward per cleared wave |
| Wave loop | `WaveSpawner` | Spawning, base-death observation and forwarding session notifications |
| Health | `Damageable`, `MobHealth`, `PlayerHealth`, `TowerHealth`, `Base` | Damage and entity-specific death/recovery |
| Enemies | `MobCore`, target selectors, combat behaviors, navigation modules | Composable enemy AI |
| Shooting | `DetectionZone`, `Shooter`, `Projectile` | Target detection, volleys and projectile damage |
| Building | `BuildingSlot`, `TowerPlacementZone` | Validated building operations and zone interaction |
| Upgrades | `TowerUpgrade`, `BaseUpgrade`, `BuildingStats` | Costs, unlocks and numeric stats |
| Presentation | `HealthBarBinding`, `HealthBarManager`, `UpgradeStatsFormatter`, modals, HUDs | Health display, formatting and user interaction |
| Allies | `BarracksBuilding`, `AllySoldier`, `AllyCommand` | Squads, combat, formation and following |

Start reading `GameSession`, then `WaveSpawner.Start`, `StartNextWave` and `RunCombat`. Then follow
`Damageable.TakeDamage` to `MobHealth.OnDeath` and back to the spawner's death
handler. See `Docs/Architecture.md` for the building flow and refactoring roadmap.

The gameplay assembly is `TowerDefense.Runtime`. Joystick code has separate
runtime/editor assemblies so gameplay and tests can reference it explicitly.
The Input Actions asset and its generated C# wrapper live together under
`Scripts/Input`; regenerate the wrapper through the Input Actions importer,
rather than editing generated code.

## Configuration belongs in assets

- Level assets define waves, timings, rewards and starting coins. The scene WaveSpawner owns spawn transforms; LevelSetup assigns the selected level.
- Game-over and victory screens require an assigned spawner; they observe session
  outcomes. The game-over screen does not decide whether the session is defeated.
- Entities with health bars require `HealthBarBinding`, with their own `Damageable`
  assigned. Prefab overrides, anchors and visibility on death belong on this binding.
  `HealthBarManager` requires the scene camera and supplies the default bar prefab.
- `Prefabs/TowerLoadout.asset` lists the available buildings and their costs.
- Enemy option assets point at enemy prefabs. Each enemy prefab explicitly
  assigns its targeting, combat and navigation modules on `MobCore`.
- Shooter components require a nonempty fire-point array and a prefab containing
  `Projectile`. A missing projectile is a setup error, not an instant-hit weapon.
- Upgrade levels are configured on the building prefabs. Multipliers apply to
  original prefab stats, not cumulatively to the previous upgrade.
- Keep `.meta` files with their assets. Move assets through Unity so GUID-based
  scene and prefab references survive.

## Testing

The project uses **Unity Test Framework + NUnit**. No separate test package
installation is needed. [Unity's testing guide](https://docs.unity.com/en-us/engine/6000.7/manual/scripting/test-framework-introduction)
explains the framework; the installed package is 1.6.0.

### Run in the Editor

1. Open **Window > General > Test Runner**.
2. In **EditMode**, select `TowerDefense.EditMode.Tests` and run the selected tests.
3. In **PlayMode**, select `TowerDefense.PlayMode.Tests` and run the selected tests.

Select the project assemblies to avoid mixing these results with tests supplied
by third-party packages. Play Mode tests temporarily enter Play mode.

| Suite | What it protects |
|---|---|
| Edit Mode | Wallet transactions; legal session transitions and terminal outcomes; duplicate/reentrant rewards; prefab health bindings; phase/unlock restrictions; enemy/shooter configuration; upgrade calculations and action limits |
| Play Mode | Missing-projectile rejection; repeated firing; damage on contact; single death notification; defeat during wave startup; health-bar creation order, enable/disable, death, revival and scene teardown; build-objective lifecycle |

The earlier 41-assertion architecture regression sweep is also called from the
Edit Mode suite. Its menu shortcut, **Tools > Tower Defense > Run Architecture
Checks**, remains available. That shortcut only runs the sweep, not every test.

### Run from a terminal or CI

For batch execution, close this project's Editor first. From the repository root
in PowerShell, using the installed Unity Editor executable:

```powershell
$unityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.0.72f1\Editor\Unity.exe'
$projectPath = (Get-Location).Path
New-Item -ItemType Directory -Force TestResults | Out-Null
& $unityEditor -batchmode -projectPath $projectPath -runTests -testPlatform EditMode -assemblyNames TowerDefense.EditMode.Tests -testResults "$projectPath/TestResults/editmode.xml" -logFile "$projectPath/TestResults/editmode.log" | Out-Host
$editModeExitCode = $LASTEXITCODE
& $unityEditor -batchmode -projectPath $projectPath -runTests -testPlatform PlayMode -assemblyNames TowerDefense.PlayMode.Tests -testResults "$projectPath/TestResults/playmode.xml" -logFile "$projectPath/TestResults/playmode.log" | Out-Host
$playModeExitCode = $LASTEXITCODE
if ($editModeExitCode -ne 0 -or $playModeExitCode -ne 0) { throw 'Unity tests failed; inspect TestResults.' }
```

Inspect each process exit code (`$LASTEXITCODE`) and NUnit XML report. Do not add
`-quit`: the test runner exits when the run is complete. CI should run each suite
as a separate step and retain its report and log even on failure. `TestResults`
is ignored by Git.

### Adding tests

Use `[Test]` and `[TestCase]` for rules that can be checked immediately. Use a
`[UnityTest]` returning `IEnumerator` when a behavior needs frames or coroutines.
Put tests in the matching folder under `Assets/Tower-Defense/Tests`; their test
assembly definitions exclude them from normal game builds.

When fixing a bug, first add a test that fails for its specific trigger. Assert
observable outcomes such as coins, health, spawned objects and events. Avoid
tests that merely duplicate the implementation. Configuration tests are useful
for required references that compilation alone cannot check.

These suites are a starting regression net, not proof that every game system is
correct. Navigation, whole-level victory/defeat, pause interactions, mobile input
and visual/audio behavior still need additional coverage and a manual playthrough.

## Dependency and fallback policy

Required references must be explicit and validated. A broken configuration should
produce an actionable error and stop the affected operation. Fix the asset or
initialization order instead of guessing a replacement or adding retry delays.
Build/upgrade menus require a wallet; they never assume unlimited money.

Expected gameplay states need explicit behavior too: a projectile losing a dead
target, an enemy finding no nearby threat, or a building reaching its last upgrade
are normal states. They should have documented rules and tests.

Cleanup is incremental. Existing delayed singleton binding, legacy wave fields,
animation compatibility and automatic tower-component shutdown still need review.
Do not remove a compatibility field until all dependent assets have been migrated
and a regression test covers the resulting contract.

Local `.vscode` and `.claude` settings are ignored and are not tracked.

## License

Project code is covered by the repository's MIT `LICENSE`. Consult the included
licenses for third-party assets and packages.

### Main menu and sound

The home screen opens level selection, audio settings, or exits the game. Music and
sound effects have separate sliders (0–100%). Changes apply immediately and persist
in PlayerPrefs. Music uses BackgroundMusic; other AudioSources follow the effects
volume. Back returns to the home screen.

During gameplay, Pause offers Resume, Settings, Restart and Main Menu. Audio
settings work while paused and share the saved music/effects volumes with the
home menu. Victory offers Next Level, Replay and Main Menu; defeat offers Main
Menu. Set Next Level on each LevelDefinition to author progression, and leave it
empty on the final level. The same Level scene is reused for every encounter.
