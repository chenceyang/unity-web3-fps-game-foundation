using UnityEngine;
using Web3Fps.GameFoundation.Gameplay.Combat;
using Web3Fps.GameFoundation.Prototype;

namespace Web3Fps.GameFoundation.Formal
{
    [DisallowMultipleComponent]
    public sealed class FormalProceduralOperatorAnimator : MonoBehaviour
    {
        [SerializeField] private Transform visualRoot;
        [SerializeField] private CharacterController controller;
        [SerializeField] private Health health;
        [SerializeField] private PrototypeBotController bot;
        [SerializeField, Min(0.1f)] private float fullSpeed = 5f;

        private Transform _torso;
        private Transform _leftArm;
        private Transform _rightArm;
        private Transform _leftLeg;
        private Transform _rightLeg;
        private Vector3 _rootPosition;
        private Quaternion _rootRotation;
        private Pose _torsoPose;
        private Pose _leftArmPose;
        private Pose _rightArmPose;
        private Pose _leftLegPose;
        private Pose _rightLegPose;
        private float _recoil;

        public void Configure(Transform root, CharacterController characterController, Health characterHealth, PrototypeBotController botController)
        {
            Unsubscribe();
            visualRoot = root;
            controller = characterController;
            health = characterHealth;
            bot = botController;
            CaptureRig();
            if (isActiveAndEnabled) Subscribe();
        }

        private void Awake() => CaptureRig();
        private void OnEnable() => Subscribe();
        private void OnDisable() => Unsubscribe();

        private void LateUpdate()
        {
            if (visualRoot == null) return;
            var speed = controller == null ? 0f : Vector3.ProjectOnPlane(controller.velocity, Vector3.up).magnitude;
            var speed01 = Mathf.Clamp01(speed / Mathf.Max(0.1f, fullSpeed));
            var stride = FormalProceduralOperatorAnimation.EvaluateStride(Time.time, speed01);
            var bob = FormalProceduralOperatorAnimation.EvaluateBob(Time.time, speed01);
            _recoil = FormalProceduralOperatorAnimation.Decay(_recoil, 15f, Time.deltaTime);

            var dead = health != null && health.IsDead;
            var targetRootRotation = dead ? _rootRotation * Quaternion.Euler(0f, 0f, -82f) : _rootRotation;
            var targetRootPosition = _rootPosition + (dead ? new Vector3(0f, -0.62f, 0f) : Vector3.up * bob);
            var blend = 1f - Mathf.Exp(-14f * Time.deltaTime);
            visualRoot.localPosition = Vector3.Lerp(visualRoot.localPosition, targetRootPosition, blend);
            visualRoot.localRotation = Quaternion.Slerp(visualRoot.localRotation, targetRootRotation, blend);
            if (dead) return;

            Apply(_torso, _torsoPose, new Vector3(-_recoil * 5f, 0f, stride * 0.08f));
            Apply(_leftArm, _leftArmPose, new Vector3(stride * 22f - _recoil * 7f, 0f, 0f));
            Apply(_rightArm, _rightArmPose, new Vector3(-stride * 22f - _recoil * 12f, 0f, 0f));
            Apply(_leftLeg, _leftLegPose, new Vector3(-stride * 28f, 0f, 0f));
            Apply(_rightLeg, _rightLegPose, new Vector3(stride * 28f, 0f, 0f));
        }

        private void CaptureRig()
        {
            if (visualRoot == null) return;
            _rootPosition = visualRoot.localPosition;
            _rootRotation = visualRoot.localRotation;
            _torso = Find("Torso");
            _leftArm = Find("LeftArm");
            _rightArm = Find("RightArm");
            _leftLeg = Find("LeftLeg");
            _rightLeg = Find("RightLeg");
            _torsoPose = Pose.Capture(_torso);
            _leftArmPose = Pose.Capture(_leftArm);
            _rightArmPose = Pose.Capture(_rightArm);
            _leftLegPose = Pose.Capture(_leftLeg);
            _rightLegPose = Pose.Capture(_rightLeg);
        }

        private Transform Find(string targetName)
        {
            foreach (var child in visualRoot.GetComponentsInChildren<Transform>(true))
                if (child.name == targetName) return child;
            return null;
        }

        private static void Apply(Transform target, Pose pose, Vector3 eulerOffset)
        {
            if (target == null) return;
            target.localPosition = pose.Position;
            target.localRotation = pose.Rotation * Quaternion.Euler(eulerOffset);
        }

        private void Subscribe()
        {
            if (bot != null) bot.ShotResolved += OnShotResolved;
        }

        private void Unsubscribe()
        {
            if (bot != null) bot.ShotResolved -= OnShotResolved;
        }

        private void OnShotResolved(Vector3 origin, Vector3 end, bool hit) => _recoil = 1f;

        private struct Pose
        {
            public Vector3 Position;
            public Quaternion Rotation;

            public static Pose Capture(Transform source)
            {
                return source == null ? default : new Pose { Position = source.localPosition, Rotation = source.localRotation };
            }
        }
    }

    public static class FormalProceduralOperatorAnimation
    {
        public static float EvaluateStride(float time, float speed01)
        {
            return Mathf.Sin(time * Mathf.Lerp(4f, 10f, Mathf.Clamp01(speed01))) * Mathf.Clamp01(speed01);
        }

        public static float EvaluateBob(float time, float speed01)
        {
            return Mathf.Abs(Mathf.Sin(time * 10f)) * 0.045f * Mathf.Clamp01(speed01);
        }

        public static float Decay(float value, float rate, float deltaTime)
        {
            return Mathf.Max(0f, value) * Mathf.Exp(-Mathf.Max(0f, rate) * Mathf.Max(0f, deltaTime));
        }
    }
}
