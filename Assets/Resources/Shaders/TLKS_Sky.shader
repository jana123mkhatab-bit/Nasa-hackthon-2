Shader "TLKS/Sky"
{
    Properties { _BaseMap("Sky",2D)="black"{} _Exposure("Exposure",Float)=1.2 }
    SubShader
    {
        Tags { "RenderType"="Background" "RenderPipeline"="UniversalPipeline" "Queue"="Background" }
        ZWrite Off
        Cull Off
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial) half _Exposure; CBUFFER_END
            struct Attr { float4 pos:POSITION; float2 uv:TEXCOORD0; };
            struct Vary { float4 posCS:SV_POSITION; float2 uv:TEXCOORD0; };
            Vary vert(Attr a){ Vary o; o.posCS=TransformObjectToHClip(a.pos.xyz); o.uv=a.uv; return o; }
            half4 frag(Vary i):SV_Target { half3 c=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb; return half4(pow(c,1.2)*_Exposure*1.6,1); }
            ENDHLSL
        }
    }
}
