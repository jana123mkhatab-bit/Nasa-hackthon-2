Shader "TLKS/Clouds"
{
    Properties { _BaseMap("Clouds",2D)="white"{} _Density("Density",Float)=1.0 }
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial) half _Density; CBUFFER_END
            struct Attr { float4 pos:POSITION; float3 nrm:NORMAL; float2 uv:TEXCOORD0; };
            struct Vary { float4 posCS:SV_POSITION; float3 nrm:TEXCOORD0; float2 uv:TEXCOORD1; };
            Vary vert(Attr a){ Vary o; o.posCS=TransformObjectToHClip(a.pos.xyz); o.nrm=TransformObjectToWorldNormal(a.nrm); o.uv=a.uv; return o; }
            half4 frag(Vary i):SV_Target
            {
                half3 t=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb;
                half a=saturate(dot(t,half3(0.33,0.34,0.33))*_Density);
                Light l=GetMainLight(); float ndl=dot(normalize(i.nrm),l.direction);
                half sh=saturate(ndl*1.2+0.04);
                half3 c=half3(1,1,1)*sh*l.color*1.3+half3(1,0.5,0.25)*exp(-pow(ndl*6,2))*0.3;
                return half4(c, a*0.9*saturate(ndl*4+0.25));
            }
            ENDHLSL
        }
    }
}
