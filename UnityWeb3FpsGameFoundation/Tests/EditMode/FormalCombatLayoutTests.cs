using NUnit.Framework;
using Web3Fps.GameFoundation.Formal;

namespace Web3Fps.GameFoundation.Tests
{
    public sealed class FormalCombatLayoutTests
    {
        [Test]
        public void OpeningSightLineClearsCentralRelay()
        {
            Assert.That(FormalCombatLayout.OpeningLaneClears(0f, FormalCombatLayout.RelayFootprintRadius), Is.True);
        }

        [Test]
        public void OpeningSightLineClearsNearestCoverRow()
        {
            Assert.That(FormalCombatLayout.OpeningLaneClears(
                FormalCombatLayout.FurthestCoverCenterZ,
                FormalCombatLayout.CoverHalfDepth), Is.True);
        }
    }
}
