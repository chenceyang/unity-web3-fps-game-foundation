using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Web3Fps.GameFoundation.Server
{
    public sealed class MockEntitlementGateway : IEntitlementGateway
    {
        private readonly Dictionary<string, uint> _confirmedTokens = new Dictionary<string, uint>(StringComparer.Ordinal);
        public int LatencyMs { get; set; }
        public Exception FailureToInject { get; set; }

        public void Grant(string tokenId, uint skinDefId)
        {
            if (string.IsNullOrWhiteSpace(tokenId)) throw new ArgumentException("tokenId is required", nameof(tokenId));
            _confirmedTokens[tokenId] = skinDefId;
        }

        public async Task<PlayerLoadoutSnapshot> ResolveAsync(
            LoadoutEntitlementRequest request,
            CancellationToken ct = default)
        {
            if (LatencyMs > 0) await Task.Delay(LatencyMs, ct);
            ct.ThrowIfCancellationRequested();
            if (FailureToInject != null) throw FailureToInject;
            var slots = new ResolvedSkinSlot[request.tokenIdsBySlot.Length];
            for (var i = 0; i < slots.Length; i++)
            {
                var requested = request.tokenIdsBySlot[i] ?? string.Empty;
                uint skinDefId = 0;
                var confirmed = !string.IsNullOrWhiteSpace(requested) && _confirmedTokens.TryGetValue(requested, out skinDefId);
                slots[i] = new ResolvedSkinSlot
                {
                    slot = i,
                    requestedTokenId = requested,
                    resolvedTokenId = confirmed ? requested : string.Empty,
                    skinDefId = confirmed ? skinDefId : 0,
                    resolution = confirmed ? "confirmed" : "default",
                    reason = confirmed || string.IsNullOrWhiteSpace(requested) ? string.Empty : "not_entitled"
                };
            }
            return new PlayerLoadoutSnapshot
            {
                snapshotId = "snapshot-" + Guid.NewGuid().ToString("N"),
                matchId = request.matchId,
                playerId = request.playerId,
                wallet = request.wallet ?? string.Empty,
                createdAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                slots = slots
            };
        }
    }
}
