using System.Collections.Generic;
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

        public GameObject BuildObject()
        {
            if (_file == null) return null;
            Debug.Assert((_file.Name != null) && (_file.Footer.RootReferences.Length > 0));

            if (_file.Footer.RootReferences.Length == 1)
            {
                var rootNiObject = _file.NiObjects[_file.Footer.RootReferences[0]];

                var gameObject = InstantiateRootNiObject(rootNiObject);

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

                return gameObject;
            }
            else
            {
                GameObject gameObject = new GameObject(_file.Name);

                foreach (var rootRef in _file.Footer.RootReferences)
                {
                    var rootBlock = _file.NiObjects[rootRef];
                    var child = InstantiateRootNiObject(rootBlock);

                    if (child != null)
                    {
                        child.transform.SetParent(gameObject.transform, false);
                    }
                }

                return gameObject;
            }
        }

        private GameObject InstantiateRootNiObject(NiObject niObject)
        {
            var gameObject = InstantiateNiObject(niObject);

            //Additional processing

            return gameObject;
        }

        private GameObject InstantiateNiObject(NiObject niObject)
        {
            return niObject switch
            {
                NiNode node => InstantiateNiNode(node),
                NiTriShape shape => InstantiateNiTriShape(shape),
                BsLodTriShape shape => InstantiateNiTriShape(shape),
                _ => null
            };
        }

        private GameObject InstantiateNiNode(NiNode node)
        {
            var gameObject = new GameObject(node.Name);

            foreach (var childRef in node.ChildrenReferences)
            {
                if (childRef < 0) continue;
                var child = InstantiateNiObject(_file.NiObjects[childRef]);

                if (child != null)
                {
                    child.transform.SetParent(gameObject.transform, false);
                }
            }

            ApplyNiAvObject(node, gameObject);

            return gameObject;
        }

        private GameObject InstantiateNiTriShape(NiTriBasedGeom triShape)
        {
            if (triShape.ShaderPropertyReference == -1)
            {
                return null;
            }

            var shaderInfo = _file.NiObjects[triShape.ShaderPropertyReference];
            MaterialProperties materialProperties;
            if (shaderInfo is BsLightingShaderProperty info)
            {
                if (info.TextureSetReference == -1) return null;
                materialProperties = CreateMaterialProps(info,
                    triShape.AlphaPropertyReference >= 0
                        ? (NiAlphaProperty)_file.NiObjects[triShape.AlphaPropertyReference]
                        : null);
            }
            else
            {
                return null;
            }

            var mesh = NiTriShapeDataToMesh((NiTriShapeData)_file.NiObjects[triShape.DataReference]);
            var gameObject = new GameObject(triShape.Name);

            gameObject.AddComponent<MeshFilter>().mesh = mesh;
            var material = _materialManager.GetMaterialFromProperties(materialProperties);
            var meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.material = material;

            ApplyNiAvObject(triShape, gameObject);
            return gameObject;
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

        private static Mesh NiTriShapeDataToMesh(NiTriShapeData data)
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

            Vector3[] normals = null;
            if (data.HasNormals)
            {
                normals = new Vector3[data.Normals.Length];
                for (var i = 0; i < normals.Length; i++)
                {
                    normals[i] = NifUtils.NifPointToUnityPoint(data.Normals[i].ToUnityVector());
                }
            }

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

            Color[] vertexColors = null;
            if (data.HasVertexColors)
            {
                vertexColors = new Color[data.VertexColors.Length];

                for (var i = 0; i < vertexColors.Length; i++)
                {
                    vertexColors[i] = data.VertexColors[i].ToColor();
                }
            }

            var mesh = new Mesh
            {
                vertices = vertices,
                triangles = triangles,
                normals = normals,
                tangents = tangents,
                uv = UVs,
                colors = vertexColors
            };

            if (!data.HasNormals)
            {
                mesh.RecalculateNormals();
            }

            mesh.RecalculateBounds();

            return mesh;
        }

        private void ApplyNiAvObject(NiAvObject anNiAvObject, GameObject obj)
        {
            obj.transform.position = NifUtils.NifPointToUnityPoint(anNiAvObject.Translation.ToUnityVector());
            obj.transform.rotation = NifUtils.NifRotationMatrixToUnityQuaternion(anNiAvObject.Rotation.ToMatrix4X4());
            obj.transform.localScale = anNiAvObject.Scale * Vector3.one;
            if (anNiAvObject.CollisionObjectReference <= 0) return;
            var collisionObject = InstantiateCollisionObject(anNiAvObject.CollisionObjectReference);
            if (collisionObject != null)
            {
                collisionObject.transform.SetParent(obj.transform, false);
            }
        }

        private GameObject InstantiateCollisionObject(int reference)
        {
            var collisionObj = _file.NiObjects[reference];
            if (collisionObj is BhkCollisionObject collisionObject)
            {
                return InstantiateBhkCollisionObject(collisionObject);
            }
            else
            {
                return null;
            }
        }

        private GameObject InstantiateBhkCollisionObject(BhkCollisionObject collisionObject)
        {
            var body = _file.NiObjects[collisionObject.BodyReference];
            if (body is BhkRigidBodyT bhkRigidBodyT)
            {
                var gameObject = InstantiateRigidBody(bhkRigidBodyT);
                gameObject.transform.position =
                    NifUtils.NifVectorToUnityVector(bhkRigidBodyT.Translation.ToUnityVector());
                gameObject.transform.rotation =
                    NifUtils.HavokQuaternionToUnityQuaternion(bhkRigidBodyT.Rotation.ToUnityQuaternion());
                return gameObject;
            }
            else if (body is BhkRigidBody bhkRigidBody)
            {
                return InstantiateRigidBody(bhkRigidBody);
            }
            else
            {
                Debug.LogWarning($"Unsupported collision object body type: {body.GetType().Name}");
                return null;
            }
        }

        private GameObject InstantiateRigidBody(BhkRigidBody bhkRigidBody)
        {
            var shapeInfo = _file.NiObjects[bhkRigidBody.ShapeReference];
            if (shapeInfo is BhkMoppBvTreeShape bhkMoppBvTreeShape)
            {
                return BvTreeShapeToGameObject(bhkMoppBvTreeShape);
            }
            else
            {
                Debug.LogWarning($"Unsupported rigidbody shape type: {shapeInfo.GetType().Name}");
                return null;
            }
        }

        private GameObject BvTreeShapeToGameObject(BhkBvTreeShape treeShape)
        {
            var shapeInfo = _file.NiObjects[treeShape.ShapeReference];
            if (shapeInfo is BhkCompressedMeshShape compressedMeshShape)
            {
                return CompressedShapeToGameObject(compressedMeshShape);
            }
            else
            {
                Debug.LogWarning($"Unsupported BV tree shape type: {shapeInfo.GetType().Name}");
                return null;
            }
        }

        private GameObject CompressedShapeToGameObject(BhkCompressedMeshShape compressedMeshShape)
        {
            var shapeData = _file.NiObjects[compressedMeshShape.DataRef];
            if (shapeData is BhkCompressedMeshShapeData compressedMeshShapeData)
            {
                var shapeObject = CompressedShapeDataToGameObject(compressedMeshShapeData);
                shapeObject.transform.localScale =
                    NifUtils.NifVectorToUnityVector(compressedMeshShape.Scale.ToUnityVector());
                return shapeObject;
            }
            else
            {
                Debug.LogWarning($"Unsupported compressed mesh shape data type: {shapeData.GetType().Name}");
                return null;
            }
        }

        private static GameObject CompressedShapeDataToGameObject(BhkCompressedMeshShapeData compressedMeshShapeData)
        {
            var rootGameObject = new GameObject("bhkCompressedMeshShape");
            if (compressedMeshShapeData.NumBigVerts > 0 && compressedMeshShapeData.NumBigTris > 0)
            {
                var vertices = new Vector3[compressedMeshShapeData.NumBigVerts];
                var triangles = new int[compressedMeshShapeData.NumBigTris * 3];
                for (var i = 0; i < compressedMeshShapeData.NumBigVerts; i++)
                {
                    vertices[i] = NifUtils.NifVectorToUnityVector(compressedMeshShapeData.BigVerts[i].ToUnityVector());
                }

                for (var i = 0; i < compressedMeshShapeData.NumBigTris; i++)
                {
                    var triangle = compressedMeshShapeData.BigTris[i];
                    triangles[i * 3] = triangle.Vertex3;
                    triangles[i * 3 + 1] = triangle.Vertex2;
                    triangles[i * 3 + 2] = triangle.Vertex1;
                }

                var mesh = new Mesh
                {
                    vertices = vertices,
                    triangles = triangles
                };
                //TODO change to mesh collider
                rootGameObject.AddComponent<MeshFilter>().mesh = mesh;
                rootGameObject.AddComponent<MeshRenderer>();
            }

            for (var i = 0; i < compressedMeshShapeData.NumChunks; i++)
            {
                var chunkMesh = CompressedChunkToMesh(compressedMeshShapeData.Chunks[i]);
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
                //TODO change to mesh collider
                chunkObject.AddComponent<MeshFilter>().mesh = chunkMesh;
                chunkObject.AddComponent<MeshRenderer>();
                chunkObject.transform.SetParent(rootGameObject.transform, false);
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
                vertices = vertices,
                triangles = triangles.ToArray()
            };
        }
    }
}