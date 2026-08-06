using System;
using UnityEngine;
using Web3Fps.GameFoundation.Gameplay.Combat;

namespace Web3Fps.GameFoundation.Prototype
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController), typeof(Health))]
    public sealed class PrototypeBotController : MonoBehaviour
    {
        [SerializeField] private PrototypeParticipant self;
        [SerializeField] private PrototypeParticipant target;
        [SerializeField, Min(0.1f)] private float moveSpeed = 3.5f;
        [SerializeField, Min(1f)] private float attackRange = 14f;
        [SerializeField, Min(0.1f)] private float attacksPerSecond = 1.5f;
        [SerializeField, Min(0.1f)] private float damage = 12f;
        [SerializeField] private LayerMask sightMask = ~0;

        private CharacterController _controller;
        private Health _health;
        private double _nextAttackAt;
        private float _avoidanceSide;

        public event Action<Vector3, Vector3, bool> ShotResolved;

        public void Configure(PrototypeParticipant bot, PrototypeParticipant player)
        {
            self = bot;
            target = player;
            ResolveComponents();
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

            var offset = target.transform.position - transform.position;
            var flatOffset = Vector3.ProjectOnPlane(offset, Vector3.up);
            if (flatOffset.sqrMagnitude > 0.01f)
            {
                var desiredDirection = flatOffset.normalized;
                var pathBlocked = IsPathBlocked(desiredDirection);
                var direction = PrototypeBotSteering.Resolve(desiredDirection, pathBlocked, _avoidanceSide);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    Quaternion.LookRotation(direction, Vector3.up),
                    360f * Time.deltaTime);
                if (flatOffset.magnitude > attackRange * 0.65f || pathBlocked)
                    _controller.SimpleMove(direction * moveSpeed);
            }

            if (offset.sqrMagnitude <= attackRange * attackRange) TryAttack();
        }

        private bool IsPathBlocked(Vector3 direction)
        {
            if (_controller == null) return false;
            var origin = transform.position + Vector3.up * Mathf.Max(0.45f, _controller.radius);
            var hits = Physics.SphereCastAll(
                origin,
                _controller.radius * 0.72f,
                direction,
                1.35f,
                sightMask,
                QueryTriggerInteraction.Ignore);
            foreach (var hit in hits)
            {
                var participant = hit.collider.GetComponentInParent<PrototypeParticipant>();
                if (participant == self || participant == target) continue;
                return true;
            }
            return false;
        }

        private void TryAttack()
        {
            if (Time.timeAsDouble < _nextAttackAt) return;
            _nextAttackAt = Time.timeAsDouble + 1d / attacksPerSecond;
            var origin = transform.position + Vector3.up * 1.45f;
            var direction = target.AimPoint - origin;
            RaycastHit hit;
            if (!Physics.Raycast(origin, direction.normalized, out hit, attackRange, sightMask, QueryTriggerInteraction.Ignore))
            {
                ShotResolved?.Invoke(origin, origin + direction.normalized * attackRange, false);
                return;
            }
            var hitParticipant = hit.collider.GetComponentInParent<PrototypeParticipant>();
            if (hitParticipant != target)
            {
                ShotResolved?.Invoke(origin, hit.point, false);
                return;
            }
            target.Health.ApplyDamage(new DamageInfo
            {
                Amount = damage,
                Source = gameObject,
                Point = hit.point,
                Direction = direction.normalized,
                ShotSequence = 0
            });
            ShotResolved?.Invoke(origin, hit.point, true);
        }
    }
}
