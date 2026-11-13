Shader "TLKS/SkyGrad"
{
    Properties
    {
        _Top("Top", Color) = (0.25,0.45,0.75,1)
        _Horizon("Horizon", Color) = (0.8,0.8,0.75,1)
        _Sun("Sun Direction", Vector) = (0,0.5,0.8,0)
        [HDR]_SunCol("Sun Color", Color) = (3,2,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Background" "RenderPipeline"="UniversalPipeline" "Queue"="Background" }
        ZWrite Off
        Cull Front
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial) half4 _Top; half4 _Horizon; float4 _Sun; half4 _SunCol; CBUFFER_END
            struct Attr { float4 pos:POSITION; };
            struct Vary { float4 posCS:SV_POSITION; float3 posWS:TEXCOORD0; };
            Vary vert(Attr a){ Vary o; VertexPositionInputs p=GetVertexPositionInputs(a.pos.xyz); o.posCS=p.positionCS; o.posWS=p.positionWS; return o; }
            half4 frag(Vary i):SV_Target
            {
                float3 d = normalize(i.posWS - _WorldSpaceCameraPos);
                float t = saturate(d.y);
                half3 col = lerp(_Horizon.rgb, _Top.rgb, pow(t, 0.5));
                col = lerp(col, _Horizon.rgb * 0.7, saturate(-d.y * 5));
                float s = saturate(dot(d, normalize(_Sun.xyz)));
                col += _SunCol.rgb * (pow(s, 900) * 12 + pow(s, 14) * 0.4 + pow(s, 3) * 0.06);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
