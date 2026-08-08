using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Web3Fps.GameFoundation.Networking;

namespace Web3Fps.GameFoundation.Match
{
    /// <summary>
    /// In-memory publisher for offline/mock mode. Mirrors the backend idempotency
    /// contract: an identical re-push succeeds, a different result for the same
    /// matchId raises the dedicated conflict error.
    /// </summary>
    public sealed class MockMatchResultPublisher : IMatchResultPublisher
    {
        private readonly List<MatchAttestationPayload> _published = new List<MatchAttestationPayload>();

        public int LatencyMs { get; set; }
        public Exception FailureToInject { get; set; }
        public IReadOnlyList<MatchAttestationPayload> Published => _published;
        public MatchAttestationPayload LastPayload => _published.Count == 0 ? null : _published[_published.Count - 1];

        public async Task PublishAsync(MatchAttestationPayload payload, CancellationToken ct = default)
        {
            if (payload == null) throw new ArgumentNullException(nameof(payload));
            if (LatencyMs > 0) await Task.Delay(LatencyMs, ct);
            ct.ThrowIfCancellationRequested();
            if (FailureToInject != null) throw FailureToInject;
            for (var i = 0; i < _published.Count; i++)
            {
                var existing = _published[i];
                if (!string.Equals(existing.Result.matchId, payload.Result.matchId, StringComparison.Ordinal)) continue;
                if (!string.Equals(existing.ResultHash, payload.ResultHash, StringComparison.Ordinal))
                    throw new MatchResultConflictException(
                        "Backend already holds a different result for match " + payload.Result.matchId);
                return;
            }
            _published.Add(payload);
        }
    }
}
