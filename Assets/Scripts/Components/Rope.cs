using System;
using UnityEngine;
using System.Linq;
using UnityEngine.Splines;
using System.Collections.Generic;

namespace BurstRope
{
    public class Rope : MonoBehaviour
    {
        public RopeTarget Target0;
        public RopeTarget Target1;
        public SplineContainer SplineContainer;
        [Tooltip("0 = Auto Rope Lenght")] public float MaxRopeLenght = 10;

        int pointCount;
        float currentVelocity = 1;
        float ropeRadius;
        [SerializeField] RopePoint[] ropePoints;
        Spline spline;
        BezierKnot[] origins;
        MeshFilter meshFilter;

        [SerializeField] int selectedTargetIndex = -1;
        public int LockedTargetIndex
        {
            get
            {
                int lockPoint = selectedTargetIndex;
                if (selectedTargetIndex == -1)
                {
                    if (ropePoints[0].Velocity.magnitude > 0.01f)
                        lockPoint = 1;
                    else if (ropePoints[pointCount - 1].Velocity.magnitude > 0.01f)
                        lockPoint = 0;
                    else
                        lockPoint = -1;
                }
                return lockPoint;
            }
        }
        [HideInInspector] public int InstanceIndex;
        public RopePoint[] RopePoints => ropePoints;
        [HideInInspector] public bool IsRebuildRequired = true;
        public bool IsCalculationRequired => currentVelocity > 0.01f ||
                 (target0Position - ropePoints[0].Position).magnitude > 0.01f ||
                 (target1Position - ropePoints[ropePoints.Length - 1].Position).magnitude > 0.01f;
        public Bounds Bounds => SplineContainer.Spline.GetBounds(transform.localToWorldMatrix);
        private Vector3 target0Position
        {
            set => Target0.transform.position = transform.TransformPoint(value);
            get => transform.InverseTransformPoint(Target0.transform.position);
        }
        private Vector3 target1Position
        {
            set => Target1.transform.position = transform.TransformPoint(value);
            get => transform.InverseTransformPoint(Target1.transform.position);
        }
        public void Init(int count, float radius, int index)
        {
            meshFilter = GetComponent<MeshFilter>();
            InstanceIndex = index;
            pointCount = count;
            ropeRadius = radius;
            SplineContainer.Spline.Clear();
            Target0.Init(this, 0);
            Target1.Init(this, 1);
            ropePoints = new RopePoint[pointCount];

            for (int i = 0; i < pointCount; i++)
            {
                float t = (float)i / (pointCount - 1f);
                ropePoints[i] = new RopePoint(Vector3.Lerp(target0Position, target1Position, t));
            }

            for (int i = 0; i < pointCount; i++)
                SplineContainer.Spline.Add(new BezierKnot(ropePoints[i].Position));

            spline = SplineContainer.Spline;
            origins = SplineContainer.Spline.Knots.ToArray();
        }

        public void StartDragging(int index)
        {
            selectedTargetIndex = index;
        }
        public void StopDragging()
        {
            selectedTargetIndex = -1;
        }

        public RopePoint[] GetPoints()
        {
            ropePoints[0].Position = target0Position;
            ropePoints[pointCount - 1].Position = target1Position;
            return ropePoints;
        }

        public void RefreshPoints()
        {
            float vel = 0;
            float dis = 0;

            if (LockedTargetIndex == 0)
            {
                Debug.Log(ropePoints[pointCount - 1].Position + " - " + target1Position);
                target1Position = ropePoints[pointCount - 1].Position;
                ropePoints[0].Velocity = Vector3.zero;
            }
            else if (LockedTargetIndex == 1)
            {
                target0Position = ropePoints[0].Position;
                ropePoints[pointCount - 1].Velocity = Vector3.zero;
            }

            for (int i = 0; i < pointCount; i++)
            {
                vel = Mathf.Max(vel, ropePoints[i].Velocity.magnitude);
                dis = Mathf.Max(dis, ((Vector3)origins[i].Position - ropePoints[i].Position).magnitude);
                origins[i].Position = ropePoints[i].Position;
                spline[i] = origins[i];
            }

            currentVelocity = vel;

            if (IsCalculationRequired || dis > 0.01f)
            {
                spline.SetTangentMode(TangentMode.AutoSmooth);
                IsRebuildRequired = true;
            }
        }

        public void SetMesh(Mesh mesh, bool force = false)
        {
            meshFilter.sharedMesh = mesh;
            IsRebuildRequired = force;
        }
    }
}