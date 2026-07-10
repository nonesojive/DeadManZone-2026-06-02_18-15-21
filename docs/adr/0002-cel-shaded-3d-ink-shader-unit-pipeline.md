# 0002 — Unit rendering: cel-shaded 3D with ink outline shader

Status: proposed (2026-07-10 greenfield art plan — no code changed yet; supersedes the sprite-billboard direction if adopted)

Combat units will be rigged 3D models under a toon/cel shader with a thick ink-outline pass, graded desaturated to serve the DD-skin identity (ADR-0001) — the same pipeline Top Troops uses, retuned from cheerful to grim. Decided because the stated bar is Top Troops-grade combat smoothness, which comes from blend trees and interpolated rigs; the incumbent pre-rendered sprite-sheet billboard pipeline already hit its frame/memory ceiling once (3584²→1024² downsize) and cannot reach that bar by adding frames. Humanoid retargeting means one animation set serves the whole infantry roster, and faction/rank variants become material swaps instead of new sheets.

Considered and rejected: 2D skeletal (Spine) — most DD-authentic surface but requires part-segmented art plus a rig per piece across a 46+ piece roster, a poor fit for a solo dev and for AI-assisted art generation; pre-rendered sheets (status quo) — cheapest continuity but smoothness-capped; hybrid 3D/2D — doubles the shader and ink-treatment surface that must visually match.

Note: this is the project's second reversal on this axis (prettycombat was 3D Synty, 2dcombatreworkv2 went sprites). The difference this time is the ink/grade shader owns the look, not the raw meshes — the earlier 3D attempt failed on style, not on smoothness. Key risk to retire early with a spike: proving low-poly 3D + shader reads as "inked illustration," not "generic toon."
