using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Game.Web3;
using UnityEngine;
using UnityEngine.Networking;

namespace Web3Fps.GameFoundation.Tournaments
{
    public sealed class HttpTournamentGateway : ITournamentGateway
    {
        private readonly string _baseUrl;
        private readonly Func<string> _tokenProvider;
        private readonly int _timeoutSeconds;

        public HttpTournamentGateway(string baseUrl, Func<string> tokenProvider, int timeoutSeconds = 10)
        {
            if (string.IsNullOrWhiteSpace(baseUrl)) throw new ArgumentException("baseUrl is required", nameof(baseUrl));
            _baseUrl = baseUrl.TrimEnd('/');
            _tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
            _timeoutSeconds = timeoutSeconds;
        }

        public Task<TournamentList> GetTournamentsAsync(CancellationToken ct = default)
            => SendAsync<TournamentList>(UnityWebRequest.kHttpVerbGET, "/v1/tournaments", null, ct);

        public Task<TournamentSummary> GetTournamentAsync(string tournamentId, CancellationToken ct = default)
            => SendAsync<TournamentSummary>(UnityWebRequest.kHttpVerbGET, Path(tournamentId), null, ct);

        public Task<TransactionIntent> BeginRegisterAsync(string tournamentId, CancellationToken ct = default)
            => SendAsync<TransactionIntent>(UnityWebRequest.kHttpVerbPOST, Path(tournamentId) + "/register-intent", null, ct);

        public Task<TransactionIntent> BeginSponsorAsync(string tournamentId, string amountWei, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(amountWei)) throw new ArgumentException("amountWei is required", nameof(amountWei));
            return SendAsync<TransactionIntent>(UnityWebRequest.kHttpVerbPOST, Path(tournamentId) + "/sponsor-intent",
                JsonUtility.ToJson(new SponsorIntentRequest { amountWei = amountWei }), ct);
        }

        public Task<TransactionIntent> BeginClaimPrizeAsync(string tournamentId, CancellationToken ct = default)
            => SendAsync<TransactionIntent>(UnityWebRequest.kHttpVerbPOST, Path(tournamentId) + "/claim-prize-intent", null, ct);

        public Task<TransactionIntent> BeginClaimRefundAsync(string tournamentId, CancellationToken ct = default)
            => SendAsync<TransactionIntent>(UnityWebRequest.kHttpVerbPOST, Path(tournamentId) + "/claim-refund-intent", null, ct);

        public Task<TransactionStatus> PollTransactionAsync(string intentId, CancellationToken ct = default)
        {
            Require(intentId, nameof(intentId));
            return SendAsync<TransactionStatus>(UnityWebRequest.kHttpVerbGET,
                "/v1/transactions/" + UnityWebRequest.EscapeURL(intentId), null, ct);
        }

        private async Task<T> SendAsync<T>(string method, string path, string body, CancellationToken ct) where T : class
        {
            using (var request = new UnityWebRequest(_baseUrl + path, method))
            {
                request.downloadHandler = new DownloadHandlerBuffer();
                request.timeout = _timeoutSeconds;
                request.SetRequestHeader("Accept", "application/json");
                var token = _tokenProvider();
                if (!string.IsNullOrWhiteSpace(token)) request.SetRequestHeader("Authorization", "Bearer " + token);
                if (body != null)
                {
                    request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
                    request.SetRequestHeader("Content-Type", "application/json");
                }

                try { await request.SendWebRequest().AwaitAsync(ct); }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex) { throw new TournamentGatewayException(method + " " + path + " failed", inner: ex); }

                if (request.result != UnityWebRequest.Result.Success)
                    throw new TournamentGatewayException(method + " " + path + " failed with HTTP " + request.responseCode,
                        (int)request.responseCode, "http_error");
                try { return JsonUtility.FromJson<T>(request.downloadHandler.text); }
                catch (Exception ex) { throw new TournamentGatewayException("Invalid tournament response", (int)request.responseCode, "invalid_response", ex); }
            }
        }

        private static string Path(string tournamentId)
        {
            Require(tournamentId, nameof(tournamentId));
            return "/v1/tournaments/" + UnityWebRequest.EscapeURL(tournamentId);
        }

        private static void Require(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException(name + " is required", name);
        }
    }
}
