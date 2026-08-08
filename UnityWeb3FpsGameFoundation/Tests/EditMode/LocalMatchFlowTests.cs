using System;
using System.Threading.Tasks;
using Game.Web3;
using NUnit.Framework;
using Web3Fps.GameFoundation.Match;
using Web3Fps.GameFoundation.Server;

namespace Web3Fps.GameFoundation.Tests
{
    public sealed class LocalMatchFlowTests
    {
        [Test]
        public void EntitlementRequestPreservesDecimalTokenStrings()
        {
            const string tokenId = "900719925474099312345678901234567890";

            var request = LocalMatchFlow.CreateEntitlementRequest("m-1", "p-1", "0xabc", new[] { tokenId, null, "" });

            Assert.That(request.tokenIdsBySlot, Is.EqualTo(new[] { tokenId, "", "" }));
            Assert.That(request.matchId, Is.EqualTo("m-1"));
            Assert.That(request.playerId, Is.EqualTo("p-1"));
        }

        [Test]
        public async Task MockChainViewSyncLetsConfirmedItemsResolveAndPendingItemsDefault()
        {
            var gateway = new MockEntitlementGateway();
            LocalMatchFlow.SyncMockChainView(gateway, new PlayerAssets
            {
                items = new[]
                {
                    new SkinItem { tokenId = "4475355550000000037", skinDefId = 1042, state = "confirmed" },
                    new SkinItem { tokenId = "4337916970000000214", skinDefId = 1010, state = "pending" }
                }
            });
            var resolver = new LoadoutSnapshotResolver(gateway, 2);
            var request = LocalMatchFlow.CreateEntitlementRequest(
                "m-1", "p-1", "", new[] { "4475355550000000037", "4337916970000000214" });

            var ticket = await LocalMatchFlow.FreezeLoadoutAsync(resolver, request);

            Assert.That(ticket.UsedFallback, Is.False);
            Assert.That(ticket.Snapshot.slots[0].skinDefId, Is.EqualTo(1042));
            Assert.That(ticket.Snapshot.slots[1].UsesDefault, Is.True);
            Assert.That(LocalMatchFlow.HasDegradedSlots(ticket.Snapshot), Is.True);
        }

        [Test]
        public async Task GatewayFailureFreezesDefaultTicketWithoutThrowing()
        {
            var gateway = new MockEntitlementGateway { FailureToInject = new InvalidOperationException("offline") };
            var resolver = new LoadoutSnapshotResolver(gateway, 2);
            var request = LocalMatchFlow.CreateEntitlementRequest("m-2", "p-1", "", new[] { "123", "" });

            var ticket = await LocalMatchFlow.FreezeLoadoutAsync(resolver, request);

            Assert.That(ticket.UsedFallback, Is.True);
            Assert.That(ticket.Snapshot.slots, Has.All.Property("UsesDefault").True);
        }

        [Test]
        public void OfflineTicketProvidesDefaultSlotsForEveryCosmetic()
        {
            var ticket = LocalMatchFlow.CreateOfflineTicket(3);

            Assert.That(ticket.Snapshot.slots, Has.Length.EqualTo(3));
            Assert.That(ticket.Snapshot.slots, Has.All.Property("UsesDefault").True);
            Assert.That(LocalMatchFlow.HasDegradedSlots(ticket.Snapshot), Is.False);
        }

        [Test]
        public void WinnerRewardSlotIsDerivedDeterministically()
        {
            var result = Result("m-3", "passed");

            var first = LocalMatchFlow.BuildRewardSlots(result);
            var second = LocalMatchFlow.BuildRewardSlots(result);

            Assert.That(first, Has.Length.EqualTo(1));
            Assert.That(first[0].slot, Is.EqualTo(0));
            Assert.That(first[0].playerId, Is.EqualTo("alpha"));
            Assert.That(first[0].rewardId, Is.EqualTo("rw_m-3_0"));
            Assert.That(second[0].rewardId, Is.EqualTo(first[0].rewardId));
        }

        [Test]
        public void HeldAntiCheatStateProducesNoRewardSlots()
        {
            Assert.That(LocalMatchFlow.BuildRewardSlots(Result("m-4", "held")), Is.Empty);
        }

        [Test]
        public void LocalIdentityMappingRewritesTheParticipantOnly()
        {
            var result = Result("m-5", "passed");

            var mapped = LocalMatchFlow.WithLocalIdentity(result, "alpha", "operator-01", "0xwallet");

            Assert.That(mapped.players[0].playerId, Is.EqualTo("operator-01"));
            Assert.That(mapped.players[0].wallet, Is.EqualTo("0xwallet"));
            Assert.That(mapped.players[1].playerId, Is.EqualTo("bravo"));
            Assert.That(result.players[0].playerId, Is.EqualTo("alpha"), "source result must stay untouched");
        }

        [Test]
        public void LocalIdentityMappingSkipsCollidingAccountIds()
        {
            var result = Result("m-6", "passed");

            var mapped = LocalMatchFlow.WithLocalIdentity(result, "alpha", "bravo", "0xwallet");

            Assert.That(mapped, Is.SameAs(result));
        }

        [Test]
        public async Task PublishOutcomeMapsPublishedThenDuplicate()
        {
            var coordinator = new MatchPublishCoordinator(new MockMatchResultPublisher());
            var payload = MatchResultHasher.CreatePayload(Result("m-7", "passed"));

            var first = await LocalMatchFlow.PublishAsync(coordinator, payload);
            var second = await LocalMatchFlow.PublishAsync(coordinator, payload);

            Assert.That(first.Outcome, Is.EqualTo(MatchPublishOutcome.Published));
            Assert.That(second.Outcome, Is.EqualTo(MatchPublishOutcome.Duplicate));
            Assert.That(first.ResultHash, Does.StartWith("0x"));
        }

        [Test]
        public async Task PublishOutcomeMapsBackendConflict()
        {
            var publisher = new MockMatchResultPublisher();
            await publisher.PublishAsync(MatchResultHasher.CreatePayload(Result("m-8", "passed", endedAt: 200)));
            var coordinator = new MatchPublishCoordinator(publisher);

            var report = await LocalMatchFlow.PublishAsync(
                coordinator, MatchResultHasher.CreatePayload(Result("m-8", "passed", endedAt: 300)));

            Assert.That(report.Outcome, Is.EqualTo(MatchPublishOutcome.Conflict));
        }

        [Test]
        public async Task PublishOutcomeMapsTransientFailureAndMissingPublisher()
        {
            var publisher = new MockMatchResultPublisher { FailureToInject = new InvalidOperationException("http 500") };
            var coordinator = new MatchPublishCoordinator(publisher);
            var payload = MatchResultHasher.CreatePayload(Result("m-9", "passed"));

            var failed = await LocalMatchFlow.PublishAsync(coordinator, payload);
            var skipped = await LocalMatchFlow.PublishAsync(null, payload);

            Assert.That(failed.Outcome, Is.EqualTo(MatchPublishOutcome.Failed));
            Assert.That(skipped.Outcome, Is.EqualTo(MatchPublishOutcome.NotAttempted));
        }

        private static MatchResult Result(string matchId, string antiCheatState, long endedAt = 200)
        {
            return new MatchResult
            {
                matchId = matchId,
                modeId = "tdm",
                mapId = "rift-relay",
                serverBuild = "build-1",
                startedAt = 100,
                endedAt = endedAt,
                antiCheatState = antiCheatState,
                players = new[]
                {
                    new MatchPlayerResult
                    {
                        playerId = "alpha", teamId = "cobalt", kills = 7, deaths = 2,
                        score = 700, placement = 1, result = "win"
                    },
                    new MatchPlayerResult
                    {
                        playerId = "bravo", teamId = "coral", kills = 2, deaths = 7,
                        score = 200, placement = 2, result = "loss"
                    }
                }
            };
        }
    }
}
