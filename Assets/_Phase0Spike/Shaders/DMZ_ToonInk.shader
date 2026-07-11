// DeadManZone Phase-0 toon-ink uber-shader (URP). Throwaway spike per docs/art/style-bible.
// Passes: ToonForward (2-band cel + ink-family shadow + fresnel rim ink + normal-derivative crease ink
//         + accent + status), Outline (inverted hull, screen-constant width, side-tint), ShadowCaster.
Shader "DMZ/ToonInk"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _ShadowColor ("Shadow Color (ink family)", Color) = (0.10,0.11,0.14,1)
        _ShadowThreshold ("Shadow Threshold", Range(-1,1)) = 0.05
        _RampSmooth ("Ramp Smoothing", Range(0.001,0.3)) = 0.03
        _MidThreshold ("Mid Threshold", Range(-1,1)) = 0.55
        _MidStrength ("Mid Band Strength", Range(0,1)) = 0.35
        _OutlineColor ("Outline Color", Color) = (0.03,0.03,0.04,1)
        _OutlineWidth ("Outline Width px", Range(0,8)) = 2.5
        _SideTint ("Side Tint", Color) = (0.20,0.40,1.0,1)
        _SideTintAmount ("Side Tint Amount", Range(0,1)) = 0.0
        _InkColor ("Interior Ink Color", Color) = (0.02,0.02,0.03,1)
        _InkStrength ("Fresnel Rim Ink", Range(0,1)) = 0.55
        _InkPower ("Fresnel Rim Power", Range(0.5,8)) = 3.0
        _CreaseStrength ("Crease Ink Strength", Range(0,1)) = 0.7
        _CreaseThreshold ("Crease Threshold", Range(0,3)) = 0.5
        _CreaseSharp ("Crease Sharpness", Range(0.01,0.6)) = 0.18
        _AccentMask ("Accent Mask (R)", 2D) = "black" {}
        _AccentColor ("Accent Color", Color) = (1.0,0.7,0.2,1)
        _AccentEmission ("Accent Emission", Range(0,4)) = 1.0
        _HitFlash ("Hit Flash", Range(0,1)) = 0
        _Desat ("Damage Desat", Range(0,1)) = 0
        _DissolveAmount ("Dissolve", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        Pass
        {
            Name "ToonForward"
            Tags { "LightMode"="UniversalForward" }
            Cull Back
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST; float4 _AccentMask_ST;
                float4 _BaseColor; float4 _ShadowColor;
                float _ShadowThreshold; float _RampSmooth; float _MidThreshold; float _MidStrength;
                float4 _OutlineColor; float _OutlineWidth; float4 _SideTint; float _SideTintAmount;
                float4 _InkColor; float _InkStrength; float _InkPower;
                float _CreaseStrength; float _CreaseThreshold; float _CreaseSharp;
                float4 _AccentColor; float _AccentEmission;
                float _HitFlash; float _Desat; float _DissolveAmount;
            CBUFFER_END

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_AccentMask); SAMPLER(sampler_AccentMask);

            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float3 normalWS:TEXCOORD1; float3 positionWS:TEXCOORD2; };

            Varyings vert(Attributes IN){
                Varyings OUT;
                VertexPositionInputs p = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = p.positionCS;
                OUT.positionWS = p.positionWS;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 frag(Varyings IN):SV_Target{
                float ign = frac(52.9829189 * frac(dot(IN.positionCS.xy, float2(0.06711056,0.00583715))));
                clip(ign - _DissolveAmount);

                half3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).rgb * _BaseColor.rgb;
                float3 N = normalize(IN.normalWS);
                float3 V = normalize(GetWorldSpaceViewDir(IN.positionWS));

                float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                float ndl = dot(N, mainLight.direction);
                float atten = mainLight.shadowAttenuation;

                float litBand = smoothstep(_ShadowThreshold - _RampSmooth, _ShadowThreshold + _RampSmooth, ndl) * atten;
                float midBand = smoothstep(_MidThreshold - _RampSmooth, _MidThreshold + _RampSmooth, ndl) * atten;

                half3 shadowed = albedo * _ShadowColor.rgb;
                half3 col = lerp(shadowed, albedo, litBand);
                col = lerp(col, albedo, midBand * _MidStrength);
                col *= mainLight.color;
                col += SampleSH(N) * albedo * 0.25;

                // fresnel rim ink (thickens silhouette from inside)
                float rim = 1.0 - saturate(dot(N, V));
                float rimInk = pow(rim, _InkPower) * _InkStrength;
                col = lerp(col, _InkColor.rgb, rimInk);

                // normal-derivative crease ink (interior faceted edges -> hand-inked line work)
                float3 dNx = ddx(N); float3 dNy = ddy(N);
                float creaseAmt = length(dNx) + length(dNy);
                float crease = smoothstep(_CreaseThreshold, _CreaseThreshold + _CreaseSharp, creaseAmt);
                col = lerp(col, _InkColor.rgb, crease * _CreaseStrength);

                float accentMask = SAMPLE_TEXTURE2D(_AccentMask, sampler_AccentMask, IN.uv).r;
                col += _AccentColor.rgb * accentMask * _AccentEmission;

                float luma = dot(col, float3(0.299,0.587,0.114));
                col = lerp(col, luma.xxx, _Desat);
                col = lerp(col, half3(1,1,1), _HitFlash);
                return half4(col, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Outline"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Front
            HLSLPROGRAM
            #pragma vertex vertOut
            #pragma fragment fragOut
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST; float4 _AccentMask_ST;
                float4 _BaseColor; float4 _ShadowColor;
                float _ShadowThreshold; float _RampSmooth; float _MidThreshold; float _MidStrength;
                float4 _OutlineColor; float _OutlineWidth; float4 _SideTint; float _SideTintAmount;
                float4 _InkColor; float _InkStrength; float _InkPower;
                float _CreaseStrength; float _CreaseThreshold; float _CreaseSharp;
                float4 _AccentColor; float _AccentEmission;
                float _HitFlash; float _Desat; float _DissolveAmount;
            CBUFFER_END

            struct AttO { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
            struct VarO { float4 positionCS:SV_POSITION; };

            VarO vertOut(AttO IN){
                VarO OUT;
                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 nWS = TransformObjectToWorldNormal(IN.normalOS);
                float4 posCS = TransformWorldToHClip(posWS);
                float4 nCS = mul(UNITY_MATRIX_VP, float4(nWS, 0.0));
                float2 off = normalize(nCS.xy + 1e-5) * (_OutlineWidth * 2.0 / _ScreenParams.y) * posCS.w;
                posCS.xy += off;
                OUT.positionCS = posCS;
                return OUT;
            }
            half4 fragOut(VarO IN):SV_Target{
                half3 c = lerp(_OutlineColor.rgb, _SideTint.rgb, _SideTintAmount);
                return half4(c, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On ColorMask 0 Cull Back
            HLSLPROGRAM
            #pragma vertex DepthOnlyVert
            #pragma fragment DepthOnlyFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST; float4 _AccentMask_ST;
                float4 _BaseColor; float4 _ShadowColor;
                float _ShadowThreshold; float _RampSmooth; float _MidThreshold; float _MidStrength;
                float4 _OutlineColor; float _OutlineWidth; float4 _SideTint; float _SideTintAmount;
                float4 _InkColor; float _InkStrength; float _InkPower;
                float _CreaseStrength; float _CreaseThreshold; float _CreaseSharp;
                float4 _AccentColor; float _AccentEmission;
                float _HitFlash; float _Desat; float _DissolveAmount;
            CBUFFER_END
            struct DOAtt { float4 positionOS:POSITION; };
            struct DOVar { float4 positionCS:SV_POSITION; };
            DOVar DepthOnlyVert(DOAtt IN){ DOVar o; o.positionCS=TransformObjectToHClip(IN.positionOS.xyz); return o; }
            half4 DepthOnlyFrag(DOVar IN):SV_Target{ return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }
            ZWrite On Cull Back
            HLSLPROGRAM
            #pragma vertex DepthNormalsVert
            #pragma fragment DepthNormalsFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST; float4 _AccentMask_ST;
                float4 _BaseColor; float4 _ShadowColor;
                float _ShadowThreshold; float _RampSmooth; float _MidThreshold; float _MidStrength;
                float4 _OutlineColor; float _OutlineWidth; float4 _SideTint; float _SideTintAmount;
                float4 _InkColor; float _InkStrength; float _InkPower;
                float _CreaseStrength; float _CreaseThreshold; float _CreaseSharp;
                float4 _AccentColor; float _AccentEmission;
                float _HitFlash; float _Desat; float _DissolveAmount;
            CBUFFER_END
            struct DNAtt { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
            struct DNVar { float4 positionCS:SV_POSITION; float3 normalWS:TEXCOORD0; };
            DNVar DepthNormalsVert(DNAtt IN){ DNVar o; o.positionCS=TransformObjectToHClip(IN.positionOS.xyz); o.normalWS=TransformObjectToWorldNormal(IN.normalOS); return o; }
            half4 DepthNormalsFrag(DNVar IN):SV_Target{ return half4(normalize(IN.normalWS), 0); }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual Cull Back
            HLSLPROGRAM
            #pragma vertex shadowVert
            #pragma fragment shadowFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST; float4 _AccentMask_ST;
                float4 _BaseColor; float4 _ShadowColor;
                float _ShadowThreshold; float _RampSmooth; float _MidThreshold; float _MidStrength;
                float4 _OutlineColor; float _OutlineWidth; float4 _SideTint; float _SideTintAmount;
                float4 _InkColor; float _InkStrength; float _InkPower;
                float _CreaseStrength; float _CreaseThreshold; float _CreaseSharp;
                float4 _AccentColor; float _AccentEmission;
                float _HitFlash; float _Desat; float _DissolveAmount;
            CBUFFER_END

            float3 _LightDirection;
            struct AttS { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
            struct VarS { float4 positionCS:SV_POSITION; };
            VarS shadowVert(AttS IN){
                VarS OUT;
                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 nWS = TransformObjectToWorldNormal(IN.normalOS);
                float4 cs = TransformWorldToHClip(ApplyShadowBias(posWS, nWS, _LightDirection));
                #if UNITY_REVERSED_Z
                    cs.z = min(cs.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    cs.z = max(cs.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                OUT.positionCS = cs;
                return OUT;
            }
            half4 shadowFrag(VarS IN):SV_Target{ return 0; }
            ENDHLSL
        }
    }
    Fallback Off
}
