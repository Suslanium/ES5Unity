using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Engine;
using NIF.NiObjects;
using NIF.NiObjects.Structures;
using UnityEngine;
using UnityEngine.Rendering;
using Vector3 = UnityEngine.Vector3;
using Vector4 = UnityEngine.Vector4;

namespace NIF.Converter
{
    /// <summary>
    /// Based on https://github.com/ColeDeanShepherd/TESUnity/blob/f4d5e19f68da380da9da745356c7904f3428b9d6/Assets/Scripts/TES/NIF/NIFObjectBuilder.cs
    /// </summary>
    public class NifObjectBuilder
    {
        private readonly NiFile _file;
        private readonly MaterialManager _materialManager;

        public NifObjectBuilder(NiFile file, MaterialManager materialManager)
        {
            _file = file;
            _materialManager = materialManager;
        }

        public IEnumerator BuildObject(Action<GameObject> onReadyCallback)
        {
            if (_file == null)
            {
                onReadyCallback(null);
                yield break;
            }

            Debug.Assert((_file.Name != null) && (_file.Footer.RootReferences.Length > 0));

            if (_file.Footer.RootReferences.Length == 1)
            {
                var rootNiObject = _file.NiObjects[_file.Footer.RootReferences[0]];

                var gameObjectCoroutine = InstantiateRootNiObject(rootNiObject,
                    gameObject =>
                    {
                        if (gameObject == null)
                        {
                            Debug.Log(_file.Name + " resulted in a null GameObject when instantiated.");

                            gameObject = new GameObject(_file.Name);
                        }
                        else if (rootNiObject is NiNode)
                        {
                            gameObject.transform.position = Vector3.zero;
                            gameObject.transform.rotation = Quaternion.identity;
                            gameObject.transform.localScale = Vector3.one;
                        }

                        onReadyCallback(gameObject);
                    });
                if (gameObjectCoroutine == null)
                {
                    onReadyCallback(null);
                    yield break;
                }

                while (gameObjectCoroutine.MoveNext())
                {
                    yield return null;
                }
            }
            else
            {
                var gameObject = new GameObject(_file.Name);

                foreach (var rootRef in _file.Footer.RootReferences)
                {
                    var rootBlock = _file.NiObjects[rootRef];
                    var childCoroutine = InstantiateRootNiObject(rootBlock, child =>
                    {
                        if (child != null)
                        {
                            child.transform.SetParent(gameObject.transform, false);
                        }
                    });

                    if (childCoroutine == null) continue;
                    while (childCoroutine.MoveNext())
                    {
                        yield return null;
                    }
                }

                onReadyCallback(gameObject);
            }
        }

        private IEnumerator InstantiateRootNiObject(NiObject niObject, Action<GameObject> onReadyCallback)
        {
            return InstantiateNiObject(niObject, onReadyCallback);
        }

        private IEnumerator InstantiateNiObject(NiObject niObject, Action<GameObject> onReadyCallback)
        {
            return niObject switch
            {
                NiNode node => InstantiateNiNode(node, onReadyCallback),
                NiTriShape shape => InstantiateNiTriShape(shape, onReadyCallback),
                BsLodTriShape shape => InstantiateNiTriShape(shape, onReadyCallback),
                _ => null
            };
        }

        private IEnumerator InstantiateNiNode(NiNode node, Action<GameObject> onReadyCallback)
        {
            var gameObject = new GameObject(node.Name);

            foreach (var childRef in node.ChildrenReferences)
            {
                if (childRef < 0) continue;
                var childCoroutine = InstantiateNiObject(_file.NiObjects[childRef], child =>
                {
                    if (child != null)
                    {
                        child.transform.SetParent(gameObject.transform, false);
                    }
                });

                if (childCoroutine == null) continue;
                while (childCoroutine.MoveNext())
                {
                    yield return null;
                }
            }

            var transformCoroutine = ApplyNiAvObject(node, gameObject);
            while (transformCoroutine.MoveNext())
            {
                yield return null;
            }

            onReadyCallback(gameObject);
        }

        private IEnumerator InstantiateNiTriShape(NiTriBasedGeom triShape, Action<GameObject> onReadyCallback)
        {
            if (triShape.ShaderPropertyReference == -1)
            {
                onReadyCallback(null);
                yield break;
            }

            var shaderInfo = _file.NiObjects[triShape.ShaderPropertyReference];
            MaterialProperties materialProperties;
            if (shaderInfo is BsLightingShaderProperty info)
            {
                if (info.TextureSetReference == -1)
                {
                    onReadyCallback(null);
                    yield break;
                }

                materialProperties = CreateMaterialProps(info,
                    triShape.AlphaPropertyReference >= 0
                        ? (NiAlphaProperty)_file.NiObjects[triShape.AlphaPropertyReference]
                        : null);
            }
            else
            {
                onReadyCallback(null);
                yield break;
            }

            var gameObject = new GameObject(triShape.Name);
            var meshCoroutine = NiTriShapeDataToMesh((NiTriShapeData)_file.NiObjects[triShape.DataReference],
                mesh => { gameObject.AddComponent<MeshFilter>().mesh = mesh; });
            while (meshCoroutine.MoveNext())
            {
                yield return null;
            }

            var materialCoroutine = _materialManager.GetMaterialFromProperties(materialProperties,
                material =>
                {
                    var meshRenderer = gameObject.AddComponent<MeshRenderer>();
                    meshRenderer.material = material;
                });
            while (materialCoroutine.MoveNext())
            {
                yield return null;
            }

            var transformCoroutine = ApplyNiAvObject(triShape, gameObject);
            while (transformCoroutine.MoveNext())
            {
                yield return null;
            }

            onReadyCallback(gameObject);
        }

        private MaterialProperties CreateMaterialProps(BsLightingShaderProperty shaderInfo,
            NiAlphaProperty alphaProperty)
        {
            var isSpecular = (shaderInfo.ShaderPropertyFlags1 & 0x1) != 0;
            var useVertexColors = (shaderInfo.ShaderPropertyFlags2 & 0x20) != 0;
            var specularStrength = shaderInfo.SpecularStrength;
            var uvOffset = shaderInfo.UVOffset.ToVector2();
            var uvScale = shaderInfo.UVScale.ToVector2();
            var glossiness = shaderInfo.Glossiness;
            var emissiveColor = shaderInfo.EmissiveColor.ToColor();
            var specularColor = shaderInfo.SpecularColor.ToColor();
            var alpha = shaderInfo.Alpha;
            var textureSet = (BsShaderTextureSet)_file.NiObjects[shaderInfo.TextureSetReference];
            var diffuseMap = textureSet.NumberOfTextures >= 1 ? textureSet.Textures[0] : "";
            var normalMap = textureSet.NumberOfTextures >= 2 ? textureSet.Textures[1] : "";
            var glowMap = textureSet.NumberOfTextures >= 3 && (shaderInfo.ShaderPropertyFlags2 & 0x40) != 0
                ? textureSet.Textures[2]
                : "";
            var metallicMap = textureSet.NumberOfTextures >= 6 ? textureSet.Textures[5] : "";
            var environmentalMap = textureSet.NumberOfTextures >= 5 ? textureSet.Textures[4] : "";
            var alphaBlend = alphaProperty != null && alphaProperty.AlphaFlags.AlphaBlend;
            var srcFunction = alphaProperty != null ? alphaProperty.AlphaFlags.SourceBlendMode : BlendMode.SrcAlpha;
            var destFunction = alphaProperty != null
                ? alphaProperty.AlphaFlags.DestinationBlendMode
                : BlendMode.OneMinusSrcAlpha;
            var alphaTest = alphaProperty != null && alphaProperty.AlphaFlags.AlphaTest;
            var alphaTestThreshold = alphaProperty?.Threshold ?? 128;
            return new MaterialProperties(isSpecular, useVertexColors, specularStrength, uvOffset, uvScale, glossiness,
                emissiveColor, specularColor,
                alpha, diffuseMap, normalMap, glowMap, metallicMap, environmentalMap, shaderInfo.EnvironmentMapScale,
                new AlphaInfo(
                    alphaBlend, srcFunction, destFunction, alphaTest, alphaTestThreshold));
        }

        private static IEnumerator NiTriShapeDataToMesh(NiTriShapeData data, Action<Mesh> onReadyCallback)
        {
            Vector3[] vertices = null;
            if (data.HasVertices)
            {
                vertices = new Vector3[data.Vertices.Length];
                for (var i = 0; i < vertices.Length; i++)
                {
                    vertices[i] = NifUtils.NifPointToUnityPoint(data.Vertices[i].ToUnityVector());
                }
            }

            yield return null;

            Vector3[] normals = null;
            if (data.HasNormals)
            {
                normals = new Vector3[data.Normals.Length];
                for (var i = 0; i < normals.Length; i++)
                {
                    normals[i] = NifUtils.NifPointToUnityPoint(data.Normals[i].ToUnityVector());
                }
            }

            yield return null;

            Vector4[] tangents = null;
            if (data.Tangents != null)
            {
                tangents = new Vector4[data.Tangents.Length];
                for (var i = 0; i < tangents.Length; i++)
                {
                    var convertedTangent = NifUtils.NifPointToUnityPoint(data.Tangents[i].ToUnityVector());
                    tangents[i] = new Vector4(convertedTangent.x, convertedTangent.y, convertedTangent.z, 1);
                }
            }

            yield return null;

            Vector2[] UVs = null;
            if (data.UVSets != null && vertices != null)
            {
                UVs = new Vector2[vertices.Length];

                for (var i = 0; i < UVs.Length; i++)
                {
                    var texCoord = data.UVSets[0, i];

                    UVs[i] = new Vector2(texCoord.U, texCoord.V);
                }
            }

            yield return null;

            int[] triangles = null;
            if (data.HasTriangles)
            {
                triangles = new int[data.TrianglePointsNumber];
                for (var i = 0; i < data.Triangles.Length; i++)
                {
                    var baseI = 3 * i;

                    triangles[baseI] = data.Triangles[i].V1;
                    triangles[baseI + 1] = data.Triangles[i].V3;
                    triangles[baseI + 2] = data.Triangles[i].V2;
                }
            }

            yield return null;

            Color[] vertexColors = null;
            if (data.HasVertexColors)
            {
                vertexColors = new Color[data.VertexColors.Length];

                for (var i = 0; i < vertexColors.Length; i++)
                {
                    vertexColors[i] = data.VertexColors[i].ToColor();
                }
            }

            yield return null;

            var mesh = new Mesh
            {
                vertices = vertices,
                triangles = triangles,
                normals = normals,
                tangents = tangents,
                uv = UVs,
                colors = vertexColors
            };

            yield return null;

            if (!data.HasNormals)
            {
                mesh.RecalculateNormals();
            }

            mesh.RecalculateBounds();

            onReadyCallback(mesh);
        }

        private IEnumerator ApplyNiAvObject(NiAvObject anNiAvObject, GameObject obj)
        {
            obj.transform.position = NifUtils.NifPointToUnityPoint(anNiAvObject.Translation.ToUnityVector());
            obj.transform.rotation = NifUtils.NifRotationMatrixToUnityQuaternion(anNiAvObject.Rotation.ToMatrix4X4());
            obj.transform.localScale = anNiAvObject.Scale * Vector3.one;
            if (anNiAvObject.CollisionObjectReference <= 0) yield break;
            var collisionObjectEnumerator = InstantiateCollisionObject(anNiAvObject.CollisionObjectReference,
                collisionObject =>
                {
                    if (collisionObject != null)
                    {
                        collisionObject.transform.SetParent(obj.transform, false);
                    }
                });
            if (collisionObjectEnumerator == null) yield break;
            while (collisionObjectEnumerator.MoveNext())
            {
                yield return null;
            }
        }

        private IEnumerator InstantiateCollisionObject(int reference, Action<GameObject> onReadyCallback)
        {
            var collisionObj = _file.NiObjects[reference];
            if (collisionObj is BhkCollisionObject collisionObject)
            {
                return InstantiateBhkCollisionObject(collisionObject, onReadyCallback);
            }
            else
            {
                return null;
            }
        }

        private IEnumerator InstantiateBhkCollisionObject(BhkCollisionObject collisionObject,
            Action<GameObject> onReadyCallback)
        {
            var body = _file.NiObjects[collisionObject.BodyReference];
            switch (body)
            {
                case BhkRigidBodyT bhkRigidBodyT:
                    GameObject gameObject = null;
                    var gameObjectCoroutine = InstantiateRigidBody(bhkRigidBodyT,
                        o => { gameObject = o; });
                    if (gameObjectCoroutine == null)
                    {
                        onReadyCallback(null);
                        yield break;
                    }

                    while (gameObjectCoroutine.MoveNext())
                    {
                        yield return null;
                    }

                    if (gameObject == null)
                    {
                        onReadyCallback(null);
                        yield break;
                    }

                    gameObject.transform.position =
                        NifUtils.NifVectorToUnityVector(bhkRigidBodyT.Translation.ToUnityVector());
                    gameObject.transform.rotation =
                        NifUtils.HavokQuaternionToUnityQuaternion(bhkRigidBodyT.Rotation.ToUnityQuaternion());
                    onReadyCallback(gameObject);
                    break;
                case BhkRigidBody bhkRigidBody:
                    var rigidBodyCoroutine = InstantiateRigidBody(bhkRigidBody, onReadyCallback);
                    if (rigidBodyCoroutine == null)
                    {
                        onReadyCallback(null);
                        yield break;
                    }

                    while (rigidBodyCoroutine.MoveNext())
                    {
                        yield return null;
                    }

                    break;
                default:
                    Debug.LogWarning($"Unsupported collision object body type: {body.GetType().Name}");
                    onReadyCallback(null);
                    break;
            }
        }

        private IEnumerator InstantiateRigidBody(BhkRigidBody bhkRigidBody, Action<GameObject> onReadyCallback)
        {
            var shapeInfo = _file.NiObjects[bhkRigidBody.ShapeReference];
            switch (shapeInfo)
            {
                case BhkListShape bhkListShape:
                    return InstantiateBhkListShape(bhkListShape, onReadyCallback);
                case BhkConvexVerticesShape bhkConvexVerticesShape:
                    return BhkConvexVerticesShapeToGameObject(bhkConvexVerticesShape, onReadyCallback);
                case BhkMoppBvTreeShape bhkMoppBvTreeShape:
                    return BvTreeShapeToGameObject(bhkMoppBvTreeShape, onReadyCallback);
                default:
                    Debug.LogWarning($"Unsupported rigidbody shape type: {shapeInfo.GetType().Name}");
                    return null;
            }
        }

        private IEnumerator InstantiateBhkListShape(BhkListShape bhkListShape, Action<GameObject> onReadyCallback)
        {
            var shapeRefs = bhkListShape.SubShapeReferences.Select(subShapeRef => _file.NiObjects[subShapeRef]);
            var rootGameObject = new GameObject("bhkListShape");
            yield return null;
            foreach (var subShape in shapeRefs)
            {
                if (subShape is BhkConvexVerticesShape convexVerticesShape)
                {
                    var shapeObjectCoroutine = BhkConvexVerticesShapeToGameObject(convexVerticesShape,
                        shapeObject => { shapeObject.transform.SetParent(rootGameObject.transform, false); });
                    while (shapeObjectCoroutine.MoveNext())
                    {
                        yield return null;
                    }
                }
                else
                {
                    Debug.LogWarning($"Unsupported list shape subshape type: {subShape.GetType().Name}");
                    yield return null;
                }
            }

            onReadyCallback(rootGameObject);
        }

        private static IEnumerator BhkConvexVerticesShapeToGameObject(BhkConvexVerticesShape convexVerticesShape,
            Action<GameObject> onReadyCallback)
        {
            var gameObject = new GameObject("bhkConvexVerticesShape");
            var vertices = convexVerticesShape.Vertices
                .Select(vertex => NifUtils.NifVectorToUnityVector(vertex.ToUnityVector())).ToArray();
            yield return null;
            var mesh = new Mesh
            {
                vertices = vertices
            };
            yield return null;
            var collider = gameObject.AddComponent<MeshCollider>();
            collider.convex = true;
            collider.sharedMesh = mesh;
            yield return null;
            onReadyCallback(gameObject);
        }

        private IEnumerator BvTreeShapeToGameObject(BhkBvTreeShape treeShape, Action<GameObject> onReadyCallback)
        {
            var shapeInfo = _file.NiObjects[treeShape.ShapeReference];
            switch (shapeInfo)
            {
                case BhkCompressedMeshShape compressedMeshShape:
                    return CompressedShapeToGameObject(compressedMeshShape, onReadyCallback);
                case BhkListShape bhkListShape:
                    return InstantiateBhkListShape(bhkListShape, onReadyCallback);
                default:
                    Debug.LogWarning($"Unsupported BV tree shape type: {shapeInfo.GetType().Name}");
                    return null;
            }
        }

        private IEnumerator CompressedShapeToGameObject(BhkCompressedMeshShape compressedMeshShape,
            Action<GameObject> onReadyCallback)
        {
            var shapeData = _file.NiObjects[compressedMeshShape.DataRef];
            if (shapeData is BhkCompressedMeshShapeData compressedMeshShapeData)
            {
                GameObject shapeObject = null;
                var shapeObjectCoroutine = CompressedShapeDataToGameObject(compressedMeshShapeData,
                    o => { shapeObject = o; });
                while (shapeObjectCoroutine.MoveNext())
                {
                    yield return null;
                }

                shapeObject.transform.localScale =
                    NifUtils.NifVectorToUnityVector(compressedMeshShape.Scale.ToUnityVector());
                onReadyCallback(shapeObject);
            }
            else
            {
                Debug.LogWarning($"Unsupported compressed mesh shape data type: {shapeData.GetType().Name}");
                onReadyCallback(null);
            }
        }

        private static IEnumerator CompressedShapeDataToGameObject(BhkCompressedMeshShapeData compressedMeshShapeData,
            Action<GameObject> onReadyCallback)
        {
            var rootGameObject = new GameObject("bhkCompressedMeshShape");
            if (compressedMeshShapeData.NumBigVerts > 0 && compressedMeshShapeData.NumBigTris > 0)
            {
                var vertices = new Vector3[compressedMeshShapeData.NumBigVerts];
                var triangles = new int[compressedMeshShapeData.NumBigTris * 3];
                yield return null;

                for (var i = 0; i < compressedMeshShapeData.NumBigVerts; i++)
                {
                    vertices[i] = NifUtils.NifVectorToUnityVector(compressedMeshShapeData.BigVerts[i].ToUnityVector());
                }

                yield return null;

                for (var i = 0; i < compressedMeshShapeData.NumBigTris; i++)
                {
                    var triangle = compressedMeshShapeData.BigTris[i];
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

            for (var i = 0; i < compressedMeshShapeData.NumChunks; i++)
            {
                var chunkObject = new GameObject($"Chunk {i}")
                {
                    transform =
                    {
                        position = NifUtils.NifVectorToUnityVector(compressedMeshShapeData
                            .ChunkTransforms[compressedMeshShapeData.Chunks[i].TransformIndex].Translation
                            .ToUnityVector()),
                        rotation = NifUtils.HavokQuaternionToUnityQuaternion(compressedMeshShapeData
                            .ChunkTransforms[compressedMeshShapeData.Chunks[i].TransformIndex].Rotation
                            .ToUnityQuaternion())
                    }
                };
                var chunkMeshCoroutine = CompressedChunkToMesh(compressedMeshShapeData.Chunks[i],
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