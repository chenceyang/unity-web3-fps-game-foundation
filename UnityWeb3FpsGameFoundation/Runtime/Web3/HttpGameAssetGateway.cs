using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Game.Web3
{
    public sealed class HttpGameAssetGateway : IGameAssetGateway
    {
        private readonly string _baseUrl;
        private readonly Func<string> _accessTokenProvider;
        private readonly int _timeoutSeconds;

        public HttpGameAssetGateway(string baseUrl, Func<string> accessTokenProvider, int timeoutSeconds = 10)
        {
            if (string.IsNullOrWhiteSpace(baseUrl)) throw new ArgumentException("baseUrl is required", nameof(baseUrl));
            if (timeoutSeconds <= 0) throw new ArgumentOutOfRangeException(nameof(timeoutSeconds));
            _baseUrl = baseUrl.TrimEnd('/');
            _accessTokenProvider = accessTokenProvider ?? throw new ArgumentNullException(nameof(accessTokenProvider));
            _timeoutSeconds = timeoutSeconds;
        }

        public Task<PlayerAssets> GetPlayerAssetsAsync(CancellationToken ct = default)
            => SendAsync<PlayerAssets>(UnityWebRequest.kHttpVerbGET, "/v1/assets", null, ct);

        public Task<WalletBindSession> BeginWalletBindAsync(CancellationToken ct = default)
            => SendAsync<WalletBindSession>(UnityWebRequest.kHttpVerbPOST, "/v1/wallet/bind", null, ct);

        public Task<WalletBindStatus> PollWalletBindAsync(string sessionId, CancellationToken ct = default)
        {
            RequireId(sessionId, nameof(sessionId));
            return SendAsync<WalletBindStatus>(UnityWebRequest.kHttpVerbGET,
                "/v1/wallet/bind/" + UnityWebRequest.EscapeURL(sessionId), null, ct);
        }

        public Task<ClaimTicket> RequestClaimAsync(string rewardId, CancellationToken ct = default)
        {
            RequireId(rewardId, nameof(rewardId));
            return SendAsync<ClaimTicket>(UnityWebRequest.kHttpVerbPOST,
                "/v1/rewards/" + UnityWebRequest.EscapeURL(rewardId) + "/claim", null, ct);
        }

        public Task<RewardStatus> PollRewardAsync(string rewardId, CancellationToken ct = default)
        {
            RequireId(rewardId, nameof(rewardId));
            return SendAsync<RewardStatus>(UnityWebRequest.kHttpVerbGET,
                "/v1/rewards/" + UnityWebRequest.EscapeURL(rewardId), null, ct);
        }

        public async Task SetLoadoutAsync(LoadoutRequest request, CancellationToken ct = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            await SendAsync<EmptyResponse>(UnityWebRequest.kHttpVerbPUT, "/v1/loadout",
                JsonUtility.ToJson(request), ct);
        }

        private async Task<T> SendAsync<T>(string method, string path, string jsonBody, CancellationToken ct)
            where T : class
        {
            using (var request = new UnityWebRequest(_baseUrl + path, method))
            {
                request.downloadHandler = new DownloadHandlerBuffer();
                request.timeout = _timeoutSeconds;
                request.SetRequestHeader("Accept", "application/json");

                if (jsonBody != null)
                {
                    request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody));
                    request.SetRequestHeader("Content-Type", "application/json");
                }

                var token = _accessTokenProvider();
                if (!string.IsNullOrWhiteSpace(token))
                    request.SetRequestHeader("Authorization", "Bearer " + token);

                try
                {
                    await request.SendWebRequest().AwaitAsync(ct);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new GameAssetException(method + " " + path + " failed: " + ex.Message, inner: ex);
                }

                if (request.result != UnityWebRequest.Result.Success)
                    throw BuildException(method, path, request);

                var body = request.downloadHandler.text;
                if (typeof(T) == typeof(EmptyResponse) || string.IsNullOrWhiteSpace(body)) return null;

                try
                {
                    return JsonUtility.FromJson<T>(body);
                }
                catch (Exception ex)
                {
                    throw new GameAssetException(method + " " + path + " returned invalid JSON",
                        (int)request.responseCode, "invalid_response", ex);
                }
            }
        }

        private static GameAssetException BuildException(string method, string path, UnityWebRequest request)
        {
            var status = (int)request.responseCode;
            var raw = request.downloadHandler != null ? request.downloadHandler.text : null;
            if (!string.IsNullOrWhiteSpace(raw))
            {
                try
                {
                    var error = JsonUtility.FromJson<ErrorResponse>(raw);
                    if (error != null && !string.IsNullOrWhiteSpace(error.code))
                        return new GameAssetException(method + " " + path + ": " +
                            (string.IsNullOrWhiteSpace(error.message) ? error.code : error.message), status, error.code);
                }
                catch
                {
                    // Fall through to the transport-level error.
                }
            }

            return new GameAssetException(method + " " + path + " failed with HTTP " + status,
                status, "http_error");
        }

        private static void RequireId(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException(paramName + " is required", paramName);
        }

        [Serializable] private sealed class EmptyResponse { }
        [Serializable] private sealed class ErrorResponse { public string code; public string message; }
    }
}
