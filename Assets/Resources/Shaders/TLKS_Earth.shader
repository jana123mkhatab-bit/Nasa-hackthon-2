Shader "TLKS/Earth"
{
    Properties { _DayMap("Day",2D)="white"{} _NightMap("Night",2D)="black"{} _NightBoost("Night Boost",Float)=2.2 }
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_DayMap); SAMPLER(sampler_DayMap); TEXTURE2D(_NightMap); SAMPLER(sampler_NightMap);
            CBUFFER_START(UnityPerMaterial) half _NightBoost; CBUFFER_END
            struct Attr { float4 pos:POSITION; float3 nrm:NORMAL; float2 uv:TEXCOORD0; };
            struct Vary { float4 posCS:SV_POSITION; float3 nrm:TEXCOORD0; float2 uv:TEXCOORD1; float3 posWS:TEXCOORD2; };
            Vary vert(Attr a){ Vary o; VertexPositionInputs p=GetVertexPositionInputs(a.pos.xyz); o.posCS=p.positionCS; o.posWS=p.positionWS; o.nrm=TransformObjectToWorldNormal(a.nrm); o.uv=a.uv; return o; }
            half4 frag(Vary i):SV_Target
            {
                float3 N=normalize(i.nrm); float3 V=normalize(GetWorldSpaceViewDir(i.posWS));
                Light l=GetMainLight(); float ndl=dot(N,l.direction);
                half3 day=SAMPLE_TEXTURE2D(_DayMap,sampler_DayMap,i.uv).rgb;
                half3 night=SAMPLE_TEXTURE2D(_NightMap,sampler_NightMap,i.uv).rgb;
                half lit=smoothstep(-0.12,0.25,ndl);
                half3 dcol=day*saturate(ndl*1.1+0.02)*l.color*1.35;
                half ocean=saturate((day.b-max(day.r,day.g)*1.15)*6);
                float3 H=normalize(l.direction+V);
                dcol+=ocean*pow(saturate(dot(N,H)),260)*0.45*l.color*saturate(ndl*3);
                half twi=exp(-pow(ndl*6,2))*0.35; dcol+=half3(1.0,0.45,0.2)*twi*day*0.8;
                half3 ncol=night*_NightBoost*half3(1,0.85,0.6)+day*0.015;
                half3 col=lerp(ncol,dcol,lit);
                half fres=pow(1-saturate(dot(N,V)),3);
                col+=half3(0.25,0.5,1)*fres*0.35*saturate(ndl+0.35);
                return half4(col,1);
            }
            ENDHLSL
        }
    }
}
