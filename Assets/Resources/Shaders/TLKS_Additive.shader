Shader "TLKS/Additive"
{
    Properties { _BaseMap("Texture",2D)="white"{} [HDR]_Tint("Tint",Color)=(1,1,1,1) }
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" }
        Blend SrcAlpha One
        ZWrite Off Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial) float4 _BaseMap_ST; half4 _Tint; CBUFFER_END
            struct Attr { float4 pos:POSITION; float2 uv:TEXCOORD0; float4 col:COLOR; };
            struct Vary { float4 posCS:SV_POSITION; float2 uv:TEXCOORD0; float4 col:COLOR; };
            Vary vert(Attr a){ Vary o; o.posCS=TransformObjectToHClip(a.pos.xyz); o.uv=TRANSFORM_TEX(a.uv,_BaseMap); o.col=a.col; return o; }
            half4 frag(Vary i):SV_Target { half4 t=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv); half4 c=t*_Tint*i.col; return half4(c.rgb, saturate(c.a)); }
            ENDHLSL
        }
    }
}
