using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TowerDefense.Tests
{
    public class PlayerGunAimTests
    {
        private GameObject player, targetObject, projectile;
        private PlayerGunAim aim;
        private Shooter shooter;
        private Transform first, second, muzzle, head;
        private Quaternion firstRest, secondRest;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            targetObject = new GameObject("Elevated target");
            targetObject.transform.position = new Vector3(0, 6, 5);
            targetObject.AddComponent<SphereCollider>();
            targetObject.AddComponent<CombatTestTarget>();
            projectile = new GameObject("Unused projectile template");
            projectile.SetActive(false);
            projectile.AddComponent<Projectile>();
            player = new GameObject("Player aim fixture");
            player.SetActive(false);
            first = new GameObject("First gun").transform;
            second = new GameObject("Second gun").transform;
            first.SetParent(player.transform);
            second.SetParent(player.transform);
            head = new GameObject("Head").transform;
            head.SetParent(player.transform);
            // Reproduce the imported robot's opposing bone axes.
            first.localRotation = firstRest = Quaternion.Euler(0, 0, -90);
            second.localRotation = secondRest = Quaternion.Euler(0, 0, 90);
            muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(first);
            muzzle.position = Vector3.forward;
            shooter = player.AddComponent<Shooter>();
            Set(shooter, "firePoints", new[] { muzzle });
            Set(shooter, "projectilePrefab", projectile);
            shooter.SuppressFire = true;
            aim = player.AddComponent<PlayerGunAim>();
            Set(aim, "shooter", shooter);
            Set(aim, "firstGun", first);
            Set(aim, "secondGun", second);
            Set(aim, "head", head);
            player.SetActive(true);
            Physics.SyncTransforms();
            yield return new WaitForSeconds(0.7f);
            yield return new WaitForEndOfFrame();
        }

        [UnityTest]
        public IEnumerator BothGunsPitchUpAndMuzzleFollowsWithoutTiltingPlayer()
        {
            yield return new WaitForEndOfFrame();
            Assert.That(shooter.HasTarget, Is.True);
            Assert.That(first.forward.y, Is.GreaterThan(0.6f));
            Assert.That(second.forward.y, Is.GreaterThan(0.6f));
            Assert.That(head.forward.y, Is.GreaterThan(0.6f));
            Assert.That(muzzle.position.y, Is.GreaterThan(0.6f));
            Assert.That(Vector3.Angle(player.transform.up, Vector3.up), Is.LessThan(0.01f));
            var settled = first.rotation;
            yield return new WaitForSeconds(0.5f);
            yield return new WaitForEndOfFrame();
            Assert.That(Quaternion.Angle(settled, first.rotation), Is.LessThan(0.1f),
                "Pitch must not accumulate on bones without animation curves.");
            Assert.That(Quaternion.Angle(head.rotation, Quaternion.LookRotation(first.forward)), Is.LessThan(0.1f));
        }

        [UnityTest]
        public IEnumerator LowerTargetRespectsDepressionLimitAndLostTargetReturnsToRest()
        {
            targetObject.transform.position = new Vector3(0, -9, 1);
            Physics.SyncTransforms();
            yield return new WaitForSeconds(0.8f);
            yield return new WaitForEndOfFrame();
            Assert.That(first.forward.y, Is.EqualTo(-Mathf.Sin(45 * Mathf.Deg2Rad)).Within(0.01f));
            Assert.That(second.forward.y, Is.EqualTo(first.forward.y).Within(0.01f));
            Assert.That(head.forward.y, Is.EqualTo(first.forward.y).Within(0.01f));
            targetObject.SetActive(false);
            yield return new WaitForSeconds(0.7f);
            yield return new WaitForEndOfFrame();
            Assert.That(Quaternion.Angle(first.localRotation, firstRest), Is.LessThan(0.1f));
            Assert.That(Quaternion.Angle(second.localRotation, secondRest), Is.LessThan(0.1f));
            Assert.That(Quaternion.Angle(head.localRotation, Quaternion.identity), Is.LessThan(0.1f));
        }

        [UnityTest]
        public IEnumerator DelayedVolleyShotsSpawnFromThePitchedMuzzles()
        {
            var secondMuzzle = new GameObject("Second muzzle").transform;
            secondMuzzle.SetParent(second);
            secondMuzzle.localPosition = Vector3.forward;
            Set(shooter, "firePoints", new[] { muzzle, secondMuzzle });
            Set(shooter, "delayBetweenFirePoints", 0.15f);
            shooter.FireRate = 0.1f;
            int shots = 0;
            float lowestMuzzle = float.PositiveInfinity;
            shooter.onFired.AddListener(() =>
            {
                lowestMuzzle = Mathf.Min(lowestMuzzle, shots == 0 ? muzzle.position.y : secondMuzzle.position.y);
                shots++;
            });
            shooter.SuppressFire = false;
            yield return new WaitForSeconds(0.4f);
            Assert.That(shots, Is.EqualTo(2));
            Assert.That(lowestMuzzle, Is.GreaterThan(0.6f), "Both shots must occur after the guns tilt.");
        }

        [UnityTest]
        public IEnumerator DisablingAimRestoresPoseAndDisablingShooterRelaxesGuns()
        {
            aim.enabled = false;
            Assert.That(Quaternion.Angle(head.localRotation, Quaternion.identity), Is.LessThan(0.1f));
            Assert.That(Quaternion.Angle(first.localRotation, firstRest), Is.LessThan(0.1f));
            Assert.That(Quaternion.Angle(second.localRotation, secondRest), Is.LessThan(0.1f));
            aim.enabled = true;
            yield return new WaitForSeconds(0.5f);
            yield return new WaitForEndOfFrame();
            Assert.That(first.forward.y, Is.GreaterThan(0.6f));
            shooter.enabled = false;
            yield return new WaitForSeconds(0.7f);
            yield return new WaitForEndOfFrame();
            Assert.That(Quaternion.Angle(first.localRotation, firstRest), Is.LessThan(0.1f));
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(player);
            Object.Destroy(targetObject);
            Object.Destroy(projectile);
            foreach (var shot in Object.FindObjectsByType<Projectile>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (shot.name == "Unused projectile template(Clone)") Object.Destroy(shot.gameObject);
            yield return null;
        }

        private static void Set(object owner, string field, object value) =>
            owner.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(owner, value);
    }
}


