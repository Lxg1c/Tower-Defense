using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TowerDefense.Tests
{
    public sealed class ShieldDropTests
    {
        private readonly List<Object> created = new();
        private ShieldPickup dropped;

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (dropped != null) Object.Destroy(dropped.gameObject);
            foreach (Object item in created)
                if (item != null) Object.Destroy(item);
            created.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator DestroyerDeathDoesNotDropShield()
        {
            GameObject template = NewObject("Shield template");
            template.transform.position = Vector3.one * 1000f;
            template.AddComponent<SphereCollider>().isTrigger = true;
            Rigidbody body = template.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            ShieldPickup pickupPrefab = template.AddComponent<ShieldPickup>();

            EnemyOption option = ScriptableObject.CreateInstance<EnemyOption>();
            created.Add(option);
            GameObject mob = NewObject("Destroyer");
            MobHealth health = mob.AddComponent<MobHealth>();
            MobLootDrop loot = mob.AddComponent<MobLootDrop>();
            loot.Configure(new EnemyLoadout.EnemyEntry
                { enemy = option, shieldChance = 1f, shieldDurationSeconds = 12f }, null, pickupPrefab);
            health.TakeDamage(health.MaxHealth);
            yield return null;

            foreach (ShieldPickup candidate in Object.FindObjectsByType<ShieldPickup>(FindObjectsSortMode.None))
                if (candidate != pickupPrefab && candidate.name.StartsWith("Shield template"))
                    dropped = candidate;
            Assert.That(dropped, Is.Null, "Enemy shield drops are disabled.");
        }

        private GameObject NewObject(string name)
        {
            var go = new GameObject(name);
            created.Add(go);
            return go;
        }
    }
}
