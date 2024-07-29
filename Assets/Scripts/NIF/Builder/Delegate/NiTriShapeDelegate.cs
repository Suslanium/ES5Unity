using System;
using System.Collections;
using Engine;
using NIF.Parser;
using NIF.Parser.NiObjects;
using UnityEngine;
using UnityEngine.Rendering;

namespace NIF.Builder.Delegate
{
    public class NiTriShapeDelegate : NiObjectDelegate<NiTriBasedGeom>
    {
        private readonly MaterialManager _materialManager;

        public NiTriShapeDelegate(MaterialManager materialManager)
        {
            _materialManager = materialManager;
        }

        public override bool IsApplicable(NiObject niObject)
        {
            return niObject.GetType() == typeof(NiTriShape) || niObject.GetType() == typeof(BsLodTriShape);
        }

        protected override IEnumerator Instantiate(NiFile niFile, NiTriBasedGeom niObject,
            InstantiateChildNiObjectDelegate instantiateChildDelegate,
            Action<GameObject> onReadyCallback)
        {
            if (niObject.ShaderPropertyReference == -1)
            {
                onReadyCallback(null);
                yield break;
            }

            var shaderInfo = niFile.NiObjects[niObject.ShaderPropertyReference];
            MaterialProperties materialProperties;
            if (shaderInfo is BsLightingShaderProperty info)
            {
                if (info.TextureSetReference == -1)
                {
                    onReadyCallback(null);
                    yield break;
                }

                materialProperties = CreateMaterialProps(info,
                    niObject.AlphaPropertyReference >= 0
                        ? (NiAlphaProperty)niFile.NiObjects[niObject.AlphaPropertyReference]
                        : null, niFile);
            }
            else
            {
                onReadyCallback(null);
                yield break;
            }

            var gameObject = new GameObject(niObject.Name);
            var meshCoroutine = NiTriShapeDataToMesh((NiTriShapeData)niFile.NiObjects[niObject.DataReference],
                mesh => { gameObject.AddComponent<MeshFilter>().mesh = mesh; });
            while (meshCoroutine.MoveNext())
            {
                yield return null;
            }

            var materialCoroutine = _materialManager.GetMaterialFromProperties(materialProperties,
                material =>
                {
                    var meshRenderer = gameObject.AddComponent<MeshRenderer>();
                    meshRenderer.sharedMaterial = material;
                });
            while (materialCoroutine.MoveNext())
            {
                yield return null;
            }

            NifUtils.ApplyNiAvObjectTransform(niObject, gameObject);

            onReadyCallback(gameObject);
        }

        private MaterialProperties CreateMaterialProps(BsLightingShaderProperty shaderInfo,
            NiAlphaProperty alphaProperty, NiFile niFile)
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
            var textureSet = (BsShaderTextureSet)niFile.NiObjects[shaderInfo.TextureSetReference];
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
    }
}