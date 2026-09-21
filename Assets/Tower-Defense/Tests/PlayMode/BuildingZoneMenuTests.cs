using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TowerDefense.Tests
{
    public class BuildingZoneMenuTests
    {
        readonly List<GameObject> objects = new();
        TowerLoadout loadout;
        TowerOption option;
        PlayerWallet wallet;
        PlayerMovement movement;
        TowerSelectionModal selection;


        GameObject selectionPanel;

        TowerPlacementZone zone;
        static void Set(object target, string field, object value) =>
            target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
        GameObject New(string name, Transform parent = null)
        {
            var go = new GameObject(name);
            go.SetActive(false);
            if (parent != null) go.transform.SetParent(parent);
            objects.Add(go);
            return go;
        }
        TowerPlacementZone Zone(Vector3 position)
        {
            var go = New("Zone");
            go.transform.position = position;
            var result = go.AddComponent<TowerPlacementZone>();
            Set(result, "loadout", loadout);
            Set(result, "zoneRadius", 1f);
            Set(result, "enterDelay", .02f);
            var fill = New("Progress fill").AddComponent<UnityEngine.UI.Image>();
            Set(result, "placementCircleFill", fill);
            go.SetActive(true);
            return result;
        }
        [UnitySetUp]
        public IEnumerator Setup()
        {
            var player = New("Player");
            var body = player.AddComponent<Rigidbody>();
            body.useGravity = false;
            var collider = player.AddComponent<SphereCollider>();
            collider.radius = .2f;
            wallet = player.AddComponent<PlayerWallet>();
            movement = player.AddComponent<PlayerMovement>();
            player.SetActive(true);

            var prefab = New("Building template");
            prefab.transform.position = Vector3.one * 1000;
            prefab.SetActive(true);
            option = new TowerOption { prefab = prefab, displayName = "Test building", cost = 5 };
            loadout = ScriptableObject.CreateInstance<TowerLoadout>();
            loadout.options = new[] { option };

            var owner = New("Selection controller");
            selection = owner.AddComponent<TowerSelectionModal>();
            selectionPanel = New("Selection panel", owner.transform);
            var content = New("Content", selectionPanel.transform).AddComponent<RectTransform>();
            content.gameObject.SetActive(true);
            var template = New("Card");
            var button = template.AddComponent<UnityEngine.UI.Button>();
            var card = template.AddComponent<TowerCard>();
            Set(card, "button", button);
            Set(selection, "modalRoot", selectionPanel);
            Set(selection, "content", content);
            Set(selection, "cardPrefab", card);
            owner.SetActive(true);

            zone = Zone(Vector3.zero);
            Set(movement, "cam", zone.transform);
            Physics.SyncTransforms();
            yield return new WaitForSeconds(.08f);
            Assert.That(selection.IsOpen, Is.True);
            Assert.That(selection.Owner, Is.EqualTo(zone));
        }
        void Leave()
        {
            wallet.GetComponent<Rigidbody>().position = new Vector3(10, 0, 0);
            Physics.SyncTransforms();
        }

        [UnityTest]
        public IEnumerator FullProgressRemainsWhileModalIsOpenAndClearsOnExit()
        {
            var fill = (UnityEngine.UI.Image)typeof(TowerPlacementZone).GetField("placementCircleFill",
                BindingFlags.NonPublic | BindingFlags.Instance).GetValue(zone);
            Assert.That(fill.fillAmount, Is.EqualTo(1f));
            yield return new WaitForSeconds(.08f);
            Assert.That(selection.IsOpen, Is.True);
            Assert.That(fill.fillAmount, Is.EqualTo(1f));
            Leave();
            yield return null;
            yield return null;
            Assert.That(fill.fillAmount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator BuildingsWithoutUpgradesDoNotReopenTheirPointOrModal()
        {
            selectionPanel.GetComponentInChildren<UnityEngine.UI.Button>().onClick.Invoke();
            var slot = (BuildingSlot)typeof(TowerPlacementZone).GetField("slot",
                BindingFlags.NonPublic | BindingFlags.Instance).GetValue(zone);
            slot.BeginBuildPhase();
            yield return new WaitForSeconds(.08f);
            Assert.That(selection.IsOpen, Is.False);
            Assert.That(slot.CanInteract(true, 1), Is.False);
            foreach (var building in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
                if (building.name == "Building template(Clone)") objects.Add(building.gameObject);
        }

        [UnityTest]
        public IEnumerator TownHallUnlockRevealsThePointAndAllowsItsUpgradeModal()
        {
            selection.Close();
            var hall = New("Town hall");
            var hallUpgrade = hall.AddComponent<BaseUpgrade>();
            Set(hallUpgrade, "levels", new[] { new BaseUpgradeLevel { upgradeCost = 0, unlockedBuildUpgradeLevel = 1 } });
            hall.SetActive(true);
            var tower = option.prefab.AddComponent<TowerUpgrade>();
            Set(tower, "levels", new[] { new TowerUpgradeLevel { upgradeCost = 0 } });
            var slot = new BuildingSlot(loadout, BuildSlotType.Defense, 0);
            Set(zone, "slot", slot);
            var visual = New("Point visual", zone.transform);
            visual.SetActive(true);
            Set(zone, "visualRoot", visual);
            Assert.That(slot.TryBuild(option, wallet, true, 1, Vector3.right * 20, Quaternion.identity, out var built), Is.True);
            objects.Add(built);
            slot.BeginBuildPhase();
            zone.SendMessage("SyncWithPhase");
            yield return new WaitForSeconds(.08f);
            Assert.That(visual.activeSelf, Is.False);
            Assert.That(selection.IsOpen, Is.False);
            Assert.That(slot.CanInteract(true, 1), Is.False);
            Assert.That(hallUpgrade.HasNextLevel, Is.True, "Town Hall itself remains upgradable.");

            Assert.That(hallUpgrade.TryUpgrade(), Is.True);
            yield return new WaitForSeconds(.08f);
            Assert.That(visual.activeSelf, Is.True);
            Assert.That(selection.IsOpen, Is.True);
            Assert.That(selection.Owner, Is.SameAs(zone));
            selectionPanel.GetComponentInChildren<UnityEngine.UI.Button>().onClick.Invoke();
            slot.BeginBuildPhase();
            zone.SendMessage("SyncWithPhase");
            yield return null;
            Assert.That(visual.activeSelf, Is.False, "Maximum-level buildings have no upgrade point.");
            Assert.That(selection.IsOpen, Is.False);
        }

        [UnityTest]
        public IEnumerator UpgradeUsesSharedPanelAndCannotRunTwice()
        {
            int purchases = 0;
            selection.OpenUpgrade(option, 1, 2, true, () => { purchases++; return true; }, zone);
            var button = selectionPanel.GetComponentInChildren<UnityEngine.UI.Button>();
            Assert.That(selection.IsOpen, Is.True);
            Assert.That(button.interactable, Is.True);
            button.onClick.Invoke();
            button.onClick.Invoke();
            Assert.That(purchases, Is.EqualTo(1));
            Assert.That(selection.IsOpen, Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator LockedMaxedAndUnaffordableCardsCannotPurchase()
        {
            int purchases = 0;
            foreach (var state in new[] { 0, 1, 2 })
            {
                int? cost = state == 0 ? (int?)null : state == 1 ? 2 : wallet.Coins + 1;
                selection.OpenUpgrade(option, 1, cost, state != 1,
                    () => { purchases++; return true; }, zone);
                var button = selectionPanel.GetComponentInChildren<UnityEngine.UI.Button>();
                Assert.That(button.interactable, Is.False);
                button.onClick.Invoke();
                Assert.That(selection.IsOpen, Is.True);
                Assert.That(purchases, Is.Zero);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayerCanWalkAwayAndReenterWithoutACloseButton()
        {
            var start = wallet.transform.position;
            Set(movement, "moveInput", Vector2.right);
            for (int i = 0; i < 35; i++) yield return new WaitForFixedUpdate();
            Assert.That(Vector3.Distance(start, wallet.transform.position), Is.GreaterThan(1.2f));
            Assert.That(selection.IsOpen, Is.False);
            Assert.That(selection.Owner, Is.Null);
            Set(movement, "moveInput", Vector2.zero);
            wallet.GetComponent<Rigidbody>().position = Vector3.zero;
            wallet.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
            Physics.SyncTransforms();
            yield return new WaitForSeconds(.08f);
            Assert.That(selection.IsOpen, Is.True);
            Assert.That(selection.Owner, Is.EqualTo(zone));
        }

        [UnityTest]
        public IEnumerator ClickAfterLeavingCannotBuildOrSpendBeforeNextZoneUpdate()
        {
            var button = selectionPanel.GetComponentInChildren<UnityEngine.UI.Button>();
            int coins = wallet.Coins;
            Leave();
            button.onClick.Invoke();
            Assert.That(zone.IsBuilt, Is.False);
            Assert.That(wallet.Coins, Is.EqualTo(coins));
            Assert.That(selection.IsOpen, Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AnotherZoneCannotStealOrCloseTheOwnersMenu()
        {
            var other = Zone(new Vector3(.2f, 0, 0));
            yield return new WaitForSeconds(.08f);
            Assert.That(selection.Owner, Is.EqualTo(zone));
            other.enabled = false;
            Assert.That(selection.IsOpen, Is.True);
            Assert.That(selection.Owner, Is.EqualTo(zone));
            zone.enabled = false;
            Assert.That(selection.IsOpen, Is.False);
        }

        [UnityTest]
        public IEnumerator BuildInsideZoneSpendsOnceAndClosesMenu()
        {
            int coins = wallet.Coins;
            var button = selectionPanel.GetComponentInChildren<UnityEngine.UI.Button>();
            button.onClick.Invoke();
            var building = Object.FindFirstObjectByType<BuildingSpawnAnimation>();
            if (building != null) objects.Add(building.gameObject);
            Assert.That(zone.IsBuilt, Is.True);
            Assert.That(zone.UsedThisPhase, Is.True);
            Assert.That(wallet.Coins, Is.EqualTo(coins - option.cost));
            Assert.That(selection.IsOpen, Is.False);
            button.onClick.Invoke();
            Assert.That(wallet.Coins, Is.EqualTo(coins - option.cost));
            yield return null;
        }

        [UnityTest]
        public IEnumerator UpgradeClosesOnExitAndStaleClickCannotSpend()
        {
            zone.enabled = false;
            var template = New("Town hall template");
            var baseUpgrade = template.AddComponent<BaseUpgrade>();
            Set(baseUpgrade, "levels", new[] { new BaseUpgradeLevel { upgradeCost = 7, hpMul = 1.2f } });
            var baseOption = new TowerOption { prefab = template, slotType = BuildSlotType.TownHall, cost = 5 };
            loadout.options = new[] { baseOption };
            Set(zone, "allowedType", BuildSlotType.TownHall);
            var slot = new BuildingSlot(loadout, BuildSlotType.TownHall, 0);
            Set(zone, "slot", slot);
            Assert.That(slot.TryBuild(baseOption, wallet, true, 0, Vector3.one * 20, Quaternion.identity, out var built), Is.True);
            objects.Add(built);
            built.SetActive(true);
            slot.BeginBuildPhase();
            zone.enabled = true;
            yield return new WaitForSeconds(.08f);
            Assert.That(selection.IsOpen, Is.True);
            Assert.That(selection.Owner, Is.EqualTo(zone));
            int coins = wallet.Coins;
            Leave();
            selectionPanel.GetComponentInChildren<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.That(wallet.Coins, Is.EqualTo(coins));
            Assert.That(slot.BuiltBase.CurrentLevel, Is.Zero);
            Assert.That(selection.IsOpen, Is.False);
            wallet.GetComponent<Rigidbody>().position = Vector3.zero;
            Physics.SyncTransforms();
            yield return new WaitForSeconds(.08f);
            Assert.That(selection.IsOpen, Is.True);
            Leave();
            yield return null;
            yield return null;
            Assert.That(selection.IsOpen, Is.False);
            Assert.That(selection.Owner, Is.Null);
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            for (int i = objects.Count - 1; i >= 0; i--)
                if (objects[i] != null) Object.Destroy(objects[i]);
            objects.Clear();
            Object.Destroy(loadout);
            Time.timeScale = 1;
            yield return null;
        }
    }
}

