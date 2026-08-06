using System;

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

        public FormalWeaponContent(
            string id,
            string name,
            string weaponCategory,
            string gameplayRole,
            float rangeMin,
            float rangeMax)
        {
            weaponId = id;
            displayName = name;
            category = weaponCategory;
            role = gameplayRole;
            idealRangeMin = rangeMin;
            idealRangeMax = rangeMax;
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

        public static readonly FormalWeaponContent[] LaunchWeapons =
        {
            new FormalWeaponContent("kestrel-7", "KESTREL-7", "Rifle", "Versatile baseline", 12f, 32f),
            new FormalWeaponContent("pulse-9", "PULSE-9", "SMG", "Fast close-range flank", 5f, 18f),
            new FormalWeaponContent("relay-3", "RELAY-3", "Pistol", "Reliable sidearm", 4f, 20f)
        };

        public static string GetSlotName(int slot)
        {
            if (slot < 0 || slot >= CosmeticSlotNames.Length)
                throw new ArgumentOutOfRangeException(nameof(slot));
            return CosmeticSlotNames[slot];
        }
    }
}
