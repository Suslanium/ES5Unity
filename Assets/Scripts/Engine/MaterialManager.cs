using System.Collections.Generic;
using UnityEngine;

namespace Engine
{
    public class MaterialManager
    {
        private static readonly Shader DefaultShader = Shader.Find("SkyrimDefaultShader");
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

        public MaterialManager(TextureManager textureManager)
        {
            _textureManager = textureManager;
        }

        public Material GetMaterialFromProperties(MaterialProperties materialProperties)
        {
            if (_materialCache.TryGetValue(materialProperties, out var cachedMaterial))
            {
                return cachedMaterial;
            }

            _textureManager.PreloadDiffuseMap(materialProperties.DiffuseMapPath);
            if (!string.IsNullOrEmpty(materialProperties.NormalMapPath))
                _textureManager.PreloadNormalMap(materialProperties.NormalMapPath);
            if (!string.IsNullOrEmpty(materialProperties.MetallicMaskPath))
                _textureManager.PreloadMetallicMap(materialProperties.MetallicMaskPath);
            if (!string.IsNullOrEmpty(materialProperties.GlowMapPath))
                _textureManager.PreloadGlowMap(materialProperties.GlowMapPath);

            var material = new Material(DefaultShader);

            material.SetTexture(MainTex, _textureManager.GetDiffuseMap(materialProperties.DiffuseMapPath));
            material.SetInt(UsesVertexColors, materialProperties.UseVertexColors ? 1 : 0);
            material.SetFloat(Alpha, materialProperties.Alpha);
            material.SetFloat(Glossiness, materialProperties.Glossiness);
            material.SetFloat(SpecularStrength,
                materialProperties.IsSpecular ? materialProperties.SpecularStrength : 0);
            material.SetColor(SpecularColor, materialProperties.SpecularColor);
            if (!string.IsNullOrEmpty(materialProperties.NormalMapPath))
            {
                material.EnableKeyword("_NORMALMAP");
                material.SetTexture(NormalMap, _textureManager.GetNormalMap(materialProperties.NormalMapPath));
            }

            if (!string.IsNullOrEmpty(materialProperties.MetallicMaskPath))
                material.SetTexture(MetallicMap, _textureManager.GetMetallicMap(materialProperties.MetallicMaskPath));
            if (materialProperties.EmissiveColor != Color.black ||
                !string.IsNullOrEmpty(materialProperties.GlowMapPath))
            {
                material.SetInt(EnableEmission, 1);
                material.SetColor(EmissionColor, materialProperties.EmissiveColor);
                if (!string.IsNullOrEmpty(materialProperties.GlowMapPath))
                    material.SetTexture(EmissionMap, _textureManager.GetGlowMap(materialProperties.GlowMapPath));
            }

            if (!string.IsNullOrEmpty(materialProperties.EnvironmentalMapPath))
            {
                material.SetTexture(Cube, _textureManager.GetEnvMap(materialProperties.EnvironmentalMapPath));
                material.SetFloat(CubeScale, materialProperties.EnvironmentalMapScale);
            }
            
            _materialCache.Add(materialProperties, material);

            return material;
        }

        /// <summary>
        /// WARNING: Call this ONLY when textures and materials are not needed anymore
        /// </summary>
        public void ClearCachedMaterialsAndTextures()
        {
            foreach (var material in _materialCache.Values)
            {
                Object.Destroy(material);
            }

            _materialCache.Clear();
            _textureManager.ClearCachedTextures();
        }
    }
}