using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace AssetKits.ParticleImage
{
    [AddComponentMenu("UI/Particle Image/Trail Renderer")]
    public class ParticleTrailRenderer : MaskableGraphic
    {
        private ParticleImage _particle;

        private Mesh _trailMesh;
        
        public Mesh trailMesh
        {
            get
            {
                if(_trailMesh == null)
                {
                    _trailMesh = new Mesh();
                    _trailMesh.MarkDynamic();
                }
                
                return _trailMesh;
            }
        }
        
        private Mesh.MeshDataArray _trailMeshDataArray;
        private Mesh.MeshData _trailMeshData;
        
        private int offset;
        private int trisOffset;
        private int trisCount;

        public ParticleImage particle
        {
            get => _particle;
            set => _particle = value;
        }

        protected override void OnPopulateMesh(VertexHelper vh) { }

        protected override void UpdateGeometry() { }

        public void PrepareMeshData(int vertexCount, int particleCount)
        {
            const int MaxVertices = 2048; // or lower if you want stricter limits

            // Clamp vertexCount to safe value
            vertexCount = Mathf.Min(vertexCount, MaxVertices);

            // Clamp trisCount to safe value based on max vertices
            trisCount = Mathf.Min((vertexCount - particleCount) * 6, MaxVertices * 3); // 3 indices per triangle

            // Use UInt32 if vertex count is too high for UInt16
            IndexFormat indexFormat = (vertexCount * 2 > 65535) ? IndexFormat.UInt32 : IndexFormat.UInt16;

            _trailMeshDataArray = Mesh.AllocateWritableMeshData(1);
            _trailMeshData = _trailMeshDataArray[0];

            _trailMeshData.SetVertexBufferParams(vertexCount * 2, 
                new VertexAttributeDescriptor(VertexAttribute.Position),
                new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.Float32, 4, 1));

            _trailMeshData.SetIndexBufferParams(trisCount, indexFormat);

            offset = 0;
            trisOffset = 0;
        }

        public void UpdateMeshData(NativeArray<Vector3> points, NativeArray<int> tris, NativeArray<Color> cols)
        {
            var vertexBuffer = _trailMeshData.GetVertexData<Vector3>();
            var colorBuffer = _trailMeshData.GetVertexData<Color>(1);
            var indexBuffer = _trailMeshData.GetIndexData<ushort>();

            int maxVertexWrite = Mathf.Min(points.Length, vertexBuffer.Length - offset);
            int maxColorWrite = Mathf.Min(cols.Length, colorBuffer.Length - offset);
            int maxIndexWrite = Mathf.Min(tris.Length, indexBuffer.Length - trisOffset);

            for (var i = 0; i < maxVertexWrite; i++)
            {
                vertexBuffer[i + offset] = points[i];
            }

            for (var i = 0; i < maxColorWrite; i++)
            {
                colorBuffer[i + offset] = cols[i];
            }

            for (var i = 0; i < maxIndexWrite; i++)
            {
                indexBuffer[i + trisOffset] = (ushort)(tris[i] + offset);
            }

            offset += maxVertexWrite;
            trisOffset += maxIndexWrite;

            if (maxVertexWrite < points.Length || maxColorWrite < cols.Length || maxIndexWrite < tris.Length)
                Debug.LogWarning("ParticleTrailRenderer: Mesh data clamped to buffer size!");
        }
        
        public void SetMeshData()
        {
            SetMeshData(_trailMeshDataArray,_trailMeshData, trisCount);
        }

        public void SetMeshData(Mesh.MeshDataArray meshDataArray, Mesh.MeshData meshData, int triCount)
        {
            meshData.subMeshCount = 1;
            meshData.SetSubMesh(0, new SubMeshDescriptor(0, triCount));
            
            Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, trailMesh, MeshUpdateFlags.DontRecalculateBounds);
            
            trailMesh.RecalculateBounds();
            canvasRenderer.SetMesh(trailMesh);
            SetMaterialDirty();
        }

        public void Clear()
        {
            trailMesh.Clear();
            canvasRenderer.SetMesh(trailMesh);
            SetMaterialDirty();
        }
    }
}