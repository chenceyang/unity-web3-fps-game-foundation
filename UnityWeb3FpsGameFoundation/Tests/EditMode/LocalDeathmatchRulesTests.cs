using NUnit.Framework;
using Web3Fps.GameFoundation.Prototype;

namespace Web3Fps.GameFoundation.Tests
{
    public sealed class LocalDeathmatchRulesTests
    {
        [Test]
        public void PlayerReachingTargetKillsFinishesMatch()
        {
            var rules = new LocalDeathmatchRules(2, 60f);
            rules.Start();

            Assert.That(rules.RecordPlayerKill(), Is.True);
            Assert.That(rules.Phase, Is.EqualTo(LocalPrototypePhase.Running));
            Assert.That(rules.RecordPlayerKill(), Is.True);

            Assert.That(rules.PlayerKills, Is.EqualTo(2));
            Assert.That(rules.Phase, Is.EqualTo(LocalPrototypePhase.Finished));
            Assert.That(rules.Outcome, Is.EqualTo(LocalPrototypeOutcome.PlayerWin));
            Assert.That(rules.RecordBotKill(), Is.False);
        }

        [TestCase(2, 1, LocalPrototypeOutcome.PlayerWin)]
        [TestCase(1, 2, LocalPrototypeOutcome.BotWin)]
        [TestCase(1, 1, LocalPrototypeOutcome.Draw)]
        public void TimeoutUsesCurrentScore(int playerKills, int botKills, LocalPrototypeOutcome expected)
        {
            var rules = new LocalDeathmatchRules(5, 10f);
            rules.Start();
            for (var i = 0; i < playerKills; i++) rules.RecordPlayerKill();
            for (var i = 0; i < botKills; i++) rules.RecordBotKill();

            rules.Tick(10f);

            Assert.That(rules.Phase, Is.EqualTo(LocalPrototypePhase.Finished));
            Assert.That(rules.Outcome, Is.EqualTo(expected));
            Assert.That(rules.RemainingSeconds, Is.Zero);
        }

        [Test]
        public void ResetClearsScoreAndRestoresTimer()
        {
            var rules = new LocalDeathmatchRules(3, 90f);
            rules.Start();
            rules.RecordBotKill();
            rules.Tick(12f);

            rules.Reset();

            Assert.That(rules.Phase, Is.EqualTo(LocalPrototypePhase.Waiting));
            Assert.That(rules.Outcome, Is.EqualTo(LocalPrototypeOutcome.Undecided));
            Assert.That(rules.PlayerKills, Is.Zero);
            Assert.That(rules.BotKills, Is.Zero);
            Assert.That(rules.RemainingSeconds, Is.EqualTo(90f));
        }
    }
}
