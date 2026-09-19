using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TowerDefense.Tests
{
    public class SessionPresentationTests
    {
        private readonly List<GameObject> objects = new();

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            foreach (var go in objects) if (go != null) Object.Destroy(go);
            objects.Clear();
            Time.timeScale = 1f;
            yield return null;
        }

        [UnityTest]
        public IEnumerator BindingBeforeManagerAndReenableDoNotLeakSubscriptions()
        {
            var binding = Binding();
            binding.gameObject.SetActive(true);
            var manager = Manager();
            manager.gameObject.SetActive(true);
            yield return null;
            Assert.That(manager.ActiveBarCount, Is.EqualTo(1));
            for (int i = 0; i < 3; i++)
            {
                binding.gameObject.SetActive(false);
                Assert.That(manager.ActiveBarCount, Is.Zero);
                binding.gameObject.SetActive(true);
                Assert.That(manager.ActiveBarCount, Is.EqualTo(1));
                manager.enabled = false;
                Assert.That(manager.ActiveBarCount, Is.Zero);
                manager.enabled = true;
                Assert.That(manager.ActiveBarCount, Is.EqualTo(1));
            }
            manager.enabled = false;
            var other = Binding();
            other.gameObject.SetActive(true);
            Assert.That(manager.ActiveBarCount, Is.Zero, "Disabled manager must not receive registry notifications.");
        }

        [UnityTest]
        public IEnumerator ManagerBeforeBindingTracksDamageDeathAndRevival()
        {
            var manager = Manager();
            manager.gameObject.SetActive(true);
            var binding = Binding();
            binding.gameObject.SetActive(true);
            yield return null;
            Assert.That(manager.ActiveBarCount, Is.EqualTo(1));
            binding.Target.TakeDamage(25);
            yield return null;
            var bars = Object.FindObjectsByType<HealthBar>(FindObjectsSortMode.None);
            Assert.That(bars.Length, Is.EqualTo(1));
            Assert.That((float)Get(bars[0], "_targetFill"), Is.EqualTo(0.75f));
            binding.Target.TakeDamage(1000);
            Assert.That(bars[0].gameObject.activeSelf, Is.False);
            binding.Target.ResetToMaxHealth();
            binding.Target.TakeDamage(10);
            Assert.That(bars[0].gameObject.activeSelf, Is.True);
            Assert.That((float)Get(bars[0], "_targetFill"), Is.EqualTo(0.9f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator BaseDeathStopsAnInFlightWaveWithoutReward()
        {
            var wallet = New("Wallet").AddComponent<PlayerWallet>();
            wallet.gameObject.SetActive(true);
            var townHall = New("Base").AddComponent<Base>();
            townHall.gameObject.SetActive(true);
            var spawner = New("Spawner").AddComponent<WaveSpawner>();
            Set(spawner, "wallet", wallet);
            Set(spawner, "waves", new[] { new WaveSpawner.Wave { coinReward = 50 } });
            int defeats = 0;
            int wins = 0;
            spawner.onDefeated.AddListener(() => defeats++);
            spawner.onAllWavesCompleted.AddListener(() => wins++);
            // Defeat during combat-start notification, before any spawning or completion.
            spawner.onCombatPhaseStarted.AddListener((_, __, ___) => townHall.TakeDamage(1000));
            spawner.gameObject.SetActive(true);
            yield return null;
            int balance = wallet.Coins;
            spawner.StartNextWave();
            yield return null;
            spawner.StartNextWave();
            Assert.That(spawner.CurrentPhase, Is.EqualTo(WaveSpawner.Phase.Defeated));
            Assert.That(wallet.Coins, Is.EqualTo(balance));
            Assert.That(defeats, Is.EqualTo(1));
            Assert.That(wins, Is.Zero);
        }

        private GameObject New(string name)
        {
            var go = new GameObject(name);
            go.SetActive(false);
            objects.Add(go);
            return go;
        }

        private HealthBarBinding Binding()
        {
            var go = New("Health target");
            var health = go.AddComponent<CombatTestTarget>();
            var binding = go.AddComponent<HealthBarBinding>();
            Set(binding, "target", health);
            return binding;
        }

        private HealthBarManager Manager()
        {
            var template = New("Health bar template");
            template.AddComponent<RectTransform>();
            var bar = template.AddComponent<HealthBar>();
            var manager = New("Health bar manager").AddComponent<HealthBarManager>();
            Set(manager, "defaultHealthBarPrefab", bar);
            var camera = New("World camera").AddComponent<Camera>();
            camera.gameObject.SetActive(true);
            Set(manager, "worldCamera", camera);
            return manager;
        }

        private static void Set(object target, string name, object value) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        private static object Get(object target, string name) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    }
}
