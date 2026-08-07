using System;
using System.Threading;
using System.Threading.Tasks;
using Game.Web3;

namespace Web3Fps.GameFoundation.Services
{
    public sealed class RewardClaimCoordinator
    {
        private readonly IGameAssetGateway _gateway;
        private readonly IExternalUrlLauncher _urlLauncher;

        public event Action<RewardStatus> StatusChanged;

        public RewardClaimCoordinator(IGameAssetGateway gateway, IExternalUrlLauncher urlLauncher)
        {
            _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
            _urlLauncher = urlLauncher ?? throw new ArgumentNullException(nameof(urlLauncher));
        }

        public async Task<RewardStatus> ClaimAsync(
            string rewardId, TimeSpan pollInterval, TimeSpan maximumDuration, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(rewardId)) throw new ArgumentException("rewardId is required", nameof(rewardId));
            if (pollInterval <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(pollInterval));
            if (maximumDuration <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(maximumDuration));
            var ticket = await _gateway.RequestClaimAsync(rewardId, ct);
            if (ticket != null && ticket.requiresPlayerAction)
            {
                if (string.IsNullOrWhiteSpace(ticket.actionUrl))
                    throw new GameAssetException("Claim requires player action but no URL was returned", 0, "invalid_response");
                _urlLauncher.Open(ticket.actionUrl);
            }

            var deadline = DateTime.UtcNow + maximumDuration;
            while (DateTime.UtcNow < deadline)
            {
                var status = await _gateway.PollRewardAsync(rewardId, ct);
                if (status != null) // A blank 2xx body deserializes to null; treat as transient.
                {
                    StatusChanged?.Invoke(status);
                    if (status.IsTerminal) return status;
                }
                await Task.Delay(pollInterval, ct);
            }

            var timeout = new RewardStatus { state = "failed", error = "Reward confirmation timed out" };
            StatusChanged?.Invoke(timeout);
            return timeout;
        }
    }
}
