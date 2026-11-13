Shader "TLKS/Sun"
{
    Properties { _BaseMap("Sun",2D)="white"{} [HDR]_Tint("Tint",Color)=(3,2,0.8,1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial) half4 _Tint; CBUFFER_END
            struct Attr { float4 pos:POSITION; float2 uv:TEXCOORD0; float3 nrm:NORMAL; };
            struct Vary { float4 posCS:SV_POSITION; float2 uv:TEXCOORD0; float3 nrm:TEXCOORD1; float3 posWS:TEXCOORD2; };
            Vary vert(Attr a){ Vary o; VertexPositionInputs p=GetVertexPositionInputs(a.pos.xyz); o.posCS=p.positionCS; o.posWS=p.positionWS; o.nrm=TransformObjectToWorldNormal(a.nrm); o.uv=a.uv; return o; }
            half4 frag(Vary i):SV_Target
            {
                float t=_Time.y*0.01;
                float2 uv=i.uv+float2(t, sin(i.uv.y*20+_Time.y*0.3)*0.002);
                half3 c=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uv).rgb;
                float3 V=normalize(GetWorldSpaceViewDir(i.posWS));
                half limb=pow(saturate(dot(normalize(i.nrm),V)),0.6);
                return half4(c*_Tint.rgb*(0.45+0.8*limb),1);
            }
            ENDHLSL
        }
    }
}
