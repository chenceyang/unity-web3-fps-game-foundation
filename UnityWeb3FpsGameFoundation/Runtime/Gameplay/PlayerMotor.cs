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
            var accel = _controller.isGrounded ? acceleration : airAcceleration;
            _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, desiredVelocity, accel * deltaTime);

            if (_controller.isGrounded)
            {
                if (_verticalVelocity < 0f) _verticalVelocity = -2f;
                if (_input.JumpPressed) _verticalVelocity = Mathf.Sqrt(2f * gravity * jumpHeight);
            }
            else
            {
                _verticalVelocity -= gravity * deltaTime;
            }

            _controller.Move(Velocity * deltaTime);
            _input.JumpPressed = false;
        }

        private void Update()
        {
            // Network adapters may disable this component and call Simulate from their own tick.
            Simulate(Time.deltaTime);
        }
    }
}
