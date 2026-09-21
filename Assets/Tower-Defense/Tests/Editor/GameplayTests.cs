using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace TowerDefense.Tests
{
    public class GameplayTests
    {
        private readonly List<UnityEngine.Object> created = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = created.Count - 1; i >= 0; i--)
                if (created[i] != null) UnityEngine.Object.DestroyImmediate(created[i]);
            created.Clear();
        }

        [Test]
        public void UpgradeAndBuildingRegressionSweep()
        {
            Assert.That(ArchitectureRefactorChecks.Run(), Does.StartWith("PASS:"));
        }

        [TestCase(-1)]
        [TestCase(int.MinValue)]
        public void NegativePaymentsAndRewardsCannotChangeBalance(int amount)
        {
            PlayerWallet wallet = Wallet();
            int balance = wallet.Coins;
            Assert.Throws<ArgumentOutOfRangeException>(() => wallet.TrySpend(amount));
            Assert.Throws<ArgumentOutOfRangeException>(() => wallet.AddCoins(amount));
            Assert.That(wallet.Coins, Is.EqualTo(balance));
        }

        [Test]
        public void UnaffordablePaymentLeavesBalanceAndEventsUntouched()
        {
            PlayerWallet wallet = Wallet();
            int notifications = 0;
            wallet.onCoinsChanged.AddListener(_ => notifications++);
            Assert.That(wallet.TrySpend(wallet.Coins + 1), Is.False);
            Assert.That(wallet.Coins, Is.EqualTo(100));
            Assert.That(notifications, Is.Zero);
        }

        [Test]
        public void ExactBalanceCanBeSpentOnce()
        {
            PlayerWallet wallet = Wallet();
            Assert.That(wallet.TrySpend(100), Is.True);
            Assert.That(wallet.Coins, Is.Zero);
            Assert.That(wallet.TrySpend(1), Is.False);
        }

        [TestCase(false, 2, false)]
        [TestCase(true, 1, false)]
        [TestCase(true, 2, true)]
        [TestCase(true, 3, true)]
        public void BuildingChecksPhaseAndTownHallLevel(bool buildPhase, int townHallLevel, bool succeeds)
        {
            var loadout = ScriptableObject.CreateInstance<TowerLoadout>();
            created.Add(loadout);
            var option = new TowerOption { prefab = NewObject("Building template"), cost = 10, slotType = BuildSlotType.Defense };
            loadout.options = new[] { option };
            var slot = new BuildingSlot(loadout, BuildSlotType.Defense, 2);
            PlayerWallet wallet = Wallet();
            bool result = slot.TryBuild(option, wallet, buildPhase, townHallLevel, Vector3.zero, Quaternion.identity, out var building);
            if (building != null) created.Add(building);
            Assert.That(result, Is.EqualTo(succeeds));
            Assert.That(wallet.Coins, Is.EqualTo(succeeds ? 90 : 100));
            Assert.That(slot.UsedThisPhase, Is.EqualTo(succeeds));
        }

        [Test]
        public void MissingShooterSetupIsReportedInsteadOfUsingTransformOrInstantDamage()
        {
            Shooter shooter = NewObject("Unconfigured shooter").AddComponent<Shooter>();
            Assert.That(shooter.ValidateConfiguration(out string error), Is.False);
            Assert.That(error, Does.Contain("fire point"));
            Set(shooter, "firePoints", new[] { shooter.transform });
            Assert.That(shooter.ValidateConfiguration(out error), Is.False);
            Assert.That(error, Does.Contain("projectile prefab"));
        }

        [Test]
        public void MissingEnemyReferencesAreReportedEvenWhenMatchingComponentsExist()
        {
            GameObject enemy = NewObject("Unconfigured enemy");
            MobCore mob = enemy.AddComponent<MobCore>();
            enemy.AddComponent<NearestThreatSelector>();
            enemy.AddComponent<StandardAttack>();
            enemy.AddComponent<DefaultNav>();
            Assert.That(mob.ValidateConfiguration(out string error), Is.False);
            Assert.That(error, Does.Contain("Assign an ITargetSelector"));
        }

        [TestCase("TowerSelectionModal")]
        [TestCase("BaseUpgradeModal")]
        public void BuildMenusRequireAWallet(string modalType)
        {
            Assert.That(PlayerWallet.Instance == null, Is.True);
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("\\[" + modalType + "\\] A PlayerWallet is required"));
            GameObject root = NewObject(modalType);
            switch (modalType)
            {
                case "TowerSelectionModal": root.AddComponent<TowerSelectionModal>().Open(Array.Empty<TowerOption>(), null); break;
                case "BaseUpgradeModal": root.AddComponent<BaseUpgradeModal>().Open(); break;
            }
        }

        [TestCase("Assets/Tower-Defense/Prefabs/Player.prefab")]
        [TestCase("Assets/Tower-Defense/Prefabs/Buildings/CannonTower.prefab")]
        public void ShooterPrefabsHaveExplicitFirePointsAndProjectiles(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab, Is.Not.Null, path);
            Shooter[] shooters = prefab.GetComponentsInChildren<Shooter>(true);
            Assert.That(shooters, Is.Not.Empty, path);
            foreach (Shooter shooter in shooters)
                Assert.That(shooter.ValidateConfiguration(out string error), Is.True, path + ": " + error);
        }

        [TestCase("Bat")]
        [TestCase("EnemyDestoyer")]
        [TestCase("EnemyShooter")]
        [TestCase("EnemySpider")]
        public void EnemyPrefabsHaveExplicitBehaviorModules(string name)
        {
            string path = "Assets/Tower-Defense/Prefabs/Mobs/" + name + ".prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab, Is.Not.Null, path);
            var mob = prefab.GetComponent<MobCore>();
            Assert.That(mob, Is.Not.Null);
            Assert.That(mob.ValidateConfiguration(out string error), Is.True, error);
        }

        private GameObject NewObject(string name)
        {
            var go = new GameObject(name);
            created.Add(go);
            return go;
        }

        private PlayerWallet Wallet()
        {
            var wallet = NewObject("Wallet").AddComponent<PlayerWallet>();
            typeof(PlayerWallet).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(wallet, null);
            return wallet;
        }

        private static void Set(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }
    }
}
