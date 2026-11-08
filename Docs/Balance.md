# Gameplay balance review

## Current Level 1 proposal — 8 November 2026

The unupgraded R-07 fires two staggered projectiles per volley. Each projectile
deals 5 damage, so a full volley deals 10 when both hit. Base enemy HP is Spider
30, Shooter 20, Bat 25 and Destroyer 360. Normal therefore needs 3/2/3/36 full
volleys; Hard uses 1.5x HP, so Spider needs 5, Shooter 3 and Bat 4 full volleys.
Nightmare uses 2x HP.
Enemy attack damage remains unchanged. Hard/Nightmare movement speed is 1.08x/1.16x;
temporary slows preserve those base speeds when they expire. Permanent player
damage skills and other damage sources change the actual number of hits.

| Wave | Normal | Hard | Nightmare |
| --- | ---: | ---: | ---: |
| 1 | 6 | 6 | 6 |
| 2 | 14 | 15 | 16 |
| 3 | 28 | 29 | 30 |
| 4 | 45 | 46 | 47 |
| 5 | 66 | 67 | 68 |

The Normal finale contains 55 Spider, 6 Bat, 4 Shooter and 1 Destroyer.
Spawn intervals are 0.65/0.55/0.45/0.35/0.25 seconds from wave 1 to wave 5
on all three difficulties. The final wave now spawns its 66 Normal enemies in
roughly 16.5 seconds of game time instead of 26.4 seconds. An Editor autopilot
run after this change completed all five Normal waves without permanent skills;
the fifth wave reached 51 enemies alive at once, up from 36 in the previous run.
These counts target a crowded fight like the reference; they are not yet a
measured performance or human balance result. Level 1 points now unlock in
part through construction: the second mine follows the first, and Pulse points
follow the cannon and then earlier Pulse points, alongside town-hall levels.
Wave 2 pays 8 build coins instead of 4, so the player can choose the town-hall
upgrade and first Pulse before wave 3 after either opening. Later wave rewards
remain 4 coins. This economy still needs a live-player balance check.

An Editor-only moving-hero autopilot with no permanent skills completed all five
Normal waves after opening with a mine. The fifth wave reached 36 enemies alive
at once. The same simple route on Hard reached wave 5 but lost; a cannon-first
route before the wave-2 reward adjustment lost in wave 3. These runs verify that
the wave and building systems execute, not that a human can or cannot win Hard.

## Previous November 2026 first-location pass

The sections below preserve earlier measurements and configurations as history.
The earlier `Level1.asset` had five waves per difficulty and 8 starting coins.
After the 5-coin town hall, the player can choose an early 2-coin mine or a
3-coin cannon. Automated moving-hero runs completed all five Normal waves
with either opening, without permanent skills or enemy/base-health cheats.
One spawn point is used by every Level 1 wave. The first location offers only
the mine and cannon initially; three new defense points offer Pulse after town
hall level 2. Later-location points remain available on Levels 2 and 3 because
all three levels share one scene.

| Wave | Normal composition |
| --- | --- |
| 1 | 5 Spider |
| 2 | 9 Spider, 2 Bat |
| 3 | 11 Spider, 3 Bat |
| 4 | 13 Spider, 3 Bat, 2 Shooter |
| 5 | 15 Spider, 4 Bat, 3 Shooter, 1 Destroyer |

| Wave | Hard composition | Nightmare composition |
| --- | --- | --- |
| 1 | 6 Spider | 7 Spider |
| 2 | 9 Spider, 2 Bat | 9 Spider, 2 Bat, 1 Shooter |
| 3 | 11 Spider, 3 Bat, 1 Shooter | 12 Spider, 3 Bat, 1 Shooter, 1 Destroyer |
| 4 | 13 Spider, 3 Bat, 2 Shooter, 1 Destroyer | 14 Spider, 3 Bat, 2 Shooter, 1 Destroyer |
| 5 | 16 Spider, 4 Bat, 3 Shooter, 1 Destroyer | 17 Spider, 4 Bat, 4 Shooter, 2 Destroyer |

All three difficulties use 1.05-second spawn intervals and 1.0 health/damage
multipliers. Difficulty now comes from composition and modest count increases:
Shooter appears earlier on Hard and Nightmare, and Destroyer appears earlier on
those difficulties. These are four existing enemy types, not newly created models.
Bat base HP was reduced from 120 to 70 on every difficulty; it is now more
fragile than Spider (90 HP). The earlier uniform +25% stat-scaling hypothesis
was retired after the author's feedback that it made familiar enemies too tough.
Because Bat is shared by all levels, Level 3 Normal wave 3 was reduced from
17 to 16 Spider to keep its existing inter-wave HP growth guardrail passing.

Automated five-wave runs with a moving, shooting R-07 and purchased buildings
cleared Normal with both mine-first and cannon-first openings before this pass.
With the new composition and no permanent skills, Hard and Nightmare mine-first
both cleared all five waves; Hard cannon-first also cleared, including a run
without using the ultimate. The victory panels showed 150 and 225 permanent
money on Hard and Nightmare. This proves the revised configurations can be
completed by this automated policy, but it does not establish human win rates
or the intended difference in effort. Human playtests must check that Hard and
Nightmare remain meaningfully harder and that Bat survives long enough to play
its mine-hunting role.

An earlier idle-hero smoke showed cannon surviving wave 1 and mine losing,
which makes active hero play matter. Bat prioritizes surviving mines before
the base; Destroyer rushes the base. Pulse damage and slow apply to ground
enemies, with a PlayMode check for 40 simultaneous colliders. The unchanged
Level 1 NavMesh has a complete five-corner path from spawn to town hall; no
rebake was needed after these data-only edits.

Baseline inspected and the first corrective pass implemented on 2026-09-20.
The initial measurements below are retained for comparison. These changes are
starting balance targets, not proof of final difficulty or player win rates.

## Implemented corrections

- Hero and cannon detection uses a horizontal circle with a separate vertical
  limit of 12 units above/below the shooter. The radius remains 10 before upgrades,
  matching the ground range indicator. Target aim points must lie inside the
  cylinder; box-query corner hits are excluded. Target layers and line of sight
  still apply. Closest-target selection uses horizontal distance and rejects dead
  or out-of-range cached targets.
- Both mine upgrades now cost **1 coin** and retain their existing income and
  health multipliers. Each adds 1 coin per surviving wave, so incremental income
  repays the upgrade after one clear. Base unlocks and one-action-per-phase rules
  remain in force. An upgrade immediately before the final wave is a health
  investment, not a source of spendable profit in this campaign.
- Wave composition now grows within an introductory HP budget of at most 60%
  per wave. This budget is a configuration guardrail, not a difficulty formula.
  Wave 2 introduces only two bats; the finale combines the established threats.

| Wave | Current composition | Total starting enemy HP |
| --- | --- | ---: |
| 1 | 10 spiders | 750 |
| 2 | 10 spiders, 2 bats | 950 |
| 3 | 10 spiders, 1 destroyer, 3 shooters, 2 bats | 1,475 |
| 4 | 12 spiders, 3 bats, 4 shooters, 2 destroyers | 2,100 |

Enemy health/damage, hero damage, starting funds, wave rewards and spawn intervals
are unchanged. The first base upgrade still competes with immediate construction.

## Original economy findings

The player starts with 10 coins. The base costs 5, a cannon 3, a pulse tower 5,
a mine 2 and barracks 5. Each wave rewards 4 coins; enemies currently reward none.
A surviving mine adds 1 coin per wave.

Base + cannon + mine spends all starting money. Clearing wave 1 with that mine
leaves 5 coins, exactly the first base upgrade's price. That upgrade opens more
building capacity and upgrades but leaves no money to use it immediately. This
is a real tradeoff to test against saving or building another mine, not proof
that upgrading the base is the right opening.

Mine upgrades originally cost 75 and 150. Each added only 1 coin to income per surviving wave.
Their income-only payback periods are therefore 75 and 150 waves, far beyond
this four-wave level. Extra health has value, but these prices are out of scale
with both income and other purchases.

For this short level, an upgrade available before wave 3 must recover its cost
before wave 4; income awarded after victory does not help win the run. This led
to the one-clear payback target above. Comparing it with another mine and combat
power remains part of human playtesting.

## Original wave progression

| Wave | Composition | Total starting enemy HP |
| --- | --- | ---: |
| 1 | 10 spiders | 750 |
| 2 | 15 spiders, 5 bats | 1,625 |
| 3 | 10 spiders, 2 destroyers, 3 shooters, 4 bats | 1,975 |
| 4 | 12 spiders, 5 bats, 6 shooters | 1,850 |

Entries spawn sequentially at one-second intervals, with random spawn points.
HP alone does not measure difficulty: range, flight, approach routes and overlap
between groups also matter. Wave 2 more than doubles total HP while introducing
flight. Wave 3 introduces two more enemy types. Wave 4 has more shooters but no
destroyers, so it is not automatically the hardest wave.

Suggested teaching sequence: establish basic combat, introduce flight with a
small readable group, introduce armored/ranged pressure, then combine known
threats for the finale. Validate changes one wave at a time.

## Instrumented baseline results

Both runs used normal purchase validation, costs, health and combat, at normal
time scale. Automation selected purchases without walking to the zones. It bought
base + cannon + mine, then the first base upgrade after wave 1. Random seed 101
was used, but frame timing means these runs are not deterministic simulations.

| Policy | Wave 1 duration | Wave 1 minimum base HP | Result |
| --- | ---: | ---: | --- |
| Stationary hero near the front, no ultimate | 23.1 s | 1,500 / 1,500 | Defeat in wave 2; one hero death |
| Stationary hero nearer the base, automatic full-charge ultimate when targeting | 24.5 s | 1,500 / 1,500 | Defeat in wave 2; one hero death |

The second run ended with five full-health bats attacking the base and no hero
target. The bats were on the correct enemy layer, but none entered the hero's
detection sphere at that position. Their collider centers were approximately
10–11.5 world units high, while the hero root was about 2 units high. Detection
uses a 3D sphere with radius 10, so aerial height consumes horizontal coverage.
This observation does not establish that a moving hero cannot counter them.

These are limited stationary-policy probes, not completed human playthroughs.
Waves 3 and 4 were not reached. Changing hero position and ultimate use together
also prevents attributing the difference to either factor individually.
Local raw traces are in ignored `Temp/balance-baseline.txt` and
`Temp/balance-guard.txt`; the durable results are recorded above.

## Follow-up playtesting

### Post-change stationary integration run

The same base-guard automation and seed 101 now produced:

| Wave | Outcome | Clear duration | Minimum base HP | Hero deaths in wave |
| --- | --- | ---: | ---: | ---: |
| 1 | Cleared | 22.2 s | 1,500 / 1,500 | 0 |
| 2 | Cleared | 37.9 s | 1,625 / 1,950 | 1 |
| 3 | Cleared | 60.4 s | 1,635 / 1,950 | 1 |
| 4 | Defeat, two enemies remaining | — | 0 / 1,950 | 2 |

The opening stayed base + cannon + mine. After wave 1 it bought the base upgrade;
after wave 2, a second cannon and the 1-coin mine upgrade; after wave 3, the first
cannon upgrade. The mine did not survive waves 2 or 3, and correctly paid no
income. A cheap upgrade still requires protecting the investment.

The run confirms progression past the old wave-2 failure and exercises the full
wave sequence, but does not prove victory is achievable with every strategy or
establish a human win rate. Multiple changes were applied together, so their
individual effects cannot be isolated from this run. The raw trace is in ignored
`Temp/balance-after.txt`. Play Mode was stopped after defeat.

### Remaining comparisons

1. Play wave 2 with active movement. Check whether the revised aerial coverage
   feels fair and whether players understand where to stand.
2. Compare the same opening with and without the early base upgrade, then a
   combat-heavy opening. Record minimum base health, hero deaths, purchases,
   unspent coins and wave duration. Use several spawn seeds for each policy.
3. Compare the revised mine upgrades with combat spending. Verify that economy
   investment is useful without becoming mandatory.
4. Complete waves 3 and 4 with those policies. Tune enemy counts/composition only
   after identifying the cause of losses. Change one variable at a time.

The intended result is several viable spending choices, an understandable first
encounter with each enemy type and a finale that tests previous lessons. Exact
win-rate or duration targets still need a design decision and human playtests.

## Regression coverage

`BalanceConfigurationTests` checks the actual mine prefab's marginal-income
payback and the Level scene's wave growth, small flight introduction and armored
finale. It opens a preview scene and closes it without saving. Existing Edit Mode
tests cover purchase rejection, unlocks and reward accounting.

Play Mode `CombatTests` reproduces the previously missed elevated target and
requires actual projectile damage. It checks cylindrical range/height boundaries,
layer filtering, duplicate colliders, dead targets, range upgrades, and mine
income after survival, destruction/repair and repeated component rebinding.

Verification: the new economy, wave-budget and elevated-target tests failed
against the original configuration/behavior. After the changes, **31/31 Edit Mode**
and **15/15 Play Mode** tests passed in Unity 6000.0.72f1. These are Editor runs;
a standalone Player build was not part of this pass.

Capture minimum base HP during combat: end-of-wave repairs would hide damage in
an end-of-wave-only measurement. Automated tests can protect mechanics; they
cannot establish whether the game feels fair or fun.

## September 2026 difficulty pass

The earlier measurements above describe the previous balance. The new pass raises
enemy health to 90/120/90/360 and attack damage to 6/12/7/35.
Level 1 begins with 8 spiders and ends with 13 spiders plus 3 shooters. Levels 2
and 3 introduce bats and destroyers sooner and contain more enemies. Spawn
intervals are 1.05 seconds in Level 1 and 0.85 seconds in Levels 2–3. The
`LevelDefinition` assets remain the developer's place to tune each wave.

Normal bullet prefabs now fly at 32 units/second and the ultimate at 30, with an
eight-second cooldown after a short charge that reaches ten seconds after a full charge.
Spider pickups currently have a 25% drop chance and grant 12 seconds of
slowing shots; each hit halves enemy movement speed for 2.5 seconds. The player
can accumulate up to 24 seconds of bonus time.
Automated tests cover cooldown and high-speed
projectile hits. Human playtests are still needed to judge whether the increased
enemy pressure feels fair.
