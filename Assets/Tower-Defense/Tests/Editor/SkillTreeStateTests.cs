using NUnit.Framework;

namespace TowerDefense.Tests
{
    public sealed class SkillTreeStateTests
    {
        [Test]
        public void ReplayedVictoryPaysAgainAndUnlocksNextDifficulty()
        {
            var state = new SkillTreeState();
            Assert.That(state.IsDifficultyUnlocked("level-1", 1), Is.True);
            Assert.That(state.IsDifficultyUnlocked("level-1", 2), Is.False);
            state.RecordVictory("level-1", 1, 100);
            Assert.That(state.TryUpgrade(PlayerSkill.Vitality), Is.True);
            state.RecordVictory("level-1", 1, 100);
            Assert.That(state.Money, Is.EqualTo(100));
            Assert.That(state.IsDifficultyUnlocked("level-1", 2), Is.True);
            Assert.That(state.IsDifficultyUnlocked("level-1", 3), Is.False);
            state.RecordVictory("level-1", 2, 150);
            Assert.That(state.Money, Is.EqualTo(250));
            Assert.That(state.IsDifficultyUnlocked("level-1", 3), Is.True);
            Assert.That(state.Rank(PlayerSkill.Vitality), Is.EqualTo(1));
        }

        [Test]
        public void UpgradesCostMoreAtHigherRanksAndStopAtMaximum()
        {
            var state = new SkillTreeState();
            state.RecordVictory("level-1", 1, 700);
            Assert.That(state.TryUpgrade(PlayerSkill.Vitality), Is.True);
            Assert.That(state.TryUpgrade(PlayerSkill.Firepower), Is.True);
            Assert.That(state.TryUpgrade(PlayerSkill.Firepower), Is.True);
            Assert.That(state.TryUpgrade(PlayerSkill.Firepower), Is.True);
            Assert.That(state.TryUpgrade(PlayerSkill.Firepower), Is.False);
            Assert.That(state.Rank(PlayerSkill.Firepower), Is.EqualTo(3));
            Assert.That(state.Money, Is.Zero);
        }

        [Test]
        public void InsufficientMoneyDoesNotChangeRank()
        {
            var state = new SkillTreeState();
            Assert.That(state.TryUpgrade(PlayerSkill.RapidFire), Is.False);
            Assert.That(state.Money, Is.Zero);
            Assert.That(state.Rank(PlayerSkill.RapidFire), Is.Zero);
        }

        [Test]
        public void FirstVitalityRankUnlocksTheOtherTwoBranches()
        {
            var state = new SkillTreeState();
            state.RecordVictory("level-1", 1, 300);
            Assert.That(state.TryUpgrade(PlayerSkill.Firepower), Is.False);
            Assert.That(state.TryUpgrade(PlayerSkill.RapidFire), Is.False);
            Assert.That(state.Money, Is.EqualTo(300));
            Assert.That(state.TryUpgrade(PlayerSkill.Vitality), Is.True);
            Assert.That(state.TryUpgrade(PlayerSkill.Firepower), Is.True);
            Assert.That(state.TryUpgrade(PlayerSkill.RapidFire), Is.True);
            Assert.That(state.Money, Is.Zero);
            Assert.That(state.HealthMultiplier, Is.EqualTo(1.15f).Within(.0001f));
            Assert.That(state.DamageMultiplier, Is.EqualTo(1.10f).Within(.0001f));
            Assert.That(state.FireRateMultiplier, Is.EqualTo(1.08f).Within(.0001f));
        }

        [Test]
        public void SaveDataRetainsDifficultyMoneyAndRanks()
        {
            var state = new SkillTreeState();
            state.RecordVictory("level-1", 1, 200);
            state.TryUpgrade(PlayerSkill.Vitality);
            var loaded = UnityEngine.JsonUtility.FromJson<SkillTreeState>(UnityEngine.JsonUtility.ToJson(state));
            Assert.DoesNotThrow(loaded.Validate);
            Assert.That(loaded.HighestClearedDifficulty("level-1"), Is.EqualTo(1));
            Assert.That(loaded.Money, Is.EqualTo(100));
            Assert.That(loaded.Rank(PlayerSkill.Vitality), Is.EqualTo(1));
        }

        [Test]
        public void LegacySavePreservesPurchasesAndConvertsPoints()
        {
            const string json = "{\"version\":1,\"points\":2,\"vitalityRank\":1,\"firepowerRank\":1,\"rapidFireRank\":0,\"claimedLevels\":[\"level-1\"]}";
            var state = SkillTreeState.MigrateLegacy(json);
            Assert.That(state.Money, Is.EqualTo(200));
            Assert.That(state.Rank(PlayerSkill.Vitality), Is.EqualTo(1));
            Assert.That(state.Rank(PlayerSkill.Firepower), Is.EqualTo(1));
            Assert.That(state.IsDifficultyUnlocked("level-1", 2), Is.True);
            Assert.That(state.IsDifficultyUnlocked("level-1", 3), Is.False);
        }

        [Test]
        public void LockedDifficultyCannotPayAndInsufficientShopMoneyIsNotSpent()
        {
            var state = new SkillTreeState();
            Assert.Throws<System.InvalidOperationException>(() => state.RecordVictory("level-1", 2, 150));
            Assert.That(state.TrySpend(10), Is.False);
            Assert.That(state.Money, Is.Zero);
        }

        [Test]
        public void VictorySkillsNavigationIsConsumedOnce()
        {
            PlayerProgression.RequestSkillsScreen();
            Assert.That(PlayerProgression.ConsumeSkillsScreenRequest(), Is.True);
            Assert.That(PlayerProgression.ConsumeSkillsScreenRequest(), Is.False);
        }
    }
}
