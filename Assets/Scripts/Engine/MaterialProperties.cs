using UnityEngine;

namespace Engine
{
    public struct MaterialProperties
    {
        public bool IsSpecular { get; private set; }
        public bool UseVertexColors { get; private set; }
        public float SpecularStrength { get; private set; }
        public Vector2 UVOffset { get; private set; }
        public Vector2 UVScale { get; private set; }
        public float Glossiness { get; private set; }
        public Color EmissiveColor { get; private set; }
        public Color SpecularColor { get; private set; }
        public float Alpha { get; private set; }
        public string DiffuseMapPath { get; private set; }
        public string NormalMapPath { get; private set; }
        public string GlowMapPath { get; private set; }
        public string MetallicMaskPath { get; private set; }
        public string EnvironmentalMapPath { get; private set; }
        public float EnvironmentalMapScale { get; private set; }

        public MaterialProperties(bool isSpecular, bool useVertexColors, float specularStrength, Vector2 uvOffset,
            Vector2 uvScale, float glossiness,
            Color emissiveColor, Color specularColor, float alpha, string diffuseMapPath, string normalMapPath,
            string glowMapPath, string metallicMaskPath, string environmentalMapPath, float environmentalMapScale)
        {
            IsSpecular = isSpecular;
            UseVertexColors = useVertexColors;
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
        }
    }
}