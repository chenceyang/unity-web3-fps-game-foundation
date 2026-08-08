using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Game.Web3;
using UnityEngine;
using UnityEngine.Networking;

namespace Web3Fps.GameFoundation.Auth
{
    /// <summary>
    /// DEMO-ONLY login seam for POST /v1/auth/login (backend/src/routes/auth.ts).
    /// The endpoint mints a session for any playerId without a password so the Web3
    /// stack is demonstrable standalone; production replaces it with the studio's
    /// real account system. Tokens are returned to the caller and must live only in
    /// memory (GameFoundationBootstrap.SetAccessToken) — never persisted.
    /// </summary>
    public interface IDemoLoginClient
    {
        Task<string> LoginAsync(string playerId, CancellationToken ct = default);
    }

    /// <summary>Pure request/response contract helpers, mirroring the backend zod schema.</summary>
    public static class DemoLogin
    {
        public const int MaxPlayerIdLength = 64;

        [Serializable]
        public sealed class LoginRequest
        {
            public string playerId = string.Empty;
        }

        [Serializable]
        public sealed class LoginResponse
        {
            public string accessToken = string.Empty;
        }

        // ^[A-Za-z0-9_.\-]+$ with max length 64, per backend/src/routes/auth.ts.
        public static bool IsValidPlayerId(string playerId)
        {
            if (string.IsNullOrEmpty(playerId) || playerId.Length > MaxPlayerIdLength) return false;
            for (var i = 0; i < playerId.Length; i++)
            {
                var ch = playerId[i];
                var valid = (ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z') ||
                            (ch >= '0' && ch <= '9') || ch == '_' || ch == '.' || ch == '-';
                if (!valid) return false;
            }
            return true;
        }

        public static string CreateRequestJson(string playerId)
        {
            if (!IsValidPlayerId(playerId))
                throw new ArgumentException("playerId may contain letters, digits, _ . - (max 64)", nameof(playerId));
            return "{\"playerId\":\"" + EscapeJsonString(playerId) + "\"}";
        }

        public static string ParseAccessToken(string responseJson)
        {
            var accessToken = ReadJsonString(responseJson, "accessToken");
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new InvalidOperationException("Demo login returned no accessToken");
            return accessToken;
        }

        private static string EscapeJsonString(string value)
        {
            return (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static string ReadJsonString(string json, string propertyName)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            var marker = "\"" + propertyName + "\"";
            var markerIndex = json.IndexOf(marker, StringComparison.Ordinal);
            if (markerIndex < 0) return null;
            var colon = json.IndexOf(':', markerIndex + marker.Length);
            if (colon < 0) return null;
            var start = json.IndexOf('"', colon + 1);
            if (start < 0) return null;
            var result = new StringBuilder();
            var escaped = false;
            for (var i = start + 1; i < json.Length; i++)
            {
                var ch = json[i];
                if (escaped)
                {
                    result.Append(ch == 'n' ? '\n' : ch == 'r' ? '\r' : ch == 't' ? '\t' : ch);
                    escaped = false;
                }
                else if (ch == '\\') escaped = true;
                else if (ch == '"') return result.ToString();
                else result.Append(ch);
            }
            return null;
        }
    }

    public sealed class HttpDemoLoginClient : IDemoLoginClient
    {
        private readonly string _baseUrl;
        private readonly int _timeoutSeconds;

        public HttpDemoLoginClient(string baseUrl, int timeoutSeconds = 10)
        {
            if (string.IsNullOrWhiteSpace(baseUrl)) throw new ArgumentException("baseUrl is required", nameof(baseUrl));
            _baseUrl = baseUrl.TrimEnd('/');
            _timeoutSeconds = Math.Max(1, timeoutSeconds);
        }

        public async Task<string> LoginAsync(string playerId, CancellationToken ct = default)
        {
            var body = Encoding.UTF8.GetBytes(DemoLogin.CreateRequestJson(playerId));
            using (var request = new UnityWebRequest(_baseUrl + "/v1/auth/login", UnityWebRequest.kHttpVerbPOST))
            {
                request.uploadHandler = new UploadHandlerRaw(body);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.timeout = _timeoutSeconds;
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Accept", "application/json");
                await request.SendWebRequest().AwaitAsync(ct);
                if (request.result != UnityWebRequest.Result.Success)
                    throw new InvalidOperationException("Demo login failed with HTTP " + request.responseCode);
                return DemoLogin.ParseAccessToken(request.downloadHandler.text);
            }
        }
    }

    /// <summary>Offline stand-in that fabricates a session token in memory.</summary>
    public sealed class MockDemoLoginClient : IDemoLoginClient
    {
        public int LatencyMs { get; set; }
        public Exception FailureToInject { get; set; }

        public async Task<string> LoginAsync(string playerId, CancellationToken ct = default)
        {
            if (!DemoLogin.IsValidPlayerId(playerId))
                throw new ArgumentException("playerId may contain letters, digits, _ . - (max 64)", nameof(playerId));
            if (LatencyMs > 0) await Task.Delay(LatencyMs, ct);
            ct.ThrowIfCancellationRequested();
            if (FailureToInject != null) throw FailureToInject;
            return "mock-token-" + playerId;
        }
    }
}
