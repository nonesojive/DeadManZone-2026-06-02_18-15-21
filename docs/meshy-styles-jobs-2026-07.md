# Meshy multi-style bake-off — job tracking (2026-07-15)

Pipeline per style: image3d @12k → remesh @12k → rig (height 1.8) → animate (0=Idle, 8=Dead).
Refs: `tools/meshy/units/conscript_rifleman/refs/styles/<id>.png`
Output: `tools/meshy/units/conscript_rifleman/styles/<id>/{idle,walk,die}.glb`

## Known ref notes (before Meshy)
- Soft-cap + mid-stocky enforced in prompts.
- **Backpack contamination:** image model often ignored "no backpack" — s03/s04 may still show pack mass (flag for style judgment, not cue purity).
- s05 retry cleared backpack; s07/s09/s10 clean.

## Jobs

| Id | Style | image3d | remesh | rig | anim idle | anim die | notes |
|---|---|---|---|---|---|---|---|
| s01_cel | inked flat-cel | | | | | | |
| s02_vector | flat vector | | | | | | |
| s03_lowpoly2d | low-poly 2D | | | | | | pack risk |
| s04_cutout | paper cutout | | | | | | pack risk |
| s05_stylized3d | stylized 3D clay | | | | | | |
| s06_plastic_toy | hard plastic toy | | | | | | |
| s07_voxel | voxel blocky | | | | | | |
| s08_woodblock | woodblock ink | | | | | | |
| s09_comic_noir | heavy-ink comic | | | | | | |
| s10_stopmo | claymation | | | | | | |
