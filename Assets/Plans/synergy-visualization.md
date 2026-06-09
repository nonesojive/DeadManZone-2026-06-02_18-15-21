# Project Overview
- **Game Title**: DeadManZone
- **High-Level Concept**: A deep, strategic WW1 retro-futuristic autobattler with a grimdark tone. Players assemble an army of units and buildings, arranging them on a grid to maximize positional synergies before watching battles play out.
- **Players**: Single-player linear roguelike gauntlet (with planned async PVP).
- **Inspiration**: Backpack Battles, The Bazaar, TFT, Autochess.
- **Tone / Art Direction**: Gritty, dark, desperate, WW1 retro-futuristic.
- **Target Platform**: PC (Windows), with future goals for Mobile and Controller support.
- **Render Pipeline**: Built-in.

# Game Mechanics
## Synergy System
The heart of the strategic depth in DeadManZone is the positional synergy system. Units and buildings (Pieces) have tags (e.g., Supply, Medic, Command, Vanguard). When placed adjacently on the board, they trigger rules defined in the `SynergyRuleCatalog`. 
- **Outbound Synergies**: A piece provides a buff to its neighbors (e.g., a "Supply" unit giving +1 Damage to all adjacent allies).
- **Inbound Synergies**: A piece receives a buff based on its neighbors.
- **Filtering**: Synergies can be restricted to certain types of neighbors (e.g., "Medic" only buffs "Infantry").

# UI
The goal is to make these "invisible" connections visible to the player during the Build phase, providing immediate feedback on their strategic choices.

## Synergy Connection Lines
- **Visualization**: Visual lines or "energy links" will be drawn between pieces that are actively exchanging synergies.
- **Styling**: Different colors for different synergy types (e.g., Green for Medic/Healing, Yellow for Command/Leadership).
- **Dynamic Updates**: Lines update in real-time as pieces are dragged and dropped.

## Synergy Tracking Panel
- **Trait List**: A side panel (similar to TFT) that lists all active traits and their current levels (e.g., "Command: 1/2 Pieces").
- **Tooltips**: Hovering over a trait in the list explains the bonus levels.

## Hover Card Enhancements
- **Stat Breakdown**: When hovering over a piece on the board, the hover card will list active synergy bonuses (e.g., "+2 Damage from adjacent Supply Depot").

# Key Asset & Context
- `Assets/_Project/Core/Combat/SynergyEngine.cs`: Core logic for evaluating synergies.
- `Assets/_Project/Core/Tags/SynergyRuleCatalog.cs`: Repository of synergy rules.
- `Assets/_Project/Presentation/Board/BoardView.cs`: The main UI view for the board.
- `Assets/_Project/Presentation/Board/PieceHoverCardController.cs`: Controls the unit tooltips.

# Implementation Steps

## Step 1: Enhance SynergyEngine for Visualization
- **Description**: 
    - Modify `SynergyEngine.EvaluateFightStart` (or add a new `EvaluateBuildPhase` method) to return a collection of "Synergy Links".
    - A `SynergyLink` will contain: `SourceInstanceId`, `TargetInstanceId`, `RuleId`, and `SynergyStat`.
    - This allows the UI to know exactly which pieces to connect.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: No

## Step 2: Create SynergyLinkView Prefab and Script
- **Description**: 
    - Create a new UI component `SynergyLinkView` that uses a simple `Image` or `LineRenderer` to draw a line between two points.
    - Implement a `Bind(Vector3 start, Vector3 end, Color color)` method.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

## Step 3: Implement BoardSynergyOverlay
- **Description**: 
    - Create a `BoardSynergyOverlay` component that sits on the `PiecesOverlay` in `BoardView`.
    - It will manage a pool of `SynergyLinkView` objects.
    - It will listen for board changes (placement, movement, removal) and refresh the links based on the `SynergyEngine` data.
- **Assigned role**: developer
- **Dependencies**: Step 1, Step 2
- **Parallelizable**: No

## Step 4: Implement Synergy Side Panel
- **Description**: 
    - Create a new UI panel `SynergySidePanel` and `SynergyTraitItem` prefab.
    - This panel will aggregate the total number of pieces with specific `SynergyTags` and display their current "Trait Level" (e.g., Bronze/Silver/Gold) based on defined thresholds.
- **Assigned role**: developer
- **Dependencies**: Step 1
- **Parallelizable**: Yes

## Step 5: Update Hover Card with Synergy Data
- **Description**: 
    - Update `PieceHoverCardController.Show` to take the `SynergyResult` for the hovered instance.
    - Modify the UI to show a list of active modifiers (e.g. "Synergy: +2 Damage").
- **Assigned role**: developer
- **Dependencies**: Step 1
- **Parallelizable**: Yes

# Verification & Testing
- **Manual Verification**:
    - Place a unit with the `Supply` tag next to another unit. Verify a connection line appears.
    - Move the unit away. Verify the line disappears.
    - Hover over the buffed unit. Verify the tooltip shows the damage bonus.
- **Unit Tests**:
    - Add tests to `SynergyEngineTests` to verify that the new "Link" data correctly identifies the source and target of every active synergy rule.
