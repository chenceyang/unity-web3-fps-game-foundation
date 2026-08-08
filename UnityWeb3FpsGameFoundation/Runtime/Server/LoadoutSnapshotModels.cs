using System;
using System.Threading;
using System.Threading.Tasks;

namespace Web3Fps.GameFoundation.Server
{
    [Serializable]
    public sealed class LoadoutEntitlementRequest
    {
        public string matchId = string.Empty;
        public string playerId = string.Empty;
        public string wallet = string.Empty;
        public string[] tokenIdsBySlot = Array.Empty<string>();
    }

    [Serializable]
    public sealed class ResolvedSkinSlot
    {
        public int slot;
        public string requestedTokenId = string.Empty;
        public string resolvedTokenId = string.Empty;
        public uint skinDefId;
        public string contentHash = string.Empty;
        public string resolution = "default";
        public string reason = string.Empty;

        public bool UsesDefault => string.IsNullOrWhiteSpace(resolvedTokenId);
    }

    [Serializable]
    public sealed class PlayerLoadoutSnapshot
    {
        public string snapshotId = string.Empty;
        public string matchId = string.Empty;
        public string playerId = string.Empty;
        public string wallet = string.Empty;
        public long createdAt;
        public ResolvedSkinSlot[] slots = Array.Empty<ResolvedSkinSlot>();
    }

    public sealed class LoadoutSnapshotResult
    {
        public PlayerLoadoutSnapshot Snapshot { get; }
        public bool UsedFallback { get; }
        public Exception Error { get; }

        public LoadoutSnapshotResult(PlayerLoadoutSnapshot snapshot, bool usedFallback, Exception error = null)
        {
            Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
            UsedFallback = usedFallback;
            Error = error;
        }
    }

    /// <summary>Dedicated-server-only seam to the game backend entitlement endpoint.</summary>
    public interface IEntitlementGateway
    {
        Task<PlayerLoadoutSnapshot> ResolveAsync(LoadoutEntitlementRequest request, CancellationToken ct = default);
    }
}
