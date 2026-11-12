using NUnit.Framework;
using UnityEngine;
using Hint = FirstLevelTutorial.Hint;

namespace TowerDefense.Tests
{
    public class TutorialAndBuildingGuideTests
    {
        private static readonly string[] PreferenceKeys =
        {
            "TowerDefense.Tutorial.Seen",
            "TowerDefense.Tutorial.Replay",
            "TowerDefense.BuildingGuide.Pulse"
        };

        private readonly int?[] savedPreferences = new int?[PreferenceKeys.Length];

        [SetUp]
        public void SetUp()
        {
            for (int i = 0; i < PreferenceKeys.Length; i++)
            {
                string key = PreferenceKeys[i];
                savedPreferences[i] = PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key) : null;
                PlayerPrefs.DeleteKey(key);
            }
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < PreferenceKeys.Length; i++)
            {
                string key = PreferenceKeys[i];
                if (savedPreferences[i] is int value) PlayerPrefs.SetInt(key, value);
                else PlayerPrefs.DeleteKey(key);
            }
            PlayerPrefs.Save();
        }

        [TestCase(BuildSlotType.Mine)]
        [TestCase(BuildSlotType.Defense)]
        public void PreparationHintsFollowCoreOfferEconomyChoiceAndCompletedPurchase(BuildSlotType choice)
        {
            var tutorial = new FirstLevelTutorial();
            Assert.That(Prepare(tutorial), Is.EqualTo(Hint.Approach));
            Assert.That(Prepare(tutorial, offer: BuildSlotType.TownHall), Is.EqualTo(Hint.Core));
            Assert.That(Prepare(tutorial, hasCore: true), Is.EqualTo(Hint.Economy));
            Assert.That(Prepare(tutorial, hasCore: true, offer: choice),
                Is.EqualTo(choice == BuildSlotType.Mine ? Hint.Mine : Hint.Turret));
            Assert.That(Prepare(tutorial, hasCore: true, hasDefense: true), Is.EqualTo(Hint.StartWave));
        }

        [Test]
        public void HintStaysInTheCurrentContextAndDoesNotRepeatAfterLeavingIt()
        {
            var tutorial = new FirstLevelTutorial();
            Assert.That(Prepare(tutorial), Is.EqualTo(Hint.Approach));
            Assert.That(Prepare(tutorial), Is.EqualTo(Hint.Approach));
            Assert.That(Prepare(tutorial, offer: BuildSlotType.TownHall), Is.EqualTo(Hint.Core));
            Assert.That(Prepare(tutorial), Is.EqualTo(Hint.None));
            Assert.That(Prepare(tutorial, offer: BuildSlotType.TownHall), Is.EqualTo(Hint.None));
        }

        [Test]
        public void NewRunSuppressesSeenHintsAndStillShowsUnseenOnes()
        {
            Assert.That(Prepare(new FirstLevelTutorial()), Is.EqualTo(Hint.Approach));

            var nextRun = new FirstLevelTutorial();
            Assert.That(Prepare(nextRun), Is.EqualTo(Hint.None));
            Assert.That(Prepare(nextRun, offer: BuildSlotType.TownHall), Is.EqualTo(Hint.Core));
        }

        [Test]
        public void ReplayWaitsForNextRunAndIsConsumedOnlyOnce()
        {
            var tutorial = new FirstLevelTutorial();
            Assert.That(Prepare(tutorial), Is.EqualTo(Hint.Approach));
            Assert.That(Prepare(tutorial, offer: BuildSlotType.TownHall), Is.EqualTo(Hint.Core));

            FirstLevelTutorial.QueueReplay();
            Assert.That(FirstLevelTutorial.ReplayQueued, Is.True);
            Assert.That(Prepare(tutorial), Is.EqualTo(Hint.None));

            var replay = new FirstLevelTutorial();
            Assert.That(FirstLevelTutorial.ReplayQueued, Is.False);
            Assert.That(Prepare(replay), Is.EqualTo(Hint.Approach));
            Assert.That(Prepare(replay, offer: BuildSlotType.TownHall), Is.EqualTo(Hint.Core));
            Assert.That(Prepare(new FirstLevelTutorial()), Is.EqualTo(Hint.None));
        }

        [Test]
        public void HiddenHintDoesNotCountAsSeen()
        {
            var tutorial = new FirstLevelTutorial();
            Assert.That(Prepare(tutorial, visible: false), Is.EqualTo(Hint.None));
            Assert.That(Prepare(new FirstLevelTutorial()), Is.EqualTo(Hint.Approach));
        }

        [Test]
        public void AutoFireHintNeedsATargetAndYieldsToUltimateAfterVisibleTime()
        {
            var tutorial = new FirstLevelTutorial();
            Assert.That(Combat(tutorial, ultimateReady: false), Is.EqualTo(Hint.None));
            Assert.That(Combat(tutorial, hasTarget: true, deltaTime: 1f), Is.EqualTo(Hint.AutoFire));
            Assert.That(Combat(tutorial, visible: false, deltaTime: 10f), Is.EqualTo(Hint.None));
            Assert.That(Combat(tutorial, deltaTime: 3f), Is.EqualTo(Hint.AutoFire));
            Assert.That(Combat(tutorial, hasTarget: true), Is.EqualTo(Hint.Ultimate));
            Assert.That(Combat(tutorial, ultimateCharging: true), Is.EqualTo(Hint.None));
            Assert.That(Combat(tutorial, hasTarget: true), Is.EqualTo(Hint.None));
        }

        [Test]
        public void UltimateHintIsSkippedIfUltimateWasAlreadyUsedWhileHintsWereHidden()
        {
            var tutorial = new FirstLevelTutorial();
            Assert.That(Combat(tutorial, visible: false, ultimateCharging: true), Is.EqualTo(Hint.None));
            Assert.That(Combat(tutorial), Is.EqualTo(Hint.None));
        }

        [Test]
        public void PulseCanOnlyBeReadAfterDiscovery()
        {
            BuildingGuide.ReadPulse();
            Assert.That(BuildingGuide.PulseDiscovered, Is.False);
            Assert.That(BuildingGuide.PulseIsNew, Is.False);

            BuildingGuide.DiscoverPulse();
            Assert.That(BuildingGuide.PulseDiscovered, Is.True);
            Assert.That(BuildingGuide.PulseIsNew, Is.True);
        }

        [Test]
        public void ReadingPulseClearsBadgeAndRediscoveryDoesNotBringItBack()
        {
            BuildingGuide.DiscoverPulse();
            BuildingGuide.ReadPulse();
            Assert.That(BuildingGuide.PulseDiscovered, Is.True);
            Assert.That(BuildingGuide.PulseIsNew, Is.False);

            BuildingGuide.DiscoverPulse();
            BuildingGuide.ReadPulse();
            Assert.That(BuildingGuide.PulseDiscovered, Is.True);
            Assert.That(BuildingGuide.PulseIsNew, Is.False);
        }

        private static Hint Prepare(FirstLevelTutorial tutorial, bool hasCore = false,
            bool hasDefense = false, BuildSlotType? offer = null, bool visible = true) =>
            tutorial.Update(visible, preparing: true, hasCore, hasDefense, offer,
                hasTarget: false, ultimateCharging: false, ultimateReady: false, deltaTime: 0f);

        private static Hint Combat(FirstLevelTutorial tutorial, bool visible = true,
            bool hasTarget = false, bool ultimateCharging = false, bool ultimateReady = true, float deltaTime = 0f) =>
            tutorial.Update(visible, preparing: false, hasCore: true, hasDefense: true, offer: null,
                hasTarget, ultimateCharging, ultimateReady, deltaTime);
    }
}
