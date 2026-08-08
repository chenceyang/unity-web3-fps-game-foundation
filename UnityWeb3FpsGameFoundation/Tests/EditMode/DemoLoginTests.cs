using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Web3Fps.GameFoundation.Auth;

namespace Web3Fps.GameFoundation.Tests
{
    public sealed class DemoLoginTests
    {
        [TestCase("operator-01", true)]
        [TestCase("A.b-C_d9", true)]
        [TestCase("", false)]
        [TestCase("bad id", false)]
        [TestCase("玩家", false)]
        [TestCase("wallet@example", false)]
        public void PlayerIdValidationMirrorsTheBackendRegex(string playerId, bool expected)
        {
            Assert.That(DemoLogin.IsValidPlayerId(playerId), Is.EqualTo(expected));
        }

        [Test]
        public void PlayerIdLengthLimitIs64()
        {
            Assert.That(DemoLogin.IsValidPlayerId(new string('a', 64)), Is.True);
            Assert.That(DemoLogin.IsValidPlayerId(new string('a', 65)), Is.False);
        }

        [Test]
        public void RequestJsonMatchesTheBackendSchema()
        {
            Assert.That(DemoLogin.CreateRequestJson("operator-01"), Is.EqualTo("{\"playerId\":\"operator-01\"}"));
            Assert.Throws<ArgumentException>(() => DemoLogin.CreateRequestJson("bad id"));
        }

        [Test]
        public void AccessTokenParsingReadsTheBackendResponse()
        {
            Assert.That(DemoLogin.ParseAccessToken("{\"accessToken\":\"jwt-123\"}"), Is.EqualTo("jwt-123"));
            Assert.Throws<InvalidOperationException>(() => DemoLogin.ParseAccessToken("{}"));
            Assert.Throws<InvalidOperationException>(() => DemoLogin.ParseAccessToken(""));
        }

        [Test]
        public async Task MockClientFabricatesAMemoryOnlyToken()
        {
            var client = new MockDemoLoginClient();

            var token = await client.LoginAsync("operator-01");

            Assert.That(token, Is.EqualTo("mock-token-operator-01"));
            Assert.ThrowsAsync<ArgumentException>(async () => await client.LoginAsync("bad id"));
        }
    }
}
