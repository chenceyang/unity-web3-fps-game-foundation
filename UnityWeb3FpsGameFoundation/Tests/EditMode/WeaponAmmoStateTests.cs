using NUnit.Framework;
using Web3Fps.GameFoundation.Gameplay.Combat;

namespace Web3Fps.GameFoundation.Tests
{
    public sealed class WeaponAmmoStateTests
    {
        [Test]
        public void FiringConsumesMagazineWithoutChangingReserve()
        {
            var ammo = new WeaponAmmoState(30, 90);
            Assert.That(ammo.ConsumeRound(), Is.True);
            Assert.That(ammo.Magazine, Is.EqualTo(29));
            Assert.That(ammo.Reserve, Is.EqualTo(90));
        }

        [Test]
        public void ReloadTransfersOnlyMissingRounds()
        {
            var ammo = new WeaponAmmoState(5, 3);
            ammo.ConsumeRound();
            ammo.ConsumeRound();
            Assert.That(ammo.BeginReload(), Is.True);
            Assert.That(ammo.CompleteReload(), Is.EqualTo(2));
            Assert.That(ammo.Magazine, Is.EqualTo(5));
            Assert.That(ammo.Reserve, Is.EqualTo(1));
        }

        [Test]
        public void EmptyReserveCannotStartReload()
        {
            var ammo = new WeaponAmmoState(2, 0);
            ammo.ConsumeRound();
            Assert.That(ammo.BeginReload(), Is.False);
        }
    }
}
