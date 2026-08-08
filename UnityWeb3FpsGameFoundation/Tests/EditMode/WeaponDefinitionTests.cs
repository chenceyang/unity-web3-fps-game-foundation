using System;
using System.Linq;
using NUnit.Framework;
using Web3Fps.GameFoundation.Formal;
using Web3Fps.GameFoundation.Gameplay.Combat;

namespace Web3Fps.GameFoundation.Tests
{
    public sealed class WeaponDefinitionTests
    {
        [Test]
        public void CatalogCoversAllSixDesignedWeaponRoles()
        {
            var ids = FormalContentCatalog.LaunchWeapons.Select(item => item.weaponId).ToArray();

            Assert.That(ids, Is.EqualTo(new[] { "kestrel-7", "pulse-9", "witness", "breach-12", "anchor", "relay-3" }));
            Assert.That(FormalContentCatalog.LaunchWeapons.Select(item => item.definition).ToArray(),
                Has.All.Not.Null);
        }

        // Ideal ranges straight from the FORMAL_CONTENT_DESIGN §6 weapon table.
        [TestCase("kestrel-7", 12f, 32f)]
        [TestCase("pulse-9", 5f, 18f)]
        [TestCase("witness", 22f, 50f)]
        [TestCase("breach-12", 2f, 10f)]
        [TestCase("anchor", 15f, 38f)]
        [TestCase("relay-3", 4f, 20f)]
        public void IdealRangesMatchTheDesignTable(string weaponId, float min, float max)
        {
            var weapon = FormalContentCatalog.FindWeapon(weaponId);

            Assert.That(weapon, Is.Not.Null);
            Assert.That(weapon.definition.idealRangeMin, Is.EqualTo(min));
            Assert.That(weapon.definition.idealRangeMax, Is.EqualTo(max));
        }

        [Test]
        public void KestrelPreservesTheShippedCombatNumbers()
        {
            var definition = FormalContentCatalog.FindWeapon("kestrel-7").definition;

            Assert.That(definition.damage, Is.EqualTo(25f));
            Assert.That(definition.roundsPerSecond, Is.EqualTo(10f));
            Assert.That(definition.magazineCapacity, Is.EqualTo(30));
            Assert.That(definition.reserveAmmo, Is.EqualTo(120));
            Assert.That(definition.reloadSeconds, Is.EqualTo(1.65f));
            Assert.That(definition.range, Is.EqualTo(150f));
            Assert.That(definition.hipSpreadDegrees, Is.EqualTo(1.15f));
            Assert.That(definition.aimSpreadDegrees, Is.EqualTo(0.28f));
        }

        [Test]
        public void AllDefinitionsCarryPositiveCombatStats()
        {
            foreach (var weapon in FormalContentCatalog.LaunchWeapons)
            {
                var definition = weapon.definition;
                Assert.That(definition.damage, Is.GreaterThan(0f), weapon.weaponId);
                Assert.That(definition.roundsPerSecond, Is.GreaterThan(0f), weapon.weaponId);
                Assert.That(definition.magazineCapacity, Is.GreaterThan(0), weapon.weaponId);
                Assert.That(definition.reserveAmmo, Is.GreaterThanOrEqualTo(0), weapon.weaponId);
                Assert.That(definition.reloadSeconds, Is.GreaterThan(0f), weapon.weaponId);
                Assert.That(definition.range, Is.GreaterThan(definition.idealRangeMax), weapon.weaponId);
            }
        }

        [Test]
        public void InvalidDefinitionsAreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new WeaponDefinition(
                "broken", "BROKEN", "Rifle", 0f, 10f, 30, 120, 1.5f, 100f,
                1f, 0.3f, 1f, 0.1f, 2f, 3f, 5f, 20f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new WeaponDefinition(
                "broken", "BROKEN", "Rifle", 20f, 10f, 30, 120, 1.5f, 100f,
                1f, 0.3f, 1f, 0.1f, 2f, 3f, 20f, 5f));
        }
    }
}
