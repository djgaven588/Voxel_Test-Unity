Shader "Custom/ChunkShader"
{
    Properties
    {
        _TextureArray ("Texture", 2DArray) = "" {}
    }
    SubShader
    {
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma require 2darray

            #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight

            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"
            #include "AutoLight.cginc"

            struct appdata
            {
                uint vertPos : POSITION;
                uint uvAndNormal : TEXCOORD0;
                uint textureIndex : COLOR;
            };

            struct v2f
            {
                float3 uv : TEXCOORD0;
                float4 pos : SV_POSITION;
                SHADOW_COORDS(1)
                fixed4 diff : COLOR0;
                fixed3 ambient : COLOR1;
            };

            UNITY_DECLARE_TEX2DARRAY(_TextureArray);
            float4 _TextureArray_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(float3(float(v.vertPos & 0x3FFu) / 31.0, float(v.vertPos >> 10u & 0x3FFu) / 31.0, float(v.vertPos >> 20u & 0x3FFu) / 31.0));
                o.uv = float3(float(v.uvAndNormal & 0x1F) / 31.0, float(v.uvAndNormal >> 5 & 0x1F) / 31.0, v.textureIndex);
                
                half3 worldNormal = UnityObjectToWorldNormal(float3((float(v.uvAndNormal >> 10 & 0x1F) - 15) / 15.0, (float(v.uvAndNormal >> 15 & 0x1F) - 15) / 15.0, (float(v.uvAndNormal >> 20 & 0x1F) - 15) / 15.0));
                half nl = max(0, dot(worldNormal, _WorldSpaceLightPos0.xyz));
                o.diff = nl * _LightColor0;
                o.diff.rgb += ShadeSH9(half4(worldNormal,1));
                o.ambient = ShadeSH9(half4(worldNormal,1));
                TRANSFER_SHADOW(o);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = UNITY_SAMPLE_TEX2DARRAY(_TextureArray, i.uv);
                if (col.a < 0.5)
                    discard;
                col.a = 1;

                fixed shadow = SHADOW_ATTENUATION(i);
                fixed3 lighting = i.diff * shadow + i.ambient;
                col.rgb *= lighting;
                return col;
            }
            ENDCG
        }
        /*
        Pass
        {
            Tags { "Queue"="Transparent" "LightMode"="ShadowCaster" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            #include "UnityCG.cginc"

            struct v2f { 
                V2F_SHADOW_CASTER;
            };

            struct appdata
            {
                uint vertPos : POSITION;
                uint uvAndNormal : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;

                // Does nothing: o.pos = UnityObjectToClipPos(float3(float(v.vertPos & 0x3FFu) / 31.0, float(v.vertPos >> 10u & 0x3FFu) / 31.0, float(v.vertPos >> 20u & 0x3FFu) / 31.0));
                // Can't do this: o.normal = float3((float(v.uvAndNormal >> 10 & 0x1F) - 15) / 15.0, (float(v.uvAndNormal >> 15 & 0x1F) - 15) / 15.0, (float(v.uvAndNormal >> 20 & 0x1F) - 15) / 15.0);
                
                // TRANSFER_SHADOW_CASTER_NORMALOFFSET wants a "vertex" and "normal" property as part of "appdata" which kills everything
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }*/
    }
}
