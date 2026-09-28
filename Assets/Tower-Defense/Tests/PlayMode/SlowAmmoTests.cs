using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TowerDefense.Tests
{
    public sealed class SlowAmmoTests
    {
        private readonly List<Object> created = new();

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            foreach (SlowAmmoPickup pickup in Object.FindObjectsByType<SlowAmmoPickup>(FindObjectsSortMode.None))
                if (pickup != null) Object.Destroy(pickup.gameObject);
            foreach (Projectile projectile in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None))
                if (projectile != null) Object.Destroy(projectile.gameObject);
            foreach (Object item in created)
                if (item != null) Object.Destroy(item);
            created.Clear();
            yield return null;
        }

        [Test]
        public void BonusTimeAccumulatesOnlyUpToCapacity()
        {
            var inventory = NewObject("Player").AddComponent<SlowAmmoInventory>();
            Assert.That(inventory.Add(12f), Is.EqualTo(12f).Within(0.1f));
            Assert.That(inventory.Add(20f), Is.EqualTo(12f).Within(0.1f));
            Assert.That(inventory.RemainingSeconds, Is.EqualTo(24f).Within(0.1f));
            Assert.That(inventory.Progress, Is.EqualTo(1f).Within(0.01f));
            Assert.That(inventory.IsActive, Is.True);
        }

        [UnityTest]
        public IEnumerator BonusExpiresEvenWhenNoShotsAreFired()
        {
            var inventory = NewObject("Player").AddComponent<SlowAmmoInventory>();
            inventory.Add(0.05f);
            yield return new WaitForSeconds(0.1f);
            Assert.That(inventory.IsActive, Is.False);
            Assert.That(inventory.RemainingSeconds, Is.Zero);
            Assert.That(inventory.Progress, Is.Zero);
        }

        [UnityTest]
        public IEnumerator SpiderDropCanBePickedUpByPlayer()
        {
            var template = NewObject("Pickup template");
            template.transform.position = Vector3.one * 1000f;
            template.AddComponent<SphereCollider>().isTrigger = true;
            Rigidbody body = template.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            var pickupPrefab = template.AddComponent<SlowAmmoPickup>();

            var option = ScriptableObject.CreateInstance<EnemyOption>();
            created.Add(option);
            var loadout = ScriptableObject.CreateInstance<EnemyLoadout>();
            created.Add(loadout);
            loadout.slowAmmoPickupPrefab = pickupPrefab;
            loadout.slowAmmoDrops = new[] { new EnemyLoadout.SlowAmmoDrop
                { enemy = option, chance = 1f, durationSeconds = 12f } };

            var spider = NewObject("Spider");
            var health = spider.AddComponent<MobHealth>();
            var loot = spider.AddComponent<MobLootDrop>();
            Assert.That(loadout.TryGetSlowAmmoDrop(option, out var drop), Is.True);
            loot.Configure(drop, loadout.slowAmmoPickupPrefab);
            health.TakeDamage(health.MaxHealth);
            yield return null;

            SlowAmmoPickup dropped = null;
            foreach (SlowAmmoPickup candidate in Object.FindObjectsByType<SlowAmmoPickup>(FindObjectsSortMode.None))
                if (candidate.gameObject != template) dropped = candidate;
            Assert.That(dropped, Is.Not.Null, "A configured spider must drop its pickup on death.");

            var player = NewObject("Player");
            var inventory = player.AddComponent<SlowAmmoInventory>();
            player.AddComponent<SphereCollider>();
            player.transform.position = dropped.transform.position;
            Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();
            yield return null;
            Assert.That(inventory.RemainingSeconds, Is.EqualTo(12f).Within(0.5f));
            Assert.That(dropped == null, Is.True, "Collected pickups must disappear.");
        }

        [UnityTest]
        public IEnumerator SlowProjectileSlowsMobThenRestoresSpeedState()
        {
            var mob = NewObject("Mob");
            var health = mob.AddComponent<MobHealth>();
            var projectile = NewObject("Slow projectile").AddComponent<Projectile>();
            projectile.Init(health, 10f, 0.5f, 0.08f);
            yield return null;

            var slow = mob.GetComponent<MobSlow>();
            Assert.That(health.CurrentHealth, Is.EqualTo(health.MaxHealth - 10f));
            Assert.That(slow, Is.Not.Null);
            Assert.That(slow.Multiplier, Is.EqualTo(0.5f));

            yield return new WaitForSeconds(0.12f);
            Assert.That(slow.Multiplier, Is.EqualTo(1f), "Slow must expire without destroying the mob.");
            slow.Apply(0.5f, 10f);
            mob.SetActive(false);
            mob.SetActive(true);
            Assert.That(slow.Multiplier, Is.EqualTo(1f), "A pooled mob must respawn without the old slow.");
        }

        private GameObject NewObject(string name, bool active = true)
        {
            var go = new GameObject(name);
            go.SetActive(active);
            created.Add(go);
            return go;
        }
    }
}
