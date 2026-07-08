Shader "Hidden/DeadManZone/CombatUnitOutline"
{
    // Per-unit billboard shader: composites the sprite frame and draws a silhouette
    // outline in the transparent margin just outside the figure. The unit texture is a
    // sprite SHEET (many frames), and the visible frame is a UV sub-rect. Outline neighbor
    // sampling MUST stay inside that sub-rect or it bleeds into adjacent frames — that is
    // what _FrameRect (u0,v0,u1,v1) clamps. C# rewrites _FrameRect on every frame swap.
    Properties
    {
        [MainTexture] _MainTex ("Sprite Sheet", 2D) = "white" {}
        [MainColor]   _BaseColor ("Tint", Color) = (1,1,1,1)
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.35
        _FrameRect ("Frame UV Rect (u0,v0,u1,v1)", Vector) = (0,0,1,1)
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineTexels ("Outline Width (texels)", Range(0,6)) = 1.25
        // 8 = CompareFunction.Always; billboards ignore ground depth (set by C#).
        _ZTest ("ZTest", Float) = 8
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "CombatUnitOutline"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest [_ZTest]
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _MainTex_TexelSize; // (1/w, 1/h, w, h)
                float4 _BaseColor;
                float4 _FrameRect;
                float4 _OutlineColor;
                float _Cutoff;
                float _OutlineTexels;
                float _ZTest;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                return OUT;
            }

            // Alpha at a UV, but 0 if the sample falls outside the active frame sub-rect —
            // this is the anti-bleed clamp that keeps outlines from picking up neighbor frames.
            float SampleFrameAlpha(float2 uv)
            {
                float inside =
                    step(_FrameRect.x, uv.x) * step(uv.x, _FrameRect.z) *
                    step(_FrameRect.y, uv.y) * step(uv.y, _FrameRect.w);
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a * inside;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float4 baseCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * _BaseColor;

                // Smooth coverage instead of a hard cutoff — DXT5 block-quantized alpha
                // flickers frame-to-frame under a binary test.
                float baseCoverage = smoothstep(_Cutoff * 0.6, _Cutoff, baseCol.a);

                // Outline: max opaque neighbor in the 8 cardinal/diagonal directions.
                float2 texel = _MainTex_TexelSize.xy * _OutlineTexels;
                float n = 0.0;
                n = max(n, SampleFrameAlpha(IN.uv + float2( texel.x, 0)));
                n = max(n, SampleFrameAlpha(IN.uv + float2(-texel.x, 0)));
                n = max(n, SampleFrameAlpha(IN.uv + float2(0,  texel.y)));
                n = max(n, SampleFrameAlpha(IN.uv + float2(0, -texel.y)));
                n = max(n, SampleFrameAlpha(IN.uv + float2( texel.x,  texel.y)));
                n = max(n, SampleFrameAlpha(IN.uv + float2(-texel.x,  texel.y)));
                n = max(n, SampleFrameAlpha(IN.uv + float2( texel.x, -texel.y)));
                n = max(n, SampleFrameAlpha(IN.uv + float2(-texel.x, -texel.y)));

                float neighborCoverage = smoothstep(_Cutoff * 0.6, _Cutoff, n);
                // Outline only where THIS pixel is (mostly) transparent but a neighbor is opaque.
                float outline = saturate(neighborCoverage - baseCoverage) * _OutlineColor.a;

                float3 rgb = lerp(_OutlineColor.rgb, baseCol.rgb, baseCoverage);
                float alpha = max(baseCoverage, outline);

                clip(alpha - 0.003);
                return float4(rgb, alpha);
            }
            ENDHLSL
        }
    }
    Fallback "Sprites/Default"
}
