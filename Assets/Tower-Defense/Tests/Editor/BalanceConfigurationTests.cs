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
                "Assets/Tower-Defense/Data/Loadouts/TowerLoadout.asset");
            Assert.That(loadout, Is.Not.Null);
            Assert.That(loadout.options.Single(option => option.displayName == "Ratusha").requiredTownHallLevel, Is.Zero);
            Assert.That(loadout.options.Single(option => option.displayName == "Cannon Turret").requiredTownHallLevel, Is.EqualTo(1));
            Assert.That(loadout.options.Single(option => option.displayName == "Mine").requiredTownHallLevel, Is.EqualTo(1));
            Assert.That(loadout.options.Single(option => option.displayName == "Pulse").requiredTownHallLevel, Is.EqualTo(2));
            Assert.That(loadout.options.Single(option => option.displayName == "Mortar").requiredTownHallLevel, Is.EqualTo(3));
            Assert.That(loadout.options.Single(option => option.displayName == "Wall").requiredTownHallLevel, Is.EqualTo(2));
            Assert.That(loadout.options.Where(option => option.slotType == BuildSlotType.Barracks)
                .Select(option => option.requiredTownHallLevel), Is.EquivalentTo(new[] { 2, 3 }));
        }

        [Test]
        public void LevelOnePlacementZonesOpenAsTheTownHallGrows()
        {
            var scene = EditorSceneManager.OpenPreviewScene("Assets/Tower-Defense/Scenes/Level 1.unity");
            try
            {
                var zones = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<TowerPlacementZone>(true))
                    .Select(zone => new SerializedObject(zone))
                    .Select(data => (
                        Type: (BuildSlotType)data.FindProperty("allowedType").enumValueIndex,
                        Level: data.FindProperty("requiredTownHallLevel").intValue))
                    .ToArray();
                Assert.That(zones.Count(zone => zone.Level <= 1 && zone.Type == BuildSlotType.Mine), Is.EqualTo(2));
                Assert.That(zones.Count(zone => zone.Level <= 1 && zone.Type == BuildSlotType.Defense), Is.EqualTo(1));
                Assert.That(zones.Count(zone => zone.Level <= 1 && zone.Type == BuildSlotType.Barracks), Is.Zero);
                Assert.That(zones.Count(zone => zone.Level <= 1 && zone.Type == BuildSlotType.Wall), Is.Zero);
                Assert.That(zones.Count(zone => zone.Level <= 2 && zone.Type == BuildSlotType.Mine), Is.EqualTo(4));
                Assert.That(zones.Count(zone => zone.Level <= 2 && zone.Type == BuildSlotType.Defense), Is.EqualTo(4));
                Assert.That(zones.Count(zone => zone.Level <= 2 && zone.Type == BuildSlotType.Barracks), Is.EqualTo(1));
                Assert.That(zones.Count(zone => zone.Level <= 2 && zone.Type == BuildSlotType.Wall), Is.EqualTo(1));
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void BarracksChoicesSpawnDifferentSquadTypesAndUnlockInSequence()
        {
            var loadout = AssetDatabase.LoadAssetAtPath<TowerLoadout>(
                "Assets/Tower-Defense/Data/Loadouts/TowerLoadout.asset");
            var barracks = new BuildingSlot(loadout, BuildSlotType.Barracks, 2);
            Assert.That(barracks.GetOptions(1), Is.Empty);
            var choices = barracks.GetOptions(2).ToArray();
            Assert.That(choices.Select(option => option.displayName),
                Is.EquivalentTo(new[] { "Barracks / Melee" }));
            choices = barracks.GetOptions(3).ToArray();
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
                .Select(option => option.displayName), Is.EqualTo(new[] { "Cannon Turret", "Pulse" }));
            Assert.That(new BuildingSlot(loadout, BuildSlotType.Defense, 1).GetOptions(3)
                .Select(option => option.displayName), Is.EqualTo(new[] { "Cannon Turret", "Pulse", "Mortar" }));
            var mortar = loadout.options.Single(option => option.displayName == "Mortar");
            Assert.That(mortar.cost, Is.GreaterThan(loadout.options
                .Where(option => option.slotType == BuildSlotType.Defense && option != mortar)
                .Max(option => option.cost)));
            Assert.That(mortar.prefab.GetComponent<MortarTower>(), Is.Not.Null);
            Assert.That(mortar.prefab.GetComponent<MortarTower>().enabled, Is.True);
            Assert.That(mortar.prefab.GetComponent<DetectionZone>().Radius, Is.GreaterThan(24f));
            var wall = loadout.options.Single(option => option.displayName == "Wall");
            Assert.That(wall.slotType, Is.EqualTo(BuildSlotType.Wall));
            Assert.That(wall.prefab.GetComponent<WallCollision>(), Is.Not.Null);
            var wallSpan = wall.prefab.GetComponent<WallSpan>();
            Assert.That(wallSpan, Is.Not.Null);
            var wallSettings = new SerializedObject(wallSpan);
            foreach (string field in new[] { "model", "body", "obstacle" })
                Assert.That(wallSettings.FindProperty(field).objectReferenceValue, Is.Not.Null, field);
            Assert.That(wall.prefab.GetComponent<UnityEngine.AI.NavMeshObstacle>().carving, Is.True);
            var cannon = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Tower-Defense/Prefabs/Buildings/CannonTower.prefab");
            Assert.That(cannon.GetComponentInChildren<DetectionZone>(true).Radius, Is.EqualTo(24f));
        }

        [Test]
        public void LongerWallFitsItsCollisionAndNavMeshObstacleToReservedCells()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Tower-Defense/Prefabs/Buildings/Wall.prefab");
            GameObject instance = Object.Instantiate(prefab);
            try
            {
                var span = instance.GetComponent<WallSpan>();
                span.Configure(4, 3f);
                float width = instance.GetComponent<BoxCollider>().size.x;
                Assert.That(width, Is.EqualTo(12f).Within(0.001f));
                Assert.That(instance.GetComponent<UnityEngine.AI.NavMeshObstacle>().size.x,
                    Is.EqualTo(width).Within(0.001f));
                Assert.That(span.LengthCells, Is.EqualTo(4));

                var model = (Transform)new SerializedObject(span).FindProperty("model").objectReferenceValue;
                Assert.That(model.localScale.x * 2.66f / 2.2f, Is.EqualTo(3f).Within(0.001f));
                Assert.That(model.localScale.y, Is.GreaterThan(2.2f));
                Assert.That(instance.GetComponent<BoxCollider>().size.y, Is.GreaterThan(1.55f));
                Assert.That(instance.GetComponent<UnityEngine.AI.NavMeshObstacle>().size.y,
                    Is.EqualTo(instance.GetComponent<BoxCollider>().size.y).Within(0.001f));
                for (int i = 0; i < 4; i++)
                {
                    Transform segment = i == 0 ? model : instance.transform.Find($"Wall segment {i + 1}");
                    Assert.That(segment, Is.Not.Null, $"Missing section for cell {i}.");
                    Assert.That(segment.localPosition.x, Is.EqualTo((i - 1.5f) * 3f).Within(0.001f));
                    Assert.That(segment.localScale, Is.EqualTo(model.localScale));
                }

                span.Configure(2, 3f);
                span.Configure(2, 3f);
                Assert.That(instance.transform.Find("Wall segment 3"), Is.Null);
                Assert.That(instance.transform.Find("Wall segment 2"), Is.Not.Null);
                Assert.That(span.LengthCells, Is.EqualTo(2));
            }
            finally { Object.DestroyImmediate(instance); }
        }

        [Test]
        public void EveryEnemyHasOneCentralDropSetting()
        {
            var loadout = AssetDatabase.LoadAssetAtPath<EnemyLoadout>(
                "Assets/Tower-Defense/Data/Loadouts/EnemyLoadout.asset");
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
                "Assets/Tower-Defense/Data/Loadouts/EnemyLoadout.asset");
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
                "Assets/Tower-Defense/Data/Loadouts/TowerLoadout.asset");
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
        public void LevelIntroducesFlightGraduallyAndGrowsInEnemyCount()
        {
            var scene = EditorSceneManager.OpenPreviewScene("Assets/Tower-Defense/Scenes/Level 1.unity");
            try
            {
                var spawner = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<WaveSpawner>(true)).Single();
                spawner.ConfigureLevel(AssetDatabase.LoadAssetAtPath<LevelDefinition>("Assets/Tower-Defense/Levels/Level3.asset"));
                Assert.That(spawner.WaveCount, Is.EqualTo(4));
                int previousCount = 0;
                for (int i = 0; i < spawner.WaveCount; i++)
                {
                    var wave = spawner.GetWave(i);
                    var entries = wave.spawnGroups != null && wave.spawnGroups.Length > 0
                        ? wave.spawnGroups.SelectMany(g => g.entries).ToArray() : wave.entries;
                    Assert.That(entries, Is.Not.Empty);
                    Assert.That(entries.All(e => e.Prefab != null && e.count > 0), Is.True);
                    int count = entries.Sum(e => e.count);
                    if (i > 0)
                    {
                        Assert.That(count, Is.GreaterThan(previousCount), $"Wave {i + 1}");
                    }
                    int flyers = entries.Where(e => e.Prefab.GetComponent<FlyingNav>() != null).Sum(e => e.count);
                    if (i == 0) Assert.That(flyers, Is.Zero);
                    if (i == 1)
                    {
                        Assert.That(flyers, Is.GreaterThan(0), "Introduce flight in the second wave.");
                        Assert.That(flyers, Is.LessThanOrEqualTo(entries.Sum(e => e.count) * .25f),
                            "The first flying group should be at most a quarter of the wave.");
                    }
                    if (i == 3)
                        Assert.That(entries.Any(e => e.Prefab.GetComponent<MobHealth>().MaxHealth >= 300),
                            Is.True, "The finale should include the previously introduced armored threat.");
                    previousCount = count;
                }
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
