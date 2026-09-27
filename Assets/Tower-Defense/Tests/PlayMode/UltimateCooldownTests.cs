using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace TowerDefense.Tests
{
    public sealed class UltimateCooldownTests
    {
        private readonly List<GameObject> created = new();

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            foreach (var orb in Object.FindObjectsByType<ChargeOrbProjectile>(FindObjectsSortMode.None))
                Object.Destroy(orb.gameObject);
            foreach (GameObject go in created)
                if (go != null) Object.Destroy(go);
            created.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator UltimateAddsFixedDelayAfterShortCharge()
        {
            PlayerUltimate ultimate = CreatePlayer();
            GameObject buttonObject = new GameObject("Ultimate button", typeof(RectTransform), typeof(UltimateButton));
            created.Add(buttonObject);
            var button = buttonObject.GetComponent<UltimateButton>();
            GameObject badge = new GameObject("Cooldown badge", typeof(RectTransform));
            badge.SetActive(false);
            created.Add(badge);
            badge.transform.SetParent(buttonObject.transform);
            GameObject labelObject = new GameObject("Cooldown seconds", typeof(RectTransform), typeof(TextMeshProUGUI));
            created.Add(labelObject);
            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.transform.SetParent(badge.transform);
            Set(button, "ultimate", ultimate);
            Set(button, "cooldownLabel", label);
            ultimate.BeginCharge();
            ultimate.Release();
            yield return null;
            yield return null;

            Assert.That(ultimate.IsOnCooldown, Is.True);
            Assert.That(ultimate.CooldownRemaining, Is.InRange(7.8f, 8.1f));
            Assert.That(ultimate.CooldownProgress, Is.LessThan(0.1f),
                "A short charge must not jump to a full radial indicator after firing.");
            Assert.That(ultimate.CanReceiveInput, Is.False);
            Assert.That(badge.activeSelf, Is.True, "The countdown panel must become visible.");
            Assert.That(label.text, Is.Not.Empty);
            ultimate.BeginCharge();
            Assert.That(ultimate.IsCharging, Is.False);
            yield return new WaitForSeconds(3.05f);
            Assert.That(ultimate.CanReceiveInput, Is.False,
                "The additional delay must still block a new charge after the old minimum cooldown.");
            yield return new WaitForSeconds(5.1f);
            yield return null;
            Assert.That(ultimate.IsOnCooldown, Is.False);
            Assert.That(ultimate.CanReceiveInput, Is.True);
            Assert.That(badge.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator FullyChargedUltimateAddsFixedDelayAfterChargeCooldown()
        {
            PlayerUltimate ultimate = CreatePlayer();
            Set(ultimate, "maxChargeTime", 0.05f);

            ultimate.BeginCharge();
            yield return new WaitForSeconds(0.15f);

            Assert.That(ultimate.IsCharging, Is.False);
            Assert.That(ultimate.CooldownRemaining, Is.InRange(9.7f, 10f));
            Assert.That(ultimate.CooldownProgress, Is.GreaterThan(0.9f));

            yield return new WaitForSeconds(3.05f);
            Assert.That(ultimate.CanReceiveInput, Is.False,
                "A full charge must still be locked after the three-second minimum.");

            yield return new WaitForSeconds(2.05f);
            Assert.That(ultimate.CanReceiveInput, Is.False,
                "The five-second charge cooldown ends before the added five-second delay.");

            yield return new WaitForSeconds(5.1f);
            Assert.That(ultimate.CanReceiveInput, Is.True);
        }

        private PlayerUltimate CreatePlayer()
        {
            GameObject bullet = NewObject("Bullet template", false);
            bullet.AddComponent<Projectile>();
            GameObject orb = NewObject("Orb template", false);

            GameObject player = NewObject("Player", false);
            Shooter shooter = player.AddComponent<Shooter>();
            Set(shooter, "firePoints", new[] { player.transform });
            Set(shooter, "projectilePrefab", bullet);
            PlayerUltimate ultimate = player.AddComponent<PlayerUltimate>();
            Set(ultimate, "logInput", false);
            Set(ultimate, "chargeOrbPrefab", orb);
            player.SetActive(true);
            return ultimate;
        }

        private GameObject NewObject(string name, bool active = true)
        {
            var go = new GameObject(name);
            go.SetActive(active);
            created.Add(go);
            return go;
        }

        private static void Set(object owner, string field, object value) => owner.GetType()
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(owner, value);
    }
}
