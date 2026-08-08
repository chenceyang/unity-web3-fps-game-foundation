using System;
using Game.Web3;
using UnityEngine;
using Web3Fps.GameFoundation.Auth;
using Web3Fps.GameFoundation.Match;
using Web3Fps.GameFoundation.Networking;
using Web3Fps.GameFoundation.Server;
using Web3Fps.GameFoundation.Services;

namespace Web3Fps.GameFoundation.Composition
{
    public sealed class GameFoundationBootstrap : MonoBehaviour
    {
        [SerializeField] private bool useMockBackend = true;
        [SerializeField] private string apiBaseUrl = "https://api.example.com";
        [SerializeField, Min(1)] private int loadoutSlots = 3;
        [SerializeField, Min(1)] private int httpTimeoutSeconds = 10;

        private string _accessToken = string.Empty;
        private string _serviceToken = string.Empty;
        public GameFoundationContext Context { get; private set; }
        public bool UseMockBackend => useMockBackend;
        public string PlayerId { get; private set; } = "guest";

        public event Action<GameFoundationContext> Ready;

        // Memory only — never serialized, logged or persisted.
        public void SetAccessToken(string accessToken) => _accessToken = accessToken ?? string.Empty;

        // Memory only, and only meaningful while this local process plays the
        // dedicated-server role in the vertical slice. A production player client
        // never holds a service token.
        public void SetServiceToken(string serviceToken) => _serviceToken = serviceToken ?? string.Empty;

        public void SetPlayerId(string playerId) =>
            PlayerId = string.IsNullOrWhiteSpace(playerId) ? "guest" : playerId;

        public void Configure(bool useMock, string baseUrl = "https://api.example.com")
        {
            useMockBackend = useMock;
            apiBaseUrl = string.IsNullOrWhiteSpace(baseUrl) ? "https://api.example.com" : baseUrl;
        }

        private void Awake()
        {
            IGameAssetGateway assets;
            ITournamentGateway tournaments;
            IExternalUrlLauncher urlLauncher;
            IEntitlementGateway entitlement;
            IMatchResultPublisher resultPublisher;
            IDemoLoginClient loginClient;
            if (useMockBackend)
            {
                assets = new MockGameAssetGateway();
                tournaments = new MockTournamentGateway();
                urlLauncher = new MockExternalUrlLauncher();
                entitlement = new MockEntitlementGateway();
                resultPublisher = new MockMatchResultPublisher();
                loginClient = new MockDemoLoginClient();
            }
            else
            {
                Func<string> tokenProvider = () => _accessToken;
                var http = new HttpApiClient(apiBaseUrl, tokenProvider, httpTimeoutSeconds);
                assets = new HttpGameAssetGateway(http);
                tournaments = new HttpTournamentGateway(http);
                urlLauncher = new SystemBrowserUrlLauncher();
                loginClient = new HttpDemoLoginClient(apiBaseUrl, httpTimeoutSeconds);
                // Server-role adapters for the local slice only: an untrusted player
                // client must never instantiate Http entitlement/publish adapters in a
                // production build. Without SetServiceToken these calls fail and the
                // resolver degrades to default skins — never blocking normal play.
                Func<string> serviceTokenProvider = () => _serviceToken;
                entitlement = new HttpEntitlementGateway(apiBaseUrl, serviceTokenProvider, httpTimeoutSeconds);
                resultPublisher = new HttpMatchResultPublisher(apiBaseUrl, serviceTokenProvider, httpTimeoutSeconds);
            }

            Context = new GameFoundationContext(
                assets, tournaments, urlLauncher, loadoutSlots, entitlement, resultPublisher, loginClient);
            Ready?.Invoke(Context);
        }
    }
}
