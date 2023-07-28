using System.Collections.Generic;
using UnityEngine;

namespace Engine
{
    public class MaterialManager
    {
        private static readonly Shader SpecularShader = Shader.Find("Standard (Specular setup)");
        private readonly TextureManager _textureManager;
        private readonly Dictionary<MaterialProperties, Material> _materialCache = new();
        private static readonly int GlossMapScale = Shader.PropertyToID("_GlossMapScale");
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        private static readonly int EmissionMap = Shader.PropertyToID("_EmissionMap");
        private static readonly int AlphaColor = Shader.PropertyToID("_Color");
        private static readonly int SpecColor = Shader.PropertyToID("_SpecColor");
        private static readonly int GlossyReflections = Shader.PropertyToID("_GlossyReflections");
        private static readonly int MainTex = Shader.PropertyToID("_MainTex");
        private static readonly int SpecGlossMap = Shader.PropertyToID("_SpecGlossMap");
        private static readonly int BumpMap = Shader.PropertyToID("_BumpMap");
        private static readonly int Glossiness = Shader.PropertyToID("_Glossiness");

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

            var material = new Material(SpecularShader);
            if (materialProperties.EmissiveColor != Color.black || materialProperties.GlowMapPath != "")
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor(EmissionColor, materialProperties.EmissiveColor);
                if (materialProperties.GlowMapPath != "")
                {
                    material.SetTexture(EmissionMap, _textureManager.GetGlowMap(materialProperties.GlowMapPath));
                }
            }

            material.SetColor(AlphaColor, new Color(1, 1, 1, materialProperties.Alpha));
            material.SetColor(SpecColor, materialProperties.SpecularColor);
            if (!materialProperties.EnableReflections)
            {
                material.EnableKeyword("_GLOSSYREFLECTIONS_OFF");
                material.SetFloat(GlossyReflections, 0f);
            }
            material.SetTexture(MainTex, _textureManager.GetDiffuseMap(materialProperties.DiffuseMapPath));
            if (materialProperties.NormalMapPath != "")
            {
                var textures = _textureManager.GetNormalAndSpecularMap(materialProperties.NormalMapPath);
                if (materialProperties.IsSpecular)
                {
                    material.EnableKeyword("_SPECGLOSSMAP");
                    material.SetTexture(SpecGlossMap, textures.Item2);
                    material.SetFloat(GlossMapScale, materialProperties.Glossiness);
                }
                else
                {
                    material.SetFloat(Glossiness, materialProperties.Glossiness);
                }
                material.EnableKeyword ("_NORMALMAP");
                material.SetTexture(BumpMap, textures.Item1);
            }
            _materialCache.Add(materialProperties, material);
            return material;
        }

        public void ClearCachedMaterialsAndTextures()
        {
            _materialCache.Clear();
            _textureManager.ClearCachedTextures();
        }
    }
}