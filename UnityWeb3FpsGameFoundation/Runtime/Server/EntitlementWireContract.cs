using System;
using UnityEngine;

namespace Web3Fps.GameFoundation.Server
{
    // Wire DTOs mirroring web3-fps-assets backend/src/routes/entitlement.ts and
    // api/openapi.yaml (/internal/v1/entitlement-check). The request field is
    // "tokenIds" (not tokenIdsBySlot) and the response is the EntitlementResult
    // shape, which the mapper converts into the package's PlayerLoadoutSnapshot.
    [Serializable]
    public sealed class EntitlementWireRequest
    {
        public string playerId = string.Empty;
        public string matchId = string.Empty;
        public string wallet = string.Empty;
        public string[] tokenIds = Array.Empty<string>();
    }

    [Serializable]
    public sealed class EntitlementWireResolvedSkin
    {
        public int slot;
        public string tokenId = string.Empty;
        public uint skinDefId;
        public string contentHash = string.Empty;
        public bool isDefault = true;
    }

    [Serializable]
    public sealed class EntitlementWireRejectedToken
    {
        public string tokenId = string.Empty;
        public string reason = string.Empty;
    }

    [Serializable]
    public sealed class EntitlementWireResponse
    {
        public bool allowed;
        public string snapshotId = string.Empty;
        public EntitlementWireResolvedSkin[] resolvedSkins = Array.Empty<EntitlementWireResolvedSkin>();
        public EntitlementWireRejectedToken[] rejectedTokenIds = Array.Empty<EntitlementWireRejectedToken>();
        public int cacheAgeSeconds;
        public bool degraded;
    }

    public static class EntitlementWireMapper
    {
        public static string ToRequestJson(LoadoutEntitlementRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var tokens = new string[request.tokenIdsBySlot.Length];
            for (var i = 0; i < tokens.Length; i++) tokens[i] = request.tokenIdsBySlot[i] ?? string.Empty;
            return JsonUtility.ToJson(new EntitlementWireRequest
            {
                playerId = request.playerId,
                matchId = request.matchId,
                wallet = request.wallet ?? string.Empty,
                tokenIds = tokens
            });
        }

        // The backend response omits matchId/playerId/wallet/createdAt; they are echoed
        // from the request so LoadoutSnapshotResolver can validate snapshot identity.
        public static PlayerLoadoutSnapshot ToSnapshot(
            EntitlementWireResponse response,
            LoadoutEntitlementRequest request,
            long createdAt)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (response == null || string.IsNullOrWhiteSpace(response.snapshotId) || response.resolvedSkins == null)
                throw new InvalidOperationException("Entitlement backend returned an incomplete response");
            if (!response.allowed)
                throw new InvalidOperationException("Entitlement backend refused the match; the contract never blocks");
            var slotCount = request.tokenIdsBySlot.Length;
            if (response.resolvedSkins.Length != slotCount)
                throw new InvalidOperationException("Entitlement response slot count does not match the request");

            var slots = new ResolvedSkinSlot[slotCount];
            for (var i = 0; i < response.resolvedSkins.Length; i++)
            {
                var wire = response.resolvedSkins[i];
                if (wire == null || wire.slot != i)
                    throw new InvalidOperationException("Entitlement response slots must be ordered and contiguous");
                var requested = request.tokenIdsBySlot[i] ?? string.Empty;
                var resolvedTokenId = wire.isDefault ? string.Empty : wire.tokenId ?? string.Empty;
                if (!wire.isDefault && string.IsNullOrWhiteSpace(resolvedTokenId))
                    throw new InvalidOperationException("A non-default entitlement slot must carry its tokenId");
                slots[i] = new ResolvedSkinSlot
                {
                    slot = i,
                    requestedTokenId = requested,
                    resolvedTokenId = resolvedTokenId,
                    skinDefId = wire.isDefault ? 0 : wire.skinDefId,
                    contentHash = wire.contentHash ?? string.Empty,
                    resolution = wire.isDefault ? "default" : "confirmed",
                    reason = ResolveReason(response, requested, wire.isDefault)
                };
            }

            return new PlayerLoadoutSnapshot
            {
                snapshotId = response.snapshotId,
                matchId = request.matchId,
                playerId = request.playerId,
                wallet = request.wallet ?? string.Empty,
                createdAt = createdAt < 0 ? 0 : createdAt,
                slots = slots
            };
        }

        private static string ResolveReason(EntitlementWireResponse response, string requestedTokenId, bool isDefault)
        {
            if (!isDefault || string.IsNullOrEmpty(requestedTokenId)) return string.Empty;
            if (response.rejectedTokenIds != null)
            {
                for (var i = 0; i < response.rejectedTokenIds.Length; i++)
                {
                    var rejected = response.rejectedTokenIds[i];
                    if (rejected != null && string.Equals(rejected.tokenId, requestedTokenId, StringComparison.Ordinal))
                        return rejected.reason ?? string.Empty;
                }
            }
            return response.degraded ? "degraded" : string.Empty;
        }
    }
}
