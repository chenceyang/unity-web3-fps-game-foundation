using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Web3Fps.GameFoundation.Composition;

namespace Web3Fps.GameFoundation.Lobby
{
    [DisallowMultipleComponent]
    public sealed class Web3LobbyController : MonoBehaviour
    {
        [SerializeField] private GameFoundationBootstrap bootstrap;
        [SerializeField, Min(0.1f)] private float pollIntervalSeconds = 0.5f;
        [SerializeField, Min(5f)] private float operationTimeoutSeconds = 60f;

        private CancellationTokenSource _lifetime;
        public Web3LobbySession Session { get; private set; }
        public event Action Changed;

        public void Configure(GameFoundationBootstrap foundation)
        {
            bootstrap = foundation;
        }

        private void Awake()
        {
            _lifetime = new CancellationTokenSource();
        }

        private async void Start()
        {
            if (bootstrap == null)
            {
                Debug.LogError("Web3LobbyController requires GameFoundationBootstrap", this);
                return;
            }
            if (bootstrap.Context == null)
            {
                bootstrap.Ready += OnFoundationReady;
                return;
            }
            Initialize(bootstrap.Context);
            await Session.RefreshAsync(_lifetime.Token);
        }

        private async void OnFoundationReady(GameFoundationContext context)
        {
            bootstrap.Ready -= OnFoundationReady;
            Initialize(context);
            await Session.RefreshAsync(_lifetime.Token);
        }

        private void Initialize(GameFoundationContext context)
        {
            if (Session != null) return;
            Session = new Web3LobbySession(
                context,
                TimeSpan.FromSeconds(pollIntervalSeconds),
                TimeSpan.FromSeconds(operationTimeoutSeconds));
            Session.Changed += OnSessionChanged;
            Changed?.Invoke();
        }

        private void OnSessionChanged() => Changed?.Invoke();

        public Task<bool> RefreshAsync() => Run(session => session.RefreshAsync(_lifetime.Token));
        public Task<bool> BindWalletAsync() => Run(session => session.BindWalletAsync(_lifetime.Token));
        public Task<bool> EquipAsync(int slot, string tokenId) => Run(session => session.EquipAsync(slot, tokenId, _lifetime.Token));
        public Task<bool> UseDefaultAsync(int slot) => Run(session => session.UseDefaultAsync(slot, _lifetime.Token));
        public Task<bool> ClaimRewardAsync(string rewardId) => Run(session => session.ClaimRewardAsync(rewardId, _lifetime.Token));
        public Task<bool> RegisterTournamentAsync(string tournamentId) => Run(session => session.RegisterTournamentAsync(tournamentId, _lifetime.Token));
        public Task<bool> SponsorTournamentAsync(string tournamentId) => Run(session => session.SponsorTournamentAsync(tournamentId, _lifetime.Token));
        public Task<bool> ClaimPrizeAsync(string tournamentId) => Run(session => session.ClaimPrizeAsync(tournamentId, _lifetime.Token));
        public Task<bool> ClaimRefundAsync(string tournamentId) => Run(session => session.ClaimRefundAsync(tournamentId, _lifetime.Token));

        private Task<bool> Run(Func<Web3LobbySession, Task<bool>> action)
        {
            return Session == null ? Task.FromResult(false) : action(Session);
        }

        private void OnDestroy()
        {
            if (bootstrap != null) bootstrap.Ready -= OnFoundationReady;
            if (Session != null) Session.Changed -= OnSessionChanged;
            _lifetime?.Cancel();
            _lifetime?.Dispose();
        }
    }
}
