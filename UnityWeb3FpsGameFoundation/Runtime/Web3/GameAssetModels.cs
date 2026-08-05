using System;

namespace Game.Web3
{
    [Serializable]
    public sealed class SkinItem
    {
        // uint256 values always remain decimal strings in the game client.
        public string tokenId = string.Empty;
        public uint skinDefId;
        public uint serial;
        public uint maxSupply;
        public float wear;
        public int rarity;
        public uint seasonId;
        public string bundleUri = string.Empty;
        public string contentHash = string.Empty;
        public string state = string.Empty;

        public bool IsConfirmed => string.Equals(state, "confirmed", StringComparison.Ordinal);
    }

    [Serializable]
    public sealed class PendingReward
    {
        public string rewardId = string.Empty;
        public uint skinDefId;
        public int rarity;
        public string expiresAt = string.Empty;
    }

    [Serializable]
    public sealed class PlayerAssets
    {
        public string playerId = string.Empty;
        public string wallet = string.Empty;
        public SkinItem[] items = Array.Empty<SkinItem>();
        public PendingReward[] pendingRewards = Array.Empty<PendingReward>();
        public int stalenessSeconds;

        public bool HasWallet => !string.IsNullOrWhiteSpace(wallet);
    }

    [Serializable]
    public sealed class WalletBindSession
    {
        public string sessionId = string.Empty;
        public string bindUrl = string.Empty;
        public string expiresAt = string.Empty;
    }

    [Serializable]
    public sealed class WalletBindStatus
    {
        public string state = string.Empty;
        public string wallet = string.Empty;
        public string error = string.Empty;

        public bool IsBound => string.Equals(state, "bound", StringComparison.Ordinal);
        public bool IsTerminal => !string.Equals(state, "pending", StringComparison.Ordinal);
    }

    [Serializable]
    public sealed class ClaimTicket
    {
        public string rewardId = string.Empty;
        public bool requiresPlayerAction;
        public string actionUrl = string.Empty;
    }

    [Serializable]
    public sealed class RewardStatus
    {
        public string state = string.Empty;
        public string tokenId = string.Empty;
        public string error = string.Empty;

        public bool IsTerminal => state == "claimed" || state == "failed" || state == "expired";
    }

    [Serializable]
    public sealed class LoadoutRequest
    {
        public string[] tokenIdsBySlot = Array.Empty<string>();
    }

    public sealed class GameAssetException : Exception
    {
        public int StatusCode { get; }
        public string Code { get; }

        public GameAssetException(string message, int statusCode = 0, string code = null, Exception inner = null)
            : base(message, inner)
        {
            StatusCode = statusCode;
            Code = code;
        }
    }
}
