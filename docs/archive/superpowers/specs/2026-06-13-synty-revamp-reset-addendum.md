> **SUPERSEDED - DO NOT DESIGN FROM THIS FILE.**
> This document is archived history. Systems described here have been renamed,
> replaced or deleted (Morale as a run resource, Gold, 8x2 reserves, 6 shop slots, ...).
> **The authoritative design is [`docs/GDD.md`](../../../GDD.md).** See `docs/archive/README.md`.

---

# Synty Asset Revamp — Reset Addendum

**Date:** 2026-06-13  
**Status:** Approved  
**Parent spec:** `2026-06-13-synty-asset-revamp-design.md`

---

## Why we reset

An incorrect workaround batch-converted thousands of Synty `.mat` files to `Universal Render Pipeline/Lit`, which:

- Stripped Sidekick/Synty shader property bindings
- Caused white untextured combat meshes
- Triggered multi-minute domain reloads and editor crashes when entering Play Mode

`Assets/Synty` is not tracked in git, so recovery required **deleting and re-importing** Synty packages plus deleting `Library/`.

---

## Correct rendering rule (mandatory)

| Do | Don't |
|----|-------|
| Assign URP pipeline project-wide | Batch-convert Synty materials to URP/Lit |
| Use native Synty shader graphs on URP | Scan/repair all materials under `Assets/Synty` |
| Build wrappers under `_Project/Art/Synty` | Runtime-remap Synty prefab materials to URP/Lit |
| Run `DeadManZone → Synty → Apply Full Synty Art Pass` | Run removed "Repair Broken URP/Lit" menus |

Combat uses **instantiated Synty prefabs as-is**. Only the fallback primitive ground plane uses URP/Lit or Standard.

---

## Recovery checklist (Phase 0 — user)

1. Close Unity
2. Delete `Assets/Synty` and `Library`
3. Re-open project; re-import Synty packages
4. `DeadManZone → Rendering → Setup URP For Project`
5. `Synty → Package Helper → Install Packages` (if needed)

---

## Phase 1 code cleanup (done)

Removed editor tools:

- `SyntyUrpMaterialFallback.cs`
- `BrokenUrpMeshMaterialRepair.cs`
- `MaterialSerializedTextureUtility.cs`
- `MisconvertedMaterialRepair.cs`
- `CombatMaterialScope.cs`

Kept:

- `DeadManZoneUrpSetup.cs` — pipeline assignment + lightweight validation
- `UrpSetupPlayModeGuard.cs` — warns if URP missing on Play

Simplified:

- `CombatArenaMaterialUtility.cs` — ground material + `IsUrpActive()` only
- Removed `RemapHierarchy` calls from arena bootstrap, units, buildings

---

## Phase 2 redo (user in Unity)

1. `DeadManZone → Rendering → Validate URP Setup`
2. `DeadManZone → Synty → Apply Full Synty Art Pass`
3. Play → Run → start combat → verify textures and stable Play entry

Success: Synty-textured units/buildings, health bars visible, Play Mode enters in seconds.
