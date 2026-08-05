using System;
using System.Threading;
using System.Threading.Tasks;
using Web3Fps.GameFoundation.Services;

namespace Web3Fps.GameFoundation.Tournaments
{
    public sealed class TournamentTransactionCoordinator
    {
        private readonly ITournamentGateway _gateway;
        private readonly IExternalUrlLauncher _urlLauncher;
        public event Action<TransactionStatus> StatusChanged;

        public TournamentTransactionCoordinator(ITournamentGateway gateway, IExternalUrlLauncher urlLauncher)
        {
            _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
            _urlLauncher = urlLauncher ?? throw new ArgumentNullException(nameof(urlLauncher));
        }

        public async Task<TransactionStatus> CompleteAsync(
            TransactionIntent intent, TimeSpan pollInterval, TimeSpan maximumDuration, CancellationToken ct = default)
        {
            if (intent == null || string.IsNullOrWhiteSpace(intent.intentId))
                throw new ArgumentException("A valid transaction intent is required", nameof(intent));
            if (intent.requiresPlayerAction)
            {
                if (string.IsNullOrWhiteSpace(intent.actionUrl))
                    throw new TournamentGatewayException("Transaction intent is missing actionUrl", 0, "invalid_response");
                _urlLauncher.Open(intent.actionUrl);
            }

            var deadline = DateTime.UtcNow + maximumDuration;
            while (DateTime.UtcNow < deadline)
            {
                var status = await _gateway.PollTransactionAsync(intent.intentId, ct);
                StatusChanged?.Invoke(status);
                if (status.IsTerminal) return status;
                await Task.Delay(pollInterval, ct);
            }

            var timeout = new TransactionStatus { state = "failed", error = "Transaction confirmation timed out" };
            StatusChanged?.Invoke(timeout);
            return timeout;
        }
    }
}
