using System;
using System.Threading.Tasks;
using Game.Web3;
using NUnit.Framework;
using Web3Fps.GameFoundation.Composition;
using Web3Fps.GameFoundation.Lobby;
using Web3Fps.GameFoundation.Services;

namespace Web3Fps.GameFoundation.Tests
{
    public sealed class Web3LobbySessionTests
    {
        [Test]
        public async Task RefreshLoadsAssetsAndTournaments()
        {
            var fixture = CreateFixture();

            var success = await fixture.Session.RefreshAsync();

            Assert.That(success, Is.True);
            Assert.That(fixture.Session.Assets.items, Has.Length.EqualTo(2));
            Assert.That(fixture.Session.Tournaments, Has.Length.EqualTo(4));
            Assert.That(fixture.Session.IsBusy, Is.False);
        }

        [Test]
        public async Task ConfirmedTokenIdRemainsAStringThroughLoadout()
        {
            var fixture = CreateFixture();
            await fixture.Session.RefreshAsync();
            var tokenId = fixture.Session.Assets.items[0].tokenId;

            var success = await fixture.Session.EquipAsync(0, tokenId);

            Assert.That(success, Is.True);
            Assert.That(fixture.Session.GetEquippedTokenId(0), Is.EqualTo(tokenId));
        }

        [Test]
        public async Task MockWalletAndRewardFlowCompletesWithoutOpeningSystemBrowser()
        {
            var fixture = CreateFixture();
            await fixture.Session.RefreshAsync();

            var bound = await fixture.Session.BindWalletAsync();
            var claimed = await fixture.Session.ClaimRewardAsync("rw_demo_1");

            Assert.That(bound, Is.True);
            Assert.That(claimed, Is.True);
            Assert.That(fixture.Session.Assets.HasWallet, Is.True);
            // The claimable reward is consumed; the anti-cheat-held one must remain.
            Assert.That(fixture.Session.Assets.pendingRewards, Has.Length.EqualTo(1));
            Assert.That(fixture.Session.Assets.pendingRewards[0].IsHeld, Is.True);
            Assert.That(fixture.UrlLauncher.LastOpenedUrl, Does.Contain("/bind/"));
        }

        [Test]
        public async Task HeldRewardIsRefusedWithExplicitCode()
        {
            var fixture = CreateFixture();
            await fixture.Session.RefreshAsync();
            await fixture.Session.BindWalletAsync();

            var claimed = await fixture.Session.ClaimRewardAsync("rw_demo_held");

            Assert.That(claimed, Is.False);
            Assert.That(fixture.Session.LastErrorCode, Is.EqualTo("reward_held"));
        }

        [Test]
        public async Task TournamentRegistrationUsesRecordedMockActionUrl()
        {
            var fixture = CreateFixture();
            await fixture.Session.RefreshAsync();

            var success = await fixture.Session.RegisterTournamentAsync("t_open");

            Assert.That(success, Is.True);
            Assert.That(fixture.UrlLauncher.LastOpenedUrl, Does.Contain("/tournaments/t_open/register"));
            Assert.That(fixture.Session.LastErrorCode, Is.Empty);
        }

        private static Fixture CreateFixture()
        {
            var assets = new MockGameAssetGateway { LatencyMs = 0, BindPollsRequired = 1, RewardStepMs = 1 };
            var tournaments = new MockTournamentGateway { LatencyMs = 0 };
            var launcher = new MockExternalUrlLauncher();
            var context = new GameFoundationContext(assets, tournaments, launcher, 3);
            var session = new Web3LobbySession(context, TimeSpan.FromMilliseconds(1), TimeSpan.FromSeconds(2));
            return new Fixture(session, launcher);
        }

        private sealed class Fixture
        {
            public Web3LobbySession Session { get; }
            public MockExternalUrlLauncher UrlLauncher { get; }

            public Fixture(Web3LobbySession session, MockExternalUrlLauncher urlLauncher)
            {
                Session = session;
                UrlLauncher = urlLauncher;
            }
        }
    }
}
