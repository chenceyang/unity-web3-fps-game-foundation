using System;
using Game.Web3;
using UnityEngine;
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
        public GameFoundationContext Context { get; private set; }
        public bool UseMockBackend => useMockBackend;

        public event Action<GameFoundationContext> Ready;

        public void SetAccessToken(string accessToken) => _accessToken = accessToken ?? string.Empty;

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
            if (useMockBackend)
            {
                assets = new MockGameAssetGateway();
                tournaments = new MockTournamentGateway();
                urlLauncher = new MockExternalUrlLauncher();
            }
            else
            {
                Func<string> tokenProvider = () => _accessToken;
                var http = new HttpApiClient(apiBaseUrl, tokenProvider, httpTimeoutSeconds);
                assets = new HttpGameAssetGateway(http);
                tournaments = new HttpTournamentGateway(http);
                urlLauncher = new SystemBrowserUrlLauncher();
            }

            Context = new GameFoundationContext(assets, tournaments, urlLauncher, loadoutSlots);
            Ready?.Invoke(Context);
        }
    }
}
