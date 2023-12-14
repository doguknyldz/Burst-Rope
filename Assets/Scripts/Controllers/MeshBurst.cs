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
    public class MeshBurst : MonoBehaviour
    {
        [HideInInspector] public List<Rope> Ropes;
        [HideInInspector] public float Radius = 0.1f;
        [HideInInspector] public float SegmentDistance = 0.1f;
        [HideInInspector] public int RadialSegments = 6;
        [HideInInspector] public bool UseCollideHight;
        [HideInInspector] public bool ForceCalculate;
        [DebugOnly] public float compileTime;

        public void Rebuild()
        {
            float startTime = Time.realtimeSinceStartup;

            List<Rope> rebuildingRopes = new List<Rope>();
            foreach (var rope in Ropes)
            {
                if (!rebuildingRopes.Contains(rope))
                    if (rope.IsRebuildRequired || ForceCalculate)
                    {
                        rebuildingRopes.Add(rope);
                        if (UseCollideHight && !ForceCalculate)
                            foreach (var r in Ropes)
                                if (!rebuildingRopes.Contains(r) && rope.Bounds.Intersects(r.Bounds))
                                    rebuildingRopes.Add(r);
                    }
            }

            if (rebuildingRopes.Count == 0)
            {
                compileTime = (compileTime + ((Time.realtimeSinceStartup - startTime) * 1000f)) * 0.5f;
                return;
            }

            rebuildingRopes.Sort(delegate (Rope x, Rope y) { return x.InstanceIndex.CompareTo(y.InstanceIndex); });

            UnsafeList<NativeSpline> ropeSplines = new UnsafeList<NativeSpline>(rebuildingRopes.Count, Allocator.TempJob);
            UnsafeList<float4x4> ropeMatrices = new UnsafeList<float4x4>(rebuildingRopes.Count, Allocator.TempJob);
            UnsafeList<RopeMesh> meshes = new UnsafeList<RopeMesh>(rebuildingRopes.Count, Allocator.TempJob);
            for (int i = 0; i < rebuildingRopes.Count; i++)
            {
                meshes.Add(new RopeMesh());
                Matrix4x4 matrix = Matrix4x4.TRS(rebuildingRopes[i].transform.position, rebuildingRopes[i].transform.rotation, rebuildingRopes[i].transform.localScale);
                ropeSplines.Add(new NativeSpline(rebuildingRopes[i].SplineContainer.Spline, matrix));
                ropeMatrices.Add(matrix);
            }

            var job = new RopeMeshJob
            {
                ropeSplines = ropeSplines,
                ropeMatrices = ropeMatrices,
                meshes = meshes,
                radius = Radius,
                useCollideHight = UseCollideHight,
                segmentDistance = SegmentDistance,
                radialSegments = RadialSegments
            };

            job.Schedule(rebuildingRopes.Count, 128).Complete();

            for (int i = 0; i < rebuildingRopes.Count; i++)
            {
                Mesh mesh = new Mesh();
                mesh.vertices = ConvertArray(meshes[i].vertices);
                mesh.normals = ConvertArray(meshes[i].normals);
                mesh.uv = ConvertArray(meshes[i].uvs);
                mesh.SetIndices(ConvertArray(meshes[i].triangles), MeshTopology.Triangles, 0);
                rebuildingRopes[i].SetMesh(mesh, ForceCalculate);
            }

            ropeSplines.Dispose();
            meshes.Dispose();

            compileTime = (compileTime + ((Time.realtimeSinceStartup - startTime) * 1000f)) * 0.5f;
        }

        private Vector3[] ConvertArray(NativeList<float3> list)
        {
            Vector3[] result = new Vector3[list.Length];
            for (int i = 0; i < list.Length; i++)
                result[i] = list[i];
            return result;
        }
        private Vector2[] ConvertArray(NativeList<float2> list)
        {
            Vector2[] result = new Vector2[list.Length];
            for (int i = 0; i < list.Length; i++)
                result[i] = list[i];
            return result;
        }
        private int[] ConvertArray(NativeList<int> list)
        {
            int[] result = new int[list.Length];
            for (int i = 0; i < list.Length; i++)
                result[i] = list[i];
            return result;
        }

        public struct RopeMesh
        {
            public NativeList<float3> vertices { get; set; }
            public NativeList<float3> normals { get; set; }
            public NativeList<float2> uvs { get; set; }
            public NativeList<int> triangles { get; set; }
        }

        [BurstCompile]
        public struct RopeMeshJob : IJobParallelFor
        {
            const float PI2 = Mathf.PI * 2f;
            [ReadOnly, NativeDisableParallelForRestriction] public UnsafeList<NativeSpline> ropeSplines;
            [ReadOnly, NativeDisableParallelForRestriction] public UnsafeList<float4x4> ropeMatrices;
            [WriteOnly] public UnsafeList<RopeMesh> meshes;
            [ReadOnly] public float radius;
            [ReadOnly] public bool useCollideHight;
            [ReadOnly] public float segmentDistance;
            [ReadOnly] public int radialSegments;

            public void Execute(int index)
            {
                var spline = ropeSplines[index];
                RopeMesh rp = new RopeMesh
                {
                    vertices = new NativeList<float3>(Allocator.TempJob),
                    normals = new NativeList<float3>(Allocator.TempJob),
                    uvs = new NativeList<float2>(Allocator.TempJob),
                    triangles = new NativeList<int>(Allocator.TempJob)
                };

                int tubularSegments = math.clamp((int)(spline.GetLength() / segmentDistance), 4, 32);
                for (int s = 0; s < tubularSegments; s++)
                {
                    float t = (float)s / (tubularSegments - 1);
                    if (s == 0) t = 0.001f;
                    if (s == tubularSegments - 1) t = 0.999f;
                    spline.Evaluate(t, out float3 sposition, out float3 stangent, out float3 snormal);
                    float3 p = sposition;
                    float3 n = snormal;
                    float3 r = math.normalize(math.cross(stangent, snormal));

                    if (p.y < radius)
                        p.y = radius;
                    if (useCollideHight && s != 0 && s != tubularSegments - 1)
                        for (int i = 0; i < ropeSplines.Length; i++)
                        {
                            if (i == index) break;
                            float d = SplineUtility.GetNearestPoint(ropeSplines[i], sposition, out float3 np, out float nt);
                            if (d <= radius * 2.5f)
                                p.y += math.abs(d - (radius * 2.5f));
                        }

                    p = math.transform(math.inverse(ropeMatrices[index]), p);
                    n = math.rotate(math.inverse(ropeMatrices[index]), n);
                    r = math.rotate(math.inverse(ropeMatrices[index]), r);

                    for (int i = 0; i <= radialSegments; i++)
                    {
                        float v = 1f * i / radialSegments * PI2;
                        var sin = math.sin(v);
                        var cos = math.cos(v);

                        float3 normal = math.normalize(cos * n + sin * r);
                        rp.vertices.Add(p + radius * normal);
                        rp.normals.Add(normal);
                    }

                    for (int i = 0; i <= radialSegments; i++)
                    {
                        float u = (float)i / radialSegments;
                        float v = (float)s / tubularSegments;
                        rp.uvs.Add(new Vector2(u, v));
                    }

                    if (s > 0)
                        for (int i = 1; i <= radialSegments; i++)
                        {
                            int a = (radialSegments + 1) * (s - 1) + (i - 1);
                            int b = (radialSegments + 1) * s + (i - 1);
                            int c = (radialSegments + 1) * s + i;
                            int d = (radialSegments + 1) * (s - 1) + i;

                            rp.triangles.Add(a); rp.triangles.Add(d); rp.triangles.Add(b);
                            rp.triangles.Add(b); rp.triangles.Add(d); rp.triangles.Add(c);
                        }
                }
                meshes[index] = rp;
            }
        }
    }
}