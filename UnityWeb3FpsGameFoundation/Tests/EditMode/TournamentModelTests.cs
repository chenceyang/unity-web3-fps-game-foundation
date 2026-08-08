using Game.Web3;
using NUnit.Framework;

namespace Web3Fps.GameFoundation.Tests
{
    public sealed class TournamentModelTests
    {
        [TestCase("open", false)]
        [TestCase("settled", true)]
        [TestCase("cancelled", true)]
        [TestCase("bad", false)]
        public void TerminalStatusesAreSettledAndCancelled(string status, bool expected)
        {
            Assert.That(TournamentStatus.IsTerminal(status), Is.EqualTo(expected));
        }

        [Test]
        public void OpenTournamentWithFreeSlotsIsJoinable()
        {
            var tournament = new TournamentSummary
            {
                status = TournamentStatus.Open,
                participantCount = 5,
                maxParticipants = 8
            };
            Assert.That(tournament.IsOpen, Is.True);
            Assert.That(tournament.IsFull, Is.False);
        }

        [Test]
        public void AmountKeepsWeiAsDecimalStringAndFormatsForDisplay()
        {
            var amount = new Amount { wei = "1000000000000000000", formatted = "1.0", symbol = "MON" };
            Assert.That(amount.wei, Is.EqualTo("1000000000000000000"));
            Assert.That(amount.ToString(), Is.EqualTo("1.0 MON"));
        }
    }
}
