using System.Linq;
using NUnit.Framework;
using Web3Fps.GameFoundation.Formal;

namespace Web3Fps.GameFoundation.Tests
{
    public sealed class FormalContentCatalogTests
    {
        [Test]
        public void ThreeCosmeticSlotsMatchServerLoadoutContract()
        {
            Assert.That(FormalContentCatalog.CosmeticSlotNames, Has.Length.EqualTo(3));
            Assert.That(FormalContentCatalog.GetSlotName((int)FormalCosmeticSlot.WeaponFinish), Is.EqualTo("Weapon Finish"));
            Assert.That(FormalContentCatalog.GetSlotName((int)FormalCosmeticSlot.OperatorShell), Is.EqualTo("Operator Shell"));
            Assert.That(FormalContentCatalog.GetSlotName((int)FormalCosmeticSlot.IdentitySignal), Is.EqualTo("Identity Signal"));
        }

        [Test]
        public void VerticalSliceWeaponsHaveUniqueIdsAndValidRanges()
        {
            Assert.That(FormalContentCatalog.LaunchWeapons.Select(item => item.weaponId).Distinct().Count(),
                Is.EqualTo(FormalContentCatalog.LaunchWeapons.Length));
            Assert.That(FormalContentCatalog.LaunchWeapons, Has.All.Matches<FormalWeaponContent>(
                item => item.idealRangeMin >= 0f && item.idealRangeMax > item.idealRangeMin));
        }

        [Test]
        public void InvalidCosmeticSlotIsRejected()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => FormalContentCatalog.GetSlotName(3));
        }
    }
}
