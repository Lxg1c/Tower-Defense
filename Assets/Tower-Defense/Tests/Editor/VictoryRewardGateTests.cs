using NUnit.Framework;

namespace TowerDefense.Tests
{
    public sealed class VictoryRewardGateTests
    {
        [Test]
        public void ReopeningVictoryDoesNotPayTwice()
        {
            var gate = new VictoryRewardGate();
            int money = 0;
            Assert.That(gate.TryAward(() => money += 100), Is.True);
            Assert.That(gate.TryAward(() => money += 100), Is.False);
            Assert.That(money, Is.EqualTo(100));
        }

        [Test]
        public void FailedSaveCanBeRetried()
        {
            var gate = new VictoryRewardGate();
            Assert.Throws<System.InvalidOperationException>(() =>
                gate.TryAward(() => throw new System.InvalidOperationException()));
            Assert.That(gate.HasAwarded, Is.False);
            Assert.That(gate.TryAward(() => { }), Is.True);
        }
    }
}
