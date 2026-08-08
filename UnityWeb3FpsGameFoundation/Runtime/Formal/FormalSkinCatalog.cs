using System;
using System.Collections.Generic;
using UnityEngine;

namespace Web3Fps.GameFoundation.Formal
{
    /// <summary>
    /// Visual identity for one skinDefId: colors and flags only. Cosmetics never
    /// carry combat numbers, and unknown ids resolve to a clearly neutral default.
    /// </summary>
    [Serializable]
    public sealed class FormalSkinVisualSpec
    {
        public uint skinDefId;
        public string displayName = string.Empty;
        public string weaponFamily = string.Empty;
        public Color primaryColor = Color.white;
        public Color secondaryColor = Color.gray;
        public Color emissionColor = Color.black;
        public float emissionIntensity;
        public bool hasPattern;

        public bool HasEmission => emissionIntensity > 0f;

        public FormalSkinVisualSpec(
            uint id,
            string name,
            string family,
            Color primary,
            Color secondary,
            Color emission,
            float emissionStrength,
            bool patterned)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("displayName is required", nameof(name));
            if (emissionStrength < 0f) throw new ArgumentOutOfRangeException(nameof(emissionStrength));
            skinDefId = id;
            displayName = name;
            weaponFamily = family ?? string.Empty;
            primaryColor = primary;
            secondaryColor = secondary;
            emissionColor = emission;
            emissionIntensity = emissionStrength;
            hasPattern = patterned;
        }
    }

    /// <summary>
    /// skinDefId → visual spec for every id seeded by the live stack — the union of
    /// backend/src/catalog.ts and contracts/script/SeedSkins.s.sol (1001, 1010, 1025,
    /// 1042, 1077). Names mirror the backend catalog; weapon families are display
    /// text only. Data-only and side-effect free so it stays fully unit-testable.
    /// </summary>
    public static class FormalSkinCatalog
    {
        public static readonly FormalSkinVisualSpec DefaultSpec = new FormalSkinVisualSpec(
            0, "Default Issue", "Any",
            new Color(0.72f, 0.73f, 0.67f), new Color(0.16f, 0.2f, 0.23f),
            Color.black, 0f, false);

        private static readonly Dictionary<uint, FormalSkinVisualSpec> Entries = Build();

        public static readonly uint[] SeededSkinDefIds = { 1001, 1010, 1025, 1042, 1077 };

        public static bool TryGet(uint skinDefId, out FormalSkinVisualSpec spec) =>
            Entries.TryGetValue(skinDefId, out spec);

        public static FormalSkinVisualSpec GetOrDefault(uint skinDefId)
        {
            FormalSkinVisualSpec spec;
            return Entries.TryGetValue(skinDefId, out spec) ? spec : DefaultSpec;
        }

        private static Dictionary<uint, FormalSkinVisualSpec> Build()
        {
            var entries = new[]
            {
                new FormalSkinVisualSpec(
                    1001, "Standard AK-47", "AK-47",
                    new Color(0.64f, 0.66f, 0.6f), new Color(0.2f, 0.23f, 0.25f),
                    Color.black, 0f, false),
                new FormalSkinVisualSpec(
                    1010, "Desert Tan M4", "M4",
                    new Color(0.76f, 0.62f, 0.4f), new Color(0.36f, 0.28f, 0.18f),
                    Color.black, 0f, false),
                new FormalSkinVisualSpec(
                    1025, "Urban Camo AWP", "AWP",
                    new Color(0.45f, 0.5f, 0.55f), new Color(0.16f, 0.18f, 0.21f),
                    Color.black, 0f, true),
                new FormalSkinVisualSpec(
                    1042, "Frostbite AK-47", "AK-47",
                    new Color(0.62f, 0.82f, 0.94f), new Color(0.1f, 0.24f, 0.42f),
                    new Color(0.35f, 0.75f, 1f), 1.2f, false),
                new FormalSkinVisualSpec(
                    1077, "Solar Flare AWP", "AWP",
                    new Color(0.95f, 0.6f, 0.2f), new Color(0.4f, 0.16f, 0.06f),
                    new Color(1f, 0.5f, 0.12f), 1.6f, false)
            };
            var map = new Dictionary<uint, FormalSkinVisualSpec>(entries.Length);
            foreach (var entry in entries) map.Add(entry.skinDefId, entry);
            return map;
        }
    }
}
