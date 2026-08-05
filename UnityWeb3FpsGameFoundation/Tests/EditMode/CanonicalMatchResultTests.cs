using System.Text;
using NUnit.Framework;
using Web3Fps.GameFoundation.Match;

namespace Web3Fps.GameFoundation.Tests
{
    public sealed class CanonicalMatchResultTests
    {
        [Test]
        public void PlayerInputOrderDoesNotChangeCanonicalBytes()
        {
            var a = MakeResult(new[] { Player("p2", 2), Player("p1", 1) });
            var b = MakeResult(new[] { Player("p1", 1), Player("p2", 2) });
            Assert.That(CanonicalMatchResultSerializer.Serialize(a),
                Is.EqualTo(CanonicalMatchResultSerializer.Serialize(b)));
        }

        [Test]
        public void PayloadContainsBytes32HexValues()
        {
            var payload = MatchResultHasher.CreatePayload(MakeResult(new[] { Player("p1", 1) }));
            Assert.That(payload.MatchIdKey, Does.StartWith("0x"));
            Assert.That(payload.ResultHash, Does.StartWith("0x"));
            Assert.That(payload.MatchIdKey.Length, Is.EqualTo(66));
            Assert.That(payload.ResultHash.Length, Is.EqualTo(66));
            Assert.That(Encoding.UTF8.GetString(payload.CanonicalUtf8), Does.Contain("\"matchId\":\"match-1\""));
        }

        [Test]
        public void DuplicatePlayerIsRejected()
        {
            var result = MakeResult(new[] { Player("p1", 1), Player("p1", 2) });
            Assert.Throws<System.ArgumentException>(() => CanonicalMatchResultSerializer.Serialize(result));
        }

        private static MatchResult MakeResult(MatchPlayerResult[] players)
        {
            return new MatchResult
            {
                matchId = "match-1", modeId = "tdm", mapId = "arena",
                startedAt = 100, endedAt = 200, serverBuild = "server-1",
                antiCheatState = "passed", players = players
            };
        }

        private static MatchPlayerResult Player(string id, int placement)
        {
            return new MatchPlayerResult
            {
                playerId = id, teamId = "red", result = placement == 1 ? "win" : "loss",
                placement = placement, kills = 1, deaths = 1, score = 100
            };
        }
    }
}
