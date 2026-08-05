using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Web3Fps.GameFoundation.Crypto;

namespace Game.Web3
{
    public sealed class MockGameAssetGateway : IGameAssetGateway
    {
        private readonly List<SkinItem> _items = new List<SkinItem>();
        private readonly List<PendingReward> _pending = new List<PendingReward>();
        private readonly Dictionary<string, RewardStatus> _rewardStates = new Dictionary<string, RewardStatus>();
        private string _wallet = string.Empty;
        private string _bindSessionId = string.Empty;
        private int _bindPollCount;
        private uint _nextSerial = 38;

        public int LatencyMs { get; set; } = 50;
        public int BindPollsRequired { get; set; } = 2;
        public GameAssetException FailureToInject { get; set; }

        public MockGameAssetGateway(bool seedDemoData = true)
        {
            if (!seedDemoData) return;
            _items.Add(MakeItem(1042, 37, 500, 0.0731f, 4, "Frostbite AK-47"));
            _items.Add(MakeItem(1010, 214, 2500, 0.4120f, 1, "Desert Tan M4"));
            _pending.Add(new PendingReward
            {
                rewardId = "rw_demo_1", skinDefId = 1077, rarity = 4,
                expiresAt = DateTime.UtcNow.AddDays(7).ToString("o")
            });
            _rewardStates["rw_demo_1"] = new RewardStatus { state = "claimable" };
        }

        public async Task<PlayerAssets> GetPlayerAssetsAsync(CancellationToken ct = default)
        {
            await SimulateAsync(ct);
            return new PlayerAssets
            {
                playerId = "p_mock_123", wallet = _wallet, items = _items.ToArray(),
                pendingRewards = _pending.ToArray(), stalenessSeconds = 2
            };
        }

        public async Task<WalletBindSession> BeginWalletBindAsync(CancellationToken ct = default)
        {
            await SimulateAsync(ct);
            _bindSessionId = "bind_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            _bindPollCount = 0;
            return new WalletBindSession
            {
                sessionId = _bindSessionId,
                bindUrl = "https://example.invalid/bind/" + _bindSessionId,
                expiresAt = DateTime.UtcNow.AddMinutes(5).ToString("o")
            };
        }

        public async Task<WalletBindStatus> PollWalletBindAsync(string sessionId, CancellationToken ct = default)
        {
            await SimulateAsync(ct);
            if (sessionId != _bindSessionId) return new WalletBindStatus { state = "expired" };
            if (++_bindPollCount < Math.Max(1, BindPollsRequired)) return new WalletBindStatus { state = "pending" };
            _wallet = "0x1111111111111111111111111111111111111111";
            return new WalletBindStatus { state = "bound", wallet = _wallet };
        }

        public async Task<ClaimTicket> RequestClaimAsync(string rewardId, CancellationToken ct = default)
        {
            await SimulateAsync(ct);
            if (!_rewardStates.ContainsKey(rewardId))
                throw new GameAssetException("Unknown reward " + rewardId, 404, "reward_not_found");
            if (string.IsNullOrWhiteSpace(_wallet))
                throw new GameAssetException("Wallet is not bound", 409, "wallet_not_bound");

            if (_rewardStates[rewardId].state == "claimed")
                return new ClaimTicket { rewardId = rewardId, requiresPlayerAction = false };

            _rewardStates[rewardId] = new RewardStatus { state = "claiming" };
            _ = CompleteClaimAfterDelayAsync(rewardId);
            return new ClaimTicket { rewardId = rewardId, requiresPlayerAction = false };
        }

        public async Task<RewardStatus> PollRewardAsync(string rewardId, CancellationToken ct = default)
        {
            await SimulateAsync(ct);
            RewardStatus state;
            if (_rewardStates.TryGetValue(rewardId, out state)) return state;
            throw new GameAssetException("Unknown reward " + rewardId, 404, "reward_not_found");
        }

        public async Task SetLoadoutAsync(LoadoutRequest request, CancellationToken ct = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            await SimulateAsync(ct);
            var owned = new HashSet<string>(_items.Where(x => x.IsConfirmed).Select(x => x.tokenId));
            foreach (var tokenId in request.tokenIdsBySlot ?? Array.Empty<string>())
            {
                if (!string.IsNullOrWhiteSpace(tokenId) && !owned.Contains(tokenId))
                    throw new GameAssetException("Player does not own confirmed token " + tokenId, 403, "not_owned");
            }
        }

        private async Task CompleteClaimAfterDelayAsync(string rewardId)
        {
            await Task.Delay(Math.Max(LatencyMs * 4, 50));
            var reward = _pending.FirstOrDefault(x => x.rewardId == rewardId);
            if (reward == null) return;
            var item = MakeItem(reward.skinDefId, _nextSerial++, 100, 0.012f, reward.rarity, "Claimed Skin");
            _items.Add(item);
            _pending.Remove(reward);
            _rewardStates[rewardId] = new RewardStatus { state = "claimed", tokenId = item.tokenId };
        }

        private async Task SimulateAsync(CancellationToken ct)
        {
            if (LatencyMs > 0) await Task.Delay(LatencyMs, ct);
            ct.ThrowIfCancellationRequested();
            if (FailureToInject != null) throw FailureToInject;
        }

        private static SkinItem MakeItem(uint skinDefId, uint serial, uint maxSupply, float wear, int rarity, string label)
        {
            var tokenId = ((ulong)skinDefId << 32) | serial;
            return new SkinItem
            {
                tokenId = tokenId.ToString(), skinDefId = skinDefId, serial = serial,
                maxSupply = maxSupply, wear = wear, rarity = rarity, seasonId = 2,
                bundleUri = "https://cdn.example.invalid/skin/" + skinDefId + "/v1.bundle",
                contentHash = Keccak256.ComputeHex(Encoding.UTF8.GetBytes(label)), state = "confirmed"
            };
        }
    }
}
