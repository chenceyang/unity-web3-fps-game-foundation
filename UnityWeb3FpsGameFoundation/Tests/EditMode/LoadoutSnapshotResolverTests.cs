using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Web3Fps.GameFoundation.Server;

namespace Web3Fps.GameFoundation.Tests
{
    public sealed class LoadoutSnapshotResolverTests
    {
        [Test]
        public async Task ConfirmedTokenIdRemainsAnExactString()
        {
            const string tokenId = "900719925474099312345678901234567890";
            var gateway = new MockEntitlementGateway();
            gateway.Grant(tokenId, 42);
            var resolver = new LoadoutSnapshotResolver(gateway, 2);

            var result = await resolver.ResolveOrDefaultAsync(Request(tokenId, ""));

            Assert.That(result.UsedFallback, Is.False);
            Assert.That(result.Snapshot.slots[0].resolvedTokenId, Is.EqualTo(tokenId));
            Assert.That(result.Snapshot.slots[0].skinDefId, Is.EqualTo(42));
            Assert.That(result.Snapshot.slots[1].UsesDefault, Is.True);
        }

        [Test]
        public async Task UnknownTokenUsesDefaultSlotWithoutRejectingSnapshot()
        {
            var resolver = new LoadoutSnapshotResolver(new MockEntitlementGateway(), 2);

            var result = await resolver.ResolveOrDefaultAsync(Request("123", ""));

            Assert.That(result.UsedFallback, Is.False);
            Assert.That(result.Snapshot.slots[0].UsesDefault, Is.True);
            Assert.That(result.Snapshot.slots[0].reason, Is.EqualTo("not_entitled"));
        }

        [Test]
        public async Task GatewayFailureFallsBackAllSlotsAndPreservesMatchEntry()
        {
            var gateway = new MockEntitlementGateway { FailureToInject = new InvalidOperationException("offline") };
            var resolver = new LoadoutSnapshotResolver(gateway, 2);

            var result = await resolver.ResolveOrDefaultAsync(Request("123", "456"));

            Assert.That(result.UsedFallback, Is.True);
            Assert.That(result.Error, Is.TypeOf<InvalidOperationException>());
            Assert.That(result.Snapshot.playerId, Is.EqualTo("player-1"));
            Assert.That(result.Snapshot.slots, Has.All.Property("UsesDefault").True);
        }

        [Test]
        public void NonDecimalTokenIdIsRejectedBeforeBackendLookup()
        {
            var resolver = new LoadoutSnapshotResolver(new MockEntitlementGateway(), 1);

            Assert.ThrowsAsync<ArgumentException>(
                async () => await resolver.ResolveOrDefaultAsync(Request("0x123")));
        }

        private static LoadoutEntitlementRequest Request(params string[] tokens)
        {
            return new LoadoutEntitlementRequest
            {
                matchId = "match-1",
                playerId = "player-1",
                wallet = "0xabc",
                tokenIdsBySlot = tokens
            };
        }
    }
}
