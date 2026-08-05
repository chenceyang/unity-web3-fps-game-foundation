using System;
using System.Threading;
using UnityEngine;
using Web3Fps.GameFoundation.Composition;

namespace Web3Fps.GameFoundation.Samples
{
    public sealed class MockFoundationExample : MonoBehaviour
    {
        [SerializeField] private GameFoundationBootstrap bootstrap;
        private CancellationTokenSource _lifetime;

        private async void Start()
        {
            _lifetime = new CancellationTokenSource();
            if (bootstrap == null || bootstrap.Context == null)
            {
                Debug.LogError("Assign a GameFoundationBootstrap that initializes before this component.", this);
                return;
            }

            var result = await bootstrap.Context.Assets.RefreshLobbyAsync(_lifetime.Token);
            Debug.Log(result.UsedFallback
                ? "Asset backend unavailable; default skins remain playable."
                : "Loaded " + result.Assets.items.Length + " owned skins.");
        }

        private void OnDestroy()
        {
            _lifetime?.Cancel();
            _lifetime?.Dispose();
        }
    }
}
