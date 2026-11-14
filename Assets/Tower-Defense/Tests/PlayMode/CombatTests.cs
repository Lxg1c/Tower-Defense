using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace TowerDefense.Tests
{
    public class CombatTests
    {
        private readonly List<GameObject> created = new();

        [UnityTest]
        public IEnumerator PlayerOnlyDivertsEnemyWhileClose()
        {
            var enemy = NewObject("Close aggro enemy", false);
            var selector = enemy.AddComponent<BaseOnlySelector>();
            var attack = enemy.AddComponent<StandardAttack>();
            var navigation = enemy.AddComponent<DefaultNav>();
            var mob = enemy.AddComponent<MobCore>();
            Set(mob, "targetSelectorRef", selector);
            Set(mob, "combatBehaviorRef", attack);
            Set(mob, "navigationModifierRef", navigation);
            Set(mob, "refreshInterval", 0f);
            var coreObject = NewObject("Distant core");
            coreObject.transform.position = Vector3.forward * 12f;
            var core = coreObject.AddComponent<Base>();
            var player = NewObject("Player").AddComponent<PlayerHealth>();
            player.transform.position = Vector3.right * 5f;
            enemy.SetActive(true);
            yield return null;
            Assert.That(mob.CurrentTarget, Is.SameAs(core));

            player.transform.position = Vector3.right * 2f;
            yield return null;
            Assert.That(mob.CurrentTarget, Is.SameAs(player));

            player.transform.position = Vector3.right * 5f;
            yield return null;
            Assert.That(mob.CurrentTarget, Is.SameAs(core));
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            foreach (GameObject go in created)
                if (go != null) Object.Destroy(go);
            // Projectiles created by this fixture are the only projectiles in the test scene.
            foreach (var projectile in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None))
                Object.Destroy(projectile.gameObject);
            foreach (var shell in Object.FindObjectsByType<MortarShell>(FindObjectsSortMode.None))
                Object.Destroy(shell.gameObject);
            created.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator DifficultySpeedSurvivesSlowAndPooledReconfiguration()
        {
            GameObject enemy = NewObject("Speed-scaled enemy", false);
            var agent = enemy.AddComponent<NavMeshAgent>();
            agent.speed = 4f;
            enemy.AddComponent<MobHealth>();
            var selector = enemy.AddComponent<NearestThreatSelector>();
            var attack = enemy.AddComponent<StandardAttack>();
            var navigation = enemy.AddComponent<DefaultNav>();
            var mob = enemy.AddComponent<MobCore>();
            Set(mob, "targetSelectorRef", selector);
            Set(mob, "combatBehaviorRef", attack);
            Set(mob, "navigationModifierRef", navigation);
            enemy.SetActive(true);
            mob.enabled = false;
            mob.ConfigureDifficulty(45f, 1f, 1.5f);
            Assert.That(agent.speed, Is.EqualTo(6f).Within(.001f));

            var slow = enemy.AddComponent<MobSlow>();
            slow.Apply(.5f, 10f);
            Assert.That(agent.speed, Is.EqualTo(3f).Within(.001f));
            slow.enabled = false;
            Assert.That(agent.speed, Is.EqualTo(6f).Within(.001f));

            mob.ConfigureDifficulty(30f, 1f, 1.1f);
            Assert.That(agent.speed, Is.EqualTo(4.4f).Within(.001f));
            slow.enabled = true;
            slow.Apply(.5f, 10f);
            Assert.That(agent.speed, Is.EqualTo(2.2f).Within(.001f));
            slow.enabled = false;
            Assert.That(agent.speed, Is.EqualTo(4.4f).Within(.001f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator DifficultyScalingRaisesMobHealthAndMeleeDamage()
        {
            GameObject enemy = NewObject("Scaled enemy", false);
            var health = enemy.AddComponent<MobHealth>();
            var selector = enemy.AddComponent<NearestThreatSelector>();
            var attack = enemy.AddComponent<StandardAttack>();
            var navigation = enemy.AddComponent<DefaultNav>();
            var mob = enemy.AddComponent<MobCore>();
            Set(mob, "targetSelectorRef", selector);
            Set(mob, "combatBehaviorRef", attack);
            Set(mob, "navigationModifierRef", navigation);
            enemy.SetActive(true);
            mob.enabled = false;
            mob.ConfigureDifficulty(125f, 1.25f);
            var target = Target(Vector3.forward);
            Set(attack, "damage", 8f);

            attack.Tick(mob, target);
            Assert.That(health.MaxHealth, Is.EqualTo(125f));
            Assert.That(health.CurrentHealth, Is.EqualTo(125f));
            Assert.That(target.CurrentHealth, Is.EqualTo(target.MaxHealth - 10f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator FlyingRaiderSwitchesFromMinesToTheBase()
        {
            GameObject enemy = NewObject("Flying raider", false);
            enemy.AddComponent<MobHealth>();
            var selector = enemy.AddComponent<MineFirstSelector>();
            var attack = enemy.AddComponent<StandardAttack>();
            var navigation = enemy.AddComponent<DefaultNav>();
            var mob = enemy.AddComponent<MobCore>();
            Set(mob, "targetSelectorRef", selector);
            Set(mob, "combatBehaviorRef", attack);
            Set(mob, "navigationModifierRef", navigation);
            enemy.SetActive(true);
            mob.enabled = false;

            var baseObject = NewObject("Base").AddComponent<Base>();
            var mine = Target(Vector3.right * 3f);
            mine.gameObject.AddComponent<MineIncome>();
            Assert.That(selector.GetTarget(mob), Is.SameAs(mine));

            mine.TakeDamage(mine.MaxHealth);
            Assert.That(selector.GetTarget(mob), Is.SameAs(baseObject));
            yield return null;
        }

        [UnityTest]
        public IEnumerator DestroyedWallOpensItsPhysicalAndNavigationBlockersUntilNextWave()
        {
            Vector3 origin = new(1000f, 0f, 1000f);
            var sources = new List<NavMeshBuildSource>
            {
                new()
                {
                    shape = NavMeshBuildSourceShape.Box,
                    size = new Vector3(4f, 1f, 20f),
                    transform = Matrix4x4.TRS(Vector3.down * 0.5f,
                        Quaternion.identity, Vector3.one),
                    area = 0
                }
            };
            NavMeshData data = NavMeshBuilder.BuildNavMeshData(
                NavMesh.GetSettingsByID(0), sources,
                new Bounds(Vector3.zero, new Vector3(8f, 6f, 24f)),
                Vector3.zero, Quaternion.identity);
            Assert.That(data, Is.Not.Null);
            NavMeshDataInstance instance = NavMesh.AddNavMeshData(data, origin, Quaternion.identity);
            GameObject wall = NewObject("Wall", false);
            wall.transform.position = origin;
            var collider = wall.AddComponent<BoxCollider>();
            collider.size = new Vector3(5f, 2f, 1.5f);
            var obstacle = wall.AddComponent<NavMeshObstacle>();
            obstacle.size = collider.size;
            obstacle.carving = true;
            var trigger = wall.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            var health = wall.AddComponent<TowerHealth>();
            health.onRespawned = new UnityEngine.Events.UnityEvent();
            wall.AddComponent<WallCollision>();
            wall.SetActive(true);
            Vector3 start = origin + Vector3.back * 8f;
            Vector3 end = origin + Vector3.forward * 8f;
            var path = new NavMeshPath();
            try
            {
                yield return new WaitForSeconds(0.8f);
                Assert.That(NavMesh.SamplePosition(start, out _, 1f, NavMesh.AllAreas), Is.True);
                Assert.That(NavMesh.SamplePosition(end, out _, 1f, NavMesh.AllAreas), Is.True);
                NavMesh.CalculatePath(start, end, NavMesh.AllAreas, path);
                Assert.That(path.status, Is.Not.EqualTo(NavMeshPathStatus.PathComplete),
                    "An intact wall should divide the narrow corridor.");

                health.TakeDamage(health.MaxHealth);
                Assert.That(health.IsDestroyed, Is.True);
                Assert.That(collider.enabled, Is.False, "A broken wall must be passable.");
                Assert.That(obstacle.enabled, Is.False, "The carved NavMesh hole must close.");
                Assert.That(trigger.enabled, Is.True, "Non-blocking triggers keep their own state.");
                yield return null;
                NavMesh.CalculatePath(start, end, NavMesh.AllAreas, path);
                Assert.That(path.status, Is.EqualTo(NavMeshPathStatus.PathComplete),
                    "The route must reopen after the wall is destroyed.");

                typeof(TowerHealth).GetMethod("OnWaveCompleted",
                    BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(health, new object[] { 0, 0 });
                Assert.That(health.IsDestroyed, Is.False);
                Assert.That(collider.enabled, Is.True);
                Assert.That(obstacle.enabled, Is.True);
                yield return new WaitForSeconds(0.8f);
                NavMesh.CalculatePath(start, end, NavMesh.AllAreas, path);
                Assert.That(path.status, Is.Not.EqualTo(NavMeshPathStatus.PathComplete),
                    "The revived wall must block the corridor again.");
            }
            finally
            {
                instance.Remove();
                Object.Destroy(data);
            }
        }

        [UnityTest]
        public IEnumerator PrewarmedBaseRaiderAcquiresBaseBuiltLater()
        {
            var selector = NewObject("Base raider").AddComponent<BaseOnlySelector>();
            Assert.That(selector.GetTarget(null), Is.Null);
            var baseObject = NewObject("Built base").AddComponent<Base>();
            Assert.That(selector.GetTarget(null), Is.SameAs(baseObject));
            yield return null;
        }

        [UnityTest]
        public IEnumerator PulseHitsDenseGroundGroupOncePerEnemy()
        {
            GameObject towerObject = NewObject("Pulse", false);
            var pulse = towerObject.AddComponent<PulseTower>();
            Set(pulse, "targetMask", (LayerMask)(1 << 7));
            Set(pulse, "ignoreTag", string.Empty);
            Set(pulse, "damage", 10f);
            var targets = new List<CombatTestTarget>();
            for (int i = 0; i < 40; i++)
            {
                var target = Target(new Vector3(i % 8 * 0.35f, 0f, i / 8 * 0.35f));
                target.gameObject.layer = 7;
                targets.Add(target);
            }
            targets[0].gameObject.AddComponent<BoxCollider>();
            towerObject.SetActive(true);
            Physics.SyncTransforms();
            yield return null;
            foreach (var target in targets)
                Assert.That(target.CurrentHealth, Is.EqualTo(target.MaxHealth - 10f));
        }

        [UnityTest]
        public IEnumerator PulseSlowsGroundEnemiesAndLeavesFlyingEnemiesUntouched()
        {
            GameObject ground = NewObject("Ground enemy", false);
            ground.layer = 7;
            ground.transform.position = Vector3.right;
            ground.AddComponent<SphereCollider>();
            var groundAgent = ground.AddComponent<NavMeshAgent>();
            var groundHealth = ground.AddComponent<MobHealth>();
            var selector = ground.AddComponent<NearestThreatSelector>();
            var attack = ground.AddComponent<StandardAttack>();
            var navigation = ground.AddComponent<DefaultNav>();
            var groundMob = ground.AddComponent<MobCore>();
            Set(groundMob, "targetSelectorRef", selector);
            Set(groundMob, "combatBehaviorRef", attack);
            Set(groundMob, "navigationModifierRef", navigation);

            GameObject flying = NewObject("Flying enemy", false);
            flying.layer = 7;
            flying.tag = "Flying";
            flying.transform.position = Vector3.left;
            flying.AddComponent<SphereCollider>();
            var flyingHealth = flying.AddComponent<MobHealth>();

            GameObject tower = NewObject("Pulse", false);
            var pulse = tower.AddComponent<PulseTower>();
            Set(pulse, "targetMask", (LayerMask)(1 << 7));
            Set(pulse, "ignoreTag", "Flying");
            Set(pulse, "damage", 10f);
            ground.SetActive(true);
            flying.SetActive(true);
            float originalSpeed = groundAgent.speed;
            tower.SetActive(true);
            Physics.SyncTransforms();
            yield return null;

            Assert.That(groundHealth.CurrentHealth, Is.EqualTo(groundHealth.MaxHealth - 10f));
            Assert.That(groundAgent.speed, Is.LessThan(originalSpeed));
            Assert.That(flyingHealth.CurrentHealth, Is.EqualTo(flyingHealth.MaxHealth));
            Assert.That(flying.GetComponent<NavMeshAgent>(), Is.Null,
                "A flying target must not receive a ground NavMeshAgent from slow effects.");
        }

        [UnityTest]
        public IEnumerator MissingProjectileDisablesShooterWithoutDamagingTarget()
        {
            var target = Target(new Vector3(0, 0, 3));
            GameObject go = NewObject("Invalid shooter", false);
            var shooter = go.AddComponent<Shooter>();
            Set(shooter, "firePoints", new[] { go.transform });
            LogAssert.Expect(LogType.Error, "[Shooter] Assign a projectile prefab with a Projectile component.");
            go.SetActive(true);
            yield return null;
            yield return null;
            Assert.That(shooter.enabled, Is.False);
            Assert.That(target.CurrentHealth, Is.EqualTo(target.MaxHealth));
        }

        [UnityTest]
        public IEnumerator MortarSplashDamagesEachEnemyOnceAndIgnoresDistantTargets()
        {
            Time.timeScale = 1f;
            var center = Target(Vector3.zero);
            center.gameObject.layer = 7;
            center.gameObject.AddComponent<BoxCollider>();
            var nearby = Target(Vector3.right * 1.5f);
            nearby.gameObject.layer = 7;
            var distant = Target(Vector3.right * 6f);
            distant.gameObject.layer = 7;
            var shell = NewObject("Mortar shell").AddComponent<MortarShell>();
            shell.transform.position = Vector3.up * 3f;
            Physics.SyncTransforms();
            shell.Launch(Vector3.zero, 20f, 2.5f, 0.01f, 1 << 7);
            float deadline = Time.realtimeSinceStartup + 1f;
            while (shell != null && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(shell == null, Is.True, "The shell should explode before checking splash damage.");

            Assert.That(center.CurrentHealth, Is.EqualTo(center.MaxHealth - 20f));
            Assert.That(nearby.CurrentHealth, Is.EqualTo(nearby.MaxHealth - 20f));
            Assert.That(distant.CurrentHealth, Is.EqualTo(distant.MaxHealth));
        }

        [UnityTest]
        public IEnumerator SingleMuzzleShooterFiresRepeatedProjectilesWithoutInstantDamage()
        {
            var target = Target(new Vector3(0, 0, 3));
            GameObject template = NewObject("Projectile template", false);
            template.transform.position = Vector3.right * 1000;
            var projectile = template.AddComponent<Projectile>();
            Set(projectile, "speed", 0f);
            Set(projectile, "lifeTime", 100f);
            projectile.Init(target, 0f);
            template.SetActive(true);

            GameObject go = NewObject("Shooter", false);
            var shooter = go.AddComponent<Shooter>();
            Set(shooter, "firePoints", new[] { go.transform });
            Set(shooter, "projectilePrefab", template);
            shooter.FireRate = 20;
            int shots = 0;
            shooter.onFired.AddListener(() => shots++);
            go.SetActive(true);
            Physics.SyncTransforms();
            float deadline = Time.time + 2f;
            while (shots < 2 && Time.time < deadline) yield return null;
            Assert.That(shots, Is.GreaterThanOrEqualTo(2), "A completed single-muzzle volley must allow another volley.");
            Assert.That(Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Length, Is.GreaterThanOrEqualTo(3));
            Assert.That(target.CurrentHealth, Is.EqualTo(target.MaxHealth), "Damage occurs on projectile hit, not firing.");
        }

        [UnityTest]
        public IEnumerator ShooterHitsAnAerialTargetInsideItsGroundRange()
        {
            // Reproduces the base-guard case: horizontal distance is 8, but
            // height puts the target outside the former radius-10 sphere.
            var target = Target(new Vector3(8, 9, 0));
            GameObject template = NewObject("Projectile template", false);
            template.transform.position = Vector3.right * 1000;
            var projectile = template.AddComponent<Projectile>();
            Set(projectile, "speed", 10f);
            Set(projectile, "lifeTime", 100f);
            projectile.Init(target, 0f);
            template.SetActive(true);

            GameObject go = NewObject("Anti-air shooter", false);
            var shooter = go.AddComponent<Shooter>();
            Set(shooter, "firePoints", new[] { go.transform });
            Set(shooter, "projectilePrefab", template);
            go.SetActive(true);
            Physics.SyncTransforms();
            float deadline = Time.time + 3f;
            while (target.CurrentHealth == target.MaxHealth && Time.time < deadline)
                yield return null;
            Assert.That(target.CurrentHealth, Is.LessThan(target.MaxHealth),
                "Aerial targets inside horizontal range must be acquired and hit.");
        }

        [UnityTest]
        public IEnumerator DetectionRespectsCylinderBoundsLayersAndDeadTargets()
        {
            var zone = NewObject("Detection").AddComponent<DetectionZone>();
            Set(zone, "targetLayers", (LayerMask)(1 << 7));
            var aerial = Target(new Vector3(8, 9, 0));
            aerial.gameObject.layer = 7;
            aerial.gameObject.AddComponent<BoxCollider>(); // One entity, two colliders.
            var outsideRadius = Target(new Vector3(8, 0, 8)); // Inside box, outside circle.
            outsideRadius.gameObject.layer = 7;
            var tooHigh = Target(new Vector3(0, 13, 0));
            tooHigh.gameObject.layer = 7;
            var tooLow = Target(new Vector3(0, -13, 0));
            tooLow.gameObject.layer = 7;
            Target(new Vector3(1, 0, 0)); // Wrong layer.
            Physics.SyncTransforms();
            yield return new WaitForSeconds(0.25f);
            Assert.That(zone.Targets, Has.Count.EqualTo(1));
            Assert.That(zone.GetClosest(), Is.SameAs(aerial));

            aerial.transform.position = new Vector3(11, 0, 0);
            Assert.That(zone.GetClosest(), Is.Null, "Range changes invalidate cached targets.");
            aerial.transform.position = new Vector3(8, 9, 0);
            Physics.SyncTransforms();
            yield return new WaitForSeconds(0.25f);
            Assert.That(zone.GetClosest(), Is.SameAs(aerial));
            aerial.TakeDamage(aerial.MaxHealth);
            Assert.That(zone.GetClosest(), Is.Null, "Dead targets must leave the cache immediately.");
        }

        [UnityTest]
        public IEnumerator DetectionChoosesNearestGroundDistanceAndHonorsRangeUpgrades()
        {
            var zone = NewObject("Detection").AddComponent<DetectionZone>();
            var aerial = Target(new Vector3(8, 9, 0));
            Target(new Vector3(9, 0, 0));
            Physics.SyncTransforms();
            yield return new WaitForSeconds(0.25f);
            Assert.That(zone.GetClosest(), Is.SameAs(aerial));
            zone.Radius = 7;
            Assert.That(zone.GetClosest(), Is.Null);
            zone.Radius = 12;
            aerial.transform.position = new Vector3(11, 9, 0);
            Physics.SyncTransforms();
            yield return new WaitForSeconds(0.25f);
            Assert.That(zone.Targets, Does.Contain(aerial), "Range upgrades must retain aerial coverage.");
        }

        [UnityTest]
        public IEnumerator MinePaysCurrentIncomeOnlyForWavesItSurvives()
        {
            var walletObject = NewObject("Wallet", false);
            var wallet = walletObject.AddComponent<PlayerWallet>();
            wallet.ConfigureStartingCoins(100);
            walletObject.SetActive(true);
            wallet.TrySpend(wallet.Coins);
            var spawner = NewObject("Wave events").AddComponent<WaveSpawner>();
            spawner.enabled = false; // Drive phase events without starting a level.
            var health = Target(Vector3.zero);
            var mine = health.gameObject.AddComponent<MineIncome>();
            mine.CoinsPerWave = 1;
            spawner.onCombatPhaseStarted.Invoke(0, 4, 4);
            spawner.onWaveCompleted.Invoke(0, 4);
            Assert.That(wallet.Coins, Is.EqualTo(1));

            mine.CoinsPerWave = 2;
            spawner.onCombatPhaseStarted.Invoke(1, 4, 4);
            health.TakeDamage(health.MaxHealth);
            health.ResetToMaxHealth(); // Wave-end repair must not earn destroyed mines income.
            spawner.onWaveCompleted.Invoke(1, 4);
            Assert.That(wallet.Coins, Is.EqualTo(1));

            for (int i = 0; i < 3; i++)
            {
                mine.enabled = false;
                mine.enabled = true;
            }
            spawner.onCombatPhaseStarted.Invoke(2, 4, 4);
            spawner.onWaveCompleted.Invoke(2, 4);
            Assert.That(wallet.Coins, Is.EqualTo(3), "Rebinding must not duplicate mine rewards.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator CoreRemainsUnderAttackBetweenHitsUntilTheLastAttackerLeavesOrDies()
        {
            var core = NewObject("Core").AddComponent<Base>();
            MobCore first = CoreAttacker(Vector3.forward);
            MobCore second = CoreAttacker(Vector3.back);
            Assert.That(core.IsUnderAttack, Is.False);
            yield return new WaitForSeconds(0.4f);

            Assert.That(core.CurrentHealth, Is.LessThan(core.MaxHealth));
            Assert.That(core.IsUnderAttack, Is.True);
            float health = core.CurrentHealth;
            first.transform.position = Vector3.forward * 10f;
            yield return null;
            Assert.That(core.CurrentHealth, Is.EqualTo(health), "Attackers are between strikes.");
            Assert.That(core.IsUnderAttack, Is.True);

            second.Health.TakeDamage(second.Health.MaxHealth);
            Assert.That(core.IsUnderAttack, Is.False);
        }

        [UnityTest]
        public IEnumerator DisablingAnAttackerClearsTheCoreWarning()
        {
            var core = NewObject("Core").AddComponent<Base>();
            MobCore enemy = CoreAttacker(Vector3.forward);
            yield return new WaitForSeconds(0.4f);
            Assert.That(core.IsUnderAttack, Is.True);
            enemy.gameObject.SetActive(false);
            Assert.That(core.IsUnderAttack, Is.False);
        }

        private MobCore CoreAttacker(Vector3 position)
        {
            var enemy = NewObject("Core attacker", false);
            enemy.transform.position = position;
            var selector = enemy.AddComponent<BaseOnlySelector>();
            var attack = enemy.AddComponent<StandardAttack>();
            var navigation = enemy.AddComponent<DefaultNav>();
            var mob = enemy.AddComponent<MobCore>();
            Set(mob, "targetSelectorRef", selector);
            Set(mob, "combatBehaviorRef", attack);
            Set(mob, "navigationModifierRef", navigation);
            enemy.SetActive(true);
            return mob;
        }

        [UnityTest]
        public IEnumerator HealthEmitsDeathOnceAndRejectsFurtherDamage()
        {
            var target = Target(Vector3.zero);
            int deaths = 0;
            target.onDied.AddListener(() => deaths++);
            target.TakeDamage(target.MaxHealth);
            target.TakeDamage(10);
            yield return null;
            Assert.That(target.CurrentHealth, Is.Zero);
            Assert.That(deaths, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ProjectileDamagesItsTargetOnContact()
        {
            var target = Target(Vector3.zero);
            var projectile = NewObject("Projectile").AddComponent<Projectile>();
            projectile.Init(target, 12);
            yield return null;
            Assert.That(target.CurrentHealth, Is.EqualTo(target.MaxHealth - 12));
            yield return null;
            Assert.That(projectile == null, Is.True);
        }

        [UnityTest]
        public IEnumerator FastProjectileCannotSkipAThinTargetInOneFrame()
        {
            var target = Target(new Vector3(0, 0, 3));
            var projectile = NewObject("Fast projectile").AddComponent<Projectile>();
            Set(projectile, "speed", 5000f);
            projectile.Init(target, 12f);
            yield return null;
            Assert.That(target.CurrentHealth, Is.EqualTo(target.MaxHealth - 12f));
            yield return null;
            Assert.That(projectile == null, Is.True);
        }

        private GameObject NewObject(string name, bool active = true)
        {
            var go = new GameObject(name);
            go.SetActive(active);
            created.Add(go);
            return go;
        }

        private CombatTestTarget Target(Vector3 position)
        {
            GameObject go = NewObject("Target");
            go.transform.position = position;
            go.AddComponent<SphereCollider>();
            return go.AddComponent<CombatTestTarget>();
        }

        private static void Set(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }
    }
}
