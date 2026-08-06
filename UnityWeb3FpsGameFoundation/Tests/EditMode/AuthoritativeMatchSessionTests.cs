using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Web3Fps.GameFoundation.Server;

namespace Web3Fps.GameFoundation.Tests
{
    public sealed class AuthoritativeMatchSessionTests
    {
        [Test]
        public async Task StartFreezesSnapshotsAndFinishIsIdempotent()
        {
            const string tokenId = "123456789012345678901234567890";
            var gateway = new MockEntitlementGateway();
            gateway.Grant(tokenId, 7);
            var session = new AuthoritativeMatchSession(new LoadoutSnapshotResolver(gateway, 1));
            session.RegisterPlayer(Player("alpha", "red", tokenId));
            session.RegisterPlayer(Player("bravo", "blue", "999"));

            await session.StartAsync("match-1", "tdm", "arena", "build-1", 100);

            Assert.That(session.Lifecycle, Is.EqualTo(AuthoritativeMatchLifecycle.Running));
            Assert.That(session.GetLoadoutSnapshot("alpha").slots[0].resolvedTokenId, Is.EqualTo(tokenId));
            Assert.That(session.GetLoadoutSnapshot("bravo").slots[0].UsesDefault, Is.True);
            session.RecordKill("alpha", "bravo");
            var first = session.Finish(120);
            var second = session.Finish(999);

            Assert.That(second, Is.SameAs(first));
            Assert.That(first.endedAt, Is.EqualTo(120));
            Assert.That(first.players[0].playerId, Is.EqualTo("alpha"));
            Assert.That(session.CreateAttestationPayload().ResultHash, Does.StartWith("0x"));
        }

        [Test]
        public async Task EntitlementFailureDoesNotBlockOrdinaryMatch()
        {
            var gateway = new MockEntitlementGateway { FailureToInject = new Exception("backend unavailable") };
            var session = new AuthoritativeMatchSession(new LoadoutSnapshotResolver(gateway, 1));
            session.RegisterPlayer(Player("alpha", "red", "123"));

            await session.StartAsync("match-2", "ffa", "arena", "build-1", 100);

            Assert.That(session.Lifecycle, Is.EqualTo(AuthoritativeMatchLifecycle.Running));
            Assert.That(session.GetLoadoutSnapshot("alpha").slots[0].UsesDefault, Is.True);
        }

        private static AuthoritativePlayerRegistration Player(string playerId, string teamId, string tokenId)
        {
            return new AuthoritativePlayerRegistration
            {
                playerId = playerId,
                teamId = teamId,
                wallet = "0x" + playerId,
                tokenIdsBySlot = new[] { tokenId }
            };
        }
    }
}
