using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

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
            GameObject buttonObject = new GameObject("Ultimate button", typeof(RectTransform), typeof(Image), typeof(UltimateButton));
            created.Add(buttonObject);
            var button = buttonObject.GetComponent<UltimateButton>();
            var fill = buttonObject.GetComponent<Image>();
            Set(button, "ultimate", ultimate);
            Set(button, "fill", fill);
            ultimate.BeginCharge();
            ultimate.Release();
            yield return null;
            yield return null;

            Assert.That(ultimate.IsOnCooldown, Is.True);
            Assert.That(ultimate.CooldownRemaining, Is.InRange(7.8f, 8.1f));
            Assert.That(ultimate.CooldownProgress, Is.LessThan(0.1f),
                "A short charge must not jump to a full radial indicator after firing.");
            Assert.That(ultimate.CanReceiveInput, Is.False);
            Assert.That(fill.fillAmount, Is.EqualTo(ultimate.CooldownProgress).Within(0.02f));
            ultimate.BeginCharge();
            Assert.That(ultimate.IsCharging, Is.False);
            yield return new WaitForSeconds(3.05f);
            Assert.That(ultimate.CanReceiveInput, Is.False,
                "The additional delay must still block a new charge after the old minimum cooldown.");
            yield return new WaitForSeconds(5.1f);
            yield return null;
            Assert.That(ultimate.IsOnCooldown, Is.False);
            Assert.That(ultimate.CanReceiveInput, Is.True);
            Assert.That(fill.fillAmount, Is.EqualTo(0f).Within(0.01f));
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
            Assert.That(ultimate.CooldownProgress, Is.InRange(0.6f, 0.8f),
                "The radial progress must move during the additional fixed cooldown.");

            yield return new WaitForSeconds(2.05f);
            Assert.That(ultimate.CanReceiveInput, Is.False,
                "The five-second charge cooldown ends before the added five-second delay.");
            Assert.That(ultimate.CooldownProgress, Is.InRange(0.35f, 0.65f));

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
