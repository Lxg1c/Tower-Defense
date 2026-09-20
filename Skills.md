# Tower Defense working rules

These are project conventions for contributors and coding agents. This file is
project guidance, not an installed Codex skill package.

## Plan before editing

- Before making any edits, present a short, concrete plan of action: the intended
  outcome, the files or systems in scope, the implementation steps and verification.
- Read and inspect the relevant current state first. Mark assumptions and resolve
  important unknowns before editing the affected system.
- Follow the stated plan and scope. Do not add adjacent refactors, visual redesigns
  or gameplay changes simply because an opportunity appears during the work.
- If evidence or new user input requires a different approach, explain the reason
  and update the plan before making the newly scoped edits. Do not persist with a
  plan shown to be wrong, or silently switch approaches.
- A plan is not a mandatory approval gate. Continue work already authorized by the
  user; ask only when a material unresolved choice needs their input.
- Finish by reporting completed steps, verification results and any remaining work.

## Architecture

- Keep Unity's component-based structure. Improve one concrete responsibility at
  a time; introduce frameworks or additional layers only for a demonstrated need.
- `GameSession` owns legal phase transitions, terminal outcomes and wave rewards.
  `WaveSpawner` handles spawning and adapts session notifications to Unity.
- `BuildingSlot` owns building transactions and action limits. Gameplay rules
  must remain independent of labels, modal layouts and formatting.
- `Damageable` owns health rules. `HealthBarBinding` and `HealthBarManager` own
  health presentation and its subscription lifetime.
- Prefer assigned references and constructor parameters for required dependencies.
  Do not silently replace missing configuration, grant free purchases, invent
  substitute gameplay, or retry initialization after an arbitrary delay.
- Distinguish setup errors from normal gameplay: target loss, insufficient funds,
  optional presentation overrides and terminal session states have explicit rules.
- Use direct operations for transactions and events for their results. Subscribe
  and unsubscribe symmetrically against the same publisher instance. Consider
  disable/re-enable, pooled objects, scene unloading and callback re-entry.

## Unity assets and source control

- Use the running Unity Editor's APIs for scene/prefab/asset edits. Inspect current
  state first and preserve existing references, prefab relationships and GUIDs.
  Do not hand-edit serialized scene or prefab YAML.
- Migrate dependent assets before deleting serialized fields. Validate the migrated
  references; successful C# compilation does not establish correct scene wiring.
- Do not save over unrelated unsaved scene edits. Preserve unrelated working-tree
  changes. Do not stage, revert or commit the entire repository as a shortcut.
- Keep `.meta` files with assets. Keep generated Input Actions code generated.
- `.vscode`, `.claude`, `Library`, `Temp` and local test reports stay out of Git.
- The supported build sequence is MainMenu then Level. Keep obsolete scenes out
  of Build Settings: the installed Test Framework also includes disabled entries
  in Player test builds.

## Tests and verification

- Add regression tests for changed gameplay rules and reproduced bugs. Assert
  observable outcomes: coins, phase, health, spawned objects and notification counts.
- Use `TowerDefense.EditMode.Tests` for rules/configuration and
  `TowerDefense.PlayMode.Tests` for Unity lifecycle, combat and integration behavior.
- Run the affected suites after code changes. Check compilation and report actual
  results; a launched test run is not a passing test run.
- Test fixtures must clean up objects, listeners, singleton state and time scale.
  Protect existing scenes; use temporary/preview scenes where appropriate.
- For presentation-only edits, inspect rendered before/after views and relevant
  aspect ratios. Add tests when behavior changes, not tests that repeat styling values.
- State verification limits: a startup check is not a full playthrough, an Editor
  run is not a Player build, and passing tests do not prove visual quality.

## UI and visuals

- Inspect the real game view before judging or editing visuals. Camera-only captures
  can omit Screen Space Overlay UI; capture the complete HUD for visual review.
- Preserve the existing art direction unless a different direction is requested.
  Improve hierarchy, contrast, spacing, typography and action feedback coherently.
- Preserve authored ground textures when adjusting readability. Do not replace
  textured surfaces with flat colors without an explicit request.
- Avoid enlarging tiny built-in UI sprites. Use suitable-resolution graphics and
  antialiased strokes; touch controls need generous hit areas and readable labels.
- Preserve button callbacks and gameplay bindings during styling. Prefer targeted
  edits to existing Canvas hierarchies over replacing whole screens.
- Keep important gameplay visible beneath the HUD. Give primary actions more visual
  emphasis than secondary controls, and make unavailable actions understandable.
- Check supported screen sizes and touch controls. Do not assume desktop window
  dimensions are the intended player resolution. Keep world-space UI separate from
  screen-space HUD scaling decisions.
- Profile visual performance changes before claiming improvements. Do not migrate
  rendering pipelines or introduce ECS merely as a general cleanup step.

## Documentation and communication

- Keep README and the architecture guide aligned with meaningful changes.
- Explain what changed, why, how it was checked and any remaining limitations.
- Continue authorized reversible work without repeatedly asking permission.
  Ask focused questions when an unresolved design choice would materially change
  the result; do not invent preferences and describe them as user requirements.
