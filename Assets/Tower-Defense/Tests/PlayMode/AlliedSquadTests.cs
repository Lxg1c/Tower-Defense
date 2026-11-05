using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace TowerDefense.Tests
{
    public sealed class AlliedSquadTests
    {
        private const string Prefix = "AlliedSquadFixture";
        private readonly List<Object> created = new();
        private NavMeshData navMeshData;
        private NavMeshDataInstance navMeshInstance;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            AllyCommand.SetFollowPlayer(false);
            var sources = new List<NavMeshBuildSource>
            {
                new()
                {
                    shape = NavMeshBuildSourceShape.Box,
                    size = new Vector3(80f, 1f, 80f),
                    transform = Matrix4x4.TRS(new Vector3(0f, -0.5f, 0f), Quaternion.identity, Vector3.one),
                    area = 0
                }
            };
            navMeshData = NavMeshBuilder.BuildNavMeshData(
                NavMesh.GetSettingsByID(0), sources,
                new Bounds(Vector3.zero, new Vector3(100f, 20f, 100f)),
                Vector3.zero, Quaternion.identity);
            navMeshInstance = NavMesh.AddNavMeshData(navMeshData);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            AllyCommand.SetFollowPlayer(false);
            foreach (AllySoldier ally in Object.FindObjectsByType<AllySoldier>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (ally != null && ally.name.StartsWith(Prefix))
                    Object.Destroy(ally.gameObject);
            foreach (Projectile projectile in Object.FindObjectsByType<Projectile>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (projectile != null && projectile.name.StartsWith(Prefix))
                    Object.Destroy(projectile.gameObject);
            foreach (Object item in created)
                if (item != null) Object.Destroy(item);
            created.Clear();
            yield return null;
            navMeshInstance.Remove();
            if (navMeshData != null) Object.Destroy(navMeshData);
        }

        [UnityTest]
        public IEnumerator MeleeBarracksSpawnsAndRespawnsOnlyMelee() =>
            VerifySingleTypeSquad(AllySoldier.CombatType.Melee);

        [UnityTest]
        public IEnumerator RangedBarracksSpawnsAndRespawnsOnlyRanged() =>
            VerifySingleTypeSquad(AllySoldier.CombatType.Ranged);

        [UnityTest]
        public IEnumerator BarracksKeepIndependentGuardOrdersThroughRecallAndRespawn()
        {
            Projectile bullet = CreateBullet();
            AllySoldier template = CreateSoldierTemplate(AllySoldier.CombatType.Melee, -30f, bullet);
            BarracksBuilding left = CreateBarracks("Left", new Vector3(-6f, 0f, 0f), template);
            BarracksBuilding right = CreateBarracks("Right", new Vector3(6f, 0f, 0f), template);
            yield return null;

            Assert.That(left.TryAssignGuardPosition(new Vector3(-12f, 0f, 8f)), Is.True);
            Assert.That(right.TryAssignGuardPosition(new Vector3(12f, 0f, -8f)), Is.True);
            Assert.That(left.GuardPosition, Is.Not.EqualTo(right.GuardPosition));

            AllySoldier[] squad = SpawnedSoldiers();
            Assert.That(squad, Has.Length.EqualTo(2));
            Assert.That(squad.All(s => s.CurrentMode == AllySoldier.Mode.GuardPosition), Is.True);
            Assert.That(squad.Single(s => s.Barracks == left).GuardPoint.x, Is.LessThan(0f));
            Assert.That(squad.Single(s => s.Barracks == right).GuardPoint.x, Is.GreaterThan(0f));

            AllyCommand.SetFollowPlayer(true);
            Assert.That(squad.All(s => s.CurrentMode == AllySoldier.Mode.FollowPlayer), Is.True);
            AllyCommand.SetFollowPlayer(false);
            Assert.That(squad.All(s => s.CurrentMode == AllySoldier.Mode.GuardPosition), Is.True);

            squad.Single(s => s.Barracks == left).TakeDamage(1000f);
            yield return new WaitForSeconds(0.25f);
            Assert.That(SpawnedSoldiers().Single(s => s.Barracks == left).CurrentMode,
                Is.EqualTo(AllySoldier.Mode.GuardPosition));
            Assert.That(right.HasGuardPosition, Is.True);
        }

        [UnityTest]
        public IEnumerator UnreachableGuardPointDoesNotReplacePreviousOrder()
        {
            Projectile bullet = CreateBullet();
            AllySoldier template = CreateSoldierTemplate(AllySoldier.CombatType.Melee, -30f, bullet);
            BarracksBuilding barracks = CreateBarracks("Guard", Vector3.zero, template);
            yield return null;

            Assert.That(barracks.TryAssignGuardPosition(new Vector3(5f, 0f, 5f)), Is.True);
            Vector3 original = barracks.GuardPosition;
            Assert.That(barracks.TryAssignGuardPosition(new Vector3(500f, 0f, 500f)), Is.False);
            Assert.That(barracks.GuardPosition, Is.EqualTo(original));
            Assert.That(SpawnedSoldiers().Single().CurrentMode,
                Is.EqualTo(AllySoldier.Mode.GuardPosition));
        }

        [UnityTest]
        public IEnumerator BarracksCanReassignGuardPointInBuildAndCombatPhases()
        {
            Projectile bullet = CreateBullet();
            AllySoldier template = CreateSoldierTemplate(AllySoldier.CombatType.Melee, -30f, bullet);
            BarracksBuilding barracks = CreateBarracks("Reposition", Vector3.zero, template);
            yield return null;

            Assert.That(barracks.TryAssignGuardPosition(new Vector3(-8f, 0f, 6f)), Is.True);
            AllySoldier soldier = SpawnedSoldiers().Single();
            Assert.That(soldier.CurrentMode, Is.EqualTo(AllySoldier.Mode.GuardPosition));
            Assert.That(soldier.GuardPoint.x, Is.LessThan(0f));

            typeof(BarracksBuilding).GetMethod("OnCombatPhaseStarted",
                    BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(barracks, new object[] { 1, 4, 0 });
            Assert.That(barracks.TryAssignGuardPosition(new Vector3(8f, 0f, 6f)), Is.True);
            Assert.That(soldier.CurrentMode, Is.EqualTo(AllySoldier.Mode.GuardPosition));
            Assert.That(soldier.GuardPoint.x, Is.GreaterThan(0f));

            typeof(BarracksBuilding).GetMethod("OnBuildPhaseStarted",
                    BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(barracks, new object[] { 2, 4, 0 });
            Assert.That(barracks.TryAssignGuardPosition(new Vector3(-6f, 0f, -6f)), Is.True);
            Assert.That(soldier.GuardPoint.x, Is.LessThan(0f));
        }

        private BarracksBuilding CreateBarracks(string name, Vector3 position, AllySoldier template)
        {
            GameObject building = New(name, false);
            building.transform.position = position;
            BarracksBuilding barracks = building.AddComponent<BarracksBuilding>();
            Set(barracks, "allyPrefab", template);
            Set(barracks, "squadSize", 1);
            Set(barracks, "spawnInterval", 0f);
            Set(barracks, "respawnDelay", 0.1f);
            building.SetActive(true);
            return barracks;
        }

        private IEnumerator VerifySingleTypeSquad(AllySoldier.CombatType type)
        {
            Projectile bullet = CreateBullet();
            AllySoldier melee = CreateSoldierTemplate(AllySoldier.CombatType.Melee, -30f, bullet);
            AllySoldier ranged = CreateSoldierTemplate(AllySoldier.CombatType.Ranged, -27f, bullet);

            GameObject building = New("Barracks", false);
            BarracksBuilding barracks = building.AddComponent<BarracksBuilding>();
            Set(barracks, "allyPrefab", melee);
            Set(barracks, "rangedAllyPrefab", ranged);
            Set(barracks, "squadSize", 3);
            Set(barracks, "squadType", type);
            Set(barracks, "spawnInterval", 0f);
            Set(barracks, "respawnDelay", 0.1f);
            building.SetActive(true);
            yield return null;

            AllySoldier[] squad = SpawnedSoldiers();
            Assert.That(squad, Has.Length.EqualTo(3));
            Assert.That(squad.All(s => s.Type == type), Is.True);

            squad[0].TakeDamage(1000f);
            yield return new WaitForSeconds(0.25f);

            squad = SpawnedSoldiers();
            Assert.That(squad, Has.Length.EqualTo(3));
            Assert.That(squad.All(s => s.Type == type), Is.True,
                "A barracks must respawn the selected ally type.");
        }

        [UnityTest]
        public IEnumerator RangedAllyFiresAProjectileBeforeTheTargetTakesDamage()
        {
            Projectile bullet = CreateBullet();
            AllySoldier template = CreateSoldierTemplate(AllySoldier.CombatType.Ranged, -30f, bullet);
            AllySoldier ranged = Object.Instantiate(template, Vector3.zero, Quaternion.identity);
            CombatTestTarget enemy = CreateEnemy(new Vector3(8f, 0f, 0f));
            ranged.SetAttackMode();

            yield return new WaitForSeconds(0.4f);
            Assert.That(Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None)
                .Any(p => p.name.StartsWith(Prefix) && p.name.Contains("(Clone)")), Is.True);
            Assert.That(enemy.CurrentHealth, Is.EqualTo(enemy.MaxHealth),
                "A ranged attack must not apply melee damage when the shot is launched.");

            yield return new WaitForSeconds(2.4f);
            Assert.That(enemy.CurrentHealth, Is.LessThan(enemy.MaxHealth),
                "The projectile should damage its target after travelling to it.");
        }

        private Projectile CreateBullet()
        {
            GameObject go = New("Bullet");
            go.transform.position = new Vector3(-35f, 1f, 0f);
            Projectile projectile = go.AddComponent<Projectile>();
            Set(projectile, "speed", 4f);
            Set(projectile, "maxHomingTurnAngle", 180f);
            Set(projectile, "lifeTimer", 1000f);
            Set(projectile, "maxTravelDistance", float.MaxValue);
            Set(projectile, "fallbackDirection", Vector3.forward);
            return projectile;
        }

        private AllySoldier CreateSoldierTemplate(AllySoldier.CombatType type, float x, Projectile bullet)
        {
            GameObject go = New(type == AllySoldier.CombatType.Melee ? "Melee template" : "Ranged template", false);
            go.transform.position = new Vector3(x, 0f, 0f);
            go.layer = LayerMask.NameToLayer("Player");
            go.AddComponent<CapsuleCollider>();
            go.AddComponent<NavMeshAgent>();
            AllySoldier ally = go.AddComponent<AllySoldier>();
            Set(ally, "combatType", type);
            Set(ally, "enemyMask", (LayerMask)(1 << LayerMask.NameToLayer("Enemy")));
            Set(ally, "attackRange", type == AllySoldier.CombatType.Ranged ? 9f : 1.8f);
            if (type == AllySoldier.CombatType.Ranged)
            {
                GameObject muzzle = new("FirePoint");
                muzzle.transform.SetParent(go.transform, false);
                muzzle.transform.localPosition = new Vector3(0f, 1.4f, 0.5f);
                Set(ally, "firePoint", muzzle.transform);
                Set(ally, "projectilePrefab", bullet);
            }
            go.SetActive(true);
            return ally;
        }

        private CombatTestTarget CreateEnemy(Vector3 position)
        {
            GameObject go = New("Enemy");
            go.transform.position = position;
            go.layer = LayerMask.NameToLayer("Enemy");
            SphereCollider collider = go.AddComponent<SphereCollider>();
            collider.center = Vector3.up;
            return go.AddComponent<CombatTestTarget>();
        }

        private AllySoldier[] SpawnedSoldiers() => Object.FindObjectsByType<AllySoldier>(FindObjectsSortMode.None)
            .Where(s => s.name.StartsWith(Prefix) && s.name.Contains("(Clone)"))
            .ToArray();

        private GameObject New(string suffix, bool active = true)
        {
            GameObject go = new($"{Prefix} {suffix}");
            go.SetActive(active);
            created.Add(go);
            return go;
        }

        private static void Set(object target, string field, object value) =>
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(target, value);
    }
}
