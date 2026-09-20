# Visual direction

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
- A project-owned ArenaGround material replaces the noisy pink/green ground albedo
  in Level with a muted solid base. The original imported material and texture are
  untouched. This is a readability pass; subtle ground texture remains future art work.

## Further refinement

- Compare player/enemy silhouettes in a full combat playthrough before changing
  lighting, outlines, effects or balance.
- Add restrained ground detail without restoring the old high-frequency color noise.
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

