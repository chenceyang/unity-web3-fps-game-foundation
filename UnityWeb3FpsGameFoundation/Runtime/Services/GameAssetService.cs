using System;
using System.Threading;
using System.Threading.Tasks;
using Game.Web3;

namespace Web3Fps.GameFoundation.Services
{
    public enum AssetLayerState
    {
        Unknown,
        Available,
        Degraded
    }

    public sealed class AssetRefreshResult
    {
        public PlayerAssets Assets { get; }
        public AssetLayerState State { get; }
        public GameAssetException Error { get; }

        public bool UsedFallback => State == AssetLayerState.Degraded;

        public AssetRefreshResult(PlayerAssets assets, AssetLayerState state, GameAssetException error = null)
        {
            Assets = assets ?? throw new ArgumentNullException(nameof(assets));
            State = state;
            Error = error;
        }
    }

    /// <summary>Lobby-only façade with a safe empty/default fallback.</summary>
    public sealed class GameAssetService
    {
        private readonly IGameAssetGateway _gateway;

        public PlayerAssets Current { get; private set; } = EmptyAssets();
        public AssetLayerState State { get; private set; } = AssetLayerState.Unknown;

        public event Action<PlayerAssets> AssetsChanged;
        public event Action<AssetLayerState> StateChanged;

        public GameAssetService(IGameAssetGateway gateway)
        {
            _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
        }

        public async Task<AssetRefreshResult> RefreshLobbyAsync(CancellationToken ct = default)
        {
            try
            {
                var assets = await _gateway.GetPlayerAssetsAsync(ct) ?? EmptyAssets();
                assets.items = assets.items ?? Array.Empty<SkinItem>();
                assets.pendingRewards = assets.pendingRewards ?? Array.Empty<PendingReward>();
                Current = assets;
                SetState(AssetLayerState.Available);
                AssetsChanged?.Invoke(Current);
                return new AssetRefreshResult(Current, State);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (GameAssetException ex)
            {
                // Never block play because the asset layer is unavailable.
                if (Current == null) Current = EmptyAssets();
                SetState(AssetLayerState.Degraded);
                AssetsChanged?.Invoke(Current);
                return new AssetRefreshResult(Current, State, ex);
            }
        }

        public SkinItem FindConfirmed(string tokenId)
        {
            if (string.IsNullOrWhiteSpace(tokenId) || Current?.items == null) return null;
            foreach (var item in Current.items)
                if (item != null && item.IsConfirmed && string.Equals(item.tokenId, tokenId, StringComparison.Ordinal))
                    return item;
            return null;
        }

        private void SetState(AssetLayerState value)
        {
            if (State == value) return;
            State = value;
            StateChanged?.Invoke(value);
        }

        private static PlayerAssets EmptyAssets()
        {
            return new PlayerAssets
            {
                wallet = string.Empty,
                items = Array.Empty<SkinItem>(),
                pendingRewards = Array.Empty<PendingReward>(),
                stalenessSeconds = 0
            };
        }
    }
}
