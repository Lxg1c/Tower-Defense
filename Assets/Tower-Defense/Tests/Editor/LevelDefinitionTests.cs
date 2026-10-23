using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TowerDefense.Tests
{
    public class LevelDefinitionTests
    {
        static LevelDefinition Level(int number) => AssetDatabase.LoadAssetAtPath<LevelDefinition>(
            $"Assets/Tower-Defense/Levels/Level{number}.asset");

        [TearDown] public void Cleanup() => RunSelection.Clear();

        [Test]
        public void WavePreviewCombinesEachEnemyTypePerSpawnPoint()
        {
            var holder = new GameObject("Preview spawner");
            holder.SetActive(false);
            var firstPoint = new GameObject("First spawn");
            var secondPoint = new GameObject("Second spawn");
            try
            {
                var spawner = holder.AddComponent<WaveSpawner>();
                var spider = AssetDatabase.LoadAssetAtPath<EnemyOption>(
                    "Assets/Tower-Defense/Prefabs/Mobs/Spider.asset");
                var shooter = AssetDatabase.LoadAssetAtPath<EnemyOption>(
                    "Assets/Tower-Defense/Prefabs/Mobs/Shooter.asset");
                Assert.That(spider, Is.Not.Null);
                Assert.That(shooter, Is.Not.Null);
                var wave = new WaveSpawner.Wave
                {
                    spawnGroups = new[]
                    {
                        new WaveSpawner.SpawnGroup { spawnPointIndex = 0, entries = new[]
                        {
                            new WaveSpawner.MobEntry { enemy = spider, count = 2 },
                            new WaveSpawner.MobEntry { enemy = shooter, count = 4 }
                        } },
                        new WaveSpawner.SpawnGroup { spawnPointIndex = 0, entries = new[]
                        {
                            new WaveSpawner.MobEntry { enemy = spider, count = 3 }
                        } },
                        new WaveSpawner.SpawnGroup { spawnPointIndex = 1, entries = new[]
                        {
                            new WaveSpawner.MobEntry { enemy = spider, count = 5 }
                        } }
                    }
                };
                typeof(WaveSpawner).GetField("spawnPoints", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(spawner, new[] { firstPoint.transform, secondPoint.transform });
                typeof(WaveSpawner).GetField("waves", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(spawner, new[] { wave });

                var preview = new List<WaveSpawner.WavePreviewEntry>();
                spawner.GetWavePreviewEntries(0, preview);

                Assert.That(preview.Count, Is.EqualTo(3));
                Assert.That(preview[0].spawnPoint, Is.SameAs(firstPoint.transform));
                Assert.That(preview[0].icon, Is.EqualTo(spider.icon));
                Assert.That(preview[0].count, Is.EqualTo(5));
                Assert.That(preview[0].stackIndex, Is.EqualTo(0));
                Assert.That(preview[0].stackCount, Is.EqualTo(2));
                Assert.That(preview[1].icon, Is.EqualTo(shooter.icon));
                Assert.That(preview[1].count, Is.EqualTo(4));
                Assert.That(preview[1].stackIndex, Is.EqualTo(1));
                Assert.That(preview[1].stackCount, Is.EqualTo(2));
                Assert.That(preview[2].spawnPoint, Is.SameAs(secondPoint.transform));
                Assert.That(preview[2].count, Is.EqualTo(5));
                Assert.That(preview[2].stackIndex, Is.EqualTo(0));
                Assert.That(preview[2].stackCount, Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(holder);
                UnityEngine.Object.DestroyImmediate(firstPoint);
                UnityEngine.Object.DestroyImmediate(secondPoint);
            }
        }

        [Test]
        public void AuthoredMoneyRewardsHaveDistinctStableIds()
        {
            var levels = Enumerable.Range(1, 3).Select(Level).ToArray();
            Assert.That(levels.All(level => level.moneyReward == 100 &&
                !string.IsNullOrWhiteSpace(level.progressionId)), Is.True);
            Assert.That(levels.Select(level => level.progressionId).Distinct().Count(), Is.EqualTo(levels.Length));
            foreach (var level in levels)
            {
                Assert.That(level.GetMoneyReward(2), Is.EqualTo(150));
                Assert.That(level.GetMoneyReward(3), Is.EqualTo(225));
            }
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void AuthoredLevelsUseValidSceneSpawnPoints(int number)
        {
            var scene = EditorSceneManager.OpenPreviewScene("Assets/Tower-Defense/Scenes/Level 1.unity");
            try
            {
                var spawner = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<WaveSpawner>(true)).Single();
                var points = new SerializedObject(spawner).FindProperty("spawnPoints");
                var level = Level(number);
                Assert.That(level, Is.Not.Null);
                Assert.DoesNotThrow(() => level.ValidateAll(points.arraySize));
                for (int i = 0; i < points.arraySize; i++)
                    Assert.That(points.GetArrayElementAtIndex(i).objectReferenceValue, Is.Not.Null);
                var setup = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<LevelSetup>(true)).Single();
                var data = new SerializedObject(setup);
                Assert.That(data.FindProperty("directPlayLevel").objectReferenceValue, Is.EqualTo(Level(1)));
                Assert.That(data.FindProperty("spawner").objectReferenceValue, Is.EqualTo(spawner));
                Assert.That(data.FindProperty("wallet").objectReferenceValue, Is.Not.Null);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void VictoryAdvancesThroughLevelsAndStopsAtTheLast()
        {
            RunSelection.Select(Level(1), "Level", 2);
            Assert.That(RunSelection.Difficulty, Is.EqualTo(2));
            Assert.That(RunSelection.TrySelectNext(Level(1), "Level"), Is.True);
            Assert.That(RunSelection.Resolve("Level", null), Is.EqualTo(Level(2)));
            Assert.That(RunSelection.Difficulty, Is.EqualTo(1));
            Assert.That(RunSelection.TrySelectNext(Level(2), "Level"), Is.True);
            Assert.That(RunSelection.Resolve("Level", null), Is.EqualTo(Level(3)));
            Assert.That(RunSelection.TrySelectNext(Level(3), "Level"), Is.False);
            Assert.That(RunSelection.Resolve("Level", null), Is.EqualTo(Level(3)));
        }

        [Test]
        public void SelectionAndRestartPreserveAuthoredDataAndMenuClearsSelection()
        {
            RunSelection.Select(Level(2), "Level", 2);
            Assert.That(RunSelection.Difficulty, Is.EqualTo(2));
            var selected = RunSelection.Resolve("Level", Level(1));
            Assert.That(selected, Is.EqualTo(Level(2)));
            var first = selected.CreateWaves(4, RunSelection.Difficulty);
            int expected = first[0].spawnGroups[0].entries[0].count;
            first[0].spawnGroups[0].entries[0].count = 999;
            first[0].spawnGroups[0].spawnPointIndex = 99;
            var restart = RunSelection.Resolve("Level", Level(1)).CreateWaves(4, RunSelection.Difficulty);
            Assert.That(restart[0].spawnGroups[0].entries[0].count, Is.EqualTo(expected));
            Assert.That(restart[0].spawnGroups[0].spawnPointIndex, Is.Not.EqualTo(99));
            Assert.Throws<ArgumentException>(() => RunSelection.Resolve("OtherScene", Level(1)));
            RunSelection.Clear();
            Assert.That(RunSelection.Difficulty, Is.EqualTo(1));
            Assert.That(RunSelection.Resolve("Level", Level(1)), Is.EqualTo(Level(1)));
            Assert.Throws<ArgumentException>(() => RunSelection.Resolve("Level", null));
        }

        [Test]
        public void BrokenLevelIsRejectedWithoutSubstituteSpawnPointsOrEnemies()
        {
            var copy = UnityEngine.Object.Instantiate(Level(1));
            try
            {
                copy.waves[0].spawnGroups[0].spawnPointIndex = 4;
                Assert.Throws<ArgumentException>(() => copy.CreateWaves(4));
                copy.waves[0].spawnGroups[0].spawnPointIndex = 0;
                copy.waves[0].spawnGroups[0].entries[0].enemy = null;
                Assert.Throws<ArgumentException>(() => copy.CreateWaves(4));
                Assert.Throws<ArgumentException>(() => RunSelection.Select(copy, "Level"));
            }
            finally { UnityEngine.Object.DestroyImmediate(copy); }
        }

        [Test]
        public void LevelsIntroduceIncreasingEnemyPressure()
        {
            int Count(LevelDefinition l) => l.waves.Sum(w => w.spawnGroups.Sum(g => g.entries.Sum(e => e.count)));
            Assert.That(Count(Level(2)), Is.GreaterThan(Count(Level(1))));
            Assert.That(Count(Level(3)), Is.GreaterThan(Count(Level(2))));
            Assert.That(Level(1).waves.SelectMany(w => w.spawnGroups).SelectMany(g => g.entries)
                .All(e => e.enemy.prefab.GetComponent<FlyingNav>() == null), Is.True);
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void HigherDifficultiesContainMoreEnemies(int number)
        {
            var level = Level(number);
            int Count(int difficulty) => level.GetWaves(difficulty)
                .Sum(w => w.spawnGroups.Sum(g => g.entries.Sum(e => e.count)));
            Assert.That(Count(2), Is.GreaterThan(Count(1)));
            Assert.That(Count(3), Is.GreaterThan(Count(2)));
        }
    }
}
