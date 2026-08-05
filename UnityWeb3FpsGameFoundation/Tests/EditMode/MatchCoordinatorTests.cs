using NUnit.Framework;
using Web3Fps.GameFoundation.Match;

namespace Web3Fps.GameFoundation.Tests
{
    public sealed class MatchCoordinatorTests
    {
        [Test]
        public void FinishRanksByScoreThenKills()
        {
            var match = new MatchCoordinator();
            match.RegisterPlayer("alpha", "red");
            match.RegisterPlayer("bravo", "blue");
            match.Start("m1", "tdm", "map1", "build1", 10);
            match.RecordKill("bravo", "alpha", 100);
            match.AddScore("alpha", 50);

            var result = match.Finish(20);
            Assert.That(result.players[0].playerId, Is.EqualTo("bravo"));
            Assert.That(result.players[0].placement, Is.EqualTo(1));
            Assert.That(result.players[0].result, Is.EqualTo("win"));
            Assert.That(match.Lifecycle, Is.EqualTo(MatchLifecycle.Finished));
        }

        [Test]
        public void StateCannotMutateAfterFinish()
        {
            var match = new MatchCoordinator();
            match.RegisterPlayer("alpha", "red");
            match.Start("m1", "tdm", "map1", "build1", 10);
            match.Finish(20);
            Assert.Throws<System.InvalidOperationException>(() => match.AddScore("alpha", 1));
        }
    }
}
