using System;

namespace Web3Fps.GameFoundation.Tournaments
{
    public enum TournamentState
    {
        Unknown,
        Open,
        Settled,
        Cancelled
    }

    [Serializable]
    public sealed class TournamentSummary
    {
        public string tournamentId = string.Empty;
        public string title = string.Empty;
        public string organizer = string.Empty;
        public string resultSubmitter = string.Empty;
        public string entryFeeWei = "0";
        public string prizePoolWei = "0";
        public int participantCount;
        public int minParticipants;
        public int maxParticipants;
        public long registrationDeadline;
        public long resultDeadline;
        public int organizerFeeBps;
        public int[] payoutBps = Array.Empty<int>();
        public string state = string.Empty;
        public bool registered;
        public string claimablePrizeWei = "0";
        public string refundableWei = "0";

        public TournamentState ParsedState
        {
            get
            {
                if (string.Equals(state, "open", StringComparison.OrdinalIgnoreCase)) return TournamentState.Open;
                if (string.Equals(state, "settled", StringComparison.OrdinalIgnoreCase)) return TournamentState.Settled;
                if (string.Equals(state, "cancelled", StringComparison.OrdinalIgnoreCase)) return TournamentState.Cancelled;
                return TournamentState.Unknown;
            }
        }
    }

    [Serializable] public sealed class TournamentList { public TournamentSummary[] items = Array.Empty<TournamentSummary>(); }

    [Serializable]
    public sealed class TransactionIntent
    {
        public string intentId = string.Empty;
        public bool requiresPlayerAction = true;
        public string actionUrl = string.Empty;
    }

    [Serializable]
    public sealed class TransactionStatus
    {
        public string state = string.Empty;
        public string txHash = string.Empty;
        public string error = string.Empty;
        public bool IsTerminal => state == "confirmed" || state == "failed" || state == "expired";
    }

    [Serializable] public sealed class SponsorIntentRequest { public string amountWei = "0"; }

    public sealed class TournamentGatewayException : Exception
    {
        public int StatusCode { get; }
        public string Code { get; }
        public TournamentGatewayException(string message, int statusCode = 0, string code = null, Exception inner = null)
            : base(message, inner) { StatusCode = statusCode; Code = code; }
    }
}
