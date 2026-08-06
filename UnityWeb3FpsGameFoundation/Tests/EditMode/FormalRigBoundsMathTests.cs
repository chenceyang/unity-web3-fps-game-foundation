using NUnit.Framework;
using UnityEngine;
using Web3Fps.GameFoundation.Formal;

namespace Web3Fps.GameFoundation.Tests
{
    public sealed class FormalRigBoundsMathTests
    {
        [Test]
        public void HumanSizedRigIsAccepted()
        {
            Assert.That(FormalRigBoundsMath.IsSane(
                new Vector3(1.2f, 1.9f, 1.1f),
                new Vector3(0f, 0.95f, 0f),
                4.5f,
                3f), Is.True);
        }

        [Test]
        public void ExplodedAnimationBoundsAreRejected()
        {
            Assert.That(FormalRigBoundsMath.IsSane(
                new Vector3(18f, 32f, 14f),
                new Vector3(0f, 16f, 0f),
                4.5f,
                3f), Is.False);
        }
    }
}
