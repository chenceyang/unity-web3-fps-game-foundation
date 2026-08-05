using UnityEngine;

namespace Web3Fps.GameFoundation.Gameplay.Combat
{
    public struct ShotCommand
    {
        public string ShooterId;
        public GameObject ShooterObject;
        public Vector3 Origin;
        public Vector3 Direction;
        public uint Sequence;
        public double ClientTimestamp;
        public float Damage;
        public float Range;
    }

    public struct ShotResult
    {
        public bool Accepted;
        public bool Hit;
        public Vector3 Point;
        public Vector3 Normal;
        public GameObject HitObject;
    }

    public interface IShotCommandSink
    {
        ShotResult Submit(ShotCommand command);
    }
}
