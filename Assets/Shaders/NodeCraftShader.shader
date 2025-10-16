Shader "Unlit/NodeCraft"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilComp ("Stencil Comparison", Float) = 8
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }
    SubShader
    {
		Blend Off
		ZWrite Off
        Cull Off
		Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100

        Pass
        {
            Stencil
            {
                Ref 1
                Comp Equal
                Pass Replace
            }

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
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };
			
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 datas[128];

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 d = (sin(i.uv*50)+1)/2;
                fixed4 result = fixed4(d.x,d.y,0,(d.x+d.y)/2);



                return result;
            }
            ENDCG
        }
    }
}
