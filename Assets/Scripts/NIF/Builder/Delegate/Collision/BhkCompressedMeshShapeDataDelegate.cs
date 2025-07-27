using System.Collections.Generic;
using NIF.Parser;
using NIF.Parser.NiObjects;
using NIF.Parser.NiObjects.Structures;
using Vector3 = UnityEngine.Vector3;
using GameObject = NIF.Builder.Components.GameObject;
using Mesh = NIF.Builder.Components.Mesh.Mesh;
using MeshCollider = NIF.Builder.Components.Mesh.MeshCollider;

namespace NIF.Builder.Delegate.Collision
{
    public class BhkCompressedMeshShapeDataDelegate : NiObjectDelegate<BhkCompressedMeshShapeData>
    {
        protected override GameObject Instantiate(NiFile niFile, BhkCompressedMeshShapeData niObject,
            InstantiateChildNiObjectDelegate instantiateChildDelegate)
        {
            var rootGameObject = new GameObject("bhkCompressedMeshShape");
            if (niObject.NumBigVerts > 0 && niObject.NumBigTris > 0)
            {
                var vertices = new Vector3[niObject.NumBigVerts];
                var triangles = new int[niObject.NumBigTris * 3];

                for (var i = 0; i < niObject.NumBigVerts; i++)
                {
                    vertices[i] = NifUtils.NifVectorToUnityVector(niObject.BigVerts[i].ToUnityVector());
                }

                for (var i = 0; i < niObject.NumBigTris; i++)
                {
                    var triangle = niObject.BigTris[i];
                    triangles[i * 3] = triangle.Vertex3;
                    triangles[i * 3 + 1] = triangle.Vertex2;
                    triangles[i * 3 + 2] = triangle.Vertex1;
                }

                var mesh = new Mesh
                {
                    Vertices = vertices,
                    Triangles = triangles
                };
                var meshCollider = new MeshCollider
                {
                    Mesh = mesh
                };
                rootGameObject.AddComponent(meshCollider);
            }

            for (var i = 0; i < niObject.NumChunks; i++)
            {
                var chunkObject = new GameObject($"Chunk {i}")
                {
                    Position = NifUtils.NifVectorToUnityVector(niObject
                        .ChunkTransforms[niObject.Chunks[i].TransformIndex].Translation
                        .ToUnityVector()),
                    Rotation = NifUtils.HavokQuaternionToUnityQuaternion(niObject
                        .ChunkTransforms[niObject.Chunks[i].TransformIndex].Rotation
                        .ToUnityQuaternion())
                };
                var chunkMesh = CompressedChunkToMesh(niObject.Chunks[i]);
                var meshCollider = new MeshCollider
                {
                    Mesh = chunkMesh
                };
                chunkObject.AddComponent(meshCollider);
                chunkObject.Parent = rootGameObject;
            }

            return rootGameObject;
        }

        private static Mesh CompressedChunkToMesh(BhkCmsChunk chunk)
        {
            var vertices = new Vector3[chunk.Vertices.Length];
            var vertexTranslation = NifUtils.NifVectorToUnityVector(chunk.Translation.ToUnityVector());
            var triangles = new List<int>();
            var indicesArrayIndex = 0;

            for (var i = 0; i < chunk.Vertices.Length; i++)
            {
                vertices[i] = NifUtils.NifVectorToUnityVector(chunk.Vertices[i].ToVector3().ToUnityVector()) +
                              vertexTranslation;
            }

            foreach (var currentStripLength in chunk.StripLengths)
            {
                for (var j = indicesArrayIndex; j < indicesArrayIndex + currentStripLength - 2; j++)
                {
                    if ((j - indicesArrayIndex) % 2 == 0)
                    {
                        triangles.Add(chunk.Indices[j + 2]);
                        triangles.Add(chunk.Indices[j + 1]);
                        triangles.Add(chunk.Indices[j]);
                    }
                    else
                    {
                        triangles.Add(chunk.Indices[j]);
                        triangles.Add(chunk.Indices[j + 1]);
                        triangles.Add(chunk.Indices[j + 2]);
                    }
                }

                indicesArrayIndex += currentStripLength;
            }

            for (var i = indicesArrayIndex; i < chunk.Indices.Length - 2; i += 3)
            {
                triangles.Add(chunk.Indices[i + 2]);
                triangles.Add(chunk.Indices[i + 1]);
                triangles.Add(chunk.Indices[i]);
            }

            return new Mesh
            {
                Vertices = vertices,
                Triangles = triangles.ToArray()
            };
        }
    }
}