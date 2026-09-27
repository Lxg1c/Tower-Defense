using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TowerDefense.Tests
{
    public class CombatTests
    {
        private readonly List<GameObject> created = new();

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (GameObject go in created)
                if (go != null) Object.Destroy(go);
            // Projectiles created by this fixture are the only projectiles in the test scene.
            foreach (var projectile in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None))
                Object.Destroy(projectile.gameObject);
            created.Clear();
            yield return null;
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
            var wallet = NewObject("Wallet").AddComponent<PlayerWallet>();
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
