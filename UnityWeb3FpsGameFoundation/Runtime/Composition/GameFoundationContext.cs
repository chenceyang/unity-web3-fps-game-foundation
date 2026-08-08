using System;
using Game.Web3;
using Web3Fps.GameFoundation.Auth;
using Web3Fps.GameFoundation.Match;
using Web3Fps.GameFoundation.Networking;
using Web3Fps.GameFoundation.Server;
using Web3Fps.GameFoundation.Services;
using Web3Fps.GameFoundation.Tournaments;

namespace Web3Fps.GameFoundation.Composition
{
    public sealed class GameFoundationContext
    {
        public IGameAssetGateway AssetGateway { get; }
        public ITournamentGateway TournamentGateway { get; }
        public IExternalUrlLauncher UrlLauncher { get; }
        public GameAssetService Assets { get; }
        public LoadoutService Loadout { get; }
        public WalletBindingCoordinator WalletBinding { get; }
        public RewardClaimCoordinator Rewards { get; }
        public TournamentTransactionCoordinator TournamentTransactions { get; }
        public IDemoLoginClient LoginClient { get; }

        // Server-role members for the local vertical slice, where this process also
        // plays the dedicated server. An untrusted player client must never hold Http
        // implementations of these in a production build — they move to the dedicated
        // server with the networking phase.
        public IEntitlementGateway EntitlementGateway { get; }
        public LoadoutSnapshotResolver EntitlementResolver { get; }
        public IMatchResultPublisher ResultPublisher { get; }
        public MatchPublishCoordinator MatchPublisher { get; }

        public GameFoundationContext(
            IGameAssetGateway assetGateway,
            ITournamentGateway tournamentGateway,
            IExternalUrlLauncher urlLauncher,
            int loadoutSlots,
            IEntitlementGateway entitlementGateway = null,
            IMatchResultPublisher resultPublisher = null,
            IDemoLoginClient loginClient = null)
        {
            AssetGateway = assetGateway ?? throw new ArgumentNullException(nameof(assetGateway));
            TournamentGateway = tournamentGateway ?? throw new ArgumentNullException(nameof(tournamentGateway));
            UrlLauncher = urlLauncher ?? throw new ArgumentNullException(nameof(urlLauncher));
            Assets = new GameAssetService(assetGateway);
            Loadout = new LoadoutService(assetGateway, Assets, loadoutSlots);
            WalletBinding = new WalletBindingCoordinator(assetGateway, urlLauncher);
            Rewards = new RewardClaimCoordinator(assetGateway, urlLauncher);
            TournamentTransactions = new TournamentTransactionCoordinator(tournamentGateway, urlLauncher);
            LoginClient = loginClient ?? new MockDemoLoginClient();
            EntitlementGateway = entitlementGateway ?? new MockEntitlementGateway();
            EntitlementResolver = new LoadoutSnapshotResolver(EntitlementGateway, loadoutSlots);
            ResultPublisher = resultPublisher ?? new MockMatchResultPublisher();
            MatchPublisher = new MatchPublishCoordinator(ResultPublisher);
        }
    }
}
