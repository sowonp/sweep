#if MATHEMATICS_INSTALLED && BURST_INSTALLED && COLLECTIONS_INSTALLED

using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

using Unity.Burst;
using Unity.Mathematics;
using System.Collections.Generic;

namespace WaveMaker
{
    public class CustomMeshManager
    {
        public Mesh Mesh { get => _mesh; }
        Mesh _mesh;
        MeshFilter _meshFilter; 
        NativeArray<Vector3> _vertices;
        Vector2[] _uvs;

        public CustomMeshManager(MeshFilter meshFilter, in IntegerPair resolution, in Vector2 size)
        {
            _meshFilter = meshFilter;
            if (_meshFilter == null)
                throw new System.NullReferenceException("Wave Maker renderer was given a null mesh filter. Cannot initialize");

            UpdateMesh(in resolution, in size);
        }

        public void SetMeshColors(ref Color[] colors)
        {
            if (_mesh != null)
                _mesh.colors = colors;
        }

        public Color[] GetMeshColorsCopy()
        {
            if (_mesh != null)
                return _mesh.colors;
            else
                return new Color[0];
        }

        /// <summary>
        /// Used to apply the base values in order to make smoothing via a normal map work, NOT to be used frequently
        /// </summary>
        public void ResetNormalsAndTangents()
        {
            Vector3[] flatNormals = new Vector3[_vertices.Length];
            Vector4[] flatTangents = new Vector4[_vertices.Length];
            for(int i = 0; i < _vertices.Length; i++)
            {
                flatNormals[i] = Vector3.up;
                flatTangents[i] = new Vector4(1, 0, 0, -1);
            }
            _mesh.SetNormals(flatNormals);
            _mesh.SetTangents(flatTangents);
        }

        internal void EnableNormalUVs()
        {
            List<Vector2> normalUVs = new List<Vector2>(_uvs.Length);
            _mesh.GetUVs(4, normalUVs);
            _mesh.SetUVs(0, normalUVs);
        }

        internal void DisableNormalUVs()
        {
            _mesh.SetUVs(0, _uvs);
        }

        public void UpdateMesh(in IntegerPair resolution, in Vector2 size)
        {
            Dispose();

            _mesh = new Mesh();
            _mesh.name = "WaveMaker Procedural Mesh";

            var cellSize = new float2(size.x / (resolution.x - 1), size.y / (resolution.z - 1));
            IntegerPair nCells = new IntegerPair(resolution.x - 1, resolution.z -1);

            // 10 resolution means 10 vertices = 10 samples and 9 mesh cells on that axis
            int nVertices = resolution.x * resolution.z;
            _vertices = new NativeArray<Vector3>(nVertices, Allocator.Persistent);
            _uvs = new Vector2[nVertices];
            Vector2[] normalUvs = new Vector2[nVertices];
            Vector3[] normals = new Vector3[nVertices];
            Vector4[] tangents = new Vector4[nVertices];

            // Two triangles between each pair of vertices.
            // 3 vertices each triangle. 
            // e.g: Resolution 4x4 would have 3x3 cells. 9 squares with 2 triangles each, with 3 vertices each
            int[] triangles = new int[2 * 3 * nCells.x * nCells.z];

            int p0Index, p1Index, p2Index, p3Index;

            float uSection = 1.0f / nCells.x;
            float vSection = 1.0f / nCells.z;

            // Uvs for normals are slightly offset so that each texel will be exactly where the vertex is. Used only if normal smoothing is enabled
            float normalUSection = 1.0f / (resolution.x - 1);
            float normalVSection = 1.0f / (resolution.z - 1);
            float normalUStart = normalUSection * 0.5f; // number of pixels in the normals texture to interpolate
            float normalVStart = normalVSection * 0.5f;

            int triangleCount = 0;

            for (int z = 0; z < resolution.z; ++z)
                for (int x = 0; x < resolution.x; ++x)
                {
                    p0Index = z * resolution.x + x;

                    // Generate the new array data for this point
                    _vertices[p0Index] = new Vector3(x * cellSize.x, 0, z * cellSize.y);
                    normals[p0Index] = Vector3.up;
                    tangents[p0Index] = new Vector4(1,0,0,-1);
                    _uvs[p0Index] = new Vector2(x * uSection, z * vSection);
                    normalUvs[p0Index] = new Vector2(normalUStart + x * normalUSection, normalVStart + z * normalVSection);

                    // Generate triangles but not on the extreme sides
                    if (x != resolution.x - 1 && z != resolution.z - 1)
                    {
                        // Calculate indices of this grid ( 2 triangles )
                        p1Index = p0Index + 1;
                        p2Index = p0Index + resolution.x;
                        p3Index = p2Index + 1;

                        //    Z
                        //    |
                        //    |
                        //    p2 -- p3
                        //    |  /  |
                        //    p0 -- p1 --> X


                        /// 0 - 3 - 1
                        triangles[triangleCount++] = p0Index;
                        triangles[triangleCount++] = p3Index;
                        triangles[triangleCount++] = p1Index;

                        //  0 - 2 - 3
                        triangles[triangleCount++] = p0Index;
                        triangles[triangleCount++] = p2Index;
                        triangles[triangleCount++] = p3Index;
                    }
                }

            // Create the mesh and assign
            _mesh.SetVertices(_vertices);
            _mesh.SetNormals(normals);
            _mesh.SetTangents(tangents);
            _mesh.SetUVs(0, _uvs);
            _mesh.SetUVs(3, normals); // Channels 1 and 2 are normally used by Unity for lightmaps and baking info
            _mesh.SetUVs(4, normalUvs);

            // WARNING: Triangles must be asigned afterwards
            _mesh.triangles = triangles;

            _meshFilter.sharedMesh = _mesh;
        }

        public void CopyHeightsAndNormals(in NativeArray<float> heights, in NativeArray<Vector3> normals, in NativeArray<Vector4> tangents,
                                          in IntegerPair resolution, in IntegerPair ghostResolution, in bool isSmoothed)
        {
            if (!Application.isPlaying || _mesh == null)
                return;

            CopyHeightsJob job = new CopyHeightsJob
            {
                heights = heights,
                vertices = _vertices,
                resolution = resolution,
                ghostResolution = ghostResolution
            };
            JobHandle handle = job.Schedule(_vertices.Length, 64, default);

            handle.Complete();

            _mesh.SetVertices(_vertices);
            if(isSmoothed)
                _mesh.SetUVs(3, normals);
            else
            {
                //TODO: Maybe tangents do not need to be generated if we are using the flat tangents if the surface is smoothed
                _mesh.SetNormals(normals);
                _mesh.SetTangents(tangents);
            }
        }

        /// <summary>
        /// Deletes information stored on this object
        /// </summary>
        public void Dispose()
        {
            if (_meshFilter != null)
                _meshFilter.sharedMesh = null;

            if (_mesh != null)
                UnityEngine.Object.DestroyImmediate(_mesh);

            if (_vertices.IsCreated)
                _vertices.Dispose();

        }
        
        [BurstCompile]
        private struct CopyHeightsJob : IJobParallelFor
        {
            public NativeArray<Vector3> vertices;
            [ReadOnly] [NativeDisableParallelForRestriction] public NativeArray<float> heights;
            [ReadOnly] public IntegerPair resolution;
            [ReadOnly] public IntegerPair ghostResolution;

            public void Execute(int index)
            {
                int ghostResolutionXDiff = (ghostResolution.x - resolution.x) / 2;
                int ghostResolutionZDiff = (ghostResolution.z - resolution.z) / 2;

                Utils.FromIndexToSampleIndices(index, in resolution, out int x, out int z);

                Vector3 vec = vertices[index];
                vec.y = heights[(z + ghostResolutionZDiff) * ghostResolution.x + (x + ghostResolutionXDiff)];
                vertices[index] = vec;
            }
        }
    }
}

#endif