using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Game.Web3;
using UnityEngine;
using UnityEngine.Networking;

namespace Web3Fps.GameFoundation.Server
{
    /// <summary>Dedicated-server-only HTTP adapter. The service token must come from process memory.</summary>
    public sealed class HttpEntitlementGateway : IEntitlementGateway
    {
        private readonly string _baseUrl;
        private readonly Func<string> _serviceTokenProvider;
        private readonly int _timeoutSeconds;

        public HttpEntitlementGateway(string baseUrl, Func<string> serviceTokenProvider, int timeoutSeconds = 10)
        {
            if (string.IsNullOrWhiteSpace(baseUrl)) throw new ArgumentException("baseUrl is required", nameof(baseUrl));
            _baseUrl = baseUrl.TrimEnd('/');
            _serviceTokenProvider = serviceTokenProvider ?? throw new ArgumentNullException(nameof(serviceTokenProvider));
            _timeoutSeconds = Math.Max(1, timeoutSeconds);
        }

        public async Task<PlayerLoadoutSnapshot> ResolveAsync(
            LoadoutEntitlementRequest entitlementRequest,
            CancellationToken ct = default)
        {
            if (entitlementRequest == null) throw new ArgumentNullException(nameof(entitlementRequest));
            // Wire format is backend/src/routes/entitlement.ts: {playerId, matchId, wallet,
            // tokenIds} in, EntitlementResult out; the mapper rebuilds the package snapshot.
            var body = Encoding.UTF8.GetBytes(EntitlementWireMapper.ToRequestJson(entitlementRequest));
            using (var request = new UnityWebRequest(
                       _baseUrl + "/internal/v1/entitlement-check",
                       UnityWebRequest.kHttpVerbPOST))
            {
                request.uploadHandler = new UploadHandlerRaw(body);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.timeout = _timeoutSeconds;
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Accept", "application/json");
                var token = _serviceTokenProvider();
                if (string.IsNullOrWhiteSpace(token))
                    throw new InvalidOperationException("Dedicated-server service token is unavailable");
                request.SetRequestHeader("Authorization", "Bearer " + token);
                await request.SendWebRequest().AwaitAsync(ct);
                if (request.result != UnityWebRequest.Result.Success)
                    throw new InvalidOperationException("Entitlement check failed with HTTP " + request.responseCode);
                var response = JsonUtility.FromJson<EntitlementWireResponse>(request.downloadHandler.text);
                if (response == null) throw new InvalidOperationException("Entitlement backend returned invalid JSON");
                return EntitlementWireMapper.ToSnapshot(
                    response, entitlementRequest, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            }
        }
    }
}
