using System;

namespace Web3Fps.GameFoundation.Match
{
    [Serializable]
    public sealed class MatchPlayerResult
    {
        public string playerId = string.Empty;
        public string wallet = string.Empty;
        public string teamId = string.Empty;
        public int kills;
        public int deaths;
        public int assists;
        public long score;
        public int placement;
        public string result = string.Empty;
    }

    [Serializable]
    public sealed class MatchRewardSlot
    {
        // uint8 in the backend schema and the on-chain requestId encoding
        // (backend/src/routes/matches.ts): an integer, never a string.
        public int slot;
        public string playerId = string.Empty;
        public string rewardId = string.Empty;
    }

    [Serializable]
    public sealed class MatchResult
    {
        public string version = "1.0";
        public string matchId = string.Empty;
        public string modeId = string.Empty;
        public string mapId = string.Empty;
        public long startedAt;
        public long endedAt;
        public string serverBuild = string.Empty;
        public string tournamentId = string.Empty;
        public string antiCheatState = "passed";
        public MatchPlayerResult[] players = Array.Empty<MatchPlayerResult>();
        public MatchRewardSlot[] rewardSlots = Array.Empty<MatchRewardSlot>();
    }

    public sealed class MatchAttestationPayload
    {
        public MatchResult Result { get; }
        public byte[] CanonicalUtf8 { get; }
        public string MatchIdKey { get; }
        public string ResultHash { get; }

        public MatchAttestationPayload(MatchResult result, byte[] canonicalUtf8, string matchIdKey, string resultHash)
        {
            Result = result ?? throw new ArgumentNullException(nameof(result));
            CanonicalUtf8 = canonicalUtf8 ?? throw new ArgumentNullException(nameof(canonicalUtf8));
            MatchIdKey = matchIdKey ?? throw new ArgumentNullException(nameof(matchIdKey));
            ResultHash = resultHash ?? throw new ArgumentNullException(nameof(resultHash));
        }
    }
}
