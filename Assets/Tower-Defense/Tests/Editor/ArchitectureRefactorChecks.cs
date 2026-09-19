using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Edit-mode regression checks for upgrade values, presentation and building transactions.
/// Uses a temporary preview scene, never saves scenes or edits prefab assets.
/// Run from Tools/Tower Defense/Run Architecture Checks, or through Unity CLI eval.
/// </summary>
public static class ArchitectureRefactorChecks
{
    private static int assertions;
    private static Scene fixture;

    [MenuItem("Tools/Tower Defense/Run Architecture Checks")]
    private static void RunFromMenu() => Debug.Log(Run());

    public static string Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Run architecture checks outside Play mode.");
        if (PlayerWallet.Instance != null || Base.Instance != null || BaseUpgrade.Instance != null)
            throw new InvalidOperationException("Gameplay singletons are active. Stop the game before running checks.");

        fixture = EditorSceneManager.NewPreviewScene();
        var originalCulture = System.Globalization.CultureInfo.CurrentCulture;
        System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
        TowerLoadout loadout = null;
        assertions = 0;
        try
        {
            var wallet = CreateObject("Test wallet").AddComponent<PlayerWallet>();
            Set(wallet, "startingCoins", 1000);
            Awake(wallet);

            var townHallObject = CreateObject("Test town hall");
            var townHallHealth = townHallObject.AddComponent<Base>();
            Awake(townHallHealth);
            townHallHealth.SetMaxHealth(200);
            var townHall = townHallObject.AddComponent<BaseUpgrade>();
            Set(townHall, "baseUnlockedBuildUpgradeLevel", 0);
            Set(townHall, "levels", new[]
            {
                new BaseUpgradeLevel { upgradeCost = 20, hpMul = 1.5f, unlockedBuildUpgradeLevel = 2 }
            });
            Awake(townHall);

            var cannon = CreateCannon("Test cannon");
            Check(!cannon.CanUpgrade, "Tower upgrades must respect town hall unlocks.");
            Check(UpgradeStatsFormatter.Locked(cannon).Contains("Current limit: 0"), "Locked upgrade text.");
            Check(UpgradeStatsFormatter.Current(townHall) == Lines("Base Level: 0", "HP: 200"), "Base current text.");
            Check(UpgradeStatsFormatter.Next(townHall) == Lines("Base Level: 1", "HP: 300"), "Base preview text.");
            Check(townHall.TryUpgrade(), "Town hall upgrade succeeds.");
            Check(wallet.Coins == 980 && townHallHealth.MaxHealth == 300, "Base upgrade cost and HP.");
            Check(townHall.TownHallLevel == 2 && cannon.CanUpgrade, "Base upgrade unlocks towers.");
            Check(!townHall.GetNextStats().HasValue && UpgradeStatsFormatter.Next(townHall) == "", "Maxed base has no preview.");

            Check(UpgradeStatsFormatter.Current(cannon) == Lines("Damage: 12.5", "Fire Rate: 2/s", "Range: 8", "HP: 100"), "Cannon current text.");
            Check(UpgradeStatsFormatter.Next(cannon) == Lines("Damage: 25", "Fire Rate: 4/s", "Range: 12", "HP: 150"), "Cannon preview text.");
            cannon.GetComponent<TowerHealth>().TakeDamage(40);
            BuildingStats predicted = cannon.GetNextStats().Value;
            Check(cannon.TryUpgrade(), "Cannon upgrade succeeds.");
            Compare(predicted, cannon.GetCurrentStats(), "Cannon preview equals applied upgrade");
            Check(wallet.Coins == 950 && cannon.GetComponent<TowerHealth>().CurrentHealth == 150, "Tower purchase charges once and tops up HP.");
            Check(cannon.TryUpgrade() && cannon.GetCurrentStats().ShooterDamage == 37.5f, "Second upgrade uses original base damage, not compounded damage.");
            Check(!cannon.GetNextStats().HasValue && UpgradeStatsFormatter.Next(cannon) == "", "Maxed cannon has no preview.");

            var pulseObject = CreateObject("Test pulse");
            var pulseHealth = pulseObject.AddComponent<TowerHealth>();
            Awake(pulseHealth);
            var pulse = pulseObject.AddComponent<PulseTower>();
            pulse.Damage = 10;
            pulse.Range = 4;
            pulse.AttackInterval = 2;
            var pulseUpgrade = pulseObject.AddComponent<TowerUpgrade>();
            Set(pulseUpgrade, "levels", new[] { Level() });
            Awake(pulseUpgrade);
            Check(UpgradeStatsFormatter.Next(pulseUpgrade) == Lines("Damage: 20", "Range: 6", "Interval: 1s", "HP: 150"), "Pulse preview preserves interval and row order.");
            predicted = pulseUpgrade.GetNextStats().Value;
            Check(pulseUpgrade.TryUpgrade(), "Pulse upgrade succeeds.");
            Compare(predicted, pulseUpgrade.GetCurrentStats(), "Pulse preview equals applied upgrade");

            var mineObject = CreateObject("Test mine");
            var mineHealth = mineObject.AddComponent<TowerHealth>();
            Awake(mineHealth);
            var mine = mineObject.AddComponent<MineIncome>();
            Awake(mine);
            mine.CoinsPerWave = 25;
            var mineUpgrade = mineObject.AddComponent<TowerUpgrade>();
            Set(mineUpgrade, "levels", new[] { Level() });
            Awake(mineUpgrade);
            Check(UpgradeStatsFormatter.Next(mineUpgrade) == Lines("HP: 150", "Income: 38"), "Mine preview preserves rounding and omits attack stats.");
            predicted = mineUpgrade.GetNextStats().Value;
            Check(mineUpgrade.TryUpgrade(), "Mine upgrade succeeds.");
            Compare(predicted, mineUpgrade.GetCurrentStats(), "Mine preview equals applied upgrade");

            var template = CreateCannon("Test build template");
            var option = new TowerOption { slotType = BuildSlotType.Defense, prefab = template.gameObject, cost = 10 };
            var wrongType = new TowerOption { slotType = BuildSlotType.Mine, prefab = template.gameObject, cost = 10 };
            var outsider = new TowerOption { slotType = BuildSlotType.Defense, prefab = template.gameObject, cost = 10 };
            loadout = ScriptableObject.CreateInstance<TowerLoadout>();
            loadout.options = new[] { option, wrongType, null };
            var slot = new BuildingSlot(loadout, BuildSlotType.Defense, 2);
            Check(slot.GetOptions().Count == 1, "Only valid options matching the slot are shown.");
            int coinsBefore = wallet.Coins;
            GameObject built;
            Check(!slot.TryBuild(option, wallet, false, 2, Vector3.zero, Quaternion.identity, out built), "Combat blocks building.");
            Check(!slot.TryBuild(option, wallet, true, 1, Vector3.zero, Quaternion.identity, out built), "Locked zone blocks building.");
            Check(!slot.TryBuild(wrongType, wallet, true, 2, Vector3.zero, Quaternion.identity, out built), "Wrong slot type rejected.");
            Check(!slot.TryBuild(outsider, wallet, true, 2, Vector3.zero, Quaternion.identity, out built), "Options outside the loadout rejected.");
            Check(!slot.TryBuild(null, wallet, true, 2, Vector3.zero, Quaternion.identity, out built), "Missing option rejected.");
            Check(!slot.TryBuild(option, null, true, 2, Vector3.zero, Quaternion.identity, out built), "Missing wallet rejected.");
            option.cost = -1;
            Check(!slot.TryBuild(option, wallet, true, 2, Vector3.zero, Quaternion.identity, out built), "Negative cost rejected.");
            option.cost = coinsBefore + 1;
            Check(!slot.TryBuild(option, wallet, true, 2, Vector3.zero, Quaternion.identity, out built), "Unaffordable building rejected.");
            Check(wallet.Coins == coinsBefore && !slot.IsBuilt && !slot.UsedThisPhase && built == null, "Rejected purchases leave coins and slot state untouched.");
            option.cost = 10;
            Vector3 position = new Vector3(3, 0, 7);
            Check(slot.TryBuild(option, wallet, true, 2, position, Quaternion.identity, out built), "Valid building succeeds.");
            SceneManager.MoveGameObjectToScene(built, fixture);
            Check(built.transform.position == position && wallet.Coins == coinsBefore - 10, "Building placed and charged correctly.");
            Check(slot.IsBuilt && slot.UsedThisPhase && slot.BuiltTower != null, "Slot records its building and consumed action.");
            Check(!slot.TryBuild(option, wallet, true, 2, position, Quaternion.identity, out _), "Duplicate building rejected.");
            Check(!slot.TryUpgrade(true, 2), "Cannot upgrade in the same phase as building.");
            Awake(built.GetComponent<TowerHealth>());
            Awake(built.GetComponent<Shooter>());
            Awake(slot.BuiltTower);
            slot.BeginBuildPhase();
            Check(slot.CanInteract(true, 2), "Next build phase reopens the slot.");
            Check(!slot.TryUpgrade(false, 2) && !slot.TryUpgrade(true, 1), "Upgrade rechecks phase and zone unlock.");
            coinsBefore = wallet.Coins;
            Check(slot.TryUpgrade(true, 2), "Slot upgrade succeeds.");
            Check(slot.UsedThisPhase && wallet.Coins == coinsBefore - 30, "Upgrade consumes exactly one action and one payment.");
            Check(!slot.TryUpgrade(true, 2) && wallet.Coins == coinsBefore - 30, "Repeated upgrade request cannot spend again.");

            return $"PASS: {assertions} architecture regression assertions.";
        }
        finally
        {
            if (loadout != null) UnityEngine.Object.DestroyImmediate(loadout);
            EditorSceneManager.ClosePreviewScene(fixture);
            // Runtime callbacks aren't guaranteed when edit-mode fixtures are destroyed.
            typeof(PlayerWallet).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { null });
            typeof(Base).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { null });
            typeof(BaseUpgrade).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { null });
            System.Globalization.CultureInfo.CurrentCulture = originalCulture;
        }
    }

    private static TowerUpgrade CreateCannon(string name)
    {
        var go = CreateObject(name);
        Awake(go.AddComponent<TowerHealth>());
        var shooter = go.AddComponent<Shooter>();
        var projectile = CreateObject("Test projectile template").AddComponent<Projectile>();
        Set(shooter, "firePoints", new[] { go.transform });
        Set(shooter, "projectilePrefab", projectile.gameObject);
        Awake(shooter);
        shooter.Damage = 12.5f;
        shooter.FireRate = 2;
        go.GetComponent<DetectionZone>().Radius = 8;
        var upgrade = go.AddComponent<TowerUpgrade>();
        Set(upgrade, "levels", new[] { Level(), new TowerUpgradeLevel { upgradeCost = 40, damageMul = 3 } });
        Awake(upgrade);
        return upgrade;
    }

    private static TowerUpgradeLevel Level() => new TowerUpgradeLevel
    {
        upgradeCost = 30, damageMul = 2, rangeMul = 1.5f, fireRateMul = 2, hpMul = 1.5f, incomeMul = 1.5f
    };

    private static GameObject CreateObject(string name)
    {
        var go = new GameObject(name);
        SceneManager.MoveGameObjectToScene(go, fixture);
        return go;
    }

    private static void Set(object target, string name, object value)
    {
        target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }

    private static void Awake(MonoBehaviour component)
    {
        // Edit-mode fixture components don't receive runtime Awake automatically.
        component.GetType().GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(component, null);
    }

    private static string Lines(params string[] lines) => string.Join(Environment.NewLine, lines);

    private static void Compare(BuildingStats expected, BuildingStats actual, string message)
    {
        Check(expected.ShooterDamage == actual.ShooterDamage && expected.ShooterFireRate == actual.ShooterFireRate
            && expected.ShooterRange == actual.ShooterRange && expected.PulseDamage == actual.PulseDamage
            && expected.PulseRange == actual.PulseRange && expected.PulseInterval == actual.PulseInterval
            && expected.MaxHealth == actual.MaxHealth && expected.Income == actual.Income, message);
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        assertions++;
    }
}
