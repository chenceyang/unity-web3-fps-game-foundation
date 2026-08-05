using System;
using UnityEngine;

namespace Web3Fps.GameFoundation.Gameplay.Combat
{
    public sealed class HitscanWeapon : MonoBehaviour
    {
        [SerializeField] private string shooterId = "local-player";
        [SerializeField] private Transform muzzle;
        [SerializeField] private MonoBehaviour shotSink;
        [SerializeField, Min(0.1f)] private float damage = 25f;
        [SerializeField, Min(1f)] private float range = 150f;
        [SerializeField, Min(0.1f)] private float roundsPerSecond = 10f;

        private IShotCommandSink _sink;
        private double _nextLocalShotAt;
        private uint _sequence;

        public event Action<ShotResult> ShotResolved;

        private void Awake()
        {
            _sink = shotSink as IShotCommandSink;
            if (_sink == null) Debug.LogError("HitscanWeapon requires a component implementing IShotCommandSink", this);
        }

        public bool TryFire(Vector3 aimDirection)
        {
            if (_sink == null || muzzle == null || Time.timeAsDouble < _nextLocalShotAt) return false;
            _nextLocalShotAt = Time.timeAsDouble + 1d / roundsPerSecond;
            var result = _sink.Submit(new ShotCommand
            {
                ShooterId = shooterId,
                ShooterObject = gameObject,
                Origin = muzzle.position,
                Direction = aimDirection.normalized,
                Sequence = ++_sequence,
                ClientTimestamp = Time.timeAsDouble,
                Damage = damage,
                Range = range
            });
            ShotResolved?.Invoke(result);
            return result.Accepted;
        }
    }
}
