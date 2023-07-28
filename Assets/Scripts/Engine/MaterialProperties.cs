using UnityEngine;

namespace Engine
{
    public struct MaterialProperties
    {
        public bool IsSpecular { get; private set; }
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
        public bool EnableReflections { get; private set; }

        public MaterialProperties(bool isSpecular, Vector2 uvOffset, Vector2 uvScale, float glossiness,
            Color emissiveColor, Color specularColor, float alpha, string diffuseMapPath, string normalMapPath,
            string glowMapPath, string metallicMaskPath, bool enableReflections)
        {
            IsSpecular = isSpecular;
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
            EnableReflections = enableReflections;
        }
    }
}