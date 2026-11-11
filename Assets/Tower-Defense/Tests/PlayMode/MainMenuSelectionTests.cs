using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TowerDefense.Tests
{
    public sealed class MainMenuSelectionTests
    {
        private static void Set(object owner, string name, object value) => owner.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(owner, value);

        [UnityTest]
        public IEnumerator SelectionOnlyStartsConfiguredLevels()
        {
            var root = new GameObject("Menu selection test");
            root.SetActive(false);
            var enemyObject = new GameObject("Enemy test prefab");
            enemyObject.SetActive(false);
            var enemy = ScriptableObject.CreateInstance<EnemyOption>();
            enemy.prefab = enemyObject.AddComponent<MobCore>();
            enemyObject.AddComponent<MobHealth>();
            enemy.displayName = "Spider";
            var configured = ScriptableObject.CreateInstance<LevelDefinition>();
            configured.displayName = "Selection Test";
            configured.progressionId = "menu-selection-test";
            configured.waves = new[] { new LevelDefinition.LevelWave { spawnGroups = new[] {
                new LevelDefinition.LevelSpawnGroup { entries = new[] {
                    new LevelDefinition.LevelEnemyEntry { enemy = enemy, count = 1 } } } } } };
            try
            {
                var menu = root.AddComponent<MainMenu>();
                Set(menu, "levels", new LevelDefinition[] { configured, null });
                Set(menu, "gameplayLabLevel", configured);
                root.SetActive(true);
                yield return null;
                Assert.That(menu.SelectedIndex, Is.Zero);
                Assert.That(menu.CanStartSelected, Is.True);
                menu.SelectLevel(1);
                Assert.That(menu.SelectedIndex, Is.EqualTo(1));
                Assert.That(menu.CanStartSelected, Is.False);
                menu.SelectLevel(0);
                Assert.That(menu.CanStartSelected, Is.True);
            }
            finally
            {
                Object.Destroy(root);
                Object.Destroy(enemyObject);
                Object.Destroy(enemy);
                Object.Destroy(configured);
                RunSelection.Clear();
            }
            yield return null;
        }
    }
}
