using System;
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

        public IEnumerator GetMaterialFromProperties(MaterialProperties materialProperties,
            Action<Material> onReadyCallback)
        {
            if (_materialCache.TryGetValue(materialProperties, out var cachedMaterial))
            {
                onReadyCallback(cachedMaterial);
                yield break;
            }

            _textureManager.PreloadMap(TextureType.DIFFUSE, materialProperties.DiffuseMapPath);
            if (!string.IsNullOrEmpty(materialProperties.NormalMapPath))
                _textureManager.PreloadMap(TextureType.NORMAL, materialProperties.NormalMapPath);
            if (!string.IsNullOrEmpty(materialProperties.MetallicMaskPath))
                _textureManager.PreloadMap(TextureType.METALLIC, materialProperties.MetallicMaskPath);
            if (!string.IsNullOrEmpty(materialProperties.GlowMapPath))
                _textureManager.PreloadMap(TextureType.GLOW, materialProperties.GlowMapPath);
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

            if (materialProperties.AlphaInfo.AlphaTest)
            {
                material.SetFloat(Cutoff, materialProperties.AlphaInfo.AlphaTestThreshold / 256f);
            }

            var diffuseMapCoroutine = _textureManager.GetMap<Texture2D>(TextureType.DIFFUSE,
                materialProperties.DiffuseMapPath,
                texture2D => { material.SetTexture(MainTex, texture2D); });
            if (diffuseMapCoroutine != null)
                while (diffuseMapCoroutine.MoveNext())
                {
                    yield return null;
                }

            material.SetInt(UsesVertexColors, materialProperties.UseVertexColors ? 1 : 0);
            material.SetFloat(Alpha, materialProperties.Alpha);
            material.SetFloat(Glossiness, materialProperties.Glossiness);
            material.SetFloat(SpecularStrength,
                materialProperties.IsSpecular ? materialProperties.SpecularStrength : 0);
            material.SetColor(SpecularColor, materialProperties.SpecularColor);

            if (!string.IsNullOrEmpty(materialProperties.NormalMapPath))
            {
                var normalMapCoroutine = _textureManager.GetMap<Texture2D>(TextureType.NORMAL,
                    materialProperties.NormalMapPath,
                    texture2D =>
                    {
                        material.EnableKeyword("_NORMALMAP");
                        material.SetTexture(NormalMap, texture2D);
                    });
                if (normalMapCoroutine != null)
                    while (normalMapCoroutine.MoveNext())
                    {
                        yield return null;
                    }
            }

            if (!string.IsNullOrEmpty(materialProperties.MetallicMaskPath))
            {
                var metallicMapCoroutine = _textureManager.GetMap<Texture2D>(TextureType.METALLIC,
                    materialProperties.MetallicMaskPath,
                    texture2D => { material.SetTexture(MetallicMap, texture2D); });
                if (metallicMapCoroutine != null)
                    while (metallicMapCoroutine.MoveNext())
                    {
                        yield return null;
                    }
            }

            if (materialProperties.EmissiveColor != Color.black ||
                !string.IsNullOrEmpty(materialProperties.GlowMapPath))
            {
                material.SetInt(EnableEmission, 1);
                material.SetColor(EmissionColor, materialProperties.EmissiveColor);
                if (!string.IsNullOrEmpty(materialProperties.GlowMapPath))
                {
                    var glowMapCoroutine = _textureManager.GetMap<Texture2D>(TextureType.GLOW,
                        materialProperties.GlowMapPath,
                        texture2D => { material.SetTexture(EmissionMap, texture2D); });
                    if (glowMapCoroutine != null)
                        while (glowMapCoroutine.MoveNext())
                        {
                            yield return null;
                        }
                }
            }

            if (!string.IsNullOrEmpty(materialProperties.EnvironmentalMapPath))
            {
                var envMapCoroutine = _textureManager.GetMap<Cubemap>(TextureType.ENVIRONMENTAL,
                    materialProperties.EnvironmentalMapPath,
                    cubeMap =>
                    {
                        material.SetTexture(Cube, cubeMap);
                        material.SetFloat(CubeScale, materialProperties.EnvironmentalMapScale);
                    });
                if (envMapCoroutine != null)
                    while (envMapCoroutine.MoveNext())
                    {
                        yield return null;
                    }
            }

            _materialCache.Add(materialProperties, material);

            onReadyCallback(material);
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