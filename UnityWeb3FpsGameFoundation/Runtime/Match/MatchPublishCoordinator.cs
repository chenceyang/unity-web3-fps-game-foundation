using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Web3Fps.GameFoundation.Networking;

namespace Web3Fps.GameFoundation.Match
{
    /// <summary>
    /// Dedicated-server publication guard. A successful match payload is sent once; a failed
    /// attempt remains retryable, while a different result for the same match is rejected.
    /// </summary>
    public sealed class MatchPublishCoordinator
    {
        private sealed class PublicationState
        {
            public string ResultHash;
            public bool Published;
        }

        private readonly IMatchResultPublisher _publisher;
        private readonly Dictionary<string, PublicationState> _states =
            new Dictionary<string, PublicationState>(StringComparer.Ordinal);
        private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);

        public MatchPublishCoordinator(IMatchResultPublisher publisher)
        {
            _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        }

        /// <returns>True when this call published; false when the identical payload was already published.</returns>
        public async Task<bool> PublishOnceAsync(MatchAttestationPayload payload, CancellationToken ct = default)
        {
            Validate(payload);
            await _gate.WaitAsync(ct);
            try
            {
                PublicationState state;
                if (!_states.TryGetValue(payload.Result.matchId, out state))
                {
                    state = new PublicationState { ResultHash = payload.ResultHash };
                    _states.Add(payload.Result.matchId, state);
                }
                else if (!string.Equals(state.ResultHash, payload.ResultHash, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("A different result already exists for match " + payload.Result.matchId);
                }

                if (state.Published) return false;
                await _publisher.PublishAsync(payload, ct);
                state.Published = true;
                return true;
            }
            finally
            {
                _gate.Release();
            }
        }

        public bool IsPublished(string matchId)
        {
            if (string.IsNullOrWhiteSpace(matchId)) return false;
            PublicationState state;
            return _states.TryGetValue(matchId, out state) && state.Published;
        }

        private static void Validate(MatchAttestationPayload payload)
        {
            if (payload == null) throw new ArgumentNullException(nameof(payload));
            if (string.IsNullOrWhiteSpace(payload.Result.matchId))
                throw new ArgumentException("Payload matchId is required", nameof(payload));
            if (string.IsNullOrWhiteSpace(payload.ResultHash))
                throw new ArgumentException("Payload resultHash is required", nameof(payload));
        }
    }
}
