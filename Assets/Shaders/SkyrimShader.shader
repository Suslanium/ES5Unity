Shader "SkyrimDefaultShader"
{
    Properties
    {
        _MainTex ("Diffuse map(RGB)", 2D) = "white" {}
        _Alpha ("Alpha", Range(0,1)) = 1
        _UsesVertexColors ("Uses vertex colors", Int) = 0
        _Glossiness ("Glossiness", Range(0,1000)) = 500
        _SpecularStrength ("Specular strength", Range(0,1000)) = 0.5
        _NormalMap ("Normal map (RGB) Specular map (A)", 2D) = "bump" {}
        _SpecColor ("Specular color", Color) = (1,1,1,1)
        _MetallicMap ("Metallic map", 2D) = "black" {}
        _EnableEmission ("Enable emission", Int) = 0
        _EmissionColor ("Emission color", Color) = (0,0,0,1)
        _EmissionMap ("Emission map", 2D) = "white" {}
        _Cube ("Environmental map", Cube) = "" {}
        _CubeScale ("Environmental map scale", Float) = 0
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf BlinnPhong fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _NormalMap;
        sampler2D _MetallicMap;
        sampler2D _EmissionMap;
        samplerCUBE _Cube;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_NormalMap;
            float4 color: COLOR;
            float3 worldRefl;
            INTERNAL_DATA
        };

        half _Glossiness;
        half _Alpha;
        half _SpecularStrength;
        fixed4 _SpecularColor;
        fixed4 _EmissionColor;
        int _EnableEmission;
        int _UsesVertexColors;
        float _CubeScale;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
        // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf(Input IN, inout SurfaceOutput o)
        {
            // Calculate albedo using diffuse map and vertex color as a tint(if needed)
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
            if (_UsesVertexColors > 0) c *= IN.color;
            o.Albedo = c.rgb;
            //Calculate glossiness and specularity using specular map (normal map alpha)
            o.Gloss = tex2D(_NormalMap, IN.uv_NormalMap).a;
            if (_SpecularStrength != 0 && _Glossiness != 0)
            {
                //I got this formula by tinkering with NifSkope BSLightingShaderProperty values and Unity material editor
                //It probably can be improved, but it works for now
                o.Specular = (_Glossiness * (10 - _SpecularStrength)) / 1000;
            }
            else
            {
                //Disable specular highlights
                o.Specular = 65504;
            }
            //Get normal and invert green channel (because Skyrim uses negative-Y normal maps)
            float4 packedNormal = tex2D(_NormalMap, IN.uv_NormalMap);
            packedNormal.g = 1-packedNormal.g;
            //Use this instead of UnpackNormal(packedNormal), because UnpackNormal gives really weird results
            half3 normal = packedNormal * 2 - 1;
            o.Normal = normal;
            //Apply "reflections" using emission
            float3 worldRefl = WorldReflectionVector(IN, o.Normal);
            fixed4 reflcol = texCUBE(_Cube, worldRefl);
            reflcol *= _CubeScale * 0.1;
            half3 emissive = reflcol;
            if (_EnableEmission > 0)
            {
                emissive += tex2D(_EmissionMap, IN.uv_MainTex) * _EmissionColor;
            }
            o.Emission = emissive;
            o.Alpha = _Alpha * c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}