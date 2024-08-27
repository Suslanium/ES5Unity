using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Engine.Textures
{
    public class MaterialManager
    {
        private static readonly Shader DefaultShader = Shader.Find("SkyrimDefaultShader");
        private static readonly Shader BlendShader = Shader.Find("SkyrimBlendShader");
        private static readonly Shader AlphaTestShader = Shader.Find("SkyrimAlphaTestShader");
        private readonly TextureManager _textureManager;
        private readonly Dictionary<MaterialProperties, Material> _materialCache = new();
        private static readonly int MainTex = Shader.PropertyToID("_MainTex");
        private static readonly int Alpha = Shader.PropertyToID("_Alpha");
        private static readonly int Glossiness = Shader.PropertyToID("_Glossiness");
        private static readonly int SpecularStrength = Shader.PropertyToID("_SpecularStrength");
        private static readonly int SpecularColor = Shader.PropertyToID("_SpecColor");
        private static readonly int NormalMap = Shader.PropertyToID("_NormalMap");
        private static readonly int MetallicMap = Shader.PropertyToID("_MetallicMap");
        private static readonly int EnableEmission = Shader.PropertyToID("_EnableEmission");
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        private static readonly int EmissionMap = Shader.PropertyToID("_EmissionMap");
        private static readonly int UsesVertexColors = Shader.PropertyToID("_UsesVertexColors");
        private static readonly int Cube = Shader.PropertyToID("_Cube");
        private static readonly int CubeScale = Shader.PropertyToID("_CubeScale");
        private static readonly int BlendSrc = Shader.PropertyToID("_BlendSrc");
        private static readonly int BlendDst = Shader.PropertyToID("_BlendDst");
        private static readonly int Cutoff = Shader.PropertyToID("_Cutoff");

        public MaterialManager(TextureManager textureManager)
        {
            _textureManager = textureManager;
        }

        public IEnumerator<Material> GetMaterialFromProperties(MaterialProperties materialProperties)
        {
            if (_materialCache.TryGetValue(materialProperties, out var cachedMaterial))
            {
                yield return cachedMaterial;
                yield break;
            }

            yield return null;

            var material = materialProperties.AlphaInfo.AlphaBlend == false
                ? materialProperties.AlphaInfo.AlphaTest == false
                    ? new Material(DefaultShader)
                    : new Material(AlphaTestShader)
                : new Material(BlendShader);
            yield return null;

            if (materialProperties.AlphaInfo.AlphaBlend)
            {
                material.SetInt(BlendSrc, (int)materialProperties.AlphaInfo.SourceBlendMode);
                material.SetInt(BlendDst, (int)materialProperties.AlphaInfo.DestinationBlendMode);
            }

            yield return null;

            if (materialProperties.AlphaInfo.AlphaTest)
            {
                material.SetFloat(Cutoff, materialProperties.AlphaInfo.AlphaTestThreshold / 256f);
            }

            yield return null;

            var diffuseMapCoroutine = _textureManager.GetMap<Texture2D>(TextureType.DIFFUSE,
                materialProperties.DiffuseMapPath);
            if (diffuseMapCoroutine != null)
            {
                while (diffuseMapCoroutine.MoveNext())
                {
                    yield return null;
                }

                var diffuseMap = diffuseMapCoroutine.Current;
                yield return null;

                if (diffuseMap != null)
                {
                    material.SetTexture(MainTex, diffuseMap);
                }
            }

            yield return null;

            material.SetInt(UsesVertexColors, materialProperties.UseVertexColors ? 1 : 0);
            material.SetFloat(Alpha, materialProperties.Alpha);
            material.SetFloat(Glossiness, materialProperties.Glossiness);
            material.SetFloat(SpecularStrength,
                materialProperties.IsSpecular ? materialProperties.SpecularStrength : 0);
            material.SetColor(SpecularColor, materialProperties.SpecularColor);
            yield return null;

            if (!string.IsNullOrEmpty(materialProperties.NormalMapPath))
            {
                var normalMapCoroutine = _textureManager.GetMap<Texture2D>(TextureType.NORMAL,
                    materialProperties.NormalMapPath);
                if (normalMapCoroutine != null)
                {
                    while (normalMapCoroutine.MoveNext())
                    {
                        yield return null;
                    }

                    var normalMap = normalMapCoroutine.Current;
                    yield return null;

                    if (normalMap != null)
                    {
                        material.EnableKeyword("_NORMALMAP");
                        material.SetTexture(NormalMap, normalMap);
                    }
                }
            }

            yield return null;

            if (!string.IsNullOrEmpty(materialProperties.MetallicMaskPath))
            {
                var metallicMapCoroutine = _textureManager.GetMap<Texture2D>(TextureType.METALLIC,
                    materialProperties.MetallicMaskPath);
                if (metallicMapCoroutine != null)
                {
                    while (metallicMapCoroutine.MoveNext())
                    {
                        yield return null;
                    }

                    var metallicMap = metallicMapCoroutine.Current;
                    yield return null;

                    if (metallicMap != null)
                    {
                        material.SetTexture(MetallicMap, metallicMap);
                    }
                }
            }

            yield return null;

            if (materialProperties.EmissiveColor != Color.black ||
                !string.IsNullOrEmpty(materialProperties.GlowMapPath))
            {
                material.SetInt(EnableEmission, 1);
                material.SetColor(EmissionColor, materialProperties.EmissiveColor);
                yield return null;
                if (!string.IsNullOrEmpty(materialProperties.GlowMapPath))
                {
                    var glowMapCoroutine = _textureManager.GetMap<Texture2D>(TextureType.GLOW,
                        materialProperties.GlowMapPath);
                    if (glowMapCoroutine != null)
                    {
                        while (glowMapCoroutine.MoveNext())
                        {
                            yield return null;
                        }

                        var glowMap = glowMapCoroutine.Current;
                        yield return null;

                        if (glowMap != null)
                        {
                            material.SetTexture(EmissionMap, glowMap);
                        }
                    }
                }
            }

            yield return null;

            if (!string.IsNullOrEmpty(materialProperties.EnvironmentalMapPath))
            {
                var envMapCoroutine = _textureManager.GetMap<Cubemap>(TextureType.ENVIRONMENTAL,
                    materialProperties.EnvironmentalMapPath);
                if (envMapCoroutine != null)
                {
                    while (envMapCoroutine.MoveNext())
                    {
                        yield return null;
                    }

                    var cubeMap = envMapCoroutine.Current;
                    yield return null;

                    if (cubeMap != null)
                    {
                        material.SetTexture(Cube, cubeMap);
                        material.SetFloat(CubeScale, materialProperties.EnvironmentalMapScale);
                    }
                }
            }

            _materialCache.Add(materialProperties, material);
            yield return null;

            yield return material;
        }

        /// <summary>
        /// WARNING: Call this ONLY when textures and materials are not needed anymore
        /// </summary>
        public IEnumerator ClearCachedMaterialsAndTextures()
        {
            foreach (var material in _materialCache.Values)
            {
                Object.Destroy(material);
                yield return null;
            }

            _materialCache.Clear();
            yield return null;
            var clearTexturesCoroutine = _textureManager.ClearCachedTextures();
            while (clearTexturesCoroutine.MoveNext())
            {
                yield return null;
            }
        }
    }
}