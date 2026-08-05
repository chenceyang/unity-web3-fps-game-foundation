using System.Collections.Generic;
using UnityEngine;

namespace Web3Fps.GameFoundation.Gameplay.Combat
{
    /// <summary>
    /// Offline/dedicated-server resolver. A networking package should replace this sink with an
    /// adapter that transports ShotCommand to the authoritative server and returns replicated FX.
    /// </summary>
    public sealed class LocalAuthoritativeShotSink : MonoBehaviour, IShotCommandSink
    {
        [SerializeField] private LayerMask hitMask = ~0;
        [SerializeField, Min(0.01f)] private float minimumShotInterval = 0.08f;
        [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

        private readonly Dictionary<string, double> _lastShotAt = new Dictionary<string, double>();
        private readonly Dictionary<string, uint> _lastSequence = new Dictionary<string, uint>();

        public ShotResult Submit(ShotCommand command)
        {
            if (string.IsNullOrWhiteSpace(command.ShooterId) || command.Direction.sqrMagnitude < 0.99f ||
                command.Damage <= 0f || command.Range <= 0f)
                return new ShotResult { Accepted = false };

            var now = Time.timeAsDouble;
            double lastAt;
            uint lastSequence;
            if (_lastShotAt.TryGetValue(command.ShooterId, out lastAt) && now - lastAt < minimumShotInterval)
                return new ShotResult { Accepted = false };
            if (_lastSequence.TryGetValue(command.ShooterId, out lastSequence) && command.Sequence <= lastSequence)
                return new ShotResult { Accepted = false };

            _lastShotAt[command.ShooterId] = now;
            _lastSequence[command.ShooterId] = command.Sequence;

            RaycastHit hit;
            if (!Physics.Raycast(command.Origin, command.Direction.normalized, out hit, command.Range, hitMask, triggerInteraction))
                return new ShotResult { Accepted = true, Hit = false, Point = command.Origin + command.Direction.normalized * command.Range };

            IDamageable damageable = null;
            var behaviours = hit.collider.GetComponentsInParent<MonoBehaviour>();
            for (var i = 0; i < behaviours.Length; i++)
            {
                damageable = behaviours[i] as IDamageable;
                if (damageable != null) break;
            }
            damageable?.ApplyDamage(new DamageInfo
            {
                Amount = command.Damage,
                Source = command.ShooterObject,
                Point = hit.point,
                Direction = command.Direction.normalized,
                ShotSequence = command.Sequence
            });

            return new ShotResult
            {
                Accepted = true, Hit = true, Point = hit.point, Normal = hit.normal,
                HitObject = hit.collider.gameObject
            };
        }
    }
}
