# Shop Screen Revamp Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Unified slot-indexed shop (6–12 offers), three-column build layout, ShopCard/UnitCard panels, multi-lock reroll, buff strip, messages priority stack.

**Architecture:** Core `ShopSlotLayoutResolver` drives generation by slot index; presentation `UnifiedShopView` renders 3×2 / 4×3 grid; fixed `UnitCardPanelView` and `BuffIconStripView` in center column. Spec: `docs/superpowers/specs/2026-06-13-shop-screen-revamp-design.md`.

**Tech Stack:** Unity, C#, NUnit Edit Mode tests, TMPro, uGUI GridLayoutGroup

---

### Task 1: Core slot layout & generator

**Files:**
- Create: `Assets/_Project/Core/Shop/ShopSlotKind.cs`
- Create: `Assets/_Project/Core/Shop/ShopSlotDefinition.cs`
- Create: `Assets/_Project/Core/Shop/ShopSlotLayoutResolver.cs`
- Modify: `Assets/_Project/Core/Shop/ShopGenerator.cs`
- Modify: `Assets/_Project/Core/Shop/ShopOffer.cs`
- Test: `Assets/_Project/Core.Tests/EditMode/ShopSlotLayoutResolverTests.cs`

### Task 2: Multi-lock & unified reroll

**Files:**
- Modify: `Assets/_Project/Core/Run/RunState.cs`
- Modify: `Assets/_Project/Game/RunOrchestrator.Shop.cs`
- Modify: `Assets/_Project/Game/RunOrchestrator.cs`
- Modify: `Assets/_Project/Game/RunManager.cs`
- Test: `Assets/_Project/Core.Tests/EditMode/RunOrchestratorTests.cs`

### Task 3: Unified shop presentation

**Files:**
- Modify: `Assets/_Project/Presentation/Shop/ShopView.cs`
- Modify: `Assets/_Project/Presentation/Shop/ShopLayoutMetrics.cs`
- Modify: `Assets/_Project/Presentation/Run/BuildLayoutMetrics.cs`
- Modify: `Assets/_Project/Presentation/Run/ShopUiBootstrap.cs`

### Task 4: Unit Card panel, messages, buff strip

**Files:**
- Create: `Assets/_Project/Presentation/Run/UnitCardPanelView.cs`
- Create: `Assets/_Project/Presentation/Run/BuildMessagesView.cs`
- Create: `Assets/_Project/Presentation/Run/BuffIconStripView.cs`
- Create: `Assets/_Project/Core/Tags/BuffStripEvaluator.cs`

### Task 5: Scene bootstrap & regression tests

**Files:**
- Modify: `Assets/_Project/Presentation/Editor/RunSceneSetup.cs`
- Modify: Play Mode / Edit Mode tests as needed
