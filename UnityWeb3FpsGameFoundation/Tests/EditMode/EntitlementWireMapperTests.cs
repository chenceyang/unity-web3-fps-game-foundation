using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Web3Fps.GameFoundation.Server;

namespace Web3Fps.GameFoundation.Tests
{
    public sealed class EntitlementWireMapperTests
    {
        [Test]
        public void RequestJsonUsesTheBackendFieldNames()
        {
            var json = EntitlementWireMapper.ToRequestJson(Request("12345", ""));

            Assert.That(json, Does.Contain("\"playerId\":\"player-1\""));
            Assert.That(json, Does.Contain("\"matchId\":\"match-1\""));
            Assert.That(json, Does.Contain("\"tokenIds\":[\"12345\",\"\"]"));
            Assert.That(json, Does.Not.Contain("tokenIdsBySlot"));
        }

        [Test]
        public void ConfirmedAndRejectedSlotsMapIntoTheSnapshotContract()
        {
            var response = new EntitlementWireResponse
            {
                allowed = true,
                snapshotId = "snap_1",
                resolvedSkins = new[]
                {
                    new EntitlementWireResolvedSkin
                    {
                        slot = 0, tokenId = "12345", skinDefId = 1042,
                        contentHash = "0xaaaa", isDefault = false
                    },
                    new EntitlementWireResolvedSkin { slot = 1, skinDefId = 0, contentHash = "0x0", isDefault = true }
                },
                rejectedTokenIds = new[]
                {
                    new EntitlementWireRejectedToken { tokenId = "678", reason = "not_owned" }
                }
            };

            var snapshot = EntitlementWireMapper.ToSnapshot(response, Request("12345", "678"), 100);

            Assert.That(snapshot.snapshotId, Is.EqualTo("snap_1"));
            Assert.That(snapshot.matchId, Is.EqualTo("match-1"));
            Assert.That(snapshot.playerId, Is.EqualTo("player-1"));
            Assert.That(snapshot.slots[0].resolvedTokenId, Is.EqualTo("12345"));
            Assert.That(snapshot.slots[0].resolution, Is.EqualTo("confirmed"));
            Assert.That(snapshot.slots[0].skinDefId, Is.EqualTo(1042));
            Assert.That(snapshot.slots[0].contentHash, Is.EqualTo("0xaaaa"));
            Assert.That(snapshot.slots[1].UsesDefault, Is.True);
            Assert.That(snapshot.slots[1].reason, Is.EqualTo("not_owned"));
        }

        [Test]
        public void DegradedDefaultSlotCarriesTheDegradedReason()
        {
            var response = new EntitlementWireResponse
            {
                allowed = true,
                snapshotId = "snap_2",
                degraded = true,
                resolvedSkins = new[]
                {
                    new EntitlementWireResolvedSkin { slot = 0, skinDefId = 0, isDefault = true }
                },
                rejectedTokenIds = Array.Empty<EntitlementWireRejectedToken>()
            };

            var snapshot = EntitlementWireMapper.ToSnapshot(response, Request("12345"), 100);

            Assert.That(snapshot.slots[0].UsesDefault, Is.True);
            Assert.That(snapshot.slots[0].reason, Is.EqualTo("degraded"));
        }

        [Test]
        public void RefusedOrIncompleteResponsesAreRejected()
        {
            Assert.Throws<InvalidOperationException>(() => EntitlementWireMapper.ToSnapshot(
                new EntitlementWireResponse { allowed = false, snapshotId = "snap_3" }, Request(""), 100));
            Assert.Throws<InvalidOperationException>(() => EntitlementWireMapper.ToSnapshot(
                new EntitlementWireResponse { allowed = true, snapshotId = " " }, Request(""), 100));
        }

        [Test]
        public async Task MappedSnapshotPassesResolverValidation()
        {
            var resolver = new LoadoutSnapshotResolver(new WireEchoGateway(), 2);

            var result = await resolver.ResolveOrDefaultAsync(Request("12345", ""));

            Assert.That(result.UsedFallback, Is.False, result.Error?.Message);
            Assert.That(result.Snapshot.slots[0].resolution, Is.EqualTo("confirmed"));
            Assert.That(result.Snapshot.slots[1].UsesDefault, Is.True);
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

        // Simulates HttpEntitlementGateway: builds the backend wire response and maps
        // it back, proving the echoed identity satisfies resolver validation.
        private sealed class WireEchoGateway : IEntitlementGateway
        {
            public Task<PlayerLoadoutSnapshot> ResolveAsync(
                LoadoutEntitlementRequest request,
                CancellationToken ct = default)
            {
                var skins = new EntitlementWireResolvedSkin[request.tokenIdsBySlot.Length];
                for (var i = 0; i < skins.Length; i++)
                {
                    var requested = request.tokenIdsBySlot[i];
                    skins[i] = new EntitlementWireResolvedSkin
                    {
                        slot = i,
                        tokenId = requested,
                        skinDefId = string.IsNullOrEmpty(requested) ? 0u : 1042u,
                        contentHash = "0xaaaa",
                        isDefault = string.IsNullOrEmpty(requested)
                    };
                }
                var response = new EntitlementWireResponse
                {
                    allowed = true,
                    snapshotId = "snap_wire",
                    resolvedSkins = skins,
                    rejectedTokenIds = Array.Empty<EntitlementWireRejectedToken>()
                };
                return Task.FromResult(EntitlementWireMapper.ToSnapshot(response, request, 100));
            }
        }
    }
}
