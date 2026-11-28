using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TowerDefense.Tests
{
    public sealed class CompanionTurretTests
    {
        private readonly List<Object> created = new();

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            foreach (CompanionTurret turret in Object.FindObjectsByType<CompanionTurret>(FindObjectsSortMode.None))
                if (turret != null && turret.name.StartsWith("Test turret")) Object.Destroy(turret.gameObject);
            foreach (CompanionTurretPickup pickup in Object.FindObjectsByType<CompanionTurretPickup>(FindObjectsSortMode.None))
                if (pickup != null && pickup.name.StartsWith("Test pickup")) Object.Destroy(pickup.gameObject);
            foreach (Projectile projectile in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None))
                if (projectile != null && projectile.name.StartsWith("Test projectile")) Object.Destroy(projectile.gameObject);
            foreach (Object item in created)
                if (item != null) Object.Destroy(item);
            created.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator PickupGrantsAtMostTwoTurretsAndTheyExpire()
        {
            CompanionTurretController controller = CreatePlayer();
            GameObject pickupTemplate = New("Test pickup template");
            pickupTemplate.transform.position = Vector3.one * 1000f;
            pickupTemplate.AddComponent<SphereCollider>().isTrigger = true;
            Rigidbody body = pickupTemplate.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            CompanionTurretPickup pickupPrefab = pickupTemplate.AddComponent<CompanionTurretPickup>();

            EnemyOption option = ScriptableObject.CreateInstance<EnemyOption>();
            created.Add(option);
            GameObject enemy = New("Test Shooter");
            // Keep the drop outside the player's trigger until the test collects it explicitly.
            enemy.transform.position = new Vector3(5f, 0f, 0f);
            MobHealth health = enemy.AddComponent<MobHealth>();
            MobLootDrop loot = enemy.AddComponent<MobLootDrop>();
            loot.Configure(new EnemyLoadout.EnemyEntry
                { enemy = option, companionChance = 1f, companionDurationSeconds = 1f },
                null, null, pickupPrefab);
            health.TakeDamage(health.MaxHealth);
            yield return null;

            CompanionTurretPickup dropped = null;
            foreach (CompanionTurretPickup candidate in Object.FindObjectsByType<CompanionTurretPickup>(FindObjectsSortMode.None))
                if (candidate != pickupPrefab && candidate.name.StartsWith("Test pickup")) dropped = candidate;
            Assert.That(dropped, Is.Not.Null, "A configured Shooter must drop its companion pickup.");
            controller.transform.position = dropped.transform.position;
            Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();
            yield return null;
            Assert.That(controller.ActiveCount, Is.EqualTo(1));
            Assert.That(controller.RemainingSeconds, Is.GreaterThan(0f));
            Assert.That(controller.Progress, Is.GreaterThan(0f));
            Assert.That(dropped == null, Is.True, "Collecting the pickup removes it.");

            Assert.That(controller.Grant(1f), Is.True);
            Assert.That(controller.Grant(1f), Is.True);
            Assert.That(controller.ActiveCount, Is.EqualTo(2), "A third pickup refreshes one turret.");
            yield return new WaitForSeconds(1.2f);
            yield return null;
            Assert.That(controller.ActiveCount, Is.Zero, "Both temporary turrets expire.");
        }

        [UnityTest]
        public IEnumerator CompanionShootsNearestEnemyAndStopsOnPlayerDeath()
        {
            CompanionTurretController controller = CreatePlayer();
            CombatTestTarget near = Target("Near enemy", new Vector3(3f, 1.1f, 0f));
            CombatTestTarget far = Target("Far enemy", new Vector3(7f, 1.1f, 0f));
            Assert.That(controller.Grant(2f), Is.True);
            yield return new WaitForSeconds(0.5f);
            Assert.That(near.CurrentHealth, Is.LessThan(near.MaxHealth), "The turret should shoot autonomously.");
            Assert.That(far.CurrentHealth, Is.EqualTo(far.MaxHealth), "It should target the nearest enemy first.");

            PlayerHealth player = controller.GetComponent<PlayerHealth>();
            player.TakeDamage(player.MaxHealth);
            Assert.That(controller.ActiveCount, Is.Zero, "Companions disappear when the player dies.");
        }

        private CompanionTurretController CreatePlayer()
        {
            GameObject projectileTemplate = New("Test projectile template");
            projectileTemplate.transform.position = Vector3.one * 1000f;
            Projectile projectile = projectileTemplate.AddComponent<Projectile>();
            Set(projectile, "lifeTimer", 1000f);
            Set(projectile, "maxTravelDistance", float.MaxValue);
            Set(projectile, "speed", 80f);
            Set(projectile, "maxHomingTurnAngle", 180f);

            GameObject turretTemplate = New("Test turret template", false);
            DetectionZone zone = turretTemplate.AddComponent<DetectionZone>();
            zone.Radius = 10f;
            Set(zone, "targetLayers", (LayerMask)(1 << LayerMask.NameToLayer("Enemy")));
            Shooter shooter = turretTemplate.AddComponent<Shooter>();
            Set(shooter, "firePoints", new[] { turretTemplate.transform });
            Set(shooter, "projectilePrefab", projectileTemplate);
            Set(shooter, "obstacleMask", (LayerMask)0);
            Set(shooter, "fireRate", 4f);
            Set(shooter, "damage", 6f);
            CompanionTurret turretPrefab = turretTemplate.AddComponent<CompanionTurret>();

            GameObject playerObject = New("Test player", false);
            playerObject.AddComponent<SphereCollider>();
            playerObject.AddComponent<PlayerHealth>();
            CompanionTurretController controller = playerObject.AddComponent<CompanionTurretController>();
            Set(controller, "turretPrefab", turretPrefab);
            playerObject.SetActive(true);
            return controller;
        }

        private CombatTestTarget Target(string name, Vector3 position)
        {
            GameObject go = New(name);
            go.layer = LayerMask.NameToLayer("Enemy");
            go.transform.position = position;
            go.AddComponent<SphereCollider>();
            return go.AddComponent<CombatTestTarget>();
        }

        private GameObject New(string name, bool active = true)
        {
            GameObject go = new(name);
            go.SetActive(active);
            created.Add(go);
            return go;
        }

        private static void Set(object target, string field, object value) =>
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(target, value);
    }
}
