using System;

namespace Web3Fps.GameFoundation.Gameplay.Combat
{
    /// <summary>
    /// Server-approved combat numbers for one weapon. This catalog data is the only
    /// source HitscanWeapon configuration may come from; NFT content never carries or
    /// overrides damage, fire rate, spread, recoil, range, reload time or colliders.
    /// </summary>
    [Serializable]
    public sealed class WeaponDefinition
    {
        public string weaponId = string.Empty;
        public string displayName = string.Empty;
        public string category = string.Empty;
        public float damage;
        public float roundsPerSecond;
        public int magazineCapacity;
        public int reserveAmmo;
        public float reloadSeconds;
        public float range;
        public float hipSpreadDegrees;
        public float aimSpreadDegrees;
        public float movementSpreadDegrees;
        public float bloomPerShotDegrees;
        public float maximumBloomDegrees;
        public float bloomRecoveryPerSecond;
        public float idealRangeMin;
        public float idealRangeMax;

        public WeaponDefinition(
            string id,
            string name,
            string weaponCategory,
            float damagePerShot,
            float fireRate,
            int magazine,
            int reserve,
            float reloadDuration,
            float maxRange,
            float hipSpread,
            float aimSpread,
            float movementSpread,
            float bloomPerShot,
            float maximumBloom,
            float bloomRecovery,
            float rangeMin,
            float rangeMax)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("weaponId is required", nameof(id));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("displayName is required", nameof(name));
            if (damagePerShot <= 0f) throw new ArgumentOutOfRangeException(nameof(damagePerShot));
            if (fireRate <= 0f) throw new ArgumentOutOfRangeException(nameof(fireRate));
            if (magazine < 1) throw new ArgumentOutOfRangeException(nameof(magazine));
            if (reserve < 0) throw new ArgumentOutOfRangeException(nameof(reserve));
            if (reloadDuration <= 0f) throw new ArgumentOutOfRangeException(nameof(reloadDuration));
            if (maxRange <= 0f) throw new ArgumentOutOfRangeException(nameof(maxRange));
            if (hipSpread < 0f || aimSpread < 0f || movementSpread < 0f ||
                bloomPerShot < 0f || maximumBloom < 0f || bloomRecovery < 0f)
                throw new ArgumentOutOfRangeException(nameof(hipSpread), "Spread values cannot be negative");
            if (rangeMin < 0f || rangeMax <= rangeMin)
                throw new ArgumentOutOfRangeException(nameof(rangeMax), "Ideal range must be a positive interval");
            weaponId = id;
            displayName = name;
            category = weaponCategory ?? string.Empty;
            damage = damagePerShot;
            roundsPerSecond = fireRate;
            magazineCapacity = magazine;
            reserveAmmo = reserve;
            reloadSeconds = reloadDuration;
            range = maxRange;
            hipSpreadDegrees = hipSpread;
            aimSpreadDegrees = aimSpread;
            movementSpreadDegrees = movementSpread;
            bloomPerShotDegrees = bloomPerShot;
            maximumBloomDegrees = maximumBloom;
            bloomRecoveryPerSecond = bloomRecovery;
            idealRangeMin = rangeMin;
            idealRangeMax = rangeMax;
        }
    }
}
