using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TowerDefense.Tests
{
    public class LevelSetupTests
    {
        private GameObject root, enemy;
        private EnemyOption option;
        private LevelDefinition level;
        static void Set(object target, string field, object value) =>
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

        [UnitySetUp]
        public IEnumerator Setup()
        {
            RunSelection.Clear();
            enemy = new GameObject("Inactive test enemy");
            enemy.SetActive(false);
            enemy.AddComponent<MobHealth>();
            var core = enemy.AddComponent<MobCore>();
            option = ScriptableObject.CreateInstance<EnemyOption>();
            option.prefab = core;
            level = ScriptableObject.CreateInstance<LevelDefinition>();
            level.displayName = "Test Level";
            level.startingCoins = 17;
            level.waves = new[] { new LevelDefinition.LevelWave {
                coinReward = 6,
                spawnGroups = new[] { new LevelDefinition.LevelSpawnGroup {
                    spawnPointIndex = 0,
                    entries = new[] { new LevelDefinition.LevelEnemyEntry { enemy = option, count = 3, spawnInterval = .5f } }
                } }
            } };
            yield return null;
        }

        private void CreateSceneObjects()
        {
            root = new GameObject("Level setup fixture");
            root.SetActive(false);
            var walletObject = new GameObject("Wallet");
            walletObject.transform.SetParent(root.transform);
            var wallet = walletObject.AddComponent<PlayerWallet>();
            var spawnerObject = new GameObject("Spawner");
            spawnerObject.transform.SetParent(root.transform);
            var spawner = spawnerObject.AddComponent<WaveSpawner>();
            Set(spawner, "wallet", wallet);
            Set(spawner, "spawnPoints", new[] { root.transform });
            var setup = root.AddComponent<LevelSetup>();
            Set(setup, "wallet", wallet);
            Set(setup, "spawner", spawner);
            Set(setup, "directPlayLevel", level);
        }

        [UnityTest]
        public IEnumerator SelectedLevelInitializesCoinsAndFreshWavesOnRestart()
        {
            RunSelection.Select(level, UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            for (int attempt = 0; attempt < 2; attempt++)
            {
                CreateSceneObjects();
                root.SetActive(true);
                yield return null;
                var wallet = root.GetComponentInChildren<PlayerWallet>();
                var spawner = root.GetComponentInChildren<WaveSpawner>();
                Assert.That(wallet.Coins, Is.EqualTo(17));
                Assert.That(spawner.WaveCount, Is.EqualTo(1));
                Assert.That(spawner.NextWave.coinReward, Is.EqualTo(6));
                Assert.That(spawner.NextWave.spawnGroups[0].entries[0].count, Is.EqualTo(3));
                Assert.That(spawner.CurrentPhase, Is.EqualTo(WaveSpawner.Phase.Build));
                wallet.TrySpend(5);
                spawner.NextWave.spawnGroups[0].entries[0].count = 99;
                Object.Destroy(root);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator MissingLevelSetupStopsSessionWithoutVictoryOrReward()
        {
            CreateSceneObjects();
            Object.DestroyImmediate(root.GetComponent<LevelSetup>());
            var spawner = root.GetComponentInChildren<WaveSpawner>(true);
            var wallet = root.GetComponentInChildren<PlayerWallet>(true);
            int victories = 0;
            spawner.onAllWavesCompleted.AddListener(() => victories++);
            LogAssert.Expect(LogType.Error, "[WaveSpawner] Configure a LevelDefinition through LevelSetup before starting the session.");
            root.SetActive(true);
            int initialCoins = wallet.Coins;
            yield return null;
            spawner.StartNextWave();
            Assert.That(spawner.enabled, Is.False);
            Assert.That(victories, Is.Zero);
            Assert.That(wallet.Coins, Is.EqualTo(initialCoins));
        }

        [UnityTest]
        public IEnumerator InvalidSpawnPointStopsSessionInsteadOfChoosingAnotherPoint()
        {
            level.waves[0].spawnGroups[0].spawnPointIndex = 1;
            CreateSceneObjects();
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("\\[LevelSetup\\].*invalid spawn point"));
            root.SetActive(true);
            yield return null;
            Assert.That(root.GetComponentInChildren<WaveSpawner>().enabled, Is.False);
            Assert.That(root.GetComponentInChildren<LevelSetup>().enabled, Is.False);
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            RunSelection.Clear();
            if (root != null) Object.Destroy(root);
            Object.Destroy(enemy);
            Object.Destroy(level);
            Object.Destroy(option);
            Time.timeScale = 1;
            yield return null;
        }
    }
}
