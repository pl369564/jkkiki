Shader "Hidden/LoadingWaveImage"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color("Color", Color) = (1,1,1,1)
        _DeColor("DeColor", Color) = (0,0,0,1)

        _StencilComp("stencil Comparison", Float) = 8
        _Stencil("stencil ID", Float) = 0
        _StencilOp("stencil Operation", Float) = 0
        _StencilWriteMask("stencil write Mask", Float) = 255
        _StencilReadMask("stencil Read Mask", Float) = 255
        _ColorMask("Color Mask", Float) = 15
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_Stencilop]
            ReadMask  [_StencilReadMask]
            WriteMask  [_StencilWriteMask]
        }
        ColorMask[_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            float4 _Color;
            float4 _DeColor;

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                // just invert the colors
                //col.rgb = 1 - col.rgb;
                float2 center = float2(0.5,0.5);
                float dis = distance(i.uv,center)*2;
                dis -= _Time.w;
                float angle = sin(dis*UNITY_PI);
                if(angle>0.95)
                col.rgb = _DeColor.rgb;
                else
                col.rgb = _Color.rgb;
                //col.rgb = _Color.rgb * sin(angle);
                return col;
            }
            ENDCG
        }
    }
}
