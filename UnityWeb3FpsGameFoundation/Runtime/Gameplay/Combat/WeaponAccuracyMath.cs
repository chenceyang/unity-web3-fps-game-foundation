using UnityEngine;

namespace Web3Fps.GameFoundation.Gameplay.Combat
{
    public static class WeaponAccuracyMath
    {
        public static float CalculateSpread(
            float hipSpread,
            float aimSpread,
            float movementSpread,
            float movementAmount,
            bool aiming,
            float bloom)
        {
            var baseSpread = aiming ? aimSpread : hipSpread;
            return Mathf.Max(0f, baseSpread) +
                   Mathf.Max(0f, movementSpread) * Mathf.Clamp01(movementAmount) +
                   Mathf.Max(0f, bloom);
        }

        public static Vector3 ApplySpread(Vector3 direction, uint sequence, float spreadDegrees)
        {
            direction = direction.sqrMagnitude < 0.0001f ? Vector3.forward : direction.normalized;
            if (spreadDegrees <= 0f) return direction;

            var seed = sequence * 747796405u + 2891336453u;
            var radius = Mathf.Sqrt(ToUnitFloat(Hash(seed))) * Mathf.Tan(spreadDegrees * Mathf.Deg2Rad);
            var angle = ToUnitFloat(Hash(seed ^ 0x9e3779b9u)) * Mathf.PI * 2f;
            var upReference = Mathf.Abs(Vector3.Dot(direction, Vector3.up)) > 0.98f ? Vector3.right : Vector3.up;
            var right = Vector3.Cross(upReference, direction).normalized;
            var up = Vector3.Cross(direction, right).normalized;
            return (direction + right * (Mathf.Cos(angle) * radius) + up * (Mathf.Sin(angle) * radius)).normalized;
        }

        private static uint Hash(uint value)
        {
            value ^= value >> 16;
            value *= 0x7feb352du;
            value ^= value >> 15;
            value *= 0x846ca68bu;
            return value ^ (value >> 16);
        }

        private static float ToUnitFloat(uint value)
        {
            return (value & 0x00ffffffu) / 16777215f;
        }
    }
}
