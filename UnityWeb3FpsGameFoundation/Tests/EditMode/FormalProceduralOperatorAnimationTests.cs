using NUnit.Framework;
using Web3Fps.GameFoundation.Formal;

namespace Web3Fps.GameFoundation.Tests
{
    public sealed class FormalProceduralOperatorAnimationTests
    {
        [Test]
        public void IdleOperatorHasNoStrideOrBob()
        {
            Assert.That(FormalProceduralOperatorAnimation.EvaluateStride(1.2f, 0f), Is.EqualTo(0f));
            Assert.That(FormalProceduralOperatorAnimation.EvaluateBob(1.2f, 0f), Is.EqualTo(0f));
        }

        [Test]
        public void RecoilDecayNeverBecomesNegative()
        {
            Assert.That(FormalProceduralOperatorAnimation.Decay(1f, 15f, 0.1f), Is.InRange(0f, 1f));
            Assert.That(FormalProceduralOperatorAnimation.Decay(-1f, 15f, 0.1f), Is.EqualTo(0f));
        }
    }
}
