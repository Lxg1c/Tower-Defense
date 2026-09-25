using System;
using System.Linq;
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

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void AuthoredLevelsUseValidSceneSpawnPoints(int number)
        {
            var scene = EditorSceneManager.OpenPreviewScene("Assets/Tower-Defense/Scenes/Level.unity");
            try
            {
                var spawner = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<WaveSpawner>(true)).Single();
                var points = new SerializedObject(spawner).FindProperty("spawnPoints");
                var level = Level(number);
                Assert.That(level, Is.Not.Null);
                Assert.DoesNotThrow(() => level.Validate(points.arraySize));
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
            RunSelection.Select(Level(1), "Level");
            Assert.That(RunSelection.TrySelectNext(Level(1), "Level"), Is.True);
            Assert.That(RunSelection.Resolve("Level", null), Is.EqualTo(Level(2)));
            Assert.That(RunSelection.TrySelectNext(Level(2), "Level"), Is.True);
            Assert.That(RunSelection.Resolve("Level", null), Is.EqualTo(Level(3)));
            Assert.That(RunSelection.TrySelectNext(Level(3), "Level"), Is.False);
            Assert.That(RunSelection.Resolve("Level", null), Is.EqualTo(Level(3)));
        }

        [Test]
        public void SelectionAndRestartPreserveAuthoredDataAndMenuClearsSelection()
        {
            RunSelection.Select(Level(2), "Level");
            var selected = RunSelection.Resolve("Level", Level(1));
            Assert.That(selected, Is.EqualTo(Level(2)));
            var first = selected.CreateWaves(4);
            int expected = first[0].spawnGroups[0].entries[0].count;
            first[0].spawnGroups[0].entries[0].count = 999;
            first[0].spawnGroups[0].spawnPointIndex = 99;
            var restart = RunSelection.Resolve("Level", Level(1)).CreateWaves(4);
            Assert.That(restart[0].spawnGroups[0].entries[0].count, Is.EqualTo(expected));
            Assert.That(restart[0].spawnGroups[0].spawnPointIndex, Is.Not.EqualTo(99));
            Assert.Throws<ArgumentException>(() => RunSelection.Resolve("OtherScene", Level(1)));
            RunSelection.Clear();
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
    }
}
