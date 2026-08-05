using System;
using UnityEngine;

namespace Web3Fps.GameFoundation.Gameplay.Combat
{
    public sealed class Health : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1f)] private float maximum = 100f;
        [SerializeField] private MonoBehaviour authorityProvider;

        public float Current { get; private set; }
        public float Maximum => maximum;
        public bool IsDead => Current <= 0f;

        public event Action<DamageInfo, float> Damaged;
        public event Action<DamageInfo> Died;
        public event Action<float> Reset;

        private void Awake() => Current = maximum;

        public bool ApplyDamage(DamageInfo damage)
        {
            if (!HasAuthority() || IsDead || damage.Amount <= 0f || float.IsNaN(damage.Amount)) return false;
            Current = Mathf.Max(0f, Current - damage.Amount);
            Damaged?.Invoke(damage, Current);
            if (IsDead) Died?.Invoke(damage);
            return true;
        }

        public bool RestoreToFull()
        {
            if (!HasAuthority()) return false;
            Current = maximum;
            Reset?.Invoke(Current);
            return true;
        }

        private bool HasAuthority()
        {
            if (authorityProvider == null) return true; // Offline or dedicated-server scene.
            var provider = authorityProvider as IAuthorityProvider;
            return provider != null && provider.IsAuthoritative;
        }
    }
}
