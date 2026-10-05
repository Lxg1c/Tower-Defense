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
        public IEnumerator DestroyerDropsShieldThatPlayerCanCollect()
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
            Assert.That(dropped, Is.Not.Null, "The configured Destroyer should drop its shield.");

            GameObject player = NewObject("Player");
            PlayerHealth playerHealth = player.AddComponent<PlayerHealth>();
            player.AddComponent<SphereCollider>();
            player.transform.position = dropped.transform.position;
            Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.That(playerHealth.CurrentShield, Is.EqualTo(playerHealth.MaxShield));
            Assert.That(playerHealth.RemainingShieldSeconds, Is.EqualTo(12f).Within(0.5f));
            Assert.That(dropped == null, Is.True, "The collected pickup should disappear.");
        }

        private GameObject NewObject(string name)
        {
            var go = new GameObject(name);
            created.Add(go);
            return go;
        }
    }
}
