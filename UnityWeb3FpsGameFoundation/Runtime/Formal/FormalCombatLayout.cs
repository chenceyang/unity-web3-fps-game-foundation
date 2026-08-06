using UnityEngine;

namespace Web3Fps.GameFoundation.Formal
{
    public static class FormalCombatLayout
    {
        public const float OpeningLaneZ = 11f;
        public const float RelayFootprintRadius = 2.4f;
        public const float FurthestCoverCenterZ = 8f;
        public const float CoverHalfDepth = 0.6f;
        public const float ActorClearance = 0.6f;

        public static Vector3 PlayerSpawn => new Vector3(-16f, 0f, OpeningLaneZ);
        public static Vector3 BotSpawn => new Vector3(8f, 0f, OpeningLaneZ);

        public static bool OpeningLaneClears(float obstacleCenterZ, float obstacleHalfDepth)
        {
            return Mathf.Abs(OpeningLaneZ - obstacleCenterZ) > obstacleHalfDepth + ActorClearance;
        }
    }
}
