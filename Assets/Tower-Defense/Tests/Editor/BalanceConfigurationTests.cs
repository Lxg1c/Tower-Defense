using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TowerDefense.Tests
{
    public class BalanceConfigurationTests
    {
        [Test]
        public void MineUpgradesRepayTheirCostAfterOneSurvivingWave()
        {
            var loadout = AssetDatabase.LoadAssetAtPath<TowerLoadout>(
                "Assets/Tower-Defense/Prefabs/TowerLoadout.asset");
            var mine = loadout.options.Single(o => o.slotType == BuildSlotType.Mine).prefab;
            int baseIncome = mine.GetComponent<MineIncome>().CoinsPerWave;
            int previousIncome = baseIncome;
            var levels = new SerializedObject(mine.GetComponent<TowerUpgrade>()).FindProperty("levels");
            Assert.That(levels.arraySize, Is.GreaterThan(0));
            for (int i = 0; i < levels.arraySize; i++)
            {
                var level = levels.GetArrayElementAtIndex(i);
                int income = Mathf.RoundToInt(baseIncome * level.FindPropertyRelative("incomeMul").floatValue);
                int cost = level.FindPropertyRelative("upgradeCost").intValue;
                Assert.That(cost, Is.GreaterThan(0), "Upgrades must not be free.");
                Assert.That(income - previousIncome, Is.GreaterThanOrEqualTo(cost),
                    $"Mine upgrade {i + 1} must repay its cost at the next surviving clear.");
                previousIncome = income;
            }
        }

        [Test]
        public void LevelIntroducesFlightGraduallyAndIncreasesWaveHealthWithinBudget()
        {
            var scene = EditorSceneManager.OpenPreviewScene("Assets/Tower-Defense/Scenes/Level.unity");
            try
            {
                var spawner = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<WaveSpawner>(true)).Single();
                spawner.ConfigureLevel(AssetDatabase.LoadAssetAtPath<LevelDefinition>("Assets/Tower-Defense/Levels/Level3.asset"));
                Assert.That(spawner.WaveCount, Is.EqualTo(4));
                float previousHealth = 0;
                for (int i = 0; i < spawner.WaveCount; i++)
                {
                    var wave = spawner.GetWave(i);
                    var entries = wave.spawnGroups != null && wave.spawnGroups.Length > 0
                        ? wave.spawnGroups.SelectMany(g => g.entries).ToArray() : wave.entries;
                    Assert.That(entries, Is.Not.Empty);
                    Assert.That(entries.All(e => e.Prefab != null && e.count > 0), Is.True);
                    float health = entries.Sum(e => e.count * e.Prefab.GetComponent<MobHealth>().MaxHealth);
                    if (i > 0)
                    {
                        Assert.That(health, Is.GreaterThan(previousHealth), $"Wave {i + 1}");
                        Assert.That(health, Is.LessThanOrEqualTo(previousHealth * 1.6f),
                            $"Wave {i + 1}: introductory wave HP budget must grow by at most 60%.");
                    }
                    int flyers = entries.Where(e => e.Prefab.GetComponent<FlyingNav>() != null).Sum(e => e.count);
                    if (i == 0) Assert.That(flyers, Is.Zero);
                    if (i == 1) Assert.That(flyers, Is.InRange(1, 2), "Introduce flight with a small group.");
                    if (i == 3)
                        Assert.That(entries.Any(e => e.Prefab.GetComponent<MobHealth>().MaxHealth >= 300),
                            Is.True, "The finale should include the previously introduced armored threat.");
                    previousHealth = health;
                }
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
