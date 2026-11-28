using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TowerDefense.Tests
{
    public sealed class MobLootDropTests
    {
        private readonly List<Object> created = new();

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            foreach (SlowAmmoPickup pickup in Object.FindObjectsByType<SlowAmmoPickup>(FindObjectsSortMode.None))
                if (pickup != null && pickup.name.StartsWith("Test loot")) Object.Destroy(pickup.gameObject);
            foreach (ShieldPickup pickup in Object.FindObjectsByType<ShieldPickup>(FindObjectsSortMode.None))
                if (pickup != null && pickup.name.StartsWith("Test loot")) Object.Destroy(pickup.gameObject);
            foreach (CompanionTurretPickup pickup in Object.FindObjectsByType<CompanionTurretPickup>(FindObjectsSortMode.None))
                if (pickup != null && pickup.name.StartsWith("Test loot")) Object.Destroy(pickup.gameObject);
            foreach (Object item in created)
                if (item != null) Object.Destroy(item);
            created.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator EachMobDropsExactlyOnePickupWhenChancesTotalOne()
        {
            SlowAmmoPickup slow = Template<SlowAmmoPickup>("Test loot slow template");
            ShieldPickup shield = Template<ShieldPickup>("Test loot shield template");
            CompanionTurretPickup companion = Template<CompanionTurretPickup>("Test loot companion template");
            EnemyOption option = ScriptableObject.CreateInstance<EnemyOption>();
            created.Add(option);
            var entry = new EnemyLoadout.EnemyEntry
            {
                enemy = option,
                slowAmmoChance = 0.5f,
                slowAmmoDurationSeconds = 12f,
                shieldChance = 0.25f,
                shieldDurationSeconds = 12f,
                companionChance = 0.25f,
                companionDurationSeconds = 18f
            };

            for (int i = 0; i < 20; i++)
            {
                GameObject mob = new GameObject($"Test mob {i}");
                created.Add(mob);
                MobHealth health = mob.AddComponent<MobHealth>();
                mob.AddComponent<MobLootDrop>().Configure(entry, slow, shield, companion);
                health.TakeDamage(health.MaxHealth);
                Assert.That(CountDrops(slow, shield, companion), Is.EqualTo(i + 1),
                    $"Mob {i} should add exactly one pickup.");
            }
            yield return null;
        }

        private T Template<T>(string name) where T : Component
        {
            GameObject go = new GameObject(name);
            created.Add(go);
            go.transform.position = Vector3.one * 1000f;
            go.AddComponent<SphereCollider>().isTrigger = true;
            Rigidbody body = go.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            return go.AddComponent<T>();
        }

        private static int CountDrops(SlowAmmoPickup slow, ShieldPickup shield,
            CompanionTurretPickup companion)
        {
            int count = 0;
            foreach (SlowAmmoPickup pickup in Object.FindObjectsByType<SlowAmmoPickup>(FindObjectsSortMode.None))
                if (pickup != slow && pickup.name.StartsWith("Test loot")) count++;
            foreach (ShieldPickup pickup in Object.FindObjectsByType<ShieldPickup>(FindObjectsSortMode.None))
                if (pickup != shield && pickup.name.StartsWith("Test loot")) count++;
            foreach (CompanionTurretPickup pickup in Object.FindObjectsByType<CompanionTurretPickup>(FindObjectsSortMode.None))
                if (pickup != companion && pickup.name.StartsWith("Test loot")) count++;
            return count;
        }
    }
}
