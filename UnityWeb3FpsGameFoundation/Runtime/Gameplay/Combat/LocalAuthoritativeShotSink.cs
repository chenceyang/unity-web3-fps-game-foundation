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
        [SerializeField] private LayerMask hitMask = ~(1 << 2); // Ignore actor movement capsules; use explicit hit zones.
        [SerializeField, Min(0.01f)] private float minimumShotInterval = 0.08f;
        [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide;

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
            if (!TryFindFirstValidHit(command, out hit))
                return new ShotResult { Accepted = true, Hit = false, Point = command.Origin + command.Direction.normalized * command.Range };

            IDamageable damageable = null;
            var behaviours = hit.collider.GetComponentsInParent<MonoBehaviour>();
            for (var i = 0; i < behaviours.Length; i++)
            {
                damageable = behaviours[i] as IDamageable;
                if (damageable != null) break;
            }
            var damage = new DamageInfo
            {
                Amount = command.Damage,
                Source = command.ShooterObject,
                Point = hit.point,
                Direction = command.Direction.normalized,
                ShotSequence = command.Sequence
            };
            var damageApplied = damageable != null && damageable.ApplyDamage(damage);
            var zone = hit.collider.GetComponent<DamageZone>();
            var appliedDamage = zone == null
                ? command.Damage
                : DamageZoneMath.ScaleDamage(command.Damage, zone.DamageMultiplier);

            return new ShotResult
            {
                Accepted = true, Hit = true, Point = hit.point, Normal = hit.normal,
                HitObject = hit.collider.gameObject, DamageApplied = damageApplied,
                AppliedDamage = damageApplied ? appliedDamage : 0f
            };
        }

        private bool TryFindFirstValidHit(ShotCommand command, out RaycastHit hit)
        {
            var direction = command.Direction.normalized;
            var hits = Physics.RaycastAll(command.Origin, direction, command.Range, hitMask, triggerInteraction);
            System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
            for (var i = 0; i < hits.Length; i++)
            {
                var candidate = hits[i];
                if (command.ShooterObject != null &&
                    candidate.collider.transform.IsChildOf(command.ShooterObject.transform)) continue;
                hit = candidate;
                return true;
            }
            hit = default;
            return false;
        }
    }
}
