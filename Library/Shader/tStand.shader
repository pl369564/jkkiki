Shader "Custom/tStand"
{
    Properties
    {

    }
        SubShader
    {
        Tags 
        { 
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
        }
        LOD 100
        //ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                //float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                //float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 _MainTex_ST;

            UNITY_INSTANCING_BUFFER_START(Props)
                // put more per-instance properties here
                UNITY_DEFINE_INSTANCED_PROP(float, _testRed)
                #define propTestRed Props

                UNITY_DEFINE_INSTANCED_PROP(float, _testGreen)
                #define propTestGreen Props

                UNITY_DEFINE_INSTANCED_PROP(float, _testBlue)
                #define propTestBlue Props

                UNITY_DEFINE_INSTANCED_PROP(float, _testAlpha)
                #define propTestAlpha Props

            UNITY_INSTANCING_BUFFER_END(Props)

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                fixed4 color;
                color.r = UNITY_ACCESS_INSTANCED_PROP(propTestRed, _testRed);
                color.g = UNITY_ACCESS_INSTANCED_PROP(propTestGreen, _testGreen);
                color.b = UNITY_ACCESS_INSTANCED_PROP(propTestBlue, _testBlue);
                color.a = UNITY_ACCESS_INSTANCED_PROP(propTestAlpha, _testAlpha);

                return color;
            }
            ENDCG
        }
    }
        FallBack "Diffuse"
}
