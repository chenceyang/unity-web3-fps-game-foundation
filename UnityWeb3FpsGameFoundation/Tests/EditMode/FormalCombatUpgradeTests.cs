using NUnit.Framework;
using UnityEngine;
using Web3Fps.GameFoundation.Formal;
using Web3Fps.GameFoundation.Gameplay.Combat;
using Web3Fps.GameFoundation.Prototype;

namespace Web3Fps.GameFoundation.Tests
{
    public sealed class FormalCombatUpgradeTests
    {
        [Test]
        public void DamageZonesApplyExpectedMultipliers()
        {
            Assert.That(DamageZoneMath.ScaleDamage(25f, 2f), Is.EqualTo(50f));
            Assert.That(DamageZoneMath.ScaleDamage(25f, 0.78f), Is.EqualTo(19.5f).Within(0.001f));
            Assert.That(DamageZoneMath.ScaleDamage(-5f, 2f), Is.Zero);
        }

        [Test]
        public void AimingIsMoreAccurateThanMovingHipFire()
        {
            var aimed = WeaponAccuracyMath.CalculateSpread(1.15f, 0.28f, 1.35f, 0f, true, 0f);
            var movingHip = WeaponAccuracyMath.CalculateSpread(1.15f, 0.28f, 1.35f, 1f, false, 0.6f);
            Assert.That(aimed, Is.LessThan(movingHip));
        }

        [Test]
        public void SpreadIsDeterministicAndBounded()
        {
            var first = WeaponAccuracyMath.ApplySpread(Vector3.forward, 17u, 2f);
            var second = WeaponAccuracyMath.ApplySpread(Vector3.forward, 17u, 2f);
            Assert.That(first, Is.EqualTo(second));
            Assert.That(Vector3.Angle(Vector3.forward, first), Is.LessThanOrEqualTo(2.01f));
        }

        [Test]
        public void BotReactionAndSightMemoryAreExplicit()
        {
            Assert.That(PrototypeBotDecisionMath.CanAttack(10.37d, 10d, 0.38f), Is.False);
            Assert.That(PrototypeBotDecisionMath.CanAttack(10.38d, 10d, 0.38f), Is.True);
            Assert.That(PrototypeBotDecisionMath.RemembersTarget(5.5d, 4d, 1.6f), Is.True);
            Assert.That(PrototypeBotDecisionMath.RemembersTarget(5.7d, 4d, 1.6f), Is.False);
        }

        [Test]
        public void BotKeepsAPlayableRangeBand()
        {
            Assert.That(PrototypeBotDecisionMath.RangeMoveSign(15f, 10f), Is.EqualTo(1f));
            Assert.That(PrototypeBotDecisionMath.RangeMoveSign(8f, 10f), Is.Zero);
            Assert.That(PrototypeBotDecisionMath.RangeMoveSign(5f, 10f), Is.EqualTo(-1f));
        }

        [Test]
        public void OpeningLaneKeepsEnemyVisibleWithoutPointBlankSpawn()
        {
            Assert.That(FormalCombatLayout.OpeningLaneClears(
                FormalCombatLayout.FurthestCoverCenterZ,
                FormalCombatLayout.CoverHalfDepth), Is.True);
            Assert.That(FormalCombatLayout.OpeningEngagementDistance, Is.GreaterThanOrEqualTo(20f));
        }
    }
}
