using Game.Web3;
using NUnit.Framework;
using Web3Fps.GameFoundation.Formal;
using Web3Fps.GameFoundation.Match;
using Web3Fps.GameFoundation.Server;

namespace Web3Fps.GameFoundation.Tests
{
    public sealed class FormalPagesTests
    {
        [Test]
        public void ConfirmSlotLinesExplainEveryOwnershipState()
        {
            var confirmed = new SkinItem { tokenId = "42", name = "Frostbite AK-47", skinDefId = 1042, state = "confirmed" };
            var pending = new SkinItem { tokenId = "43", name = "Solar Flare AWP", skinDefId = 1077, state = "pending" };

            Assert.That(FormalMatchConfirmView.DescribeSlot(0, "", null), Does.Contain("DEFAULT ISSUE"));
            Assert.That(FormalMatchConfirmView.DescribeSlot(0, "42", confirmed), Does.Contain("CONFIRMED"));
            Assert.That(FormalMatchConfirmView.DescribeSlot(1, "43", pending), Does.Contain("PENDING — DEFAULT THIS MATCH"));
            Assert.That(FormalMatchConfirmView.DescribeSlot(2, "44", null), Does.Contain("NOT OWNED"));
        }

        [Test]
        public void PublishOutcomesRenderDistinctLobbyVisibleText()
        {
            var hash = "0x" + new string('a', 64);

            Assert.That(FormalPostMatchView.DescribePublish(
                    new LocalMatchPublishReport("m", MatchPublishOutcome.Published, hash, "")),
                Does.Contain("PUBLISHED"));
            Assert.That(FormalPostMatchView.DescribePublish(
                    new LocalMatchPublishReport("m", MatchPublishOutcome.Duplicate, hash, "")),
                Does.Contain("ALREADY PUBLISHED"));
            Assert.That(FormalPostMatchView.DescribePublish(
                    new LocalMatchPublishReport("m", MatchPublishOutcome.Conflict, hash, "")),
                Does.Contain("CONFLICT"));
            Assert.That(FormalPostMatchView.DescribePublish(
                    new LocalMatchPublishReport("m", MatchPublishOutcome.Failed, hash, "")),
                Does.Contain("SCORE UNAFFECTED"));
            Assert.That(FormalPostMatchView.DescribePublish(
                    new LocalMatchPublishReport("m", MatchPublishOutcome.NotAttempted, "", "")),
                Does.Contain("KEPT LOCALLY"));
        }

        [Test]
        public void AttestationFailureNeverHidesTheScoreboardMessage()
        {
            var failed = new MatchRecord
            {
                matchId = "m_demo_failed",
                attestation = new MatchAttestationInfo { state = AttestationState.Failed }
            };
            var attested = new MatchRecord
            {
                matchId = "m_demo_1",
                attestation = new MatchAttestationInfo
                {
                    state = AttestationState.Attested,
                    txHash = "0x" + new string('b', 64)
                }
            };

            Assert.That(FormalPostMatchView.DescribeAttestation(failed), Does.Contain("SCORE REMAINS VALID"));
            Assert.That(FormalPostMatchView.DescribeAttestation(attested), Does.Contain("VERIFIED"));
            Assert.That(FormalPostMatchView.DescribeAttestation(null), Does.Contain("SCORE REMAINS VALID"));
        }

        [Test]
        public void PlacementLineReadsTheLocalParticipantResult()
        {
            var result = new MatchResult
            {
                players = new[]
                {
                    new MatchPlayerResult { playerId = "local-player", placement = 1, kills = 7, score = 700 },
                    new MatchPlayerResult { playerId = "prototype-bot", placement = 2, kills = 2, score = 200 }
                }
            };

            Assert.That(FormalPostMatchView.DescribePlacement(result, "local-player"), Does.Contain("#1"));
            Assert.That(FormalPostMatchView.DescribePlacement(result, "missing"), Is.EqualTo("PLACEMENT —"));
        }

        [Test]
        public void AssetDetailExplainsStatesAndShortensHashes()
        {
            Assert.That(FormalAssetDetailView.DescribeState("confirmed"), Does.Contain("EQUIPPABLE"));
            Assert.That(FormalAssetDetailView.DescribeState("pending"), Does.Contain("CANNOT ENTER A FORMAL LOADOUT"));
            var hash = "0x" + new string('c', 64);
            var shortened = FormalAssetDetailView.ShortenHash(hash);
            Assert.That(shortened.Length, Is.LessThan(hash.Length));
            Assert.That(shortened, Does.StartWith("0x"));
            Assert.That(FormalAssetDetailView.ShortenHash(""), Is.EqualTo("—"));
        }

        [Test]
        public void ProofFeedSurfacesTheLastPublishOutcome()
        {
            var report = new LocalMatchPublishReport(
                "m", MatchPublishOutcome.Published, "0x" + new string('d', 64), "");

            Assert.That(FormalLobbyView.DescribeProofFeed(null, report), Does.Contain("LAST MATCH"));
            Assert.That(FormalLobbyView.DescribeProofFeed(null, null), Does.Contain("VERIFICATION"));
        }
    }
}
