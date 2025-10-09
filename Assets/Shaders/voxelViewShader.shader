Shader "Unlit/VoxelView"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _VoxelWidth  ("Voxel Width",  Float) = 128
        _VoxelHeight ("Voxel Height", Float) = 72
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Pass
        {
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ UNITY_SINGLE_PASS_STEREO
            #include "UnityCG.cginc"


            sampler2D _MainTex;  
            float _VoxelWidth;
            float _VoxelHeight;

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            float intBound(float s, float ds)
            {
                if (ds == 0) 
                    return 999999999;
                else
                {
                    float sOffset = ds > 0 ? ceil(s) - s : s - floor(s);
                    return sOffset / abs(ds);
                }
            }

            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float3 worldToCam = _WorldSpaceCameraPos - i.worldPos;
                float3 localToCam = mul(unity_WorldToObject, float4(worldToCam, 0.0)).xyz;
                float3 pos = mul(unity_WorldToObject, float4(i.worldPos, 1.0)).xyz;

                float2 vox = float2(_VoxelWidth, _VoxelHeight);

                float3 orig = pos;
                orig.z *= 0.99;
                orig.z += 0.5;
                orig.xy *= vox-0.01;
                orig.xy += vox/2;
                

                int2 current = int2(orig.xy);
                
                float3 dir = -localToCam;

                float dirX = dir.z/dir.x*(vox.y/vox.x);
                float dirY = dir.z/dir.y;
                dir.xy *= vox;
                int2 step = int2(dir.x>0?1:-1, dir.y>0?1:-1);

                float2 tMax = float2(
                    intBound(orig.x, dir.x),
                    intBound(orig.y, dir.y)
                );

                float2 tDelta = float2(
                    abs(1 / dir.x),
                    abs(1 / dir.y)
                );

                float currentZ = 0;

                for (int k = 0; k < 128; k++)
                {
                    if (current.x < 0 || current.x > vox.x-1 ||
                         current.y < 0 || current.y > vox.y-1)
                        break;

                    fixed4 col = tex2D(_MainTex, (current.xy+0.5) / vox);
                    col.a = (col.r + col.g + col.b) / 3;

                    if(col.a > 0.99-currentZ)
                    {
                        col.rgb *= 1-currentZ;
                        col.a = 1;

                        return col;
                    }
                    float delta = 0;
                    if (tMax.x < tMax.y)
                    {
                        tMax.x += tDelta.x;
                        current.x += step.x;
                        float delta = current.x - orig.x + (step.x<0?1:0);
                        currentZ = dirX * delta;
                    }
                    else
                    {
                        tMax.y += tDelta.y;
                        current.y += step.y;
                        float delta = current.y - orig.y + (step.y<0?1:0);
                        currentZ = dirY * delta;
                    }


                    if(col.a > 0.99- currentZ)
                    {
                        col.a = 1;
                        return col;
                    }
                }

                return fixed4(0,0,0,0);
            }
            ENDCG
        }
    }
}
