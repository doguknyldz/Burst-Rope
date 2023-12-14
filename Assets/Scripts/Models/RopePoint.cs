using UnityEngine;

namespace BurstRope
{
    [System.Serializable]
    public class RopePoint
    {
        public Vector3 Position;
        public Vector3 Velocity;

        public RopePoint(Vector3 pos)
        {
            Position = pos;
            Velocity = Vector3.zero;
        }
    }
}