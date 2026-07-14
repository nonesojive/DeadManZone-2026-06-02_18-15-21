// Unit base ring as the health display (owner-decided, 2026-07-11): the disc drains
// like a top-down health orb — a flat cutoff line empties the disc from the far edge
// (screen top from the gameplay camera) downward with the unit's HP fraction, while a
// thin always-on outer rim keeps the side read (muted blue/red) even at near-zero HP.
// The quad is laid flat with +V toward world +Z; the camera looks from -Z, so high V
// = the far edge = screen top. Fill f means the near (screen-bottom) f of the disc
// stays filled. _Fill driven per unit via MaterialPropertyBlock.
Shader "DMZ/CombatRingFill"
{
    Properties
    {
        _FillColor ("Fill Color", Color) = (0.2, 0.28, 0.42, 1)
        _RimColor ("Rim Color", Color) = (0.28, 0.40, 0.60, 1)
        _EmptyColor ("Drained Color", Color) = (0.09, 0.10, 0.12, 1)
        _Fill ("Health Fill", Range(0, 1)) = 1
        _Gutter ("Morale Gutter (0 solid - 1 breaking)", Range(0, 1)) = 0
        _DiscRadius ("Disc Radius (UV)", Range(0.1, 0.5)) = 0.40
        _RimInnerRadius ("Rim Inner Radius (UV)", Range(0.1, 0.5)) = 0.435
        _RimOuterRadius ("Rim Outer Radius (UV)", Range(0.1, 0.5)) = 0.48
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        Pass
        {
            Name "RingFill"
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            Offset -1, -1 // sits 0.02 above the ground plane; bias away from z-fighting

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _FillColor;
                float4 _RimColor;
                float4 _EmptyColor;
                float _Fill;
                float _Gutter;
                float _DiscRadius;
                float _RimInnerRadius;
                float _RimOuterRadius;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings  { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 d = IN.uv - 0.5;
                float r = length(d);
                clip(_RimOuterRadius - r); // circular silhouette out of a quad

                // Always-on outer rim: side identity survives at any HP.
                // Morale gutter: as _Gutter rises the rim notches and flickers like a dying
                // flame — angular noise gates rim pixels, animated by _Time. Achromatic on
                // purpose: shape/flicker only, hue untouched (audit spec 2026-07-14 section 2.1).
                if (r >= _RimInnerRadius)
                {
                    if (_Gutter > 0.001)
                    {
                        float ang = atan2(d.y, d.x);
                        // Two beat frequencies so the flicker doesn't read as a smooth rotation.
                        // n is in [-1,1], density peaked near 0 (product of sines).
                        float n = sin(ang * 9.0 + _Time.y * 14.0) * sin(ang * 23.0 - _Time.y * 31.0);
                        // Threshold slides -1 -> 0.2 with _Gutter: gates nothing when healthy,
                        // sputters roughly 60% of the rim off when breaking. Tune 1.2 in review.
                        if (n < _Gutter * 1.2 - 1.0)
                            return half4(_EmptyColor.rgb, 1);
                    }
                    return half4(_RimColor.rgb, 1);
                }

                // Thin dark separator so the rim still reads against a full disc.
                if (r >= _DiscRadius)
                    return half4(_EmptyColor.rgb, 1);

                // Level fill: normalize the disc's V extent to 0..1 (0 = near/screen-
                // bottom edge, 1 = far/screen-top edge) and fill the bottom _Fill of it,
                // so damage drains the disc from the top down like a health orb.
                float level01 = saturate((d.y + _DiscRadius) / (2.0 * _DiscRadius));
                bool filled = level01 <= _Fill;
                return half4(filled ? _FillColor.rgb : _EmptyColor.rgb, 1);
            }
            ENDHLSL
        }
    }
}
