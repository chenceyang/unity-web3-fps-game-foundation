using System;
using Web3Fps.GameFoundation.Gameplay.Combat;

namespace Web3Fps.GameFoundation.Formal
{
    public enum FormalCosmeticSlot
    {
        WeaponFinish = 0,
        OperatorShell = 1,
        IdentitySignal = 2
    }

    [Serializable]
    public sealed class FormalWeaponContent
    {
        public string weaponId = string.Empty;
        public string displayName = string.Empty;
        public string category = string.Empty;
        public string role = string.Empty;
        public float idealRangeMin;
        public float idealRangeMax;
        public WeaponDefinition definition;

        public FormalWeaponContent(WeaponDefinition weaponDefinition, string gameplayRole)
        {
            definition = weaponDefinition ?? throw new ArgumentNullException(nameof(weaponDefinition));
            weaponId = weaponDefinition.weaponId;
            displayName = weaponDefinition.displayName;
            category = weaponDefinition.category;
            role = gameplayRole ?? string.Empty;
            idealRangeMin = weaponDefinition.idealRangeMin;
            idealRangeMax = weaponDefinition.idealRangeMax;
        }
    }

    /// <summary>Presentation-only catalog for the ASH//LEDGER vertical slice.</summary>
    public static class FormalContentCatalog
    {
        public const string ProductName = "ASH//LEDGER";
        public const string LobbySceneName = "AshLedgerLobby";
        public const string RiftRelaySceneName = "RiftRelay";
        public const string MapId = "rift-relay";
        public const string ModeId = "prototype-deathmatch";

        public static readonly string[] CosmeticSlotNames =
        {
            "Weapon Finish",
            "Operator Shell",
            "Identity Signal"
        };

        // Combat numbers per FORMAL_CONTENT_DESIGN §6: greybox values whose ideal
        // ranges follow the design table. KESTREL-7 keeps the exact numbers the
        // generated slice has shipped with since v1.5.
        public static readonly FormalWeaponContent[] LaunchWeapons =
        {
            new FormalWeaponContent(new WeaponDefinition(
                "kestrel-7", "KESTREL-7", "Rifle",
                25f, 10f, 30, 120, 1.65f, 150f,
                1.15f, 0.28f, 1.35f, 0.16f, 1.8f, 3.4f,
                12f, 32f), "Versatile baseline"),
            new FormalWeaponContent(new WeaponDefinition(
                "pulse-9", "PULSE-9", "SMG",
                16f, 13.5f, 32, 128, 1.4f, 90f,
                1.7f, 0.5f, 1.1f, 0.14f, 2.2f, 4.2f,
                5f, 18f), "Fast close-range flank"),
            new FormalWeaponContent(new WeaponDefinition(
                "witness", "WITNESS", "Marksman",
                55f, 3.2f, 12, 48, 2.1f, 240f,
                1.9f, 0.12f, 2.2f, 0.3f, 2.4f, 2.8f,
                22f, 50f), "Mid-range precision and information"),
            new FormalWeaponContent(new WeaponDefinition(
                "breach-12", "BREACH-12", "Shotgun",
                85f, 1.5f, 6, 24, 2.6f, 26f,
                3.6f, 2.4f, 1.6f, 0.5f, 4.5f, 5f,
                2f, 10f), "Room breach punishing bad positioning"),
            new FormalWeaponContent(new WeaponDefinition(
                "anchor", "ANCHOR", "LMG",
                22f, 9f, 60, 180, 3.4f, 170f,
                1.6f, 0.5f, 2.6f, 0.1f, 2.6f, 2.6f,
                15f, 38f), "Sustained point-holding fire"),
            new FormalWeaponContent(new WeaponDefinition(
                "relay-3", "RELAY-3", "Pistol",
                20f, 6f, 12, 60, 1.2f, 110f,
                1.3f, 0.4f, 1.2f, 0.2f, 2f, 4f,
                4f, 20f), "Reliable sidearm")
        };

        public static string GetSlotName(int slot)
        {
            if (slot < 0 || slot >= CosmeticSlotNames.Length)
                throw new ArgumentOutOfRangeException(nameof(slot));
            return CosmeticSlotNames[slot];
        }

        public static FormalWeaponContent FindWeapon(string weaponId)
        {
            if (string.IsNullOrWhiteSpace(weaponId)) return null;
            foreach (var weapon in LaunchWeapons)
            {
                if (string.Equals(weapon.weaponId, weaponId, StringComparison.Ordinal)) return weapon;
            }
            return null;
        }
    }
}
