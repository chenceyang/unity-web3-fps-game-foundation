using System;
using System.Threading;
using System.Threading.Tasks;
using Game.Web3;

namespace Web3Fps.GameFoundation.Services
{
    public sealed class WalletBindingCoordinator
    {
        private readonly IGameAssetGateway _gateway;
        private readonly IExternalUrlLauncher _urlLauncher;

        public event Action<WalletBindStatus> StatusChanged;

        public WalletBindingCoordinator(IGameAssetGateway gateway, IExternalUrlLauncher urlLauncher)
        {
            _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
            _urlLauncher = urlLauncher ?? throw new ArgumentNullException(nameof(urlLauncher));
        }

        public async Task<WalletBindStatus> BindAsync(
            TimeSpan pollInterval, TimeSpan maximumDuration, CancellationToken ct = default)
        {
            if (pollInterval <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(pollInterval));
            if (maximumDuration <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(maximumDuration));

            var session = await _gateway.BeginWalletBindAsync(ct);
            if (session == null || string.IsNullOrWhiteSpace(session.sessionId) || string.IsNullOrWhiteSpace(session.bindUrl))
                throw new GameAssetException("Wallet bind endpoint returned an incomplete session", 0, "invalid_response");

            _urlLauncher.Open(session.bindUrl); // Always the system browser implementation in production.
            var deadline = DateTime.UtcNow + maximumDuration;
            var pending = new WalletBindStatus { state = "pending" };
            StatusChanged?.Invoke(pending);

            while (DateTime.UtcNow < deadline)
            {
                await Task.Delay(pollInterval, ct);
                var status = await _gateway.PollWalletBindAsync(session.sessionId, ct);
                if (status == null) continue; // A blank 2xx body deserializes to null; treat as transient.
                StatusChanged?.Invoke(status);
                if (status.IsTerminal) return status;
            }

            var expired = new WalletBindStatus { state = "expired", error = "Wallet binding timed out" };
            StatusChanged?.Invoke(expired);
            return expired;
        }
    }
}
