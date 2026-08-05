using System;
using System.Threading;
using System.Threading.Tasks;
using Game.Web3;
using UnityEngine;
using UnityEngine.Networking;
using Web3Fps.GameFoundation.Crypto;

namespace Web3Fps.GameFoundation.Assets
{
    public sealed class SkinBundleLoadResult
    {
        public AssetBundle Bundle { get; }
        public string ErrorCode { get; }
        public string Message { get; }
        public bool Succeeded => Bundle != null;

        private SkinBundleLoadResult(AssetBundle bundle, string errorCode, string message)
        {
            Bundle = bundle;
            ErrorCode = errorCode;
            Message = message;
        }

        public static SkinBundleLoadResult Success(AssetBundle bundle) => new SkinBundleLoadResult(bundle, null, null);
        public static SkinBundleLoadResult Failure(string code, string message) => new SkinBundleLoadResult(null, code, message);
    }

    /// <summary>Downloads and verifies the on-chain contentHash before loading an AssetBundle.</summary>
    public sealed class VerifiedSkinBundleLoader
    {
        private readonly int _timeoutSeconds;

        public VerifiedSkinBundleLoader(int timeoutSeconds = 20)
        {
            if (timeoutSeconds <= 0) throw new ArgumentOutOfRangeException(nameof(timeoutSeconds));
            _timeoutSeconds = timeoutSeconds;
        }

        public async Task<SkinBundleLoadResult> LoadAsync(SkinItem item, CancellationToken ct = default)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (!item.IsConfirmed) return SkinBundleLoadResult.Failure("asset_pending", "Asset is not confirmed");
            if (string.IsNullOrWhiteSpace(item.bundleUri) || string.IsNullOrWhiteSpace(item.contentHash))
                return SkinBundleLoadResult.Failure("asset_metadata_invalid", "Bundle URI or content hash is missing");

            using (var request = UnityWebRequest.Get(item.bundleUri))
            {
                request.timeout = _timeoutSeconds;
                try { await request.SendWebRequest().AwaitAsync(ct); }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex) { return SkinBundleLoadResult.Failure("asset_download_failed", ex.Message); }
                if (request.result != UnityWebRequest.Result.Success)
                    return SkinBundleLoadResult.Failure("asset_download_failed", request.error);

                var bytes = request.downloadHandler.data;
                if (!Keccak256.Verify(bytes, item.contentHash))
                    return SkinBundleLoadResult.Failure("asset_hash_mismatch", "Downloaded bundle does not match contentHash");

                var operation = AssetBundle.LoadFromMemoryAsync(bytes);
                await AsyncOperationTask.AwaitAsync(operation, ct);
                return operation.assetBundle != null
                    ? SkinBundleLoadResult.Success(operation.assetBundle)
                    : SkinBundleLoadResult.Failure("asset_bundle_invalid", "Unity could not load the verified bundle");
            }
        }
    }

    internal static class AsyncOperationTask
    {
        public static Task AwaitAsync(AsyncOperation operation, CancellationToken ct)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));
            if (operation.isDone) return Task.CompletedTask;
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            CancellationTokenRegistration registration = default;
            void Complete(AsyncOperation _)
            {
                registration.Dispose();
                tcs.TrySetResult(true);
            }
            operation.completed += Complete;
            if (ct.CanBeCanceled)
            {
                registration = ct.Register(() =>
                {
                    operation.completed -= Complete;
                    tcs.TrySetCanceled(ct);
                });
            }
            return tcs.Task;
        }
    }
}
