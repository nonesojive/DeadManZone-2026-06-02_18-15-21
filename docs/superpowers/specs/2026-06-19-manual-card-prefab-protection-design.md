# Manual Card Prefab Protection

## Goal

`UnitDetailCard.prefab` and `ShopOfferCard.prefab` are manually authored. Only the user may permanently change the **prefab assets**. Runtime code may resize, recolor, and spawn children on **instances during Play** — those changes revert when Play mode ends unless the user explicitly applies overrides to the prefab.

## What is protected (permanent)

1. **Bake menus** — no `SaveAsPrefabAsset` on either protected path.
2. **`AuthoredCardPrefabGuard`** — blocks programmatic prefab asset writes.
3. **`PieceCardView.EnsureRuntimeUi`** — still skipped when `nameText` is wired, so procedural fallback does not rebuild a hand-authored hierarchy (Play-mode instance only; not an asset write).

## What is allowed (runtime instances)

- `ShopOfferView.ConfigureLayout` / theme colors / badge spawn during Play.
- `PieceCardView.ApplyTheme` and tag chip instantiation during Play.
- Editor theme refresh on scene instances (does not touch prefab assets unless user applies overrides).

## User workflow

1. Edit prefabs in Prefab mode and save.
2. Play mode may mutate instances for layout/theme — normal Unity revert on Stop.
3. Do **not** click **Apply** on the prefab after Play unless you intend to keep those changes.
4. Avoid **DeadManZone → UI → Bake Card Prefabs**.
