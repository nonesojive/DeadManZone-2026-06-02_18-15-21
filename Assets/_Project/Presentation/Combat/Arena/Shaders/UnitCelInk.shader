// DMZ/UnitCelInk — spike v0 (Phase 0 gate, specs 2026-07-14).
// Pass 1: inverted-hull ink outline; _OutlineColor lerps toward _SideColor by _SideTint,
//         implementing the side channel on the outline (arena spec section 3).
// Pass 2: cel-banded forward lit — hard 3-band ramp on the main light, texture albedo.
// Interior ink in v0 comes from the albedo texture (inked refs bake it); no edge-detect term.
Shader "DMZ/UnitCelInk"
{
    Properties
    {
        [MainTexture] _BaseMap ("Albedo", 2D) = "white" {}
        [MainColor] _BaseColor ("Tint", Color) = (1,1,1,1)
        _SideColor ("Side Color", Color) = (0.25, 0.45, 0.9, 1)
        _SideTint ("Outline Side Tint", Range(0,1)) = 0.65
        _OutlineColor ("Ink Color", Color) = (0.04, 0.03, 0.03, 1)
        _OutlineWidth ("Outline Width (m)", Range(0, 0.05)) = 0.012
        _Bands ("Cel Bands", Range(2, 4)) = 3
        _ShadowFloor ("Darkest Band Level", Range(0, 1)) = 0.35
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        Pass // ---- ink outline: inverted hull ----
        {
            Name "InkOutline"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Front
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST; float4 _BaseColor;
                float4 _SideColor; float _SideTint;
                float4 _OutlineColor; float _OutlineWidth;
                float _Bands; float _ShadowFloor;
            CBUFFER_END
            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct V { float4 positionCS : SV_POSITION; };
            V vert(A IN)
            {
                V OUT;
                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 nWS = TransformObjectToWorldNormal(IN.normalOS);
                // Constant-ish screen thickness: scale extrusion by view depth.
                float depth = length(GetCameraPositionWS() - posWS);
                posWS += normalize(nWS) * _OutlineWidth * max(depth * 0.12, 1.0);
                OUT.positionCS = TransformWorldToHClip(posWS);
                return OUT;
            }
            half4 frag(V IN) : SV_Target
            {
                float3 ink = lerp(_OutlineColor.rgb, _SideColor.rgb, _SideTint);
                return half4(ink, 1);
            }
            ENDHLSL
        }

        Pass // ---- cel-banded lit ----
        {
            Name "CelLit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST; float4 _BaseColor;
                float4 _SideColor; float _SideTint;
                float4 _OutlineColor; float _OutlineWidth;
                float _Bands; float _ShadowFloor;
            CBUFFER_END
            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct V
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
            };
            V vert(A IN)
            {
                V OUT;
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionCS = TransformWorldToHClip(OUT.positionWS);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }
            half4 frag(V IN) : SV_Target
            {
                float4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(IN.positionWS));
                float ndl = saturate(dot(normalize(IN.normalWS), mainLight.direction));
                ndl *= mainLight.shadowAttenuation;
                // Hard bands: quantize NdotL into _Bands steps, floor at _ShadowFloor.
                float band = floor(ndl * _Bands) / max(_Bands - 1.0, 1.0);
                float lightLevel = lerp(_ShadowFloor, 1.0, saturate(band));
                float3 lit = albedo.rgb * mainLight.color * lightLevel
                           + albedo.rgb * SampleSH(IN.normalWS) * 0.35;
                return half4(lit, 1);
            }
            ENDHLSL
        }
    }
    Fallback "Universal Render Pipeline/Lit"
}
