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
        _SpecularColor ("Specular color", Color) = (1,1,1,1)
        _MetallicMap ("Metallic map", 2D) = "black" {}
        _EnableEmission ("Enable emission", Int) = 0
        _EmissionColor ("Emission color", Color) = (0,0,0,1)
        _EmissionMap ("Emission map", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf StandardSpecular fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _NormalMap;
        sampler2D _MetallicMap;
        sampler2D _EmissionMap;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_NormalMap;
            float2 uv_MetallicMap;
            float2 uv_EmissionMap;
            float4 color: COLOR;
        };

        half _Glossiness;
        half _Alpha;
        half _SpecularStrength;
        fixed4 _SpecularColor;
        fixed4 _EmissionColor;
        int _EnableEmission;
        int _UsesVertexColors;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandardSpecular o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
            if (_UsesVertexColors > 0) c *= IN.color;
            o.Albedo = c.rgb;
            o.Normal = UnpackNormal(tex2D(_NormalMap, IN.uv_NormalMap));
            // Metallic and smoothness come from slider variables
            //o.Smoothness = tex2D(_NormalMap, IN.uv_NormalMap).a;
            o.Specular = tex2D(_NormalMap, IN.uv_NormalMap).a * _SpecularStrength * _SpecularColor * ((1-_Glossiness)/1000);
            if (_EnableEmission > 0)
            {
                o.Emission = tex2D(_EmissionMap, IN.uv_EmissionMap) * _EmissionColor;
            }
            o.Alpha = _Alpha * c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
