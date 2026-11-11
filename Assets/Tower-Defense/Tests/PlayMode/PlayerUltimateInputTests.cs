using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace TowerDefense.Tests
{
    public sealed class PlayerUltimateInputTests : InputTestFixture
    {
        private GameObject player;
        private GameObject orb;

        [UnityTest]
        public IEnumerator SpaceStartsAutomaticUltimate()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            orb = new GameObject("Ultimate test orb");
            orb.SetActive(false);
            player = new GameObject("Ultimate test player");
            player.SetActive(false);
            Rigidbody body = player.AddComponent<Rigidbody>();
            body.useGravity = false;
            player.AddComponent<PlayerMovement>();
            PlayerUltimate ultimate = player.AddComponent<PlayerUltimate>();
            Set(ultimate, "chargeOrbPrefab", orb);
            Set(ultimate, "maxChargeTime", .2f);
            Set(ultimate, "manualCharge", false);
            player.SetActive(true);

            Press(keyboard.spaceKey);
            yield return null;
            Assert.That(ultimate.IsCharging || ultimate.IsOnCooldown, Is.True,
                "Space must start the ultimate through PlayerMovement.");
            Release(keyboard.spaceKey);
            yield return new WaitForSeconds(.3f);
            Assert.That(ultimate.IsOnCooldown, Is.True,
                "Automatic charge must fire without holding Space.");
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            foreach (ChargeOrbProjectile projectile in Object.FindObjectsByType<ChargeOrbProjectile>(
                         FindObjectsSortMode.None))
                Object.Destroy(projectile.gameObject);
            if (player != null) Object.Destroy(player);
            if (orb != null) Object.Destroy(orb);
            yield return null;
        }

        private static void Set(object owner, string field, object value) => owner.GetType()
            .GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(owner, value);
    }
}
