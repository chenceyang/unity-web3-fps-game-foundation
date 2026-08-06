using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Web3Fps.GameFoundation.Match;
using Web3Fps.GameFoundation.Networking;

namespace Web3Fps.GameFoundation.Tests
{
    public sealed class MatchPublishCoordinatorTests
    {
        [Test]
        public async Task FailedPublishCanRetryThenSuccessfulPayloadIsSentOnce()
        {
            var publisher = new RecordingPublisher { FailuresRemaining = 1 };
            var coordinator = new MatchPublishCoordinator(publisher);
            var payload = Payload("match-1", 10);

            Assert.ThrowsAsync<InvalidOperationException>(async () => await coordinator.PublishOnceAsync(payload));
            Assert.That(await coordinator.PublishOnceAsync(payload), Is.True);
            Assert.That(await coordinator.PublishOnceAsync(payload), Is.False);
            Assert.That(publisher.CallCount, Is.EqualTo(2));
            Assert.That(coordinator.IsPublished("match-1"), Is.True);
        }

        [Test]
        public async Task DifferentResultForSameMatchIsRejected()
        {
            var coordinator = new MatchPublishCoordinator(new RecordingPublisher());
            await coordinator.PublishOnceAsync(Payload("match-1", 10));

            Assert.ThrowsAsync<InvalidOperationException>(
                async () => await coordinator.PublishOnceAsync(Payload("match-1", 20)));
        }

        private static MatchAttestationPayload Payload(string matchId, long endedAt)
        {
            return MatchResultHasher.CreatePayload(new MatchResult
            {
                matchId = matchId,
                modeId = "tdm",
                mapId = "arena",
                serverBuild = "build-1",
                startedAt = 0,
                endedAt = endedAt,
                players = new[]
                {
                    new MatchPlayerResult
                    {
                        playerId = "alpha", teamId = "red", placement = 1, result = "win"
                    }
                }
            });
        }

        private sealed class RecordingPublisher : IMatchResultPublisher
        {
            public int FailuresRemaining { get; set; }
            public int CallCount { get; private set; }

            public Task PublishAsync(MatchAttestationPayload payload, CancellationToken ct = default)
            {
                CallCount++;
                if (FailuresRemaining > 0)
                {
                    FailuresRemaining--;
                    throw new InvalidOperationException("injected failure");
                }
                return Task.CompletedTask;
            }
        }
    }
}
