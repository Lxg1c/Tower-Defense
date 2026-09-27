using NUnit.Framework;

namespace TowerDefense.Tests
{
    public class FrameRatePolicyTests
    {
        [TestCase(59.94, 60)]
        [TestCase(60, 60)]
        [TestCase(90, 90)]
        [TestCase(119.88, 120)]
        [TestCase(0, 60)]
        public void MobileRequestsDisplayRateWithoutRoundingDown(double hz, int expected)
        {
            // Rounding 59.94 down to 59 can make mobile pacing drop to half refresh.
            Assert.That(GamePerformanceSettings.SelectTargetFrameRate(true, hz), Is.EqualTo(expected));
        }

        [Test]
        public void DesktopHasNoSoftwareFrameLimit()
        {
            Assert.That(GamePerformanceSettings.SelectTargetFrameRate(false, 144), Is.EqualTo(-1));
        }
    }
}
