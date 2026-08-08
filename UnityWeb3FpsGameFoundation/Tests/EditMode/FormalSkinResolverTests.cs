using System.Threading;
using System.Threading.Tasks;
using Game.Web3;
using NUnit.Framework;
using Web3Fps.GameFoundation.Assets;
using Web3Fps.GameFoundation.Formal;

namespace Web3Fps.GameFoundation.Tests
{
    public sealed class FormalSkinResolverTests
    {
        [Test]
        public async Task NullItemYieldsTheDefaultLook()
        {
            var resolver = new FormalSkinResolver(CountingLoader(null, out _), true);

            var resolution = await resolver.ResolveAsync(null);

            Assert.That(resolution.Spec, Is.SameAs(FormalSkinCatalog.DefaultSpec));
            Assert.That(resolution.Source, Is.EqualTo(FormalSkinSource.DefaultFallback));
        }

        [Test]
        public async Task RemoteDisabledNeverDownloadsAndUsesTheProceduralSpec()
        {
            var loader = CountingLoader(SkinBundleLoadResult.Failure("x", "x"), out var calls);
            var resolver = new FormalSkinResolver(loader, false);

            var resolution = await resolver.ResolveAsync(ConfirmedItem());

            Assert.That(resolution.Source, Is.EqualTo(FormalSkinSource.Procedural));
            Assert.That(resolution.Spec.skinDefId, Is.EqualTo(1042));
            Assert.That(calls.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task PendingItemsNeverAttemptTheBundlePath()
        {
            var loader = CountingLoader(SkinBundleLoadResult.Failure("x", "x"), out var calls);
            var resolver = new FormalSkinResolver(loader, true);
            var item = ConfirmedItem();
            item.state = "pending";

            var resolution = await resolver.ResolveAsync(item);

            Assert.That(resolution.Source, Is.EqualTo(FormalSkinSource.Procedural));
            Assert.That(calls.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task HashMismatchDegradesToTheDefaultLookWithAWarning()
        {
            var loader = CountingLoader(
                SkinBundleLoadResult.Failure("asset_hash_mismatch", "hash mismatch"), out var calls);
            var resolver = new FormalSkinResolver(loader, true);

            var resolution = await resolver.ResolveAsync(ConfirmedItem());

            Assert.That(calls.Count, Is.EqualTo(1));
            Assert.That(resolution.Spec, Is.SameAs(FormalSkinCatalog.DefaultSpec));
            Assert.That(resolution.Source, Is.EqualTo(FormalSkinSource.DefaultFallback));
            Assert.That(resolution.WarningCode, Is.EqualTo("asset_hash_mismatch"));
        }

        [Test]
        public async Task DownloadFailureKeepsTheTrustedProceduralSpec()
        {
            var loader = CountingLoader(
                SkinBundleLoadResult.Failure("asset_download_failed", "offline"), out _);
            var resolver = new FormalSkinResolver(loader, true);

            var resolution = await resolver.ResolveAsync(ConfirmedItem());

            Assert.That(resolution.Spec.skinDefId, Is.EqualTo(1042));
            Assert.That(resolution.Source, Is.EqualTo(FormalSkinSource.Procedural));
            Assert.That(resolution.WarningCode, Is.EqualTo("asset_download_failed"));
        }

        private static SkinItem ConfirmedItem()
        {
            return new SkinItem
            {
                tokenId = "4475355550000000037",
                skinDefId = 1042,
                state = "confirmed",
                bundleUri = "https://cdn.example.invalid/skin/1042/v1.bundle",
                contentHash = "0x" + new string('a', 64)
            };
        }

        private static System.Func<SkinItem, CancellationToken, Task<SkinBundleLoadResult>> CountingLoader(
            SkinBundleLoadResult result,
            out System.Collections.Generic.List<string> calls)
        {
            var recorded = new System.Collections.Generic.List<string>();
            calls = recorded;
            return (item, ct) =>
            {
                recorded.Add(item.tokenId);
                return Task.FromResult(result);
            };
        }
    }
}
