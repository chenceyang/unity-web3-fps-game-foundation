using System;
using Game.Web3;
using Web3Fps.GameFoundation.Services;
using Web3Fps.GameFoundation.Tournaments;

namespace Web3Fps.GameFoundation.Composition
{
    public sealed class GameFoundationContext
    {
        public IGameAssetGateway AssetGateway { get; }
        public ITournamentGateway TournamentGateway { get; }
        public GameAssetService Assets { get; }
        public LoadoutService Loadout { get; }
        public WalletBindingCoordinator WalletBinding { get; }
        public RewardClaimCoordinator Rewards { get; }
        public TournamentTransactionCoordinator TournamentTransactions { get; }

        public GameFoundationContext(IGameAssetGateway assetGateway, ITournamentGateway tournamentGateway,
            IExternalUrlLauncher urlLauncher, int loadoutSlots)
        {
            AssetGateway = assetGateway ?? throw new ArgumentNullException(nameof(assetGateway));
            TournamentGateway = tournamentGateway ?? throw new ArgumentNullException(nameof(tournamentGateway));
            if (urlLauncher == null) throw new ArgumentNullException(nameof(urlLauncher));
            Assets = new GameAssetService(assetGateway);
            Loadout = new LoadoutService(assetGateway, Assets, loadoutSlots);
            WalletBinding = new WalletBindingCoordinator(assetGateway, urlLauncher);
            Rewards = new RewardClaimCoordinator(assetGateway, urlLauncher);
            TournamentTransactions = new TournamentTransactionCoordinator(tournamentGateway, urlLauncher);
        }
    }
}
