using UnityEngine;

namespace Web3Fps.GameFoundation.Gameplay
{
    public sealed class FirstPersonLook : MonoBehaviour
    {
        [SerializeField] private Transform pitchPivot;
        [SerializeField, Min(0.01f)] private float sensitivity = 0.12f;
        [SerializeField] private float minPitch = -88f;
        [SerializeField] private float maxPitch = 88f;

        private Vector2 _lookDelta;
        private float _pitch;
        private float _recoilPitch;
        private float _recoilYaw;

        public void SetLookDelta(Vector2 delta) => _lookDelta = delta;

        /// <summary>
        /// Applies the look delta immediately. The input driver calls this during its
        /// Update, before movement and firing sample this transform, so aim direction
        /// and move direction never lag the mouse by a frame. LateUpdate still runs to
        /// decay recoil and to serve adapters that only call <see cref="SetLookDelta"/>.
        /// </summary>
        public void ApplyLook(Vector2 delta)
        {
            _lookDelta = delta;
            ConsumePendingDelta();
            ApplyPivotRotation();
        }

        public void Configure(Transform pivot, float lookSensitivity = 0.12f)
        {
            pitchPivot = pivot;
            sensitivity = Mathf.Max(0.01f, lookSensitivity);
        }

        public void AddRecoil(float upwardDegrees, float yawDegrees)
        {
            _recoilPitch = Mathf.Clamp(_recoilPitch - Mathf.Max(0f, upwardDegrees), -8f, 0f);
            _recoilYaw = Mathf.Clamp(_recoilYaw + yawDegrees, -3f, 3f);
        }

        public void Simulate()
        {
            ConsumePendingDelta();
            _recoilPitch = Mathf.MoveTowards(_recoilPitch, 0f, 8f * Time.deltaTime);
            _recoilYaw = Mathf.MoveTowards(_recoilYaw, 0f, 6f * Time.deltaTime);
            ApplyPivotRotation();
        }

        private void ConsumePendingDelta()
        {
            if (_lookDelta == Vector2.zero) return;
            transform.Rotate(0f, _lookDelta.x * sensitivity, 0f, Space.Self);
            _pitch = Mathf.Clamp(_pitch - _lookDelta.y * sensitivity, minPitch, maxPitch);
            _lookDelta = Vector2.zero;
        }

        private void ApplyPivotRotation()
        {
            if (pitchPivot != null) pitchPivot.localRotation = Quaternion.Euler(_pitch + _recoilPitch, _recoilYaw, 0f);
        }

        private void LateUpdate() => Simulate();
    }
}
