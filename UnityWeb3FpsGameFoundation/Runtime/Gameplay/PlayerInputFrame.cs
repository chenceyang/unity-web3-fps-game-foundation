using UnityEngine;

namespace Web3Fps.GameFoundation.Gameplay
{
    /// <summary>Network/input-system-neutral input captured for one simulation step.</summary>
    public struct PlayerInputFrame
    {
        public Vector2 Move;
        public Vector2 Look;
        public bool JumpPressed;
        public bool SprintHeld;
        public bool FireHeld;
        public bool AimHeld;
        public uint Sequence;
    }
}
