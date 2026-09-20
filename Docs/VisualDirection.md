# Visual direction

## Authored level selection — 2026-09-20

The menu now presents Level 1, Level 2 and Level 3, with the same dark/cyan
controls and a preview of difficulty, starting coins, wave count and enemy types.
All load the shared Level scene. The previous seeded pack controls are replaced.
Nightfall: Kingdom Frontier TD is the user's gameplay reference; this pass adds
authored encounters, without adding progression unlocks or a separate map scene.

Verification: 44 Edit Mode and 17 Play Mode tests passed. Each of the three real
menu actions loaded its expected waves and coins; Level 3 retained its recipe
on scene reload. The menu was inspected at 1334×750 and 1920×750. When switching
from Device Simulator to Game View, the preview canvas needed a runtime refresh
to discard the simulator's stale dimensions. No preview-only changes were saved.
The existing Constant Pixel Size mode remains unchanged. These are Editor
startup and layout checks, not complete balance playthroughs or device tests.


## Modal, menu and environment pass — 2026-09-20

Build cards and the shared tower/base upgrade modal now use opaque dark surfaces,
smooth cyan borders, readable current/next columns and capsule primary actions.
Close buttons are explicitly wired in Level; prefab overrides are recorded so
scene reloads retain both styling and callbacks. Existing artwork and purchase
references remain attached. Main Menu uses the same palette for wave setup.
Its existing Constant Pixel Size canvas is retained pending the user's scaling
decision; the compact panel was checked at 1334×750 and 1920×750.

`MobileEnvironment.shader` is applied through `MobileRocks.mat` to the 38 rock
renderers in Level. It preserves the original albedo texture and tint, adds a
cooler shaded-face tint, soft main-light wrap and a subtle view-dependent edge
accent. The original rock material is retained for comparison/reversion. The
floor, trees, buildings and character materials are unchanged.

The shader targets opaque, unbaked environment meshes. It uses one albedo sample
in the forward pass, main-light shadows, spherical-harmonic ambient and fog. It
has instancing support and a shared per-material constant buffer, plus shadow,
depth and depth-normal passes used when requested by URP. It adds no full-screen
pass, geometry outline, normal-map sampling, additional-light loop or new texture.
It intentionally does not implement baked lightmaps, transparency or the full
URP Lit feature set; do not replace unrelated materials indiscriminately.

Verification: 39 Edit Mode and 15 Play Mode tests passed. The generated Main Menu
preview was compared with the loaded Level recipe for Air Pressure / seed 12345;
empty seed input disabled Start. A real cannon upgrade spent 4 coins, applied the
first tier, disabled the locked next tier and closed through the new callback.
Modal layouts were inspected at 1334×750 and 1920×750. The rock shader was compared
with the original material in Play Mode and reported no Editor shader messages.

These are Editor checks, not an Android/iOS build or a device GPU benchmark.
Before shipping, compare GPU frame time and thermals on the weakest supported
phone, including a full final wave, with the original and new rock materials.
Only retain wider shader adoption if that measurement fits the frame budget.

## Current direction

Keep the overgrown ruins, saturated vegetation and cyan/pink sci-fi accents.
Use quieter surfaces behind units, dark UI panels, near-white functional text,
and cyan for important actions. Deltha remains a display font for large headings;
Liberation Sans is used for functional labels and numbers.

## Implemented in Level

- Grouped wave/time/enemy information and currency cards on dark panels.
- Cyan primary wave action and a larger backed pause control.
- Consistent typography in pause, outcome and upgrade screens; corrected defeat heading.
- Joystick, Home, Rally and charge controls share dark circular plates, cyan rims,
  white symbols and compact labels. Existing input handlers and charge/rally state
  indicators remain connected.
- Both screen-space canvases use the same 1920×1080 reference and height-based
  scaling. HUD and touch groups fit device safe areas.
- A build-phase hint explains the home marker before the base exists, then prompts
  building/upgrading and starting the wave. Combat and terminal phases hide it.
- Wave-preview icons and counts share one card. A chevron points toward the spawn;
  cards stay above the lower touch-control region.
- The earlier flat-ground experiment was reverted. Level keeps the user's authored
  ground texture; subsequent material work must preserve it.

## Further refinement

- Compare player/enemy silhouettes in a full combat playthrough before changing
  lighting, outlines, effects or balance.
- Preserve ground texture while reviewing unit contrast and lighting.
- Extend onboarding with building costs and clearer marker symbols when reviewing
  the complete purchase flow.
- Review purchase cards in context: role, affordability, locked states and next stats.
- Verify touch reach and safe areas on physical target devices, and review all modals.

## Verification

The health-bar teardown exception was reproduced by a failing regression test
before the ownership/pool fix. Play Mode coverage also checks additive scene unload,
objective lifecycle and wave-preview positioning. All 11 project Play Mode tests
passed after the final code changes.

The complete runtime HUD was reviewed in Device Simulator at landscape 2658×960
(foldable) and 1334×750 (iOS Classic). The smaller layout exposed the preview/Home
label overlap, leading to the reserved touch region. Rechecked both layouts after
that correction and verified Pause/Resume on the compact phone. Restored the
foldable preset and exited Play Mode. These checks are not a Player
build, a physical-device touch test, or a full gameplay/balance playthrough.


## Touch-control edge correction

The open Level scene now uses SmoothControl materials for antialiased discs, rings
and plain icon strokes, avoiding the shaded built-in sprite's dark borders.
Home/Rally are 190, charge 260 and joystick 310 reference pixels. Verified in
Play Mode on punch-hole and compact iOS simulator presets. These scene adjustments
remain unsaved because the scene already contained unrelated unsaved edits.


## User-provided icons and Start layout

Converted the new FlagIcon, homeIcon and lightningIcon scene objects from
world-space SpriteRenderers to centered, non-raycasting UI Images. Their source
artwork is unchanged. Restored Start to unit scale with a START WAVE heading and
aligned reward row. Saved Level and verified both phone layouts in Play Mode.
Start was temporarily forced visible only during the visual preview; its normal
base/build-phase visibility rules remain unchanged.


Start styling now matches the touch controls: a dark capsule, smooth cyan outline,
white title and cyan reward accent. Verified in the complete runtime HUD. Its
scene changes remain unsaved because Level was already dirty before this pass.
