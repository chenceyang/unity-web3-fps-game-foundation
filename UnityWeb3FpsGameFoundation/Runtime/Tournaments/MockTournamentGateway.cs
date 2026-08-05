using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Web3Fps.GameFoundation.Tournaments
{
    public sealed class MockTournamentGateway : ITournamentGateway
    {
        private readonly Dictionary<string, TournamentSummary> _tournaments = new Dictionary<string, TournamentSummary>();
        private readonly Dictionary<string, int> _intentPolls = new Dictionary<string, int>();
        public int LatencyMs { get; set; } = 30;
        public TournamentGatewayException FailureToInject { get; set; }

        public MockTournamentGateway()
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            _tournaments["1"] = new TournamentSummary
            {
                tournamentId = "1", title = "Founders Arena", organizer = "0x2222222222222222222222222222222222222222",
                resultSubmitter = "0x3333333333333333333333333333333333333333",
                entryFeeWei = "10000000000000000", prizePoolWei = "320000000000000000",
                participantCount = 16, minParticipants = 8, maxParticipants = 32,
                registrationDeadline = now + 86400, resultDeadline = now + 172800,
                organizerFeeBps = 500, payoutBps = new[] { 6000, 3000, 1000 }, state = "open"
            };
        }

        public async Task<TournamentList> GetTournamentsAsync(CancellationToken ct = default)
        {
            await Simulate(ct);
            var values = new TournamentSummary[_tournaments.Count];
            _tournaments.Values.CopyTo(values, 0);
            return new TournamentList { items = values };
        }

        public async Task<TournamentSummary> GetTournamentAsync(string tournamentId, CancellationToken ct = default)
        {
            await Simulate(ct);
            TournamentSummary value;
            if (_tournaments.TryGetValue(tournamentId, out value)) return value;
            throw new TournamentGatewayException("Tournament not found", 404, "not_found");
        }

        public Task<TransactionIntent> BeginRegisterAsync(string id, CancellationToken ct = default) => Intent(id, "register", ct);
        public Task<TransactionIntent> BeginSponsorAsync(string id, string amountWei, CancellationToken ct = default) => Intent(id, "sponsor", ct);
        public Task<TransactionIntent> BeginClaimPrizeAsync(string id, CancellationToken ct = default) => Intent(id, "prize", ct);
        public Task<TransactionIntent> BeginClaimRefundAsync(string id, CancellationToken ct = default) => Intent(id, "refund", ct);

        public async Task<TransactionStatus> PollTransactionAsync(string intentId, CancellationToken ct = default)
        {
            await Simulate(ct);
            if (!_intentPolls.ContainsKey(intentId)) throw new TournamentGatewayException("Intent not found", 404, "not_found");
            var polls = ++_intentPolls[intentId];
            return polls < 2
                ? new TransactionStatus { state = "pending" }
                : new TransactionStatus { state = "confirmed", txHash = "0x" + new string('1', 64) };
        }

        private async Task<TransactionIntent> Intent(string tournamentId, string action, CancellationToken ct)
        {
            await GetTournamentAsync(tournamentId, ct);
            var intentId = action + "_" + Guid.NewGuid().ToString("N").Substring(0, 10);
            _intentPolls[intentId] = 0;
            return new TransactionIntent
            {
                intentId = intentId, requiresPlayerAction = true,
                actionUrl = "https://example.invalid/tournaments/" + tournamentId + "/" + action
            };
        }

        private async Task Simulate(CancellationToken ct)
        {
            if (LatencyMs > 0) await Task.Delay(LatencyMs, ct);
            if (FailureToInject != null) throw FailureToInject;
        }
    }
}
