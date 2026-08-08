using NUnit.Framework;
using Web3Fps.GameFoundation.Gameplay;

namespace Web3Fps.GameFoundation.Tests
{
    public sealed class PlayerMotorMathTests
    {
        [Test]
        public void SnapCoversOneFrameOfDescentAtSprintSpeed()
        {
            // 7.5 m/s down a 45 degree ramp needs 7.5 * tan(45) / 60 = 0.125 m per frame.
            var snap = PlayerMotorMath.GroundSnapDistance(7.5f, 45f, 1f / 60f, 0.3f);
            Assert.That(snap, Is.EqualTo(0.145f).Within(0.005f));
        }

        [Test]
        public void SnapIsClampedToStepOffsetSoLedgesDoNotTeleport()
        {
            var snap = PlayerMotorMath.GroundSnapDistance(50f, 60f, 0.1f, 0.3f);
            Assert.That(snap, Is.EqualTo(0.3f));
        }

        [Test]
        public void ZeroDeltaTimeProducesNoSnap()
        {
            Assert.That(PlayerMotorMath.GroundSnapDistance(5f, 45f, 0f, 0.3f), Is.Zero);
        }

        [Test]
        public void StationaryControllerStillGetsContactMargin()
        {
            var snap = PlayerMotorMath.GroundSnapDistance(0f, 45f, 1f / 60f, 0.3f);
            Assert.That(snap, Is.EqualTo(0.02f).Within(0.0001f));
        }
    }
}
