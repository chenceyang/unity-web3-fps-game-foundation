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
            transform.Rotate(0f, _lookDelta.x * sensitivity, 0f, Space.Self);
            _pitch = Mathf.Clamp(_pitch - _lookDelta.y * sensitivity, minPitch, maxPitch);
            if (pitchPivot != null) pitchPivot.localRotation = Quaternion.Euler(_pitch + _recoilPitch, _recoilYaw, 0f);
            _recoilPitch = Mathf.MoveTowards(_recoilPitch, 0f, 8f * Time.deltaTime);
            _recoilYaw = Mathf.MoveTowards(_recoilYaw, 0f, 6f * Time.deltaTime);
            _lookDelta = Vector2.zero;
        }

        private void LateUpdate() => Simulate();
    }
}
