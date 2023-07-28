using Engine;
using NIF.NiObjects;
using UnityEngine;

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
                materialProperties = CreateMaterialProps(info);
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

        private MaterialProperties CreateMaterialProps(BsLightingShaderProperty shaderInfo)
        {
            var isSpecular = (shaderInfo.ShaderPropertyFlags1 & 0x1) != 0;
            var uvOffset = shaderInfo.UVOffset.ToVector2();
            var uvScale = shaderInfo.UVScale.ToVector2();
            var glossiness = shaderInfo.Glossiness / 1000f;
            var emissiveColor = shaderInfo.EmissiveColor.ToColor();
            var specularColor = shaderInfo.SpecularColor.ToColor();
            var alpha = shaderInfo.Alpha;
            var textureSet = (BsShaderTextureSet)_file.NiObjects[shaderInfo.TextureSetReference];
            var diffuseMap = textureSet.NumberOfTextures >= 1 ? textureSet.Textures[0] : "";
            var normalMap = textureSet.NumberOfTextures >= 2 ? textureSet.Textures[1] : "";
            var glowMap = textureSet.NumberOfTextures >= 3 && (shaderInfo.ShaderPropertyFlags2 & 0x40) != 0
                ? textureSet.Textures[2]
                : "";
            return new MaterialProperties(isSpecular, uvOffset, uvScale, glossiness, emissiveColor, specularColor,
                alpha, diffuseMap, normalMap, glowMap, false);
        }

        private Mesh NiTriShapeDataToMesh(NiTriShapeData data)
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
                normals = normals,
                tangents = tangents,
                uv = UVs,
                triangles = triangles,
                colors = vertexColors
            };

            if (!data.HasNormals)
            {
                mesh.RecalculateNormals();
            }

            mesh.RecalculateBounds();

            return mesh;
        }

        private static void ApplyNiAvObject(NiAvObject anNiAvObject, GameObject obj)
        {
            obj.transform.position = NifUtils.NifPointToUnityPoint(anNiAvObject.Translation.ToUnityVector());
            obj.transform.rotation = NifUtils.NifRotationMatrixToUnityQuaternion(anNiAvObject.Rotation.ToMatrix4X4());
            obj.transform.localScale = anNiAvObject.Scale * Vector3.one;
        }
    }
}