using System;
using Game.Web3;
using UnityEngine;
using Web3Fps.GameFoundation.Services;
using Web3Fps.GameFoundation.Tournaments;

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

        public event Action<GameFoundationContext> Ready;

        public void SetAccessToken(string accessToken) => _accessToken = accessToken ?? string.Empty;

        private void Awake()
        {
            IGameAssetGateway assets;
            ITournamentGateway tournaments;
            if (useMockBackend)
            {
                assets = new MockGameAssetGateway();
                tournaments = new MockTournamentGateway();
            }
            else
            {
                Func<string> tokenProvider = () => _accessToken;
                assets = new HttpGameAssetGateway(apiBaseUrl, tokenProvider, httpTimeoutSeconds);
                tournaments = new HttpTournamentGateway(apiBaseUrl, tokenProvider, httpTimeoutSeconds);
            }

            Context = new GameFoundationContext(assets, tournaments, new SystemBrowserUrlLauncher(), loadoutSlots);
            Ready?.Invoke(Context);
        }
    }
}
