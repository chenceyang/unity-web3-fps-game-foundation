using UnityEngine;

namespace Web3Fps.GameFoundation.Gameplay.Combat
{
    [DisallowMultipleComponent]
    public sealed class DamageZone : MonoBehaviour, IDamageable
    {
        [SerializeField] private Health target;
        [SerializeField] private string zoneId = "torso";
        [SerializeField, Min(0f)] private float damageMultiplier = 1f;

        public string ZoneId => zoneId;
        public float DamageMultiplier => damageMultiplier;

        public void Configure(Health health, string id, float multiplier)
        {
            target = health;
            zoneId = string.IsNullOrWhiteSpace(id) ? "torso" : id;
            damageMultiplier = Mathf.Max(0f, multiplier);
        }

        public bool ApplyDamage(DamageInfo damage)
        {
            if (target == null) return false;
            damage.Amount = DamageZoneMath.ScaleDamage(damage.Amount, damageMultiplier);
            return target.ApplyDamage(damage);
        }
    }

    public static class DamageZoneMath
    {
        public static float ScaleDamage(float amount, float multiplier)
        {
            if (float.IsNaN(amount) || float.IsNaN(multiplier)) return 0f;
            return Mathf.Max(0f, amount) * Mathf.Max(0f, multiplier);
        }
    }
}
