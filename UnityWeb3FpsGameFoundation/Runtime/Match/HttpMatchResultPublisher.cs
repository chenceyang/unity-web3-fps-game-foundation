using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Game.Web3;
using UnityEngine;
using UnityEngine.Networking;
using Web3Fps.GameFoundation.Networking;

namespace Web3Fps.GameFoundation.Match
{
    /// <summary>Dedicated-server-only publisher. Do not instantiate this in an untrusted player client.</summary>
    public sealed class HttpMatchResultPublisher : IMatchResultPublisher
    {
        private readonly string _baseUrl;
        private readonly Func<string> _serviceTokenProvider;
        private readonly int _timeoutSeconds;

        public HttpMatchResultPublisher(string baseUrl, Func<string> serviceTokenProvider, int timeoutSeconds = 10)
        {
            if (string.IsNullOrWhiteSpace(baseUrl)) throw new ArgumentException("baseUrl is required", nameof(baseUrl));
            _baseUrl = baseUrl.TrimEnd('/');
            _serviceTokenProvider = serviceTokenProvider ?? throw new ArgumentNullException(nameof(serviceTokenProvider));
            _timeoutSeconds = timeoutSeconds;
        }

        public async Task PublishAsync(MatchAttestationPayload payload, CancellationToken ct = default)
        {
            if (payload == null) throw new ArgumentNullException(nameof(payload));
            var body = JsonUtility.ToJson(new PublishRequest
            {
                matchId = payload.Result.matchId,
                matchIdKey = payload.MatchIdKey,
                resultHash = payload.ResultHash,
                canonicalJson = Encoding.UTF8.GetString(payload.CanonicalUtf8)
            });

            using (var request = new UnityWebRequest(_baseUrl + "/internal/v1/matches", UnityWebRequest.kHttpVerbPOST))
            {
                request.downloadHandler = new DownloadHandlerBuffer();
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
                request.timeout = _timeoutSeconds;
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Accept", "application/json");
                var token = _serviceTokenProvider();
                if (!string.IsNullOrWhiteSpace(token)) request.SetRequestHeader("Authorization", "Bearer " + token);
                await request.SendWebRequest().AwaitAsync(ct);
                if (request.result != UnityWebRequest.Result.Success)
                {
                    if ((int)request.responseCode == 409)
                        throw new MatchResultConflictException(
                            "Backend already holds a different result for match " + payload.Result.matchId);
                    throw new InvalidOperationException("Match publish failed with HTTP " + request.responseCode);
                }
            }
        }

        [Serializable]
        private sealed class PublishRequest
        {
            public string matchId;
            public string matchIdKey;
            public string resultHash;
            public string canonicalJson;
        }
    }
}
