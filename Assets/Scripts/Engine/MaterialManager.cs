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
            
            _textureManager.PreloadDiffuseMap(materialProperties.DiffuseMapPath);
            if (!string.IsNullOrEmpty(materialProperties.NormalMapPath)) _textureManager.PreloadNormalMap(materialProperties.NormalMapPath);
            if (!string.IsNullOrEmpty(materialProperties.MetallicMaskPath)) _textureManager.PreloadMetallicMap(materialProperties.NormalMapPath, materialProperties.MetallicMaskPath);
            if (!string.IsNullOrEmpty(materialProperties.GlowMapPath)) _textureManager.PreloadGlowMap(materialProperties.GlowMapPath);
            
            var material = new Material(SpecularShader);
            
            //Initialize emission if needed
            if (materialProperties.EmissiveColor != Color.black || materialProperties.GlowMapPath != "")
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor(EmissionColor, materialProperties.EmissiveColor);
                if (!string.IsNullOrEmpty(materialProperties.GlowMapPath))
                {
                    material.SetTexture(EmissionMap, _textureManager.GetGlowMap(materialProperties.GlowMapPath));
                }
            }

            //Set material alpha
            material.SetColor(AlphaColor, new Color(1, 1, 1, materialProperties.Alpha));
            
            //Disable reflections if needed
            if (!materialProperties.EnableReflections)
            {
                material.EnableKeyword("_GLOSSYREFLECTIONS_OFF");
                material.SetFloat(GlossyReflections, 0f);
            }

            //Set base texture
            material.SetTexture(MainTex, _textureManager.GetDiffuseMap(materialProperties.DiffuseMapPath));
            
            //Set normal and (maybe) specular map
            if (!string.IsNullOrEmpty(materialProperties.NormalMapPath))
            {
                var normalMap = _textureManager.GetNormalMapAndExtractSpecular(materialProperties.NormalMapPath, materialProperties.MetallicMaskPath);
                material.EnableKeyword("_NORMALMAP");
                material.SetTexture(BumpMap, normalMap);
                //If material is specular - extract grayscale specular map cached from normal map alpha channel and apply tint to it
                if (materialProperties.IsSpecular)
                {
                    var specularMap = _textureManager.GetTintedSpecularMap(materialProperties.SpecularColor,
                        materialProperties.NormalMapPath);
                    material.EnableKeyword("_SPECGLOSSMAP");
                    material.SetTexture(SpecGlossMap, specularMap);
                    material.SetFloat(GlossMapScale, materialProperties.Glossiness);
                }
                else
                {
                    //material.SetColor(SpecColor, materialProperties.SpecularColor);
                    material.SetFloat(Glossiness, materialProperties.Glossiness);
                }
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