using System.Threading;
using System.Threading.Tasks;

namespace Game.Web3
{
    /// <summary>
    /// The only Unity-facing seam to the Web3 asset backend. Never call it during a match.
    /// The client never receives private keys, RPC credentials or contract ABIs.
    /// </summary>
    public interface IGameAssetGateway
    {
        Task<PlayerAssets> GetPlayerAssetsAsync(CancellationToken ct = default);
        Task<WalletBindSession> BeginWalletBindAsync(CancellationToken ct = default);
        Task<WalletBindStatus> PollWalletBindAsync(string sessionId, CancellationToken ct = default);
        Task<ClaimTicket> RequestClaimAsync(string rewardId, CancellationToken ct = default);
        Task<RewardStatus> PollRewardAsync(string rewardId, CancellationToken ct = default);
        Task SetLoadoutAsync(LoadoutRequest request, CancellationToken ct = default);
    }
}
