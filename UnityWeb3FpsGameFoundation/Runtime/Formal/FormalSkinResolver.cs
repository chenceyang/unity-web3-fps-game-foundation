using System;
using System.Threading;
using System.Threading.Tasks;
using Game.Web3;
using Web3Fps.GameFoundation.Assets;

namespace Web3Fps.GameFoundation.Formal
{
    public enum FormalSkinSource
    {
        Procedural,
        VerifiedBundle,
        DefaultFallback
    }

    public sealed class FormalSkinResolution
    {
        public FormalSkinVisualSpec Spec { get; }
        public FormalSkinSource Source { get; }
        public string WarningCode { get; }

        public FormalSkinResolution(FormalSkinVisualSpec spec, FormalSkinSource source, string warningCode = null)
        {
            Spec = spec ?? throw new ArgumentNullException(nameof(spec));
            Source = source;
            WarningCode = warningCode ?? string.Empty;
        }
    }

    /// <summary>
    /// Resolves the look for one SkinItem. When remote bundles are enabled and the
    /// item carries a bundleUri, the hash-verified loader is attempted first; any
    /// failure degrades without blocking. A contentHash mismatch specifically drops
    /// to the neutral default look (the remote content is untrusted), while other
    /// failures keep the trusted package-local procedural spec.
    /// </summary>
    public sealed class FormalSkinResolver
    {
        private readonly Func<SkinItem, CancellationToken, Task<SkinBundleLoadResult>> _loadBundle;
        private readonly bool _remoteBundlesEnabled;

        public FormalSkinResolver(VerifiedSkinBundleLoader loader = null, bool remoteBundlesEnabled = false)
            : this(loader == null ? null : (Func<SkinItem, CancellationToken, Task<SkinBundleLoadResult>>)loader.LoadAsync,
                remoteBundlesEnabled)
        {
        }

        public FormalSkinResolver(
            Func<SkinItem, CancellationToken, Task<SkinBundleLoadResult>> loadBundle,
            bool remoteBundlesEnabled)
        {
            _loadBundle = loadBundle;
            _remoteBundlesEnabled = remoteBundlesEnabled && loadBundle != null;
        }

        public async Task<FormalSkinResolution> ResolveAsync(SkinItem item, CancellationToken ct = default)
        {
            if (item == null)
                return new FormalSkinResolution(FormalSkinCatalog.DefaultSpec, FormalSkinSource.DefaultFallback);
            var spec = FormalSkinCatalog.GetOrDefault(item.skinDefId);
            if (!_remoteBundlesEnabled || !item.IsConfirmed || string.IsNullOrWhiteSpace(item.bundleUri))
                return new FormalSkinResolution(spec, FormalSkinSource.Procedural);

            var loaded = await _loadBundle(item, ct);
            if (loaded.Succeeded)
            {
                // The verified-content pipeline lands later; the slice proves
                // hash-verified delivery, then releases the bundle and renders the
                // procedural spec.
                loaded.Bundle.Unload(true);
                return new FormalSkinResolution(spec, FormalSkinSource.VerifiedBundle);
            }
            return string.Equals(loaded.ErrorCode, "asset_hash_mismatch", StringComparison.Ordinal)
                ? new FormalSkinResolution(FormalSkinCatalog.DefaultSpec, FormalSkinSource.DefaultFallback, loaded.ErrorCode)
                : new FormalSkinResolution(spec, FormalSkinSource.Procedural, loaded.ErrorCode);
        }
    }
}
