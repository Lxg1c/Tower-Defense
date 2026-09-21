using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace TowerDefense.Tests
{
    public class BuildingLevelListTests
    {
        [TestCase("CannonTower", 4)]
        [TestCase("PulseTower", 4)]
        [TestCase("Mine", 3)]
        [TestCase("Base", 4)]
        public void AuthoredLevelsIncludeBaseAndEveryUpgrade(string name, int count)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"Assets/Tower-Defense/Prefabs/Buildings/{name}.prefab");
            Assert.That(prefab, Is.Not.Null);
            var values = BuildingLevelList.Read(new TowerOption { prefab = prefab });
            Assert.That(values.Count, Is.EqualTo(count));
            var health = prefab.GetComponentInChildren<Damageable>(true).MaxHealth;
            Assert.That(values[0].Health, Is.EqualTo(health));
            Assert.That(values[count - 1].Health, Is.GreaterThan(health));
            if (name == "Mine")
            {
                Assert.That(values[0].Damage, Is.Null);
                Assert.That(values[count - 1].Income, Is.GreaterThan(values[0].Income));
            }
            else if (name == "Base") Assert.That(values[0].Damage, Is.Null);
            else Assert.That(values[count - 1].Damage, Is.GreaterThan(values[0].Damage));
        }
    }
}
