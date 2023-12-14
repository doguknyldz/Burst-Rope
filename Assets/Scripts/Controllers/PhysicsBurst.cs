using System;
using Unity.Jobs;
using Unity.Burst;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Splines;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace BurstRope
{
    public class PhysicsBurst : MonoBehaviour
    {
        [HideInInspector] public List<Rope> Ropes;
        [HideInInspector] public int PointCount = 8;
        [HideInInspector] public int Iterations = 8;
        [HideInInspector] public int VelocityLimit = 6;
        [HideInInspector] public float Gravity = 10;
        [HideInInspector] public float Damping = 1.2f;
        [HideInInspector] public float Friction = 8f;
        [HideInInspector] public float PathMultiplier = 0.8f;
        [HideInInspector] public float Radius = 0.1f;
        [HideInInspector] public bool ForceCalculate;
        [DebugOnly] public float compileTime;

        public void Recalculate(float deltaTime)
        {
            float startTime = Time.realtimeSinceStartup;

            List<Rope> calcutatingRopes = new List<Rope>();
            foreach (var rope in Ropes)
            {
                if (rope.IsCalculationRequired || ForceCalculate)
                    calcutatingRopes.Add(rope);
            }

            if (calcutatingRopes.Count == 0)
            {
                compileTime = (compileTime + ((Time.realtimeSinceStartup - startTime) * 1000f)) * 0.5f;
                return;
            }

            NativeArray<int> lockPoints = new NativeArray<int>(calcutatingRopes.Count, Allocator.TempJob);
            NativeArray<float> ropeLenghts = new NativeArray<float>(calcutatingRopes.Count, Allocator.TempJob);
            NativeArray<float3> positions = new NativeArray<float3>(PointCount * calcutatingRopes.Count, Allocator.TempJob);
            NativeArray<float3> velocities = new NativeArray<float3>(PointCount * calcutatingRopes.Count, Allocator.TempJob);

            for (int r = 0; r < calcutatingRopes.Count; r++)
            {
                RopePoint[] points = calcutatingRopes[r].GetPoints();
                ropeLenghts[r] = calcutatingRopes[r].MaxRopeLenght;
                lockPoints[r] = calcutatingRopes[r].LockedTargetIndex;

                for (int i = 0; i < PointCount; i++)
                {
                    positions[r * PointCount + i] = points[i].Position;
                    velocities[r * PointCount + i] = points[i].Velocity;
                }
            }

            var job = new RopePhysicsJob
            {
                velocities = velocities,
                positions = positions,
                lockPoints = lockPoints,
                ropeLenghts = ropeLenghts,
                gravity = Gravity,
                limit = VelocityLimit,
                iterations = Iterations,
                pointCount = PointCount,
                multiplier = PathMultiplier,
                deltaTime = deltaTime,
                friction = Friction,
                damping = Damping,
            };

            job.Schedule(calcutatingRopes.Count, 128).Complete();

            for (int r = 0; r < calcutatingRopes.Count; r++)
            {
                for (int i = 0; i < PointCount; i++)
                {
                    calcutatingRopes[r].RopePoints[i].Position = positions[r * PointCount + i];
                    calcutatingRopes[r].RopePoints[i].Velocity = velocities[r * PointCount + i];
                }
                calcutatingRopes[r].RefreshPoints();
            }

            positions.Dispose();
            velocities.Dispose();

            compileTime = (compileTime + ((Time.realtimeSinceStartup - startTime) * 1000f)) * 0.5f;
        }

        [BurstCompile]
        public struct RopePhysicsJob : IJobParallelFor
        {

            [NativeDisableParallelForRestriction] public NativeArray<float3> positions;
            [NativeDisableParallelForRestriction] public NativeArray<float3> velocities;
            [ReadOnly] public NativeArray<int> lockPoints;
            [ReadOnly] public NativeArray<float> ropeLenghts;
            [ReadOnly] public float gravity;
            [ReadOnly] public float limit;
            [ReadOnly] public float iterations;
            [ReadOnly] public int pointCount;
            [ReadOnly] public float multiplier;
            [ReadOnly] public float deltaTime;
            [ReadOnly] public float friction;
            [ReadOnly] public float damping;

            public void Execute(int index)
            {
                NativeArray<float3> poses = new NativeArray<float3>(pointCount, Allocator.Temp);
                for (int i = 0; i < pointCount; i++)
                    poses[i] = positions[index * pointCount + i];

                int lockPointIndex = lockPoints[index] == 1 ? pointCount - 1 : lockPoints[index];
                int freePointIndex = lockPointIndex == 0 ? pointCount - 1 : 0;

                float pointLenght = GetPointLenght(poses);
                if (lockPointIndex >= 0 && ropeLenghts[index] > 0)
                {
                    if (pointLenght > ropeLenghts[index])
                        poses[freePointIndex] = poses[lockPointIndex] + ClampMagnitude(poses[freePointIndex] - poses[lockPointIndex], ropeLenghts[index]);
                    else
                        velocities[index * pointCount + freePointIndex] *= 0.8f;

                    if (multiplier == 0)
                        pointLenght = ropeLenghts[index];
                }


                float spacing = pointLenght / pointCount * (multiplier == 0 ? 1 : multiplier);
                for (int x = 0; x < iterations; x++)
                {
                    for (int i = 0; i < pointCount - 1; i++)
                    {
                        float3 centre = (poses[i] + poses[i + 1]) / 2;
                        float3 offset = poses[i] - poses[i + 1];
                        float length = math.length(offset);
                        float3 dir = offset / length;
                        if (length > spacing || length < spacing * 0.5f)
                        {
                            if (i != 0)
                                poses[i] = centre + dir * spacing / 2;
                            if (i + 1 != pointCount - 1)
                                poses[i + 1] = centre - dir * spacing / 2;
                        }
                    }
                }

                for (int i = 0; i < pointCount; i++)
                {
                    if (i == lockPointIndex) continue;
                    float3 pos = positions[index * pointCount + i];
                    float3 vel = velocities[index * pointCount + i];

                    float3 v = (pos - poses[i]) + new float3(0, gravity, 0);
                    vel -= v * damping;

                    pos += vel * deltaTime;
                    if (pos.y < 0)
                    {
                        vel.y = 0;
                        pos.y = 0;
                    }

                    if (friction > 0)
                    {
                        float lockMult = (lockPointIndex >= 0 && i == freePointIndex) ? 3 : 1;
                        vel = MoveTowards(vel, float3.zero, lockMult * friction * deltaTime);
                        vel = ClampMagnitude(vel, limit / lockMult);
                    }

                    positions[index * pointCount + i] = pos;
                    velocities[index * pointCount + i] = vel;
                }
            }

            float3 MoveTowards(float3 current, float3 target, float maxDistanceDelta)
            {
                float deltaX = target.x - current.x;
                float deltaY = target.y - current.y;
                float deltaZ = target.z - current.z;

                float sqdist = deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ;

                if (sqdist == 0 || sqdist <= maxDistanceDelta * maxDistanceDelta)
                    return target;
                float dist = (float)math.sqrt(sqdist);

                return new float3(current.x + deltaX / dist * maxDistanceDelta,
                    current.y + deltaY / dist * maxDistanceDelta,
                    current.z + deltaZ / dist * maxDistanceDelta);
            }

            float3 ClampMagnitude(float3 vector, float maxLength)
            {
                float clampedLength = math.clamp(math.length(vector), -maxLength, maxLength);
                if (clampedLength != 0)
                    vector = math.normalize(vector) * clampedLength;
                return vector;
            }

            float GetPointLenght(NativeArray<float3> points)
            {
                float lenght = 0;
                for (int i = 0; i < points.Length - 1; i++)
                    lenght += math.length(points[i] - points[i + 1]);
                return lenght;
            }
        }
    }
}