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
        public void BuildingOptionsUnlockAtConfiguredTownHallLevels()
        {
            var loadout = AssetDatabase.LoadAssetAtPath<TowerLoadout>(
                "Assets/Tower-Defense/Prefabs/TowerLoadout.asset");
            Assert.That(loadout, Is.Not.Null);
            Assert.That(loadout.options.Single(option => option.displayName == "Ratusha").requiredTownHallLevel, Is.Zero);
            Assert.That(loadout.options.Single(option => option.displayName == "Cannon Turret").requiredTownHallLevel, Is.EqualTo(1));
            Assert.That(loadout.options.Single(option => option.displayName == "Mine").requiredTownHallLevel, Is.EqualTo(1));
            Assert.That(loadout.options.Single(option => option.displayName == "Pulse").requiredTownHallLevel, Is.EqualTo(2));
            Assert.That(loadout.options.Where(option => option.slotType == BuildSlotType.Barracks)
                .Select(option => option.requiredTownHallLevel), Is.EquivalentTo(new[] { 2, 2 }));
        }

        [Test]
        public void BarracksChoicesSpawnDifferentSquadTypesAndUnlockTogether()
        {
            var loadout = AssetDatabase.LoadAssetAtPath<TowerLoadout>(
                "Assets/Tower-Defense/Prefabs/TowerLoadout.asset");
            var barracks = new BuildingSlot(loadout, BuildSlotType.Barracks, 2);
            Assert.That(barracks.GetOptions(1), Is.Empty);
            var choices = barracks.GetOptions(2).ToArray();
            Assert.That(choices.Select(option => option.displayName),
                Is.EquivalentTo(new[] { "Barracks / Melee", "Barracks / Ranged" }));
            Assert.That(choices.Select(option => option.prefab).Distinct().Count(), Is.EqualTo(2));
            Assert.That(choices.Select(option => new SerializedObject(
                    option.prefab.GetComponentInChildren<BarracksBuilding>(true))
                    .FindProperty("squadType").enumValueIndex),
                Is.EquivalentTo(new[] { (int)AllySoldier.CombatType.Melee,
                    (int)AllySoldier.CombatType.Ranged }));
            foreach (var choice in choices)
            {
                var model = choice.prefab.transform.Find("barrack");
                Assert.That(model, Is.Not.Null);
                Assert.That(Quaternion.Angle(model.localRotation, Quaternion.identity), Is.LessThan(1f));
            }
            Assert.That(new BuildingSlot(loadout, BuildSlotType.Defense, 1).GetOptions(1)
                .Select(option => option.displayName), Is.EquivalentTo(new[] { "Cannon Turret" }));
            Assert.That(new BuildingSlot(loadout, BuildSlotType.Defense, 1).GetOptions(2)
                .Select(option => option.displayName), Is.EquivalentTo(new[] { "Cannon Turret", "Pulse" }));
            var cannon = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Tower-Defense/Prefabs/Buildings/CannonTower.prefab");
            Assert.That(cannon.GetComponentInChildren<DetectionZone>(true).Radius, Is.EqualTo(14f));
        }

        [Test]
        public void EveryEnemyHasOneCentralDropSetting()
        {
            var loadout = AssetDatabase.LoadAssetAtPath<EnemyLoadout>(
                "Assets/Tower-Defense/Prefabs/EnemyLoadout.asset");
            Assert.That(loadout, Is.Not.Null);
            Assert.DoesNotThrow(loadout.Validate);
            Assert.That(loadout.enemies.Length, Is.EqualTo(4));
            var spider = loadout.enemies.Single(entry => entry.enemy.name == "Spider");
            Assert.That(spider.slowAmmoChance, Is.EqualTo(0.25f));
            Assert.That(spider.slowAmmoDurationSeconds, Is.EqualTo(12f));
            Assert.That(spider.shieldChance, Is.Zero);
            Assert.That(spider.companionChance, Is.Zero);
            Assert.That(loadout.slowAmmoPickupPrefab, Is.Not.Null);
            var destroyer = loadout.enemies.Single(entry => entry.enemy.name == "Destoyer");
            Assert.That(destroyer.shieldChance, Is.EqualTo(0.25f));
            Assert.That(destroyer.shieldDurationSeconds, Is.EqualTo(12f));
            Assert.That(loadout.shieldPickupPrefab, Is.Not.Null);
            var shooter = loadout.enemies.Single(entry => entry.enemy.name == "Shooter");
            Assert.That(shooter.companionChance, Is.EqualTo(0.2f));
            Assert.That(shooter.companionDurationSeconds, Is.EqualTo(18f));
            Assert.That(loadout.companionPickupPrefab, Is.Not.Null);
            Assert.That(loadout.slowAmmoPickupPrefab.GetComponent<PickupHighlight>(), Is.Not.Null);
            Assert.That(loadout.shieldPickupPrefab.GetComponent<PickupHighlight>(), Is.Not.Null);
            Assert.That(loadout.companionPickupPrefab.GetComponent<PickupHighlight>(), Is.Not.Null);
            var player = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Tower-Defense/Prefabs/Player.prefab");
            var companion = player.GetComponent<CompanionTurretController>();
            Assert.That(companion, Is.Not.Null);
            Assert.That(new SerializedObject(companion).FindProperty("turretPrefab").objectReferenceValue,
                Is.Not.Null);
        }

        [TestCase(0.10f, EnemyLoadout.PickupKind.SlowAmmo)]
        [TestCase(0.30f, EnemyLoadout.PickupKind.Shield)]
        [TestCase(0.60f, EnemyLoadout.PickupKind.Companion)]
        [TestCase(0.90f, EnemyLoadout.PickupKind.None)]
        public void OneRollSelectsAtMostOnePickup(float roll, EnemyLoadout.PickupKind expected)
        {
            var entry = new EnemyLoadout.EnemyEntry
            {
                slowAmmoChance = 0.25f,
                shieldChance = 0.25f,
                companionChance = 0.25f
            };
            Assert.That(entry.PickDrop(roll), Is.EqualTo(expected));
        }

        [Test]
        public void FullDropTableIncludesTheUpperRandomBoundary()
        {
            var entry = new EnemyLoadout.EnemyEntry
            {
                slowAmmoChance = 0.5f,
                shieldChance = 0.25f,
                companionChance = 0.25f
            };
            Assert.That(entry.PickDrop(1f), Is.EqualTo(EnemyLoadout.PickupKind.Companion));
        }

        [Test]
        public void DropChancesAboveOneHundredPercentAreRejected()
        {
            var source = AssetDatabase.LoadAssetAtPath<EnemyLoadout>(
                "Assets/Tower-Defense/Prefabs/EnemyLoadout.asset");
            var copy = Object.Instantiate(source);
            try
            {
                var spider = copy.enemies[3];
                spider.slowAmmoChance = 0.6f;
                spider.shieldChance = 0.6f;
                copy.enemies[3] = spider;
                Assert.Throws<System.ArgumentException>(copy.Validate);
            }
            finally { Object.DestroyImmediate(copy); }
        }

        [Test]
        public void PlayerAndSpiderUseTheLayersIgnoredDuringTheLevel()
        {
            var player = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Tower-Defense/Prefabs/Player.prefab");
            var spider = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Tower-Defense/Prefabs/Mobs/EnemySpider.prefab");
            Assert.That(player.layer, Is.EqualTo(LayerMask.NameToLayer("Player")));
            Assert.That(spider.layer, Is.EqualTo(LayerMask.NameToLayer("Enemy")));
            Assert.That(player.GetComponent<Collider>().isTrigger, Is.False);
            Assert.That(spider.GetComponent<Collider>().isTrigger, Is.False);
        }

        [Test]
        public void LevelWiresCompanionStatusIntoTheEffectRow()
        {
            var scene = EditorSceneManager.OpenPreviewScene("Assets/Tower-Defense/Scenes/Level 1.unity");
            try
            {
                var panel = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<EffectStatusPanel>(true)).Single();
                var serialized = new SerializedObject(panel);
                foreach (string field in new[] { "companionTurrets", "companionIndicator",
                             "companionRing", "companionTime", "companionCount" })
                    Assert.That(serialized.FindProperty(field).objectReferenceValue, Is.Not.Null, field);
                Assert.That(panel.transform.Find("TurretEffect"), Is.Not.Null);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

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
            var scene = EditorSceneManager.OpenPreviewScene("Assets/Tower-Defense/Scenes/Level 1.unity");
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
                        Assert.That(health, Is.LessThanOrEqualTo(previousHealth * 1.8f),
                            $"Wave {i + 1}: wave HP must grow by at most 80%.");
                    }
                    int flyers = entries.Where(e => e.Prefab.GetComponent<FlyingNav>() != null).Sum(e => e.count);
                    if (i == 0) Assert.That(flyers, Is.Zero);
                    if (i == 1) Assert.That(flyers, Is.InRange(1, 3), "Introduce flight with a small group.");
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
