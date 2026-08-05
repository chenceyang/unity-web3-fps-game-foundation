using UnityEngine;

namespace Web3Fps.GameFoundation.Gameplay.Combat
{
    public struct DamageInfo
    {
        public float Amount;
        public GameObject Source;
        public Vector3 Point;
        public Vector3 Direction;
        public uint ShotSequence;
    }

    public interface IDamageable
    {
        bool ApplyDamage(DamageInfo damage);
    }

    public interface IAuthorityProvider
    {
        bool IsAuthoritative { get; }
    }
}
