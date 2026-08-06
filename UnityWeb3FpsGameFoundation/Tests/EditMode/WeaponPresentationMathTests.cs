using NUnit.Framework;
using Web3Fps.GameFoundation.Formal;

namespace Web3Fps.GameFoundation.Tests
{
    public sealed class WeaponPresentationMathTests
    {
        [Test]
        public void ReloadPoseStartsAndEndsAtRest()
        {
            var start = WeaponPresentationMath.EvaluateReload(0f);
            var end = WeaponPresentationMath.EvaluateReload(1f);
            Assert.That(start.Arc, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(end.Arc, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(start.MagazineDrop, Is.EqualTo(0f));
            Assert.That(end.MagazineDrop, Is.EqualTo(0f));
        }

        [Test]
        public void ReloadPoseDropsMagazineNearMidpoint()
        {
            var midpoint = WeaponPresentationMath.EvaluateReload(0.5f);
            Assert.That(midpoint.Arc, Is.GreaterThan(0.99f));
            Assert.That(midpoint.MagazineDrop, Is.GreaterThan(0.95f));
        }
    }
}
