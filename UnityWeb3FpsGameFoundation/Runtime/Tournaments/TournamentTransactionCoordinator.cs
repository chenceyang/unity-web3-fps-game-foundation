using System;
using System.Threading;
using System.Threading.Tasks;
using Game.Web3;
using Web3Fps.GameFoundation.Services;

namespace Web3Fps.GameFoundation.Tournaments
{
    /// <summary>
    /// Creates a tournament transaction intent and hands its actionUrl to the system
    /// browser. All tournament money movement is an on-chain transaction signed
    /// outside the game, so there is nothing to poll here: the lobby refreshes
    /// tournament state from the backend afterwards and the contract stays the
    /// source of truth.
    /// </summary>
    public sealed class TournamentTransactionCoordinator
    {
        private readonly ITournamentGateway _gateway;
        private readonly IExternalUrlLauncher _urlLauncher;

        public event Action<TournamentIntent> IntentLaunched;

        public TournamentTransactionCoordinator(ITournamentGateway gateway, IExternalUrlLauncher urlLauncher)
        {
            _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
            _urlLauncher = urlLauncher ?? throw new ArgumentNullException(nameof(urlLauncher));
        }

        /// <param name="action">One of the <see cref="TournamentAction"/> constants.</param>
        public async Task<TournamentIntent> LaunchAsync(
            string tournamentId, string action, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(tournamentId))
                throw new ArgumentException("tournamentId is required", nameof(tournamentId));
            if (string.IsNullOrWhiteSpace(action))
                throw new ArgumentException("action is required", nameof(action));

            var intent = await _gateway.CreateIntentAsync(tournamentId, action, ct);
            if (intent == null || string.IsNullOrWhiteSpace(intent.actionUrl))
                throw new GameAssetException("Tournament intent is missing actionUrl", 0, "invalid_response");

            _urlLauncher.Open(intent.actionUrl); // Always the system browser in production.
            IntentLaunched?.Invoke(intent);
            return intent;
        }
    }
}
