// DeadManZone Phase-0 fullscreen interior-ink (URP Full Screen Pass Renderer Feature).
// Roberts-cross edge detect on scene depth + normals -> composites dark ink at part/geometry boundaries.
Shader "DMZ/InteriorInkFullscreen"
{
    Properties
    {
        _InkColor ("Ink Color", Color) = (0.02,0.02,0.03,1)
        _Thickness ("Thickness (px)", Range(0.5,4)) = 1.2
        _DepthScale ("Depth Sensitivity", Range(0,60)) = 10
        _DepthBias ("Depth Bias", Range(0,3)) = 0.5
        _NormalScale ("Normal Sensitivity", Range(0,8)) = 2.2
        _InkStrength ("Ink Strength", Range(0,1)) = 0.9
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off
        Pass
        {
            Name "InteriorInk"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            float4 _InkColor; float _Thickness; float _DepthScale; float _DepthBias; float _NormalScale; float _InkStrength;

            float LinDepth(float2 uv){ return LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams); }

            half4 Frag(Varyings IN):SV_Target
            {
                float2 uv = IN.texcoord;
                half3 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb;

                float2 texel = (1.0 / _ScreenParams.xy) * _Thickness;
                float2 a = uv + float2(-texel.x,-texel.y);
                float2 b = uv + float2( texel.x, texel.y);
                float2 c = uv + float2( texel.x,-texel.y);
                float2 d = uv + float2(-texel.x, texel.y);

                float dc = LinDepth(uv);
                float dEdge = (abs(LinDepth(a)-LinDepth(b)) + abs(LinDepth(c)-LinDepth(d))) / max(dc,0.01);
                dEdge = saturate((dEdge - _DepthBias*0.01) * _DepthScale);

                float3 na=SampleSceneNormals(a), nb=SampleSceneNormals(b), nc=SampleSceneNormals(c), nd=SampleSceneNormals(d);
                float nEdge = saturate((distance(na,nb) + distance(nc,nd)) * _NormalScale);

                float edge = saturate(max(dEdge, nEdge)) * _InkStrength;
                col = lerp(col, _InkColor.rgb, edge);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
