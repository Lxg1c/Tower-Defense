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
