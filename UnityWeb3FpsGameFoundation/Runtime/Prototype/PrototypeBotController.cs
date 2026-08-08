using System;
using UnityEngine;
using Web3Fps.GameFoundation.Gameplay.Combat;

namespace Web3Fps.GameFoundation.Prototype
{
    public enum PrototypeBotDifficulty
    {
        Recruit,
        Standard,
        Veteran
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController), typeof(Health))]
    public sealed class PrototypeBotController : MonoBehaviour
    {
        [SerializeField] private PrototypeParticipant self;
        [SerializeField] private PrototypeParticipant target;
        [SerializeField] private PrototypeBotDifficulty difficulty = PrototypeBotDifficulty.Standard;
        [SerializeField, Min(0.1f)] private float moveSpeed = 3.8f;
        [SerializeField, Min(1f)] private float detectionRange = 42f;
        [SerializeField, Min(1f)] private float attackRange = 18f;
        [SerializeField, Min(1f)] private float preferredRange = 10.5f;
        [SerializeField, Min(0.1f)] private float attacksPerSecond = 2.2f;
        [SerializeField, Min(0.1f)] private float damage = 10f;
        [SerializeField, Min(0f)] private float reactionSeconds = 0.38f;
        [SerializeField, Min(0f)] private float sightMemorySeconds = 1.6f;
        [SerializeField, Min(0f)] private float aimSpreadDegrees = 2.1f;
        [SerializeField] private LayerMask sightMask = ~(1 << 2);

        // Shared scratch buffer for the per-frame sight/steering casts. Bots update
        // sequentially on the main thread, and the allocating RaycastAll/SphereCastAll
        // variants were a steady per-frame garbage source that caused GC hitches.
        private static readonly RaycastHit[] CastBuffer = new RaycastHit[24];

        private CharacterController _controller;
        private Health _health;
        private double _nextAttackAt;
        private double _firstSeenAt = -1d;
        private double _lastSeenAt = -999d;
        private double _nextStrafeSwitchAt;
        private float _avoidanceSide;
        private Vector3 _lastKnownTarget;
        private uint _shotSequence;

        public event Action<Vector3, Vector3, bool> ShotResolved;

        public void Configure(PrototypeParticipant bot, PrototypeParticipant player)
        {
            self = bot;
            target = player;
            ResolveComponents();
        }

        public void ConfigureDifficulty(PrototypeBotDifficulty value)
        {
            difficulty = value;
            switch (difficulty)
            {
                case PrototypeBotDifficulty.Recruit:
                    reactionSeconds = 0.65f;
                    attacksPerSecond = 1.55f;
                    aimSpreadDegrees = 3.4f;
                    break;
                case PrototypeBotDifficulty.Veteran:
                    reactionSeconds = 0.2f;
                    attacksPerSecond = 2.8f;
                    aimSpreadDegrees = 1.15f;
                    break;
                default:
                    reactionSeconds = 0.38f;
                    attacksPerSecond = 2.2f;
                    aimSpreadDegrees = 2.1f;
                    break;
            }
        }

        private void Awake() => ResolveComponents();

        private void ResolveComponents()
        {
            _controller = GetComponent<CharacterController>();
            _health = GetComponent<Health>();
            if (Mathf.Approximately(_avoidanceSide, 0f)) _avoidanceSide = GetInstanceID() % 2 == 0 ? 1f : -1f;
        }

        private void Update()
        {
            if (self == null || target == null || _health == null || _health.IsDead ||
                target.Health == null || target.Health.IsDead) return;

            var now = Time.timeAsDouble;
            var origin = transform.position + Vector3.up * 1.45f;
            var visible = CanSeeTarget(origin, out var targetPoint);
            if (visible)
            {
                if (_firstSeenAt < 0d || now - _lastSeenAt > sightMemorySeconds) _firstSeenAt = now;
                _lastSeenAt = now;
                _lastKnownTarget = targetPoint;
            }
            else if (!PrototypeBotDecisionMath.RemembersTarget(now, _lastSeenAt, sightMemorySeconds))
            {
                _firstSeenAt = -1d;
                return;
            }

            UpdateStrafe(now);
            MoveAndFace(visible);
            if (visible && PrototypeBotDecisionMath.CanAttack(now, _firstSeenAt, reactionSeconds) &&
                Vector3.Distance(origin, targetPoint) <= attackRange) TryAttack(origin, targetPoint);
        }

        private void MoveAndFace(bool targetVisible)
        {
            var offset = _lastKnownTarget - transform.position;
            var flatOffset = Vector3.ProjectOnPlane(offset, Vector3.up);
            if (flatOffset.sqrMagnitude <= 0.01f || _controller == null) return;

            var forward = flatOffset.normalized;
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                Quaternion.LookRotation(forward, Vector3.up),
                420f * Time.deltaTime);

            var rangeSign = PrototypeBotDecisionMath.RangeMoveSign(flatOffset.magnitude, preferredRange);
            var strafe = Vector3.Cross(Vector3.up, forward) * _avoidanceSide;
            var direction = forward * rangeSign;
            if (targetVisible && Mathf.Abs(flatOffset.magnitude - preferredRange) < preferredRange * 0.55f)
                direction = (direction * 0.35f + strafe * 0.85f).normalized;
            var blocked = direction.sqrMagnitude > 0.001f && IsPathBlocked(direction);
            direction = PrototypeBotSteering.Resolve(direction, blocked, _avoidanceSide);
            if (direction.sqrMagnitude > 0.001f) _controller.SimpleMove(direction * moveSpeed);
        }

        private void UpdateStrafe(double now)
        {
            if (now < _nextStrafeSwitchAt) return;
            _avoidanceSide *= -1f;
            _nextStrafeSwitchAt = now + 1.25d + ((_shotSequence * 37u) % 100u) / 100d;
        }

        private bool IsPathBlocked(Vector3 direction)
        {
            if (_controller == null) return false;
            var origin = transform.position + Vector3.up * Mathf.Max(0.45f, _controller.radius);
            var count = Physics.SphereCastNonAlloc(
                origin, _controller.radius * 0.72f, direction, CastBuffer, 1.35f, sightMask, QueryTriggerInteraction.Ignore);
            for (var i = 0; i < count; i++)
            {
                var participant = CastBuffer[i].collider.GetComponentInParent<PrototypeParticipant>();
                if (participant == self || participant == target) continue;
                return true;
            }
            return false;
        }

        private bool CanSeeTarget(Vector3 origin, out Vector3 point)
        {
            point = target.AimPoint;
            var offset = point - origin;
            if (offset.sqrMagnitude > detectionRange * detectionRange) return false;
            return TryGetFirstParticipant(origin, offset.normalized, offset.magnitude + 0.35f, out var participant, out _) &&
                   participant == target;
        }

        private void TryAttack(Vector3 origin, Vector3 targetPoint)
        {
            if (Time.timeAsDouble < _nextAttackAt) return;
            _nextAttackAt = Time.timeAsDouble + 1d / attacksPerSecond;
            var direction = WeaponAccuracyMath.ApplySpread((targetPoint - origin).normalized, ++_shotSequence, aimSpreadDegrees);
            if (!TryGetFirstParticipant(origin, direction, attackRange, out var hitParticipant, out var hit))
            {
                ShotResolved?.Invoke(origin, origin + direction * attackRange, false);
                return;
            }
            if (hitParticipant != target)
            {
                ShotResolved?.Invoke(origin, hit.point, false);
                return;
            }

            var damageable = hit.collider.GetComponentInParent<IDamageable>();
            var applied = damageable != null && damageable.ApplyDamage(new DamageInfo
            {
                Amount = damage,
                Source = gameObject,
                Point = hit.point,
                Direction = direction,
                ShotSequence = _shotSequence
            });
            ShotResolved?.Invoke(origin, hit.point, applied);
        }

        private bool TryGetFirstParticipant(
            Vector3 origin,
            Vector3 direction,
            float range,
            out PrototypeParticipant participant,
            out RaycastHit hit)
        {
            var count = Physics.RaycastNonAlloc(origin, direction, CastBuffer, range, sightMask, QueryTriggerInteraction.Collide);
            var bestDistance = float.MaxValue;
            var bestIndex = -1;
            for (var i = 0; i < count; i++)
            {
                if (CastBuffer[i].distance >= bestDistance) continue;
                if (CastBuffer[i].collider.transform.IsChildOf(transform)) continue;
                bestDistance = CastBuffer[i].distance;
                bestIndex = i;
            }
            if (bestIndex < 0)
            {
                hit = default;
                participant = null;
                return false;
            }
            hit = CastBuffer[bestIndex];
            participant = hit.collider.GetComponentInParent<PrototypeParticipant>();
            return true;
        }
    }

    public static class PrototypeBotDecisionMath
    {
        public static bool RemembersTarget(double now, double lastSeenAt, float memorySeconds)
        {
            return memorySeconds > 0f && now >= lastSeenAt && now - lastSeenAt <= memorySeconds;
        }

        public static bool CanAttack(double now, double firstSeenAt, float reactionSeconds)
        {
            return firstSeenAt >= 0d && now - firstSeenAt >= Mathf.Max(0f, reactionSeconds);
        }

        public static float RangeMoveSign(float distance, float preferredRange)
        {
            var range = Mathf.Max(0.1f, preferredRange);
            if (distance > range * 1.15f) return 1f;
            if (distance < range * 0.65f) return -1f;
            return 0f;
        }
    }
}
