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
        public IEnumerator DestroyedBarIsNotReturnedToThePoolDuringTeardown()
        {
            var manager = Manager();
            manager.gameObject.SetActive(true);
            var binding = Binding();
            binding.gameObject.SetActive(true);
            binding.Target.TakeDamage(25);
            yield return null;
            var bar = Object.FindFirstObjectByType<HealthBar>();
            Assert.That(bar, Is.Not.Null);
            Object.Destroy(bar.gameObject);
            yield return null;
            binding.gameObject.SetActive(false);
            Assert.That(manager.ActiveBarCount, Is.Zero);
            binding.gameObject.SetActive(true);
            binding.Target.TakeDamage(5);
            yield return null;
            Assert.That(manager.ActiveBarCount, Is.EqualTo(1));
            Assert.That(Object.FindFirstObjectByType<HealthBar>(), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator SceneUnloadReleasesBindingsAndOwnedCanvas()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.CreateScene("Health presentation teardown");
            var manager = Manager();
            var binding = Binding();
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(manager.gameObject, scene);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(binding.gameObject, scene);
            manager.gameObject.SetActive(true);
            binding.gameObject.SetActive(true);
            yield return null;
            var canvas = (Canvas)Get(manager, "worldCanvas");
            Assert.That(manager.ActiveBarCount, Is.EqualTo(1));
            yield return UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(scene);
            Assert.That(canvas == null, Is.True);
            Assert.That(System.Linq.Enumerable.Contains(HealthBarBinding.Active, binding), Is.False);
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

        [UnityTest]
        public IEnumerator BuildGuideTracksBaseAndHidesOnCombatAndDefeat()
        {
            var wallet = New("Wallet").AddComponent<PlayerWallet>();
            wallet.gameObject.SetActive(true);
            var spawner = New("Spawner").AddComponent<WaveSpawner>();
            Set(spawner, "wallet", wallet);
            Set(spawner, "waves", new[] { new WaveSpawner.Wave() });
            spawner.gameObject.SetActive(true);
            var panel = New("Guide panel");
            var label = panel.AddComponent<TMPro.TextMeshProUGUI>();
            var guide = New("Guide owner").AddComponent<BuildPhaseGuide>();
            Set(guide, "spawner", spawner);
            Set(guide, "label", label);
            Set(guide, "panel", panel);
            guide.gameObject.SetActive(true);
            yield return null;
            Assert.That(panel.activeSelf, Is.True);
            Assert.That(label.text, Is.EqualTo("Stand on the home marker to build your base"));

            guide.enabled = false;
            var townHall = New("Base").AddComponent<Base>();
            townHall.gameObject.SetActive(true);
            Assert.That(label.text, Is.EqualTo("Stand on the home marker to build your base"), "Disabled guide must detach its listeners.");
            guide.enabled = true;
            Assert.That(label.text, Does.StartWith("Prepare your defenses"));
            bool hiddenDuringCombat = false;
            spawner.onCombatPhaseStarted.AddListener((_, __, ___) =>
            {
                hiddenDuringCombat = !panel.activeSelf;
                townHall.TakeDamage(1000);
            });
            spawner.StartNextWave();
            Assert.That(hiddenDuringCombat, Is.True);
            Assert.That(panel.activeSelf, Is.False);
            Assert.That(spawner.CurrentPhase, Is.EqualTo(WaveSpawner.Phase.Defeated));
        }

        [UnityTest]
        public IEnumerator WavePreviewLeavesTouchControlsClearAndPointsToSpawn()
        {
            var camera = New("Preview camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.gameObject.SetActive(true);
            var canvas = New("Preview canvas").AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.gameObject.SetActive(true);
            var marker = New("Wave marker");
            var rect = marker.AddComponent<RectTransform>();
            rect.SetParent(canvas.transform, false);
            var indicator = marker.AddComponent<WaveDirectionIndicator>();
            var arrow = New("Direction arrow").AddComponent<RectTransform>();
            arrow.SetParent(rect, false);
            Set(indicator, "directionArrow", arrow);
            Set(indicator, "worldCamera", camera);
            marker.SetActive(true);
            var spawn = New("Spawn").transform;
            foreach (var position in new[] { new Vector3(10, -10, 10), new Vector3(-10, -10, 10) })
            {
                spawn.position = position;
                indicator.Bind(new WaveSpawner.WavePreviewEntry { spawnPoint = spawn, count = 10 });
                yield return null;
                Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, rect.position);
                Assert.That(screen.y, Is.GreaterThanOrEqualTo(Screen.safeArea.yMin + Screen.safeArea.height * 0.36f - 1));
                Vector2 desired = (Vector2)camera.WorldToScreenPoint(position) - screen;
                Assert.That(Vector2.Dot(arrow.up, desired.normalized), Is.GreaterThan(0.99f));
            }
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
