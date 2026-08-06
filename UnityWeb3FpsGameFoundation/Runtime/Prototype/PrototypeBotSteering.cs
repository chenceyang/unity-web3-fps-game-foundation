using UnityEngine;

namespace Web3Fps.GameFoundation.Prototype
{
    public static class PrototypeBotSteering
    {
        public static Vector3 Resolve(Vector3 desiredDirection, bool pathBlocked, float avoidanceSide)
        {
            var desired = Vector3.ProjectOnPlane(desiredDirection, Vector3.up).normalized;
            if (!pathBlocked || desired.sqrMagnitude < 0.001f) return desired;
            var side = avoidanceSide < 0f ? -1f : 1f;
            var tangent = Vector3.Cross(Vector3.up, desired) * side;
            return (desired * 0.35f + tangent).normalized;
        }
    }
}
