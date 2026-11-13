Shader "TLKS/Atmos"
{
    Properties { [HDR]_Color("Color",Color)=(0.3,0.6,1,1) _Power("Power",Float)=3 _Gain("Gain",Float)=1 _SunLit("Sun Lit Mask",Float)=1 }
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+1" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Back
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial) half4 _Color; half _Power; half _Gain; half _SunLit; CBUFFER_END
            struct Attr { float4 pos:POSITION; float3 nrm:NORMAL; };
            struct Vary { float4 posCS:SV_POSITION; float3 nrm:TEXCOORD0; float3 posWS:TEXCOORD1; };
            Vary vert(Attr a){ Vary o; VertexPositionInputs p=GetVertexPositionInputs(a.pos.xyz); o.posCS=p.positionCS; o.posWS=p.positionWS; o.nrm=TransformObjectToWorldNormal(a.nrm); return o; }
            half4 frag(Vary i):SV_Target
            {
                float3 N=normalize(i.nrm); float3 V=normalize(GetWorldSpaceViewDir(i.posWS));
                half f=pow(1-saturate(dot(N,V)),_Power);
                Light l=GetMainLight(); half ndl=dot(N,l.direction);
                half m=lerp(1,saturate(ndl*1.6+0.35),_SunLit);
                return half4(_Color.rgb, saturate(f*m*_Gain));
            }
            ENDHLSL
        }
    }
}
