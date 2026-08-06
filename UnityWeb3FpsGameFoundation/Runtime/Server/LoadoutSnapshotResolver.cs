using System;
using System.Threading;
using System.Threading.Tasks;

namespace Web3Fps.GameFoundation.Server
{
    /// <summary>Validates backend snapshots and converts any asset-layer failure to default cosmetics.</summary>
    public sealed class LoadoutSnapshotResolver
    {
        private readonly IEntitlementGateway _gateway;
        private readonly int _slotCount;

        public LoadoutSnapshotResolver(IEntitlementGateway gateway, int slotCount)
        {
            _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
            if (slotCount < 1) throw new ArgumentOutOfRangeException(nameof(slotCount));
            _slotCount = slotCount;
        }

        public async Task<LoadoutSnapshotResult> ResolveOrDefaultAsync(
            LoadoutEntitlementRequest request,
            CancellationToken ct = default)
        {
            ValidateRequest(request);
            try
            {
                var snapshot = await _gateway.ResolveAsync(CloneRequest(request), ct);
                ValidateSnapshot(request, snapshot);
                return new LoadoutSnapshotResult(snapshot, false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                return new LoadoutSnapshotResult(CreateDefaultSnapshot(request, "entitlement_unavailable"), true, exception);
            }
        }

        private void ValidateRequest(LoadoutEntitlementRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            Require(request.matchId, nameof(request.matchId));
            Require(request.playerId, nameof(request.playerId));
            if (request.tokenIdsBySlot == null || request.tokenIdsBySlot.Length != _slotCount)
                throw new ArgumentException("tokenIdsBySlot must contain exactly " + _slotCount + " slots", nameof(request));
            for (var i = 0; i < request.tokenIdsBySlot.Length; i++)
            {
                var tokenId = request.tokenIdsBySlot[i] ?? string.Empty;
                if (!IsDecimalStringOrEmpty(tokenId))
                    throw new ArgumentException("tokenIdsBySlot must contain only decimal strings or empty defaults", nameof(request));
            }
        }

        private void ValidateSnapshot(LoadoutEntitlementRequest request, PlayerLoadoutSnapshot snapshot)
        {
            if (snapshot == null || string.IsNullOrWhiteSpace(snapshot.snapshotId))
                throw new InvalidOperationException("Entitlement backend returned an incomplete snapshot");
            if (!string.Equals(snapshot.matchId, request.matchId, StringComparison.Ordinal) ||
                !string.Equals(snapshot.playerId, request.playerId, StringComparison.Ordinal))
                throw new InvalidOperationException("Entitlement snapshot identity does not match the request");
            if (!string.Equals(snapshot.wallet ?? string.Empty, request.wallet ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Entitlement snapshot wallet does not match the request");
            if (snapshot.createdAt < 0)
                throw new InvalidOperationException("Entitlement snapshot timestamp is invalid");
            if (snapshot.slots == null || snapshot.slots.Length != _slotCount)
                throw new InvalidOperationException("Entitlement snapshot slot count is invalid");
            for (var i = 0; i < snapshot.slots.Length; i++)
            {
                var slot = snapshot.slots[i];
                if (slot == null || slot.slot != i)
                    throw new InvalidOperationException("Entitlement snapshot slots must be ordered and contiguous");
                slot.requestedTokenId = request.tokenIdsBySlot[i] ?? string.Empty;
                slot.resolvedTokenId = slot.resolvedTokenId ?? string.Empty;
                if (!IsDecimalStringOrEmpty(slot.resolvedTokenId))
                    throw new InvalidOperationException("Resolved tokenId must be a decimal string or empty default");
                if (!string.IsNullOrEmpty(slot.resolvedTokenId) &&
                    !string.Equals(slot.resolvedTokenId, slot.requestedTokenId, StringComparison.Ordinal))
                    throw new InvalidOperationException("Resolved tokenId does not match the requested tokenId");
                if (string.IsNullOrEmpty(slot.resolvedTokenId) &&
                    !string.Equals(slot.resolution, "default", StringComparison.Ordinal))
                    throw new InvalidOperationException("An empty resolved tokenId must use the default resolution");
                if (!string.IsNullOrEmpty(slot.resolvedTokenId) &&
                    !string.Equals(slot.resolution, "confirmed", StringComparison.Ordinal))
                    throw new InvalidOperationException("A resolved tokenId must use the confirmed resolution");
            }
        }

        private PlayerLoadoutSnapshot CreateDefaultSnapshot(LoadoutEntitlementRequest request, string reason)
        {
            var slots = new ResolvedSkinSlot[_slotCount];
            for (var i = 0; i < slots.Length; i++)
            {
                slots[i] = new ResolvedSkinSlot
                {
                    slot = i,
                    requestedTokenId = request.tokenIdsBySlot[i] ?? string.Empty,
                    resolvedTokenId = string.Empty,
                    skinDefId = 0,
                    resolution = "default",
                    reason = reason
                };
            }
            return new PlayerLoadoutSnapshot
            {
                snapshotId = "fallback-" + Guid.NewGuid().ToString("N"),
                matchId = request.matchId,
                playerId = request.playerId,
                wallet = request.wallet ?? string.Empty,
                createdAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                slots = slots
            };
        }

        private static LoadoutEntitlementRequest CloneRequest(LoadoutEntitlementRequest request)
        {
            var tokens = new string[request.tokenIdsBySlot.Length];
            Array.Copy(request.tokenIdsBySlot, tokens, tokens.Length);
            return new LoadoutEntitlementRequest
            {
                matchId = request.matchId,
                playerId = request.playerId,
                wallet = request.wallet ?? string.Empty,
                tokenIdsBySlot = tokens
            };
        }

        private static void Require(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException(name + " is required", name);
        }

        private static bool IsDecimalStringOrEmpty(string value)
        {
            for (var i = 0; i < value.Length; i++)
            {
                if (value[i] < '0' || value[i] > '9') return false;
            }
            return true;
        }
    }
}
