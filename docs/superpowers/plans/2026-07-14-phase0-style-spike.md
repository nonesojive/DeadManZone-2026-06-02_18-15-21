# Phase 0 Style Spike + Reference Bake-off Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Run the consolidated Phase 0 gate — four conscript reference-image variants through the
full Meshy chain, rendered beside the enlisted baseline through a new cel/ink uber-shader at
oversized combat scale, with the morale-gutter ring state — and record the six verdicts that
unlock all roster/icon/morale work.

**Architecture:** Everything is Presentation-layer or tooling; the Core sim is untouched. New
artifacts: a ref-check CLI (`tools/refcheck.py`), four ref images + four Meshy GLB sets, one URP
cel/ink shader (`DMZ/UnitCelInk`), a `_Gutter` extension to the existing `CombatRingFill` shader,
one spike scene cloned from `Combat3D_Demo.unity`, and a verdicts doc.

**Tech Stack:** Python 3 + Pillow (refcheck), Meshy API via existing `tools/meshy/meshy_client.py`
(`MESHY_API_KEY` user env var), Unity 6 URP hand-written HLSL, Unity MCP tools for scene work and
screenshots.

**Specs served:** `docs/superpowers/specs/2026-07-14-unit-art-readability-design.md` (§4, §7),
`docs/superpowers/specs/2026-07-14-art-direction-readability-audit-design.md` (§2.1, §2.5, §5).

**The six verdicts this plan must output** (unit-art spec §4):
1. Interior ink: full pass / close-camera pass / fail
2. Morale ring guttering legibility at 20+ units
3. Within-archetype cue legibility (cap vs helmet at battle distance)
4. Oversized-scale grid read (does 120–140% break the board?)
5. Ref style: inked flat-cel vs neutral geometry (where the ink lives)
6. Proportions: stocky vs realistic, committed roster-wide

**Known constraint (spec reconciliation):** `CombatRingFill.shader` already carries **HP** as the
disc fill (owner decision 2026-07-11, documented in the shader header). Morale therefore rides the
same ring as a *gutter* channel (`_Gutter` flicker/notching of rim + disc), NOT as the fill. `_Fill`
stays HP. Task 5 implements this; the audit spec's §2.1 "ring = side channel only" baseline is
superseded on this point.

---

## Task 1: Ref-check CLI (`tools/refcheck.py`)

The pre-Meshy black-shape gate (unit-art spec §7.3): threshold a ref to flat black, shrink to
combat size, emit a judgment strip. A failed ref costs a re-prompt, not a Meshy chain.

**Files:**
- Create: `tools/refcheck.py`
- Create: `tools/requirements.txt`

- [ ] **Step 1: Write `tools/requirements.txt`**

```text
Pillow>=10.0
```

- [ ] **Step 2: Write `tools/refcheck.py`**

```python
"""
DeadManZone - pre-Meshy black-shape gate (unit-art spec section 7.3).

Thresholds a reference image to a flat black silhouette, renders it at combat
size (~40 px tall) and at 4x zoom, and writes a side-by-side judgment strip.
A human/agent judges: does archetype + piece cue still read? If not, re-prompt
the ref BEFORE spending a Meshy chain.

Usage:
  python refcheck.py <ref.png> [--out <dir>] [--alpha-bg]

--alpha-bg: treat transparent pixels as background (for refs with alpha).
Otherwise background = pixels close to the dominant corner color.
"""
import argparse
import os
import sys

from PIL import Image

COMBAT_HEIGHT = 40   # px, spec section 7.3
ZOOM = 4
BG_TOLERANCE = 28    # per-channel distance from corner color counted as background


def silhouette(img: Image.Image, alpha_bg: bool) -> Image.Image:
    rgba = img.convert("RGBA")
    px = rgba.load()
    w, h = rgba.size
    if alpha_bg:
        def is_bg(p):
            return p[3] < 16
    else:
        # ponytail: dominant-corner background detection; ceiling is refs with
        # busy backgrounds — the spec template mandates solid backgrounds anyway.
        corner = px[0, 0]

        def is_bg(p):
            return all(abs(p[i] - corner[i]) <= BG_TOLERANCE for i in range(3))
    out = Image.new("RGB", (w, h), "white")
    opx = out.load()
    filled = 0
    for y in range(h):
        for x in range(w):
            if not is_bg(px[x, y]):
                opx[x, y] = (0, 0, 0)
                filled += 1
    if filled < (w * h) // 200:  # <0.5% figure pixels = detection failed
        sys.exit("silhouette detection found almost nothing - wrong --alpha-bg? busy background?")
    return out


def strip(sil: Image.Image) -> Image.Image:
    w, h = sil.size
    combat = sil.resize((max(1, w * COMBAT_HEIGHT // h), COMBAT_HEIGHT), Image.LANCZOS)
    zoomed = combat.resize((combat.width * ZOOM, combat.height * ZOOM), Image.NEAREST)
    pad = 10
    total_w = combat.width + zoomed.width + 3 * pad
    total_h = max(combat.height, zoomed.height) + 2 * pad
    canvas = Image.new("RGB", (total_w, total_h), "white")
    canvas.paste(combat, (pad, (total_h - combat.height) // 2))
    canvas.paste(zoomed, (combat.width + 2 * pad, (total_h - zoomed.height) // 2))
    return canvas


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("ref")
    ap.add_argument("--out", default=None)
    ap.add_argument("--alpha-bg", action="store_true")
    args = ap.parse_args()
    img = Image.open(args.ref)
    result = strip(silhouette(img, args.alpha_bg))
    out_dir = args.out or os.path.dirname(args.ref) or "."
    base = os.path.splitext(os.path.basename(args.ref))[0]
    dest = os.path.join(out_dir, f"{base}_shape.png")
    result.save(dest)
    print(dest)


if __name__ == "__main__":
    main()
```

- [ ] **Step 3: Install Pillow and self-check the tool**

Run (from repo root):

```powershell
pip install -r tools/requirements.txt
python tools/refcheck.py "Assets/_Project/Art/Combat2D/Icons/ShopV2/shop_icon_conscript_rifleman.png"
```

Expected: prints the path `...shop_icon_conscript_rifleman_shape.png`; the file exists and shows a
black soldier silhouette at 40px + 4x zoom on white. (This also produces the *painterly baseline*
silhouette for later comparison — keep the output.)

- [ ] **Step 4: Read the generated `_shape.png` and confirm the silhouette is a recognizable
soldier** (agent: use the Read tool on the image). If the icon's dark frame breaks corner
detection, re-run with a crop or note the limitation — the gate is for *new* refs which have
clean backgrounds by template.

- [ ] **Step 5: Commit**

```powershell
git add tools/refcheck.py tools/requirements.txt
git commit -m "tools: refcheck black-shape gate for Meshy reference images (spec 7.3)"
```

---

## Task 2: Author the four conscript reference images

Unit-art spec §7.4: (inked flat-cel | neutral geometry) × (stocky | realistic). Conscript cue row
(spec §2.3): **rifle archetype, soft field cap, no pack**.

**Files:**
- Create: `tools/meshy/units/conscript_rifleman/refs/cel_stocky.png`
- Create: `tools/meshy/units/conscript_rifleman/refs/cel_real.png`
- Create: `tools/meshy/units/conscript_rifleman/refs/neutral_stocky.png`
- Create: `tools/meshy/units/conscript_rifleman/refs/neutral_real.png`

- [ ] **Step 1: Create the refs directory**

```powershell
mkdir tools/meshy/units/conscript_rifleman/refs
```

- [ ] **Step 2: Generate the four refs** with an image model (GenerateImage tool, Grok, or
Gemini — whatever is on hand; match the historical Grok workflow if keys exist). Shared base
prompt (every variant):

> Full-body character reference of a single WW1 grimdark conscript soldier, olive drab worn
> uniform, **soft field cap (no helmet), no backpack**, bolt-action rifle held vertically at his
> side, neutral relaxed A-pose standing, 3/4 front view, flat even studio lighting, plain solid
> light-grey background, no ground shadow, no atmosphere, entire figure in frame, hard clear
> boundaries between cloth / leather / metal parts.

Per-variant additions:

| File | Add to prompt |
|---|---|
| `cel_stocky.png` | "flat cel shading with bold black ink outlines, inked illustration style" + "stocky toy-soldier proportions, about 4.5 head-heights tall, oversized head, hands and rifle" |
| `cel_real.png` | "flat cel shading with bold black ink outlines, inked illustration style" + "realistic military proportions" |
| `neutral_stocky.png` | "clean stylized 3D character render look, matte surfaces, NO outlines, no painterly texture" + "stocky toy-soldier proportions, about 4.5 head-heights tall, oversized head, hands and rifle" |
| `neutral_real.png` | "clean stylized 3D character render look, matte surfaces, NO outlines, no painterly texture" + "realistic military proportions" |

- [ ] **Step 3: Gate every ref**

```powershell
python tools/refcheck.py tools/meshy/units/conscript_rifleman/refs/cel_stocky.png
python tools/refcheck.py tools/meshy/units/conscript_rifleman/refs/cel_real.png
python tools/refcheck.py tools/meshy/units/conscript_rifleman/refs/neutral_stocky.png
python tools/refcheck.py tools/meshy/units/conscript_rifleman/refs/neutral_real.png
```

Read each `_shape.png`. Pass condition per ref: at the 40px silhouette you can still see
(a) upright rifle-infantry archetype (long-gun line), (b) **cap-not-helmet** head shape. If a ref
fails, re-prompt that variant (adjust wording, regenerate) and re-gate. Do not proceed with a
failing ref — that is the whole point of the gate.

- [ ] **Step 4: Commit**

```powershell
git add tools/meshy/units/conscript_rifleman/refs/
git commit -m "art: conscript ref bake-off images (cel/neutral x stocky/real), black-shape gated"
```

---

## Task 3: Run the four Meshy chains

Pipeline per unit (matches `docs/meshy-roster-jobs-2026-07-11.md`): image3d @12k → remesh @12k →
rig → animate idle(0) + die(8); walk comes free with the rig.

**Files:**
- Create: `docs/meshy-spike-jobs-2026-07.md` (job tracking, same format as the 2026-07-11 doc)
- Create: `tools/meshy/units/conscript_rifleman/spike/<variant>/{idle,walk,die}.glb` ×4 variants

- [ ] **Step 1: Verify the API key is present**

```powershell
python -c "import os; print('OK' if os.environ.get('MESHY_API_KEY') else 'MISSING')"
```

If MISSING, check the user env var: `[Environment]::GetEnvironmentVariable('MESHY_API_KEY','User')`
and set it in the session. If genuinely absent, STOP and ask the owner — this plan cannot proceed
without it.

- [ ] **Step 2: Queue the four image3d jobs** (one per variant; record every task id in
`docs/meshy-spike-jobs-2026-07.md` as you go, same table format as the 2026-07-11 doc):

```powershell
python tools/meshy/meshy_client.py image3d --image tools/meshy/units/conscript_rifleman/refs/cel_stocky.png --polycount 12000
python tools/meshy/meshy_client.py image3d --image tools/meshy/units/conscript_rifleman/refs/cel_real.png --polycount 12000
python tools/meshy/meshy_client.py image3d --image tools/meshy/units/conscript_rifleman/refs/neutral_stocky.png --polycount 12000
python tools/meshy/meshy_client.py image3d --image tools/meshy/units/conscript_rifleman/refs/neutral_real.png --polycount 12000
```

- [ ] **Step 3: For each variant, walk the chain** (wait → remesh → wait → rig → wait → animate):

```powershell
python tools/meshy/meshy_client.py wait image3d <id>
python tools/meshy/meshy_client.py remesh <image3d_id> --polycount 12000
python tools/meshy/meshy_client.py wait remesh <remesh_id>
python tools/meshy/meshy_client.py rig <remesh_id> --height 1.8
python tools/meshy/meshy_client.py wait rig <rig_id>
python tools/meshy/meshy_client.py animate <rig_id> 0    # idle
python tools/meshy/meshy_client.py animate <rig_id> 8    # die
python tools/meshy/meshy_client.py wait anim <anim_id>   # each
```

Gotchas (from the 2026-07-11 run, verbatim): texture color varies per gen (accept or re-gen); do
NOT blunt-decimate in Blender (weld + recalc normals only); Meshy shoot anims are bow-and-arrow
(unusable — do not order one).

- [ ] **Step 4: Download each variant's GLBs**

```powershell
python tools/meshy/meshy_client.py download anim <idle_anim_id> --out tools/meshy/units/conscript_rifleman/spike/cel_stocky --prefix idle_ --filter glb
python tools/meshy/meshy_client.py download rig <rig_id> --out tools/meshy/units/conscript_rifleman/spike/cel_stocky --prefix rig_ --filter walk
```

(repeat per variant; exact filenames inside `--out` vary by API response — normalize to
`idle.glb` / `walk.glb` / `die.glb` after download, matching `Combat3D/Models/<unit>/` convention.)

- [ ] **Step 5: Commit** (job doc + refs; GLBs under `tools/` follow whatever the 2026-07-11 run
committed — mirror that: if `tools/meshy/units/*/glb12k` is tracked, track spike GLBs too)

```powershell
git add docs/meshy-spike-jobs-2026-07.md tools/meshy/units/conscript_rifleman/spike/
git commit -m "art: conscript bake-off Meshy chains complete (4 variants, idle/walk/die GLBs)"
```

---

## Task 4: The cel/ink uber-shader v0 (`DMZ/UnitCelInk`)

Arena spec §4 shader, minimum spike slice: cel ramp (priority 1) + ink outline pass with side-tint
parameter (priority 2). Interior ink (priority 3) is judged from the *texture* the inked-cel refs
bake in — no edge-detect term in v0 (YAGNI until verdict 5 says shader-side ink is needed).
Status hooks (priority 4) and accent masks (priority 5) come after the spike.

**Files:**
- Create: `Assets/_Project/Presentation/Combat/Arena/Shaders/UnitCelInk.shader`

- [ ] **Step 1: Write the shader**

```hlsl
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
```

- [ ] **Step 2: Refresh assets and check compilation**

Unity MCP: `assets-refresh`, then `assets-shader-get-data` on
`Assets/_Project/Presentation/Combat/Arena/Shaders/UnitCelInk.shader`.
Expected: `isSupported: true`, no compilation messages. Also `console-get-logs` filtered to
Error — expect none referencing the shader.

- [ ] **Step 3: Create the two side materials** via Unity MCP `assets-material-create` +
`assets-modify`:
- `Assets/_Project/Combat3D/UnitCelInk_Player.mat` — shader `DMZ/UnitCelInk`, `_SideColor` ≈
  (0.25, 0.45, 0.90) cool blue.
- `Assets/_Project/Combat3D/UnitCelInk_Enemy.mat` — `_SideColor` ≈ (0.85, 0.25, 0.20) warm red.

- [ ] **Step 4: Commit**

```powershell
git add Assets/_Project/Presentation/Combat/Arena/Shaders/UnitCelInk.shader Assets/_Project/Combat3D/UnitCelInk_*.mat Assets/_Project/Combat3D/UnitCelInk_*.mat.meta Assets/_Project/Presentation/Combat/Arena/Shaders/UnitCelInk.shader.meta
git commit -m "feat(presentation): DMZ/UnitCelInk spike shader - cel bands + side-tinted ink outline"
```

---

## Task 5: Morale gutter state on the base ring

Audit spec §2 reconciled with the 2026-07-11 ring-is-HP decision: `_Fill` stays HP; add `_Gutter`
(0 = healthy → 1 = breaking) that makes the **rim flicker and notch** like a dying flame. Judged
at 20+ units for verdict 2.

**Files:**
- Modify: `Assets/_Project/Presentation/Combat/Arena/Shaders/CombatRingFill.shader`

- [ ] **Step 1: Add `_Gutter` to Properties and CBUFFER**

In `Properties` add:

```hlsl
_Gutter ("Morale Gutter (0 solid - 1 breaking)", Range(0, 1)) = 0
```

In `CBUFFER_START(UnityPerMaterial)` add:

```hlsl
float _Gutter;
```

- [ ] **Step 2: Gutter the rim in `frag`** — replace the rim branch:

```hlsl
// Always-on outer rim: side identity survives at any HP.
if (r >= _RimInnerRadius)
    return half4(_RimColor.rgb, 1);
```

with:

```hlsl
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
```

- [ ] **Step 3: Refresh + compile check** — `assets-refresh`, `assets-shader-get-data` on
`CombatRingFill.shader`: `isSupported: true`, no errors.

- [ ] **Step 4: Visual smoke** — in the spike scene (Task 6) set `_Gutter` to 0 / 0.5 / 1.0 on
three rings via material inspection (or `script-execute` with a `MaterialPropertyBlock`):
0 = solid rim, 0.5 = notched/flickering, 1.0 = sputtering with most of the rim dark. The three
states must be distinguishable in a paused screenshot AND the flicker must animate in play mode.

- [ ] **Step 5: Commit**

```powershell
git add Assets/_Project/Presentation/Combat/Arena/Shaders/CombatRingFill.shader
git commit -m "feat(presentation): morale gutter channel on CombatRingFill rim (_Gutter, achromatic flicker)"
```

---

## Task 6: The spike scene

**Files:**
- Create: `Assets/_Project/Scenes/StyleSpike_Phase0.unity` (copy of `Combat3D_Demo.unity`)

- [ ] **Step 1: Copy the demo scene** — Unity MCP `assets-copy`
`Assets/_Project/Scenes/Combat3D_Demo.unity` → `Assets/_Project/Scenes/StyleSpike_Phase0.unity`,
then `scene-open` it (Single).

- [ ] **Step 2: Import the four conscript variants** — copy each variant's `idle.glb` to
`Assets/_Project/Combat3D/Models/_Spike/conscript_<variant>/idle.glb` (plain file copy +
`assets-refresh`). Do NOT hand-edit `.meta` files.

- [ ] **Step 3: Verify the camera matches the arena spec §1** — perspective, pitch 45–55°,
FOV 30–40°, player left / enemy right. `Combat3D_Demo` should already be close (it was built for
the 3D arena); correct via `gameobject-modify` if it isn't. Record the actual values used in the
verdicts doc.

- [ ] **Step 4: Stage the lineup.** Read the demo's cell size from
`Assets/_Project/Combat3D/CombatArena3DDemoConfig.asset` (`assets-get-data`). Place, on adjacent
cells in one row, each with a `RingFill_Player`-material ring quad under it:
1. enlisted_rifleman (existing `Combat3D/Models/enlisted_rifleman/idle.glb`) — the baseline
2. conscript cel_stocky · 3. conscript cel_real · 4. conscript neutral_stocky · 5. conscript neutral_real

Apply `UnitCelInk_Player.mat` to all five (`gameobject-component-modify` on each renderer).
**Scale law:** uniform-scale each model so its footprint diameter ≈ **1.3 × cell size**
(oversized-scale midpoint). Then duplicate the five into a second row with
`UnitCelInk_Enemy.mat` facing them.

- [ ] **Step 5: Build the 20+ crowd for the noise tests** — `gameobject-duplicate` the ten units
into a 24-unit block (4 rows × 6 columns) on the grid. Set ring `_Gutter` per row: row 1 = 0,
row 2 = 0.35, row 3 = 0.7, row 4 = 1.0 (via `script-execute` MaterialPropertyBlock loop or four
ring material variants).

- [ ] **Step 6: Save** — `scene-save`. Commit:

```powershell
git add Assets/_Project/Scenes/StyleSpike_Phase0.unity Assets/_Project/Scenes/StyleSpike_Phase0.unity.meta Assets/_Project/Combat3D/Models/_Spike/
git commit -m "feat(spike): Phase 0 style-spike scene - 4 conscript variants + enlisted baseline at oversized scale"
```

---

## Task 7: Screenshot review and the six verdicts

**Files:**
- Create: `docs/superpowers/specs/2026-07-phase0-verdicts.md`
- Create: `Screenshots/phase0/` captures (folder already gitignored per handoff notes — verdicts
  doc embeds conclusions, not files)

- [ ] **Step 1: Capture the evidence set** with Unity MCP screenshots:
- `screenshot-game-view` at 1920×1080 — full lineup at combat scale (the primary judgment shot).
- `screenshot-isolated` per conscript variant — close-up (punch-in distance judgment).
- Play mode ON (`editor-application-set-state`) + `screenshot-game-view` ×3 over a few seconds —
  gutter flicker animation check.
- Painterly baseline: the Task 1 `_shape.png` strips beside the 3D lineup shot.

- [ ] **Step 2: Judge each verdict against its spec definition** (agent reads every screenshot,
then the owner confirms — these are owner calls, present the evidence):

| # | Verdict | Question | Evidence |
|---|---|---|---|
| 1 | Interior ink | Does the cel_* texture ink read as "inked illustration" at battle distance? At close? | lineup + isolated shots |
| 2 | Ring gutter | Are the 4 gutter rows distinguishable at 24 units without screen noise? | crowd shots + play-mode frames |
| 3 | Cue legibility | Cap (conscript) vs helmet+pack (enlisted) tellable apart at battle distance? | lineup shot |
| 4 | Oversized scale | Does 1.3× footprint break the grid read / cause bad overlap? | crowd shot |
| 5 | Ref style | Which column (cel vs neutral) produced better geometry AND better final look through the shader? | all shots |
| 6 | Proportions | Stocky vs realistic: which reads at 40px on screen? | lineup shot |

- [ ] **Step 3: Write `docs/superpowers/specs/2026-07-phase0-verdicts.md`** — for each verdict:
the call, one sentence of evidence, and what it unlocks/changes (e.g. verdict 5+6 lock the §7.2
ref template defaults; verdict 1 decides the two-tier ink surface; a verdict 3 FAIL stops roster
work per spec §4). Include the camera values from Task 6 Step 3 and the model scale factors used.

- [ ] **Step 4: Update the two 2026-07-14 specs** — fill the "pending bake-off" holes (unit-art
§2.1 proportions, §7.2 template defaults) with the verdicts, marked
`(Phase 0 verdict, 2026-07)`.

- [ ] **Step 5: Commit**

```powershell
git add docs/superpowers/specs/2026-07-phase0-verdicts.md docs/superpowers/specs/2026-07-14-unit-art-readability-design.md docs/superpowers/specs/2026-07-14-art-direction-readability-audit-design.md
git commit -m "docs: Phase 0 verdicts recorded; ref template and proportion defaults locked"
```

---

## Out of scope (queued as separate plans, in order)

1. **Build-phase threshold legibility** (audit spec §3) + the **Critical Mass HQ-board Core fix**
   (audit spec §6.1 — Core + tests + GDD same-commit) — independent of these verdicts, can be
   planned in parallel.
2. **Roster regeneration** in confusion-priority order (unit-art spec §7.5) — blocked on verdicts 5/6.
3. **Icon render tool** (unit-art spec §3.1) — blocked on the shader surviving verdict 1.
4. **Full morale channel + pause register** (audit spec §2/§4) — blocked on verdict 2 and the
   marksman stance answer (§5.1).
5. **Superseding ADR for mesh sourcing** (replaces ADR-0003) — write once verdicts land, so the
   ADR records the *validated* pipeline.
