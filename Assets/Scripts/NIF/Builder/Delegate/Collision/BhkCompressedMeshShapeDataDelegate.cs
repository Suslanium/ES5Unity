using System;
using System.Collections;
using System.Collections.Generic;
using NIF.Parser;
using NIF.Parser.NiObjects;
using NIF.Parser.NiObjects.Structures;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace NIF.Builder.Delegate.Collision
{
    public class BhkCompressedMeshShapeDataDelegate : NiObjectDelegate<BhkCompressedMeshShapeData>
    {
        protected override IEnumerator Instantiate(NiFile niFile, BhkCompressedMeshShapeData niObject,
            InstantiateChildNiObjectDelegate instantiateChildDelegate, Action<GameObject> onReadyCallback)
        {
            var rootGameObject = new GameObject("bhkCompressedMeshShape");
            if (niObject.NumBigVerts > 0 && niObject.NumBigTris > 0)
            {
                var vertices = new Vector3[niObject.NumBigVerts];
                var triangles = new int[niObject.NumBigTris * 3];
                yield return null;

                for (var i = 0; i < niObject.NumBigVerts; i++)
                {
                    vertices[i] = NifUtils.NifVectorToUnityVector(niObject.BigVerts[i].ToUnityVector());
                }

                yield return null;

                for (var i = 0; i < niObject.NumBigTris; i++)
                {
                    var triangle = niObject.BigTris[i];
                    triangles[i * 3] = triangle.Vertex3;
                    triangles[i * 3 + 1] = triangle.Vertex2;
                    triangles[i * 3 + 2] = triangle.Vertex1;
                }

                yield return null;

                var mesh = new Mesh
                {
                    vertices = vertices,
                    triangles = triangles
                };
                rootGameObject.AddComponent<MeshCollider>().sharedMesh = mesh;
                yield return null;
            }

            for (var i = 0; i < niObject.NumChunks; i++)
            {
                var chunkObject = new GameObject($"Chunk {i}")
                {
                    transform =
                    {
                        position = NifUtils.NifVectorToUnityVector(niObject
                            .ChunkTransforms[niObject.Chunks[i].TransformIndex].Translation
                            .ToUnityVector()),
                        rotation = NifUtils.HavokQuaternionToUnityQuaternion(niObject
                            .ChunkTransforms[niObject.Chunks[i].TransformIndex].Rotation
                            .ToUnityQuaternion())
                    }
                };
                var chunkMeshCoroutine = CompressedChunkToMesh(niObject.Chunks[i],
                    chunkMesh =>
                    {
                        chunkObject.AddComponent<MeshCollider>().sharedMesh = chunkMesh;
                        chunkObject.transform.SetParent(rootGameObject.transform, false);
                    });
                while (chunkMeshCoroutine.MoveNext())
                {
                    yield return null;
                }
            }

            yield return null;

            onReadyCallback(rootGameObject);
        }
        
        private static IEnumerator CompressedChunkToMesh(BhkCmsChunk chunk, Action<Mesh> onReadyCallback)
        {
            var vertices = new Vector3[chunk.Vertices.Length];
            var vertexTranslation = NifUtils.NifVectorToUnityVector(chunk.Translation.ToUnityVector());
            var triangles = new List<int>();
            var indicesArrayIndex = 0;
            yield return null;

            for (var i = 0; i < chunk.Vertices.Length; i++)
            {
                vertices[i] = NifUtils.NifVectorToUnityVector(chunk.Vertices[i].ToVector3().ToUnityVector()) +
                              vertexTranslation;
            }

            yield return null;

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

            yield return null;

            for (var i = indicesArrayIndex; i < chunk.Indices.Length - 2; i += 3)
            {
                triangles.Add(chunk.Indices[i + 2]);
                triangles.Add(chunk.Indices[i + 1]);
                triangles.Add(chunk.Indices[i]);
            }

            yield return null;

            onReadyCallback(new Mesh
            {
                vertices = vertices,
                triangles = triangles.ToArray()
            });
        }
    }
}