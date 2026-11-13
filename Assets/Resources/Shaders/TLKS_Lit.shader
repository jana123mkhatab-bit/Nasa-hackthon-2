Shader "TLKS/Lit"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _BaseColor ("Color", Color) = (1,1,1,1)
        [HDR] _EmissionColor ("Emission", Color) = (0,0,0,1)
        _Smoothness ("Smoothness", Range(0,1)) = 0.4
        _VC ("Use Vertex Color", Float) = 0
        _Ambient ("Ambient Boost", Range(0,2)) = 1
        _Cutoff ("Cutoff", Range(0,1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST; half4 _BaseColor; half4 _EmissionColor; half _Smoothness; half _VC; half _Ambient; half _Cutoff;
            CBUFFER_END

            struct Attr { float4 pos:POSITION; float3 nrm:NORMAL; float2 uv:TEXCOORD0; float4 col:COLOR; };
            struct Vary { float4 posCS:SV_POSITION; float3 nrmWS:TEXCOORD0; float2 uv:TEXCOORD1; float3 posWS:TEXCOORD2; float4 col:COLOR; half fog:TEXCOORD3; };

            Vary vert(Attr a)
            {
                Vary o;
                VertexPositionInputs p = GetVertexPositionInputs(a.pos.xyz);
                o.posCS = p.positionCS; o.posWS = p.positionWS;
                o.nrmWS = TransformObjectToWorldNormal(a.nrm);
                o.uv = TRANSFORM_TEX(a.uv, _BaseMap);
                o.col = a.col;
                o.fog = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            half4 frag(Vary i) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
                half3 albedo = tex.rgb * _BaseColor.rgb;
                albedo *= lerp(half3(1,1,1), i.col.rgb, _VC);
                float3 N = normalize(i.nrmWS);
                float3 V = normalize(GetWorldSpaceViewDir(i.posWS));
                float4 sc = TransformWorldToShadowCoord(i.posWS);
                Light l = GetMainLight(sc);
                half ndl = saturate(dot(N, l.direction));
                half3 col = albedo * l.color * ndl * l.shadowAttenuation;
                float3 H = normalize(l.direction + V);
                half spec = pow(saturate(dot(N,H)), exp2(1 + _Smoothness*9)) * _Smoothness * ndl * l.shadowAttenuation;
                col += l.color * spec;
                #ifdef _ADDITIONAL_LIGHTS
                uint cnt = GetAdditionalLightsCount();
                for (uint k = 0; k < cnt; k++) {
                    Light al = GetAdditionalLight(k, i.posWS);
                    col += albedo * al.color * saturate(dot(N, al.direction)) * al.distanceAttenuation;
                }
                #endif
                half3 amb = SampleSH(N) * _Ambient;
                col += albedo * amb;
                half rim = pow(1 - saturate(dot(N,V)), 3) * 0.15;
                col += rim * (0.3 + 0.7*amb);
                col += _EmissionColor.rgb;
                col = MixFog(col, i.fog);
                return half4(col, 1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/SHADOWCASTER"
    }
}
