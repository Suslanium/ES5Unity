using UnityEngine;
using UnityEngine.Rendering;

namespace Engine.Textures
{
    public struct MaterialProperties
    {
        public readonly bool IsSpecular;
        public readonly bool UseVertexColors;
        public readonly bool UseVertexAlpha;
        public readonly bool DoubleSided;
        public readonly float SpecularStrength;
        public readonly Vector2 UVOffset;
        public readonly Vector2 UVScale;
        public readonly float Glossiness;
        public readonly Color EmissiveColor;
        public readonly Color SpecularColor;
        public readonly float Alpha;
        public readonly string DiffuseMapPath;
        public readonly string NormalMapPath;
        public readonly string GlowMapPath;
        public readonly string MetallicMaskPath;
        public readonly string EnvironmentalMapPath;
        public readonly float EnvironmentalMapScale;
        public readonly AlphaInfo AlphaInfo;

        public MaterialProperties(bool isSpecular, bool useVertexColors, bool useVertexAlpha, bool doubleSided, float specularStrength, Vector2 uvOffset,
            Vector2 uvScale, float glossiness,
            Color emissiveColor, Color specularColor, float alpha, string diffuseMapPath, string normalMapPath,
            string glowMapPath, string metallicMaskPath, string environmentalMapPath, float environmentalMapScale,
            AlphaInfo alphaInfo)
        {
            IsSpecular = isSpecular;
            UseVertexColors = useVertexColors;
            UseVertexAlpha = useVertexAlpha;
            DoubleSided = doubleSided;
            SpecularStrength = specularStrength;
            UVOffset = uvOffset;
            UVScale = uvScale;
            Glossiness = glossiness;
            EmissiveColor = emissiveColor;
            SpecularColor = specularColor;
            Alpha = alpha;
            DiffuseMapPath = diffuseMapPath;
            NormalMapPath = normalMapPath;
            GlowMapPath = glowMapPath;
            MetallicMaskPath = metallicMaskPath;
            EnvironmentalMapPath = environmentalMapPath;
            EnvironmentalMapScale = environmentalMapScale;
            AlphaInfo = alphaInfo;
        }
    }

    public struct AlphaInfo
    {
        public readonly bool AlphaBlend;

        public readonly BlendMode SourceBlendMode;

        public readonly BlendMode DestinationBlendMode;

        public readonly bool AlphaTest;

        public readonly byte AlphaTestThreshold;

        public AlphaInfo(bool alphaBlend, BlendMode sourceBlendMode, BlendMode destinationBlendMode, bool alphaTest,
            byte alphaTestThreshold)
        {
            AlphaBlend = alphaBlend;
            SourceBlendMode = sourceBlendMode;
            DestinationBlendMode = destinationBlendMode;
            AlphaTest = alphaTest;
            AlphaTestThreshold = alphaTestThreshold;
        }
    }
}