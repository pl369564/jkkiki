Shader "Custom/axis"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _x("x",Range(0.1,1)) = 1
        _y("y",Range(0.1,1)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Lighting Off
        Cull Front
        //ZWrite Off
        LOD 200

        CGPROGRAM

        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Unlit noambient
        //#pragma surface surf Standard

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        half4 LightingUnlit(SurfaceOutput s,half3 lightDir,half atten) {
            return fixed4(s.Albedo,s.Alpha);
        }

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
        };

        fixed4 _Color;
        float _x;
        float _y;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutput o)
        {
            float2 uv_XY = IN.uv_MainTex;
            float2 uv_MainTex2 = float2(uv_XY.x*_x, uv_XY.y*_y);
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, uv_MainTex2) * _Color;
            o.Albedo = c.rgb;
            // Metallic and smoothness come from slider variables
            //o.Metallic = _Metallic;
            //o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
