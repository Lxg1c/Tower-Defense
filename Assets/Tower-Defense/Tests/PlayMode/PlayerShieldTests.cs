using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TowerDefense.Tests
{
    public sealed class PlayerShieldTests
    {
        private GameObject player;

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (player != null) Object.Destroy(player);
            yield return null;
        }

        [Test]
        public void ShieldAbsorbsDamageBeforeHealthAndReportsChanges()
        {
            player = new GameObject("Player");
            var health = player.AddComponent<PlayerHealth>();
            float reported = -1f;
            health.onShieldChanged.AddListener(value => reported = value);

            Assert.That(health.CurrentShield, Is.Zero, "Shield should come from a pickup.");
            Assert.That(health.GrantShield(12f), Is.True);
            Assert.That(health.RemainingShieldSeconds, Is.EqualTo(12f).Within(0.1f));

            health.TakeDamage(20f);
            Assert.That(health.CurrentShield, Is.EqualTo(30f));
            Assert.That(health.CurrentHealth, Is.EqualTo(health.MaxHealth));
            Assert.That(reported, Is.EqualTo(30f));

            health.TakeDamage(40f);
            Assert.That(health.CurrentShield, Is.Zero);
            Assert.That(health.CurrentHealth, Is.EqualTo(health.MaxHealth - 10f));
            Assert.That(reported, Is.Zero);

            Assert.That(health.RemainingShieldSeconds, Is.Zero);
            Assert.That(health.GrantShield(12f), Is.True);
            Assert.That(health.CurrentShield, Is.EqualTo(health.MaxShield));
        }

        [UnityTest]
        public IEnumerator ShieldExpiresWithoutTakingDamage()
        {
            player = new GameObject("Player");
            var health = player.AddComponent<PlayerHealth>();
            health.GrantShield(0.05f);
            yield return new WaitForSeconds(0.1f);
            Assert.That(health.CurrentShield, Is.Zero);
            Assert.That(health.RemainingShieldSeconds, Is.Zero);
            Assert.That(health.ShieldProgress, Is.Zero);
        }

        [Test]
        public void AnotherPickupRefillsShieldAndExtendsTimeOnlyToCapacity()
        {
            player = new GameObject("Player");
            var health = player.AddComponent<PlayerHealth>();
            health.GrantShield(12f);
            health.TakeDamage(20f);
            Assert.That(health.GrantShield(20f), Is.True);
            Assert.That(health.CurrentShield, Is.EqualTo(health.MaxShield));
            Assert.That(health.RemainingShieldSeconds, Is.EqualTo(24f).Within(0.1f));
        }
    }
}
