using NUnit.Framework;

namespace TowerDefense.Tests
{
    public class UltimateReadyCueTests
    {
        [Test]
        public void CueStartsOnlyWhenChargeReturnsAndEndsWhileStillReady()
        {
            var cue = new UltimateReadyCue();
            Assert.That(cue.Update(ready: true, visible: true, deltaTime: 0.1f), Is.False);
            Assert.That(cue.Update(ready: false, visible: true, deltaTime: 0.1f), Is.False);
            Assert.That(cue.Update(ready: true, visible: true, deltaTime: 0.3f), Is.True);
            Assert.That(cue.Update(ready: true, visible: true, deltaTime: 0.3f), Is.True);
            Assert.That(cue.Update(ready: true, visible: true, deltaTime: 0.1f), Is.False);
        }

        [Test]
        public void CueWaitsForVisibleControlsAndCancelsWhenUltimateIsUsed()
        {
            var cue = new UltimateReadyCue();
            cue.Update(ready: false, visible: false, deltaTime: 0f);
            Assert.That(cue.Update(ready: true, visible: false, deltaTime: 2f), Is.False);
            Assert.That(cue.Update(ready: true, visible: true, deltaTime: 0.1f), Is.True);
            Assert.That(cue.Update(ready: false, visible: true, deltaTime: 0.1f), Is.False);
        }
    }
}
