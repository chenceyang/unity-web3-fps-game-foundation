using UnityEngine;

namespace Web3Fps.GameFoundation.Gameplay
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float walkSpeed = 5f;
        [SerializeField, Min(0.1f)] private float sprintSpeed = 7.5f;
        [SerializeField, Min(0f)] private float acceleration = 24f;
        [SerializeField, Min(0f)] private float airAcceleration = 8f;
        [SerializeField, Min(0f)] private float jumpHeight = 1.25f;
        [SerializeField, Min(0f)] private float gravity = 25f;

        private CharacterController _controller;
        private PlayerInputFrame _input;
        private Vector3 _horizontalVelocity;
        private float _verticalVelocity;

        public Vector3 Velocity => _horizontalVelocity + Vector3.up * _verticalVelocity;
        public float MovementAmount => sprintSpeed <= 0f ? 0f : Mathf.Clamp01(_horizontalVelocity.magnitude / sprintSpeed);

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
        }

        public void SetInput(PlayerInputFrame input)
        {
            _input = input;
        }

        public void Simulate(float deltaTime)
        {
            if (deltaTime <= 0f || !_controller.enabled) return;
            var move = Vector2.ClampMagnitude(_input.Move, 1f);
            var desiredDirection = transform.right * move.x + transform.forward * move.y;
            var speed = _input.SprintHeld ? sprintSpeed : walkSpeed;
            var desiredVelocity = desiredDirection * speed;
            var wasGrounded = _controller.isGrounded;
            var accel = wasGrounded ? acceleration : airAcceleration;
            _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, desiredVelocity, accel * deltaTime);

            var jumped = false;
            if (wasGrounded)
            {
                if (_verticalVelocity < 0f) _verticalVelocity = -2f;
                if (_input.JumpPressed)
                {
                    _verticalVelocity = Mathf.Sqrt(2f * gravity * jumpHeight);
                    jumped = true;
                }
            }
            else
            {
                _verticalVelocity -= gravity * deltaTime;
            }

            _controller.Move(Velocity * deltaTime);

            // Running down a ramp lifts the capsule off the surface for a frame at a
            // time, which reads as rhythmic bouncing. Pull the controller back onto
            // the walkable surface it just left instead of letting gravity slowly
            // re-accumulate; a real ledge only ever costs one clamped snap step.
            if (wasGrounded && !jumped && !_controller.isGrounded && _verticalVelocity <= 0f)
            {
                var snap = PlayerMotorMath.GroundSnapDistance(
                    _horizontalVelocity.magnitude, _controller.slopeLimit, deltaTime, _controller.stepOffset);
                if (snap > 0f) _controller.Move(Vector3.down * snap);
            }
            _input.JumpPressed = false;
        }

        private void Update()
        {
            // Network adapters may disable this component and call Simulate from their own tick.
            Simulate(Time.deltaTime);
        }
    }

    public static class PlayerMotorMath
    {
        /// <summary>
        /// Downward probe distance used to keep a grounded controller attached to a
        /// descending slope: the vertical drop of one frame of horizontal travel at
        /// the steepest walkable angle, clamped so walking off a ledge never snaps
        /// further than the controller's step offset.
        /// </summary>
        public static float GroundSnapDistance(float horizontalSpeed, float slopeLimitDegrees, float deltaTime, float maximumDistance)
        {
            if (deltaTime <= 0f || maximumDistance <= 0f) return 0f;
            var slope = Mathf.Clamp(slopeLimitDegrees, 0f, 80f);
            var drop = Mathf.Max(0f, horizontalSpeed) * Mathf.Tan(slope * Mathf.Deg2Rad) * deltaTime + 0.02f;
            return Mathf.Min(drop, maximumDistance);
        }
    }
}
