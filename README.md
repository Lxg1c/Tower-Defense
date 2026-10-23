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
   Open `Assets/Tower-Defense/Scenes/Level 1.unity` directly when iterating on gameplay.

The build scenes are **MainMenu**, then **Level 1**. The obsolete `GameScene.unity`
has been removed. Keep unused scenes out of Build Settings: the installed Test
Framework includes disabled entries when building a test Player.

Use the on-screen joystick to move (drag it with the mouse in the Editor).
The hero aims and shoots automatically. Hold the ultimate button to charge an
orb and release to fire it. Stand in a building zone until its progress circle
fills to choose a building or upgrade. Use Start Wave to begin combat after the
town hall exists. At town hall level 2, the barracks card offers melee and ranged
variants; the chosen type stays with that barracks. The ally command button switches
soldiers to following the hero.

Spiders can drop cyan slowing pickups. Walk over one to activate slowing shots
for 12 seconds (up to 24 seconds can be stored). While active, every normal
shot can slow a surviving enemy to half movement speed for 2.5 seconds. The
snowflake ring between Pause and the joystick shows the time left. Pickups
disappear after 12 seconds.
Destroyers can drop a shield pickup. Collecting it gives 50 shield points for
12 seconds; another pickup refills the shield and extends the remaining time,
up to 24 seconds. The snowflake and blue shield rings appear in one HUD row only
while their effects are active; the row closes gaps automatically. Damage removes
shield before HP, and either depletion or expiry ends it.
Shooter enemies can also drop a companion capsule. Collecting one summons an
automatic turret beside the hero for 18 seconds. Up to two can follow at once;
another pickup refreshes the one with less time remaining. The companion uses
the existing projectile targeting and disappears when its time ends or the hero
dies. Moving glimmers make all three pickup types easier to spot on the ground.
The in-game effect row shows the active companion count and remaining time.
The hero passes through enemy colliders, so mobs cannot serve as platforms.

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

The blue value above the health bar shows active shield strength, and a
transparent sphere surrounds the hero while the bonus is active. Shield is
only gained from pickups; respawning does not restore it.

### Frame rate and FPS display

The settings panels in the main menu and pause menu include **FPS: ON/OFF**.
The preference is saved locally. The counter reports frames per real second over
a 0.25-second window, including while gameplay is paused.
`GamePerformanceSettings` disables desktop VSync and the software frame limit.
On mobile it requests the current display refresh rate instead of Unity's default
30 FPS; actual performance still depends on the device and operating system.

### Permanent money and skill tree

The Main Menu has a **Skills** screen. Each victory awards permanent money:
100 on Normal, 150 on Hard and 225 on Nightmare. Replaying a cleared difficulty
pays again; reopening the Victory panel within the same run does not. The money
balance and purchases are saved locally with PlayerPrefs and are separate from
the per-run coins used to build towers. Use **Skills** on the Victory screen to
open the tree between levels, or open it from the main menu. This same permanent
balance can fund future loadout purchases.

Buy **Vitality** first to unlock the two branches. Each branch has three ranks:
Vitality adds 15% maximum player HP per rank, Firepower adds 10% normal-shot
damage per rank, and Rapid Fire adds 8% normal-shot fire rate per rank. Rank
costs are 100, 200, then 300 money. Bonuses apply when a level starts and do
not change an already-running level. Existing skill saves are migrated once:
one unused old point becomes 100 money, purchased ranks remain purchased, and
an old first-clear claim counts as Normal cleared. The three `LevelDefinition`
assets hold stable progression IDs and reward amounts; keep an ID unchanged
after release so existing saves still recognize cleared difficulties.

### Authored levels on one map

Main Menu shows one level card at a time. Swipe horizontally, use the side arrows,
or choose a page number to select **Level 1**, **Level 2**, or **Level 3**, and
choose Normal, Hard or Nightmare, then press **Play Level**. Hard opens after
beating Normal on that level, and Nightmare after beating Hard. Cleared
difficulties remain replayable. All choices load the same Level 1 scene with
different authored waves, spawn directions and starting coins. Restart retains
the selected level and difficulty; returning to the menu clears the selection.
Direct Editor play uses the explicitly assigned **Direct Play Level** on the
scene's LevelSetup (currently Level 1) on Normal.

`Level 1.unity` contains the shared gameplay setup, eight build zones and four
enemy spawn points. The new terrain has a MeshCollider; buildings, rocks and
fences have simple static colliders. Its `Surface` object uses the baked
`Scenes/Level 1/NavMesh-Surface.asset`. If you move the terrain or obstacles,
rebake that NavMeshSurface and check that all four spawn points remain on it.
The five buildable prefabs were reduced to fit this map; keep their model scale
and placement zones in sync when changing the environment.

To lay out more building spots, drag `Prefabs/level/LevelGrid.prefab` into the
scene and select it. The Scene view shows a 20 × 20 grid with 3-unit cells;
adjust its dimensions, cell size and position in the Inspector to match the
buildable floor. Optionally assign the scene's `TowerBuildZones` as Zones Parent
so new spots sit alongside the existing ones. Choose a building type, press
**Place building zones**, then click a free building footprint. The purple ghost
previews the building, while the green footprint shows
where it will stand; red means that position is blocked. Next, move the pointer
to one of the highlighted cells directly beside that footprint and click to
place the interaction zone there. The zone snaps only to those adjacent cells;
press Esc to cancel the building position. Town Hall and Barracks default to
2 × 2 cells, Defense and Mine to 1 × 1; these sizes are editable. The second
click creates an undoable `TowerBuildZone` prefab instance. Its marker sits in
the chosen adjacent cell, while its spawn point and building rotation stay at
the selected footprint and faces the interaction zone. The chosen model remains as a bright cyan editor-only
ghost after placement, including
when placement mode is stopped. Red marked cells are occupied building
footprints; orange marked cells are occupied interaction zones. These marks
also cover manually placed zones while the grid is selected. They are not
saved as scene objects and do not appear in the game. While placement mode is
active in Scene view, use
1–4 to select Town Hall, Defense, Mine or Barracks. Hover a grid-created
building or its interaction marker and press Delete/Backspace to remove its
zone; Ctrl+Z restores it. Click either cell to select the pair: a cyan line
connects its building footprint to its interaction cell. In the LevelGrid
Inspector, use **Move building** to choose a new footprint and then its adjacent
interaction cell, **Move interaction zone** to change only the marker side, or
**Delete selected zone** to remove the pair. **Face existing buildings toward their zones**
updates the rotation of previously placed grid buildings. Esc cancels a pending move or
clears the selection. Manually placed zones can be deleted with Unity's
normal selection and Delete. Stop placement with the same
button. Multiple grids can coexist in one scene, each with its own transform and
dimensions. Select only the building type in the grid Inspector. The editor
automatically shows the first matching `TowerLoadout` model as a placement ghost;
it does not choose the tower for the player. The grid is an Editor authoring aid, not 225 floor objects or a
runtime building system; existing spots are left where they were.

For an optional tiled floor, use the grid's `LevelGridTileSurface`. Enable
**Show Tiles** to display `Materials/LevelGridTile.mat`, made from the imported
tile base and normal maps. The supplied mask map has constant channels, so its
metallic, occlusion and smoothness values are stored as material constants
instead of an extra mobile texture sample. The material is assigned to the LevelGrid
prefab; rerun **Tools > Tower Defense > Configure Level Grid Tile Material**
if the source maps change. The texture is
mapped once onto each cell, but all cells share one mesh and material for mobile
rendering. **Height Above Grid** raises it slightly to avoid overlapping the
underlying ground. Leave **Solid Floor** off when the existing terrain supplies
collision; turn it on only when the tiles themselves are the walkable floor, then
rebake the NavMesh. The existing Level 1 floor is not replaced automatically.

| Level | Focus | Waves | Starting coins |
|---|---|---|---|
| 1 | Ground enemies; learn building and economy | 3 | 12 |
| 2 | Introduce flight and different approaches | 4 | 10 |
| 3 | Mixed enemies with armored threats | 4 | 10 |

These are starting balance presets, not fully playtested difficulty guarantees.
Each level asset contains its Normal waves plus two additional difficulty
variants. The current Hard and Nightmare presets increase enemy counts and
shorten spawn intervals; all numbers and enemy packs can be tuned independently
in the Inspector. The level's descriptive difficulty text is separate from
this three-step difficulty selection.

To create or tune a level:

1. Select an asset in Assets/Tower-Defense/Levels, duplicate one, or use
   **Create > Tower Defense > Level**.
2. Give it a unique progression ID and edit its title, description, Normal
   waves, starting coins and permanent money reward.
3. Expand **Waves**. Each wave has a coin reward and ordered **Spawn Groups**.
   Each group has a spawn-point index and enemy entries with counts and intervals.
4. Indices 0–3 refer to the four transforms in the scene WaveSpawner's
   **Spawn Points** array, in Inspector order. Groups execute sequentially.
5. Set both **Additional Difficulties** entries with their own starting coins,
   money rewards and waves. Add the asset to the Main Menu Canvas component's
   **Levels** list.
   Its selection button is created automatically. No additional scene is needed.
6. For direct iteration, assign that asset to **LevelSetup > Direct Play Level**
   in Level 1. Run the EditMode and PlayMode suites after changing code or settings.

Invalid level data is rejected. Runtime waves are copied, so playing or restarting
does not mutate authored assets. LevelDefinition assets are the sole authored
wave source; the earlier seeded-wave prototype and inline scene waves have been removed.
During the build phase, the HUD previews the next wave at each authored spawn
point. Each indicator points toward its spawn position and shows enemy icons
with total counts by type; repeated groups from the same point are combined.
The indicators disappear when combat starts and update for the following wave.


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
| Allies | `BarracksBuilding`, `AllySoldier`, `AllyCommand` | Player-selected melee or ranged barracks, combat, formation and following |

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
- `Prefabs/TowerLoadout.asset` lists buildings, costs and the town hall level
  required to show each build option. Town hall is available first; Cannon Tower
  and Mine unlock at level 1, Pulse Tower and both Barracks choices at level 2.
- Enemy option assets point at enemy prefabs. Each enemy prefab explicitly
  assigns its targeting, combat and navigation modules on `MobCore`.
- `Prefabs/EnemyLoadout.asset` contains one entry per enemy with slow-ammo,
  shield and companion pickup chances and durations. One roll selects at most one
  pickup per enemy; the chances in an entry must total no more than 100%.
  Set a chance to 0% to disable that drop;
  assign the shared pickup prefabs there. The Level 1 scene's WaveSpawner references
  this asset.
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
| Edit Mode | Wallet transactions; legal session transitions and terminal outcomes; duplicate/reentrant rewards; prefab health bindings; phase/unlock restrictions; enemy/shooter configuration; upgrade calculations and action limits; skill rewards, prerequisites, save data and scene bindings |
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
empty on the final level. The same Level 1 scene is reused for every encounter.

The ultimate waits eight seconds after a short charge and ten seconds after a
full charge: the 3–5 second charge cooldown has an additional five-second delay.
Its button shows the remaining seconds, while
the radial fill drains from the charge reached before firing.
