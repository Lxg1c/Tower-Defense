using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

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
        public IEnumerator ManagerBeforeBindingRendersInToolkitWithoutCanvas()
        {
            var manager = Manager();
            manager.gameObject.SetActive(true);
            var root = new VisualElement();
            manager.SetPresentationRoot(root);
            var binding = Binding();
            binding.gameObject.SetActive(true);
            yield return null;
            Assert.That(manager.ActiveBarCount, Is.EqualTo(1));
            Assert.That(root.Q<VisualElement>(className: "entity-health-bar"), Is.Not.Null);
            binding.Target.TakeDamage(25);
            yield return null;
            binding.Target.TakeDamage(1000);
            binding.Target.ResetToMaxHealth();
            binding.Target.TakeDamage(10);
            Assert.That(manager.ActiveBarCount, Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None), Is.Empty);
        }

        [UnityTest]
        public IEnumerator BindingReplacementReleasesOldToolkitElement()
        {
            var manager = Manager();
            manager.gameObject.SetActive(true);
            var root = new VisualElement();
            manager.SetPresentationRoot(root);
            var binding = Binding();
            binding.gameObject.SetActive(true);
            yield return null;
            Assert.That(root.Q<VisualElement>(className: "entity-health-bar"), Is.Not.Null);
            binding.gameObject.SetActive(false);
            Assert.That(manager.ActiveBarCount, Is.Zero);
            Assert.That(root.Q<VisualElement>(className: "entity-health-bar"), Is.Null);
            var replacement = Binding();
            replacement.gameObject.SetActive(true);
            yield return null;
            Assert.That(manager.ActiveBarCount, Is.EqualTo(1));
            Assert.That(root.Q<VisualElement>(className: "entity-health-bar"), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator SceneUnloadReleasesBindingsAndToolkitPresentation()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.CreateScene("Health presentation teardown");
            var manager = Manager();
            var binding = Binding();
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(manager.gameObject, scene);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(binding.gameObject, scene);
            manager.gameObject.SetActive(true);
            var root = new VisualElement();
            manager.SetPresentationRoot(root);
            binding.gameObject.SetActive(true);
            yield return null;
            Assert.That(manager.ActiveBarCount, Is.EqualTo(1));
            yield return UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(scene);
            Assert.That(root.Q<VisualElement>(className: "health-bar-layer"), Is.Null);
            Assert.That(System.Linq.Enumerable.Contains(HealthBarBinding.Active, binding), Is.False);
        }

        [UnityTest]
        public IEnumerator BaseDeathStopsAnInFlightWaveWithoutReward()
        {
            var wallet = New("Wallet").AddComponent<PlayerWallet>();
            wallet.ConfigureStartingCoins(0);
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

        [UnityTest]
        public IEnumerator PlatformPreviewSwitchesToolkitLayout()
        {
            var root = New("Gameplay HUD");
            var layout = root.AddComponent<ResponsiveGameplayHud>();
            Set(layout, "editorPreview", ResponsiveGameplayHud.EditorPreviewMode.Desktop);
            root.SetActive(true);
            yield return null;
            Assert.That(layout.IsDesktopLayout, Is.True);
            Set(layout, "editorPreview", ResponsiveGameplayHud.EditorPreviewMode.Mobile);
            yield return null;
            Assert.That(layout.IsDesktopLayout, Is.False);
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
            var manager = New("Health bar manager").AddComponent<HealthBarManager>();
            var camera = New("World camera").AddComponent<Camera>();
            camera.gameObject.SetActive(true);
            Set(manager, "worldCamera", camera);
            return manager;
        }

        private static void Set(object target, string name, object value) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
