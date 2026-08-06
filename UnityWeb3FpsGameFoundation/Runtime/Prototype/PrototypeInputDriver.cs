using UnityEngine;
using Web3Fps.GameFoundation.Gameplay;
using Web3Fps.GameFoundation.Gameplay.Combat;

namespace Web3Fps.GameFoundation.Prototype
{
    [DisallowMultipleComponent]
    public sealed class PrototypeInputDriver : MonoBehaviour
    {
        [SerializeField] private PrototypeParticipant participant;
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private FirstPersonLook look;
        [SerializeField] private HitscanWeapon weapon;
        [SerializeField] private Transform aimSource;
        [SerializeField] private PrototypeMatchController match;

        private uint _sequence;

        public void Configure(
            PrototypeParticipant actor,
            PlayerMotor playerMotor,
            FirstPersonLook playerLook,
            HitscanWeapon playerWeapon,
            Transform playerAimSource,
            PrototypeMatchController matchController)
        {
            participant = actor;
            motor = playerMotor;
            look = playerLook;
            weapon = playerWeapon;
            aimSource = playerAimSource;
            match = matchController;
        }

        private void OnEnable()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                var shouldLock = Cursor.lockState != CursorLockMode.Locked;
                Cursor.lockState = shouldLock ? CursorLockMode.Locked : CursorLockMode.None;
                Cursor.visible = !shouldLock;
            }

            if (match != null && match.IsFinished && Input.GetKeyDown(KeyCode.R))
            {
                match.RestartMatch();
                return;
            }

            if (participant == null || participant.Health == null || participant.Health.IsDead ||
                match == null || !match.IsRunning) return;

            var frame = new PlayerInputFrame
            {
                Move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")),
                Look = Cursor.lockState == CursorLockMode.Locked
                    ? new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"))
                    : Vector2.zero,
                JumpPressed = Input.GetButtonDown("Jump"),
                SprintHeld = Input.GetKey(KeyCode.LeftShift),
                FireHeld = Input.GetButton("Fire1"),
                AimHeld = Input.GetButton("Fire2"),
                Sequence = ++_sequence
            };
            if (motor != null) motor.SetInput(frame);
            if (look != null) look.SetLookDelta(frame.Look);
            if (frame.FireHeld && weapon != null && aimSource != null) weapon.TryFire(aimSource.forward);
        }
    }
}
