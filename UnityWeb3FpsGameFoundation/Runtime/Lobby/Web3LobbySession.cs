using System;
using System.Threading;
using System.Threading.Tasks;
using Game.Web3;
using Web3Fps.GameFoundation.Composition;

namespace Web3Fps.GameFoundation.Lobby
{
    public enum LobbyOperation
    {
        Idle,
        Refreshing,
        SavingLoadout,
        BindingWallet,
        ClaimingReward,
        RegisteringTournament,
        SponsoringTournament,
        ClaimingPrize,
        ClaimingRefund
    }

    /// <summary>
    /// Lobby-only application controller. No method is called from the combat simulation loop.
    /// </summary>
    public sealed class Web3LobbySession
    {
        private readonly GameFoundationContext _context;
        private readonly TimeSpan _pollInterval;
        private readonly TimeSpan _operationTimeout;
        private int _busy;

        public PlayerAssets Assets => _context.Assets.Current;
        public TournamentSummary[] Tournaments { get; private set; } = Array.Empty<TournamentSummary>();
        public LobbyOperation Operation { get; private set; } = LobbyOperation.Idle;
        public string StatusMessage { get; private set; } = "Ready";
        public string LastErrorCode { get; private set; } = string.Empty;
        public bool IsBusy => Volatile.Read(ref _busy) != 0;

        public event Action Changed;

        public Web3LobbySession(
            GameFoundationContext context,
            TimeSpan? pollInterval = null,
            TimeSpan? operationTimeout = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _pollInterval = pollInterval ?? TimeSpan.FromSeconds(1);
            _operationTimeout = operationTimeout ?? TimeSpan.FromMinutes(2);
            if (_pollInterval <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(pollInterval));
            if (_operationTimeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(operationTimeout));
        }

        public string GetEquippedTokenId(int slot) => _context.Loadout.GetTokenId(slot);

        public Task<bool> RefreshAsync(CancellationToken ct = default)
        {
            return RunAsync(LobbyOperation.Refreshing, "Refreshing lobby…", RefreshCoreAsync, ct);
        }

        public Task<bool> EquipAsync(int slot, string tokenId, CancellationToken ct = default)
        {
            return RunAsync(LobbyOperation.SavingLoadout, "Saving loadout…", async token =>
            {
                if (!_context.Loadout.TryEquip(slot, tokenId))
                    throw new GameAssetException("Only confirmed owned skins can be equipped", 0, "skin_not_confirmed");
                await _context.Loadout.SaveIntentAsync(token);
                SetStatus("Loadout saved. The server will verify ownership again before the match.");
            }, ct);
        }

        public Task<bool> UseDefaultAsync(int slot, CancellationToken ct = default)
        {
            return RunAsync(LobbyOperation.SavingLoadout, "Saving default loadout…", async token =>
            {
                _context.Loadout.UseDefault(slot);
                await _context.Loadout.SaveIntentAsync(token);
                SetStatus("Default cosmetic selected for slot " + (slot + 1) + ".");
            }, ct);
        }

        public Task<bool> BindWalletAsync(CancellationToken ct = default)
        {
            return RunAsync(LobbyOperation.BindingWallet, "Waiting for wallet binding…", async token =>
            {
                var status = await _context.WalletBinding.BindAsync(_pollInterval, _operationTimeout, token);
                if (!status.IsBound)
                    throw new GameAssetException(status.error ?? "Wallet binding did not complete", 0, status.state);
                await RefreshCoreAsync(token);
                SetStatus("Wallet bound: " + Shorten(status.wallet));
            }, ct);
        }

        public Task<bool> ClaimRewardAsync(string rewardId, CancellationToken ct = default)
        {
            return RunAsync(LobbyOperation.ClaimingReward, "Claiming reward…", async token =>
            {
                var status = await _context.Rewards.ClaimAsync(rewardId, _pollInterval, _operationTimeout, token);
                if (!status.CanEquip)
                    throw new GameAssetException(status.error ?? "Reward was not confirmed", 0, status.state);
                await RefreshCoreAsync(token);
                SetStatus("Reward confirmed on-chain. Token ID: " + status.tokenId);
            }, ct);
        }

        public Task<bool> RegisterTournamentAsync(string tournamentId, CancellationToken ct = default)
        {
            return LaunchTournamentActionAsync(
                LobbyOperation.RegisteringTournament, "Preparing registration…",
                tournamentId, TournamentAction.Register, ct);
        }

        public Task<bool> SponsorTournamentAsync(string tournamentId, CancellationToken ct = default)
        {
            return LaunchTournamentActionAsync(
                LobbyOperation.SponsoringTournament, "Preparing sponsorship…",
                tournamentId, TournamentAction.Sponsor, ct);
        }

        public Task<bool> ClaimPrizeAsync(string tournamentId, CancellationToken ct = default)
        {
            return LaunchTournamentActionAsync(
                LobbyOperation.ClaimingPrize, "Preparing prize claim…",
                tournamentId, TournamentAction.ClaimPrize, ct);
        }

        public Task<bool> ClaimRefundAsync(string tournamentId, CancellationToken ct = default)
        {
            return LaunchTournamentActionAsync(
                LobbyOperation.ClaimingRefund, "Preparing refund claim…",
                tournamentId, TournamentAction.ClaimRefund, ct);
        }

        // Tournament money movement is signed in the browser, not in Unity; the game
        // only opens the intent URL. Final state arrives via the backend on refresh,
        // so there is no transaction polling loop here anymore.
        private Task<bool> LaunchTournamentActionAsync(
            LobbyOperation operation,
            string pendingMessage,
            string tournamentId,
            string action,
            CancellationToken ct)
        {
            return RunAsync(operation, pendingMessage, async token =>
            {
                await _context.TournamentTransactions.LaunchAsync(tournamentId, action, token);
                await RefreshTournamentsCoreAsync(token);
                SetStatus("Continue the " + action + " transaction in your browser. " +
                          "Tournament state updates after it lands on-chain.");
            }, ct);
        }

        private async Task RefreshCoreAsync(CancellationToken ct)
        {
            var assetResult = await _context.Assets.RefreshLobbyAsync(ct);
            _context.Loadout.ReconcileOwnership();
            try
            {
                await RefreshTournamentsCoreAsync(ct);
            }
            catch (GameAssetException exception)
            {
                LastErrorCode = exception.Code ?? "tournament_unavailable";
                SetStatus(assetResult.UsedFallback
                    ? "Assets and tournaments are unavailable. Default cosmetics and normal play remain enabled."
                    : "Assets loaded; tournaments are temporarily unavailable.");
                return;
            }

            LastErrorCode = assetResult.Error?.Code ?? string.Empty;
            SetStatus(assetResult.UsedFallback
                ? "Asset backend unavailable. Default cosmetics and normal play remain enabled."
                : "Lobby data refreshed.");
        }

        private async Task RefreshTournamentsCoreAsync(CancellationToken ct)
        {
            Tournaments = await _context.TournamentGateway.ListTournamentsAsync(null, ct)
                          ?? Array.Empty<TournamentSummary>();
        }

        private async Task<bool> RunAsync(
            LobbyOperation operation,
            string pendingMessage,
            Func<CancellationToken, Task> action,
            CancellationToken ct)
        {
            if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0) return false;
            Operation = operation;
            LastErrorCode = string.Empty;
            SetStatus(pendingMessage);
            try
            {
                await action(ct);
                return true;
            }
            catch (OperationCanceledException)
            {
                LastErrorCode = "cancelled";
                SetStatus("Operation cancelled.");
                return false;
            }
            catch (GameAssetException exception)
            {
                LastErrorCode = exception.Code ?? "asset_error";
                SetStatus(exception.Message + " Normal play remains available.");
                return false;
            }
            catch (Exception exception)
            {
                LastErrorCode = "unexpected_error";
                SetStatus(exception.Message + " Normal play remains available.");
                return false;
            }
            finally
            {
                Operation = LobbyOperation.Idle;
                Interlocked.Exchange(ref _busy, 0);
                Changed?.Invoke();
            }
        }

        private void SetStatus(string value)
        {
            StatusMessage = value ?? string.Empty;
            Changed?.Invoke();
        }

        private static string Shorten(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length <= 14) return value ?? string.Empty;
            return value.Substring(0, 8) + "…" + value.Substring(value.Length - 6);
        }
    }
}
