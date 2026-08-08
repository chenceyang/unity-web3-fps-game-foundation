using System.Collections.Generic;
using NUnit.Framework;
using Web3Fps.GameFoundation.Formal;

namespace Web3Fps.GameFoundation.Tests
{
    public sealed class FormalSkinCatalogTests
    {
        [Test]
        public void EverySeededSkinDefIdFromBackendAndContractsIsCovered()
        {
            // Union of backend/src/catalog.ts and contracts/script/SeedSkins.s.sol.
            var expected = new uint[] { 1001, 1010, 1025, 1042, 1077 };

            Assert.That(FormalSkinCatalog.SeededSkinDefIds, Is.EquivalentTo(expected));
            foreach (var skinDefId in expected)
            {
                Assert.That(FormalSkinCatalog.TryGet(skinDefId, out var spec), Is.True, "missing " + skinDefId);
                Assert.That(spec.skinDefId, Is.EqualTo(skinDefId));
            }
        }

        [Test]
        public void DisplayNamesMirrorTheBackendCatalog()
        {
            Assert.That(FormalSkinCatalog.GetOrDefault(1001).displayName, Is.EqualTo("Standard AK-47"));
            Assert.That(FormalSkinCatalog.GetOrDefault(1010).displayName, Is.EqualTo("Desert Tan M4"));
            Assert.That(FormalSkinCatalog.GetOrDefault(1025).displayName, Is.EqualTo("Urban Camo AWP"));
            Assert.That(FormalSkinCatalog.GetOrDefault(1042).displayName, Is.EqualTo("Frostbite AK-47"));
            Assert.That(FormalSkinCatalog.GetOrDefault(1077).displayName, Is.EqualTo("Solar Flare AWP"));
        }

        [Test]
        public void UnknownIdFallsBackToTheNeutralDefault()
        {
            var spec = FormalSkinCatalog.GetOrDefault(424242);

            Assert.That(spec, Is.SameAs(FormalSkinCatalog.DefaultSpec));
            Assert.That(spec.HasEmission, Is.False);
            Assert.That(spec.hasPattern, Is.False);
            Assert.That(spec.skinDefId, Is.EqualTo(0));
        }

        [Test]
        public void SeededSpecsKeepDistinctPrimaryColors()
        {
            var seen = new HashSet<UnityEngine.Color>();
            foreach (var skinDefId in FormalSkinCatalog.SeededSkinDefIds)
                Assert.That(seen.Add(FormalSkinCatalog.GetOrDefault(skinDefId).primaryColor), Is.True);
        }

        [Test]
        public void ApplicatorPartClassificationDrivesAccentAndPatternColors()
        {
            var patterned = FormalSkinCatalog.GetOrDefault(1025);
            var plain = FormalSkinCatalog.GetOrDefault(1010);

            Assert.That(FormalSkinApplicator.ResolvePartColor("AccentRail", plain), Is.EqualTo(plain.secondaryColor));
            Assert.That(FormalSkinApplicator.ResolvePartColor("Magazine", plain), Is.EqualTo(plain.secondaryColor));
            Assert.That(FormalSkinApplicator.ResolvePartColor("Receiver", plain), Is.EqualTo(plain.primaryColor));
            Assert.That(FormalSkinApplicator.ResolvePartColor("BarrelShroud", plain), Is.EqualTo(plain.primaryColor));
            Assert.That(FormalSkinApplicator.ResolvePartColor("BarrelShroud", patterned), Is.EqualTo(patterned.secondaryColor));
        }
    }
}
