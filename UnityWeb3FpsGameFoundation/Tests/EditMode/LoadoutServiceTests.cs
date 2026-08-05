using System.Threading.Tasks;
using Game.Web3;
using NUnit.Framework;
using Web3Fps.GameFoundation.Services;

namespace Web3Fps.GameFoundation.Tests
{
    public sealed class LoadoutServiceTests
    {
        [Test]
        public async Task ConfirmedOwnedTokenCanBeEquippedAndSaved()
        {
            var gateway = new MockGameAssetGateway { LatencyMs = 0 };
            var assets = new GameAssetService(gateway);
            var refresh = await assets.RefreshLobbyAsync();
            var loadout = new LoadoutService(gateway, assets, 2);
            var tokenId = refresh.Assets.items[0].tokenId;

            Assert.That(loadout.TryEquip(0, tokenId), Is.True);
            Assert.DoesNotThrowAsync(async () => await loadout.SaveIntentAsync());
        }

        [Test]
        public async Task UnknownTokenFallsBackToDefault()
        {
            var gateway = new MockGameAssetGateway { LatencyMs = 0 };
            var assets = new GameAssetService(gateway);
            await assets.RefreshLobbyAsync();
            var loadout = new LoadoutService(gateway, assets, 1);
            Assert.That(loadout.TryEquip(0, "999999999999999999999999999999"), Is.False);
            Assert.That(loadout.GetTokenId(0), Is.Empty);
        }

        [Test]
        public async Task BackendFailureReturnsDegradedResultInsteadOfThrowing()
        {
            var gateway = new MockGameAssetGateway(false)
            {
                LatencyMs = 0,
                FailureToInject = new GameAssetException("down", 503, "unavailable")
            };
            var assets = new GameAssetService(gateway);
            var result = await assets.RefreshLobbyAsync();
            Assert.That(result.UsedFallback, Is.True);
            Assert.That(result.Assets.items, Is.Empty);
        }
    }
}
