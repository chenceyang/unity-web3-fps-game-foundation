using UnityEngine;
using Web3Fps.GameFoundation.Prototype;

namespace Web3Fps.GameFoundation.Formal
{
    public enum FormalRigRecoveryState
    {
        Healthy,
        AnimationDisabled,
        FallbackActive
    }

    [DisallowMultipleComponent]
    public sealed class FormalCharacterRigGuard : MonoBehaviour
    {
        [SerializeField] private GameObject rigRoot;
        [SerializeField] private GameObject fallbackRoot;
        [SerializeField] private Animator animator;
        [SerializeField, Min(1f)] private float maximumExtent = 4.5f;
        [SerializeField, Min(1f)] private float maximumCenterOffset = 3f;

        private PoseNode[] _restPose;
        private Renderer[] _renderers;
        private bool _animationDisabled;
        private bool _fallbackActivated;
        private PrototypeParticipant _participant;

        public FormalRigRecoveryState RecoveryState => _fallbackActivated
            ? FormalRigRecoveryState.FallbackActive
            : _animationDisabled ? FormalRigRecoveryState.AnimationDisabled : FormalRigRecoveryState.Healthy;

        public void Configure(GameObject skeletalRig, GameObject proceduralFallback, Animator targetAnimator)
        {
            rigRoot = skeletalRig;
            fallbackRoot = proceduralFallback;
            animator = targetAnimator;
            _renderers = null;
        }

        private void Awake()
        {
            CaptureRestPose();
            _participant = GetComponentInParent<PrototypeParticipant>();
        }

        private void OnEnable()
        {
            if (_participant == null) _participant = GetComponentInParent<PrototypeParticipant>();
            if (_participant != null) _participant.Respawned += OnRespawned;
        }

        private void OnDisable()
        {
            if (_participant != null) _participant.Respawned -= OnRespawned;
        }

        private void LateUpdate()
        {
            if (_fallbackActivated || rigRoot == null || IsRigSane()) return;

            if (!_animationDisabled)
            {
                _animationDisabled = true;
                if (animator != null) animator.enabled = false;
                RestoreRestPose();
                Debug.LogWarning("Skeletal animation produced invalid character bounds. Animation was disabled and the imported rest pose was restored.", this);
                return;
            }

            rigRoot.SetActive(false);
            if (fallbackRoot != null) fallbackRoot.SetActive(true);
            _fallbackActivated = true;
            Debug.LogError("Skeletal character bounds remained invalid after pose recovery. Activated the safe humanoid fallback.", this);
        }

        private bool IsRigSane()
        {
            // This runs every frame per character; the renderer set is cached because
            // GetComponentsInChildren allocates a fresh array each call and that steady
            // garbage shows up as periodic GC hitches during combat.
            if (_renderers == null || _renderers.Length == 0)
                _renderers = rigRoot.GetComponentsInChildren<Renderer>(false);
            if (_renderers.Length == 0) return false;
            var hasBounds = false;
            var bounds = new Bounds();
            for (var i = 0; i < _renderers.Length; i++)
            {
                var renderer = _renderers[i];
                if (renderer == null)
                {
                    _renderers = null;
                    return true;
                }
                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }
            return hasBounds && FormalRigBoundsMath.IsSane(
                bounds.size,
                bounds.center - transform.position,
                maximumExtent,
                maximumCenterOffset);
        }

        private void CaptureRestPose()
        {
            if (rigRoot == null) return;
            var transforms = rigRoot.GetComponentsInChildren<Transform>(true);
            _restPose = new PoseNode[transforms.Length];
            for (var i = 0; i < transforms.Length; i++)
            {
                _restPose[i] = new PoseNode
                {
                    Transform = transforms[i],
                    LocalPosition = transforms[i].localPosition,
                    LocalRotation = transforms[i].localRotation,
                    LocalScale = transforms[i].localScale
                };
            }
        }

        private void RestoreRestPose()
        {
            if (_restPose == null) return;
            foreach (var node in _restPose)
            {
                if (node.Transform == null) continue;
                node.Transform.localPosition = node.LocalPosition;
                node.Transform.localRotation = node.LocalRotation;
                node.Transform.localScale = node.LocalScale;
            }
        }

        private void OnRespawned()
        {
            RestoreRestPose();
            if (rigRoot != null) rigRoot.SetActive(true);
            if (fallbackRoot != null) fallbackRoot.SetActive(false);
            if (animator != null) animator.enabled = true;
            _animationDisabled = false;
            _fallbackActivated = false;
            _renderers = null;
        }

        private struct PoseNode
        {
            public Transform Transform;
            public Vector3 LocalPosition;
            public Quaternion LocalRotation;
            public Vector3 LocalScale;
        }
    }

    public static class FormalRigBoundsMath
    {
        public static bool IsSane(Vector3 size, Vector3 centerOffset, float maximumExtent, float maximumCenterOffset)
        {
            if (maximumExtent <= 0f || maximumCenterOffset <= 0f) return false;
            if (!IsFinite(size) || !IsFinite(centerOffset)) return false;
            return size.x > 0.01f && size.y > 0.01f && size.z > 0.01f &&
                   size.x <= maximumExtent && size.y <= maximumExtent && size.z <= maximumExtent &&
                   centerOffset.magnitude <= maximumCenterOffset;
        }

        private static bool IsFinite(Vector3 value)
        {
            return !float.IsNaN(value.x) && !float.IsInfinity(value.x) &&
                   !float.IsNaN(value.y) && !float.IsInfinity(value.y) &&
                   !float.IsNaN(value.z) && !float.IsInfinity(value.z);
        }
    }
}
