using NUnit.Framework;
using Web3Fps.GameFoundation.Tournaments;

namespace Web3Fps.GameFoundation.Tests
{
    public sealed class TournamentModelTests
    {
        [TestCase("open", TournamentState.Open)]
        [TestCase("Settled", TournamentState.Settled)]
        [TestCase("cancelled", TournamentState.Cancelled)]
        [TestCase("bad", TournamentState.Unknown)]
        public void ParsesChainState(string value, TournamentState expected)
        {
            Assert.That(new TournamentSummary { state = value }.ParsedState, Is.EqualTo(expected));
        }
    }
}
