using NUnit.Framework;
using UnityEngine;
using Web3Fps.GameFoundation.Prototype;

namespace Web3Fps.GameFoundation.Tests
{
    public sealed class PrototypeBotSteeringTests
    {
        [Test]
        public void ClearPathKeepsDesiredDirection()
        {
            var result = PrototypeBotSteering.Resolve(Vector3.forward, false, 1f);
            Assert.That(result, Is.EqualTo(Vector3.forward));
        }

        [Test]
        public void BlockedPathAddsStableLateralMovement()
        {
            var right = PrototypeBotSteering.Resolve(Vector3.forward, true, 1f);
            var left = PrototypeBotSteering.Resolve(Vector3.forward, true, -1f);
            Assert.That(Mathf.Abs(right.x), Is.GreaterThan(0.8f));
            Assert.That(right.x, Is.EqualTo(-left.x).Within(0.0001f));
            Assert.That(right.z, Is.GreaterThan(0f));
        }
    }
}
