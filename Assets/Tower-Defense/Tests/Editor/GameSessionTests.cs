using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace TowerDefense.Tests
{
    public class GameSessionTests
    {
        [Test]
        public void GameplayPrefabsHaveExplicitHealthPresentationBindings()
        {
            int checkedTargets = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Tower-Defense" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                foreach (var health in prefab.GetComponentsInChildren<Damageable>(true))
                {
                    var binding = health.GetComponent<HealthBarBinding>();
                    Assert.That(binding, Is.Not.Null, path);
                    Assert.That(binding.Target, Is.EqualTo(health), path);
                    Assert.That(binding.HideOnDeath, Is.EqualTo(!(health is PlayerHealth)), path);
                    checkedTargets++;
                }
            }
            Assert.That(checkedTargets, Is.GreaterThan(0));
        }

        [Test]
        public void EachWavePaysOnceAndFinalWaveWins()
        {
            int coins = 0;
            var session = new GameSession(new[] { 20, 30 }, amount => coins += amount);
            Assert.That(session.TryCompleteWave(0), Is.False);
            Assert.That(session.TryStartWave(true, false), Is.True);
            Assert.That(session.TryStartWave(true, false), Is.False);
            Assert.That(session.TryCompleteWave(1), Is.False);
            Assert.That(session.TryCompleteWave(0), Is.True);
            Assert.That(session.TryCompleteWave(0), Is.False);
            Assert.That(coins, Is.EqualTo(20));
            Assert.That(session.CurrentPhase, Is.EqualTo(GameSession.Phase.Build));
            Assert.That(session.TryStartWave(true, false), Is.True);
            Assert.That(session.TryCompleteWave(1), Is.True);
            Assert.That(session.CurrentPhase, Is.EqualTo(GameSession.Phase.AllCompleted));
            Assert.That(session.TryDefeat(), Is.False);
            Assert.That(session.TryStartWave(true, false), Is.False);
            Assert.That(session.TryCompleteWave(1), Is.False);
            Assert.That(coins, Is.EqualTo(50));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DefeatPreventsCompletionRewardsAndFurtherWaves(bool duringCombat)
        {
            int coins = 0;
            var session = new GameSession(new[] { 50 }, amount => coins += amount);
            if (duringCombat) session.TryStartWave(true, false);
            Assert.That(session.TryDefeat(), Is.True);
            Assert.That(session.TryDefeat(), Is.False);
            Assert.That(session.TryCompleteWave(0), Is.False);
            Assert.That(session.TryStartWave(true, false), Is.False);
            Assert.That(session.CurrentPhase, Is.EqualTo(GameSession.Phase.Defeated));
            Assert.That(coins, Is.Zero);
        }

        [TestCase(false, false)]
        [TestCase(true, true)]
        public void StartRequiresLivingBaseAndClosedSelection(bool baseAlive, bool selectionOpen)
        {
            var session = new GameSession(new[] { 10 }, _ => { });
            Assert.That(session.TryStartWave(baseAlive, selectionOpen), Is.False);
            Assert.That(session.CurrentPhase, Is.EqualTo(GameSession.Phase.Build));
        }

        [Test]
        public void RewardListenerCannotCompleteTheSameWaveAgain()
        {
            GameSession session = null;
            int awards = 0;
            session = new GameSession(new[] { 10 }, _ =>
            {
                awards++;
                Assert.That(session.TryCompleteWave(0), Is.False);
            });
            session.TryStartWave(true, false);
            session.TryCompleteWave(0);
            Assert.That(awards, Is.EqualTo(1));
        }

        [Test]
        public void RewardListenerCannotStartAnotherWaveDuringCompletion()
        {
            GameSession session = null;
            session = new GameSession(new[] { 10, 20 }, _ =>
                Assert.That(session.TryStartWave(true, false), Is.False));
            session.TryStartWave(true, false);
            session.TryCompleteWave(0);
            Assert.That(session.TryStartWave(true, false), Is.True);
        }

        [Test]
        public void EmptySessionCompletesWithoutReward()
        {
            var session = new GameSession(new int[0], _ => Assert.Fail("No waves means no rewards."));
            Assert.That(session.CurrentPhase, Is.EqualTo(GameSession.Phase.AllCompleted));
            Assert.That(session.TryStartWave(true, false), Is.False);
        }
    }
}
