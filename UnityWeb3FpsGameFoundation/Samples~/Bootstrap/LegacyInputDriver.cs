using UnityEngine;
using Web3Fps.GameFoundation.Gameplay;
using Web3Fps.GameFoundation.Gameplay.Combat;

namespace Web3Fps.GameFoundation.Samples
{
    public sealed class LegacyInputDriver : MonoBehaviour
    {
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private FirstPersonLook look;
        [SerializeField] private HitscanWeapon weapon;
        [SerializeField] private Transform aimSource;
        private uint _sequence;

        private void Update()
        {
            var frame = new PlayerInputFrame
            {
                Move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")),
                Look = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")),
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
