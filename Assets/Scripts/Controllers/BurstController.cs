using UnityEngine;
using BurstRope.Utils;
using System.Collections.Generic;

namespace BurstRope
{
    public class BurstController : MonoBehaviour
    {
        public List<Rope> Ropes;
        public bool ForceCalculate;
        public bool EnableDebuging;

        [DebugOnly] public float PhysicsCompileTime;
        [DebugOnly] public float MeshGenerationCompileTime;

        [Space(), Header("Mesh Generation")]
        public float Radius = 0.1f;
        public float SegmentDistance = 0.1f;
        public int RadialSegments = 6;
        public bool UseCollideHight;

        [Space(), Header("Physics")]
        public int PointCount = 8;
        public int Iterations = 8;
        public int VelocityLimit = 6;
        public float Gravity = 10;
        public float Damping = 1.2f;
        public float Friction = 8f;
        [Tooltip("0 = Fixed Rope Lenght")] public float PathMultiplier = 0.8f;

        MeshBurst meshBurst;
        PhysicsBurst physicsBurst;

        private void Start()
        {
            for (int i = 0; i < Ropes.Count; i++)
                Ropes[i].Init(PointCount, Radius, i);

            meshBurst = gameObject.AddComponent<MeshBurst>();
            physicsBurst = gameObject.AddComponent<PhysicsBurst>();
            SetVariables();
        }

        private void FixedUpdate()
        {
            physicsBurst.Recalculate(Time.fixedDeltaTime);
            meshBurst.Rebuild();

            PhysicsCompileTime = physicsBurst.compileTime;
            MeshGenerationCompileTime = meshBurst.compileTime;
            if (EnableDebuging)
            {
                ScreenDebugger.Log("Mesh", MeshGenerationCompileTime.ToString("00.0") + " ms");
                ScreenDebugger.Log("Physics", PhysicsCompileTime.ToString("00.0") + " ms");
            }

        }

        private void OnValidate()
        {
            SetVariables();
        }

        private void SetVariables()
        {
            if (physicsBurst != null)
            {
                physicsBurst.Ropes = Ropes;
                physicsBurst.PointCount = PointCount;
                physicsBurst.Iterations = Iterations;
                physicsBurst.VelocityLimit = VelocityLimit;
                physicsBurst.Gravity = Gravity;
                physicsBurst.Damping = Damping;
                physicsBurst.Friction = Friction;
                physicsBurst.PathMultiplier = PathMultiplier;
                physicsBurst.ForceCalculate = ForceCalculate;
            }

            if (meshBurst != null)
            {
                meshBurst.Ropes = Ropes;
                meshBurst.SegmentDistance = SegmentDistance;
                meshBurst.RadialSegments = RadialSegments;
                meshBurst.Radius = Radius;
                meshBurst.ForceCalculate = ForceCalculate;
                meshBurst.UseCollideHight = UseCollideHight;
            }
        }
    }
}