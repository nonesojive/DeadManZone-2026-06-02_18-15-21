# Creates 8 stacked PR branches for the DeadManZone rework split.
param([int]$StartFrom = 1)

$ErrorActionPreference = "Stop"
$MainRoot = "C:\Users\jiveg\OneDrive\Desktop\Game Projects\DeadManZone"
$RepoRoot = Join-Path $MainRoot ".worktrees\pr-split"
Set-Location $RepoRoot

function Invoke-Git { param([Parameter(ValueFromRemainingArguments=$true)][string[]]$GitArgs) & git @GitArgs; if ($LASTEXITCODE -ne 0) { throw "git failed: $($GitArgs -join ' ')" } }

function Clear-AssumeUnchanged {
    Invoke-Git ls-files | ForEach-Object { Invoke-Git update-index --no-assume-unchanged $_ 2>$null }
}

function Sync-Worktree {
    param([string]$Ref)
    Clear-AssumeUnchanged
    Invoke-Git checkout -f $Ref
    Invoke-Git reset --hard $Ref
}

function Get-SanitizedRootTree {
    param([string]$TreeHash)
    $raw = (& git cat-file -p $TreeHash) -split "`n"
    $hasDeadManZoneDir = $false
    foreach ($line in $raw) {
        if ($line -match "^040000 tree " -and $line -match "`tDeadManZone$") { $hasDeadManZoneDir = $true; break }
    }
    $filtered = [System.Collections.Generic.List[string]]::new()
    foreach ($line in $raw) {
        $line = $line.TrimEnd("`r")
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        if ($hasDeadManZoneDir -and $line -match "^160000 commit " -and $line -match "`tDeadManZone$") { continue }
        $filtered.Add($line)
    }
    $tempFile = [System.IO.Path]::GetTempFileName()
    try {
        [System.IO.File]::WriteAllText($tempFile, (($filtered -join "`n") + "`n"), [System.Text.UTF8Encoding]::new($false))
        $newTree = (cmd /c "git mktree < `"$tempFile`"").Trim()
        if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($newTree)) { throw "git mktree failed for tree $TreeHash" }
        return $newTree
    } finally {
        Remove-Item -Force $tempFile -ErrorAction SilentlyContinue
    }
}

function New-TreeCommit {
    param([string]$Parent, [string]$SourceRef, [string]$Message)
    $sourceTree = (& git rev-parse "${SourceRef}^{tree}").Trim()
    $tree = (Get-SanitizedRootTree $sourceTree).Trim()
    $commit = (& git commit-tree $tree -p $Parent -m $Message).Trim()
    return $commit
}

# pr/* branches are global; keep the main worktree off them so this worktree can check them out.
$mainBranch = (& git -C $MainRoot rev-parse --abbrev-ref HEAD).Trim()
if ($mainBranch -like "pr/*") {
    & git -C $MainRoot checkout -f master
    if ($LASTEXITCODE -ne 0) { throw "git failed: checkout master in main worktree" }
}

Clear-AssumeUnchanged

$Bulk = "98e3d87"

if ($StartFrom -le 1) {
    Sync-Worktree "origin/master"
    Invoke-Git checkout -B pr/1-docs
    Invoke-Git cherry-pick 0f0bdf3 cff0840
}

if ($StartFrom -le 2) {
    $parent = (& git rev-parse pr/1-docs).Trim()
    $commit = New-TreeCommit $parent "d8c55b6" "feat(core): economy, manpower, morale, and emergency draft"
    Invoke-Git branch -f pr/2-economy $commit
}

if ($StartFrom -le 3) {
    $parent = (& git rev-parse pr/2-economy).Trim()
    $commit = New-TreeCommit $parent "f906981" "feat(game): gauntlet and horizontal column board zones"
    Invoke-Git branch -f pr/3-gauntlet-board $commit
}

if ($StartFrom -le 4) {
    Sync-Worktree "pr/3-gauntlet-board"
    Invoke-Git checkout -B pr/4-shop
    $pr4 = @(
        "Assets/_Project/Core/Shop",
        "Assets/_Project/Core.Tests/EditMode/ShopGeneratorTests.cs",
        "Assets/_Project/Core.Tests/EditMode/SpecialtyLaneUnlockTests.cs",
        "Assets/_Project/Core.Tests/EditMode/SpecialtyLaneUnlockTests.cs.meta",
        "Assets/_Project/Core.Tests/EditMode/ContentDatabaseTests.cs",
        "Assets/_Project/Core.Tests/EditMode/RunSaveSerializerTests.cs",
        "Assets/_Project/Core.Tests/EditMode/TestContentRegistry.cs",
        "Assets/_Project/Game/RunOrchestrator.Shop.cs",
        "Assets/_Project/Data/ScriptableObjects/PieceDefinitionSO.cs",
        "Assets/_Project/Presentation/Shop/ShopOfferView.cs",
        "Assets/_Project/Presentation/Shop/ShopView.cs",
        "Assets/_Project/Presentation/Visual/UiThemeSO.cs",
        "Assets/_Project/Tests.PlayMode/ShopViewPlayModeTests.cs"
    )
    Invoke-Git checkout $Bulk -- @pr4
    Invoke-Git commit -m "feat(core): shop lanes and specialty unlock"
}

if ($StartFrom -le 5) {
    Sync-Worktree "pr/4-shop"
    Invoke-Git checkout -B pr/5-tick-combat
    $pr5 = @(
        "Assets/_Project/Core/Board/BattlefieldLayout.cs",
        "Assets/_Project/Core/Board/BattlefieldLayout.cs.meta",
        "Assets/_Project/Core/Board/BattlefieldState.cs",
        "Assets/_Project/Core/Board/BattlefieldState.cs.meta",
        "Assets/_Project/Core/Board/BoardAdjacency.cs",
        "Assets/_Project/Core/Board/BoardLayout.cs",
        "Assets/_Project/Core/Board/BoardState.cs",
        "Assets/_Project/Core/Board/PieceShape.cs",
        "Assets/_Project/Core/Board/PlacedPiece.cs",
        "Assets/_Project/Core/Board/ZoneType.cs",
        "Assets/_Project/Core/Combat",
        "Assets/_Project/Core/Common",
        "Assets/_Project/Core/Content/ContentRegistry.cs",
        "Assets/_Project/Core/DeadManZone.Core.asmdef",
        "Assets/_Project/Core/Run/BoardSnapshot.cs",
        "Assets/_Project/Core.Tests/EditMode/BattlefieldStateTests.cs",
        "Assets/_Project/Core.Tests/EditMode/BattlefieldStateTests.cs.meta",
        "Assets/_Project/Core.Tests/EditMode/CombatEventLogTests.cs",
        "Assets/_Project/Core.Tests/EditMode/CombatResolverTests.cs",
        "Assets/_Project/Core.Tests/EditMode/CombatWinCheckerTests.cs",
        "Assets/_Project/Core.Tests/EditMode/CombatWinCheckerTests.cs.meta",
        "Assets/_Project/Core.Tests/EditMode/CommandProcessorTests.cs",
        "Assets/_Project/Core.Tests/EditMode/PieceShapeTests.cs",
        "Assets/_Project/Core.Tests/EditMode/RngTests.cs"
    )
    Invoke-Git checkout $Bulk -- @pr5
    Invoke-Git commit -m "feat(core): tick combat and battlefield simulation"
}

if ($StartFrom -le 6) {
    Sync-Worktree "pr/5-tick-combat"
    Invoke-Git checkout -B pr/6-orchestration
    $pr6 = @(
        "Assets/_Project/Game/DeadManZone.Game.asmdef",
        "Assets/_Project/Game/GameScenes.cs",
        "Assets/_Project/Game/RunManager.cs",
        "Assets/_Project/Game/RunOrchestrator.cs",
        "Assets/_Project/Game/RunSaveBootstrap.cs",
        "Assets/_Project/Game/SaveManager.cs",
        "Assets/_Project/Core/Run/RunPhase.cs",
        "Assets/_Project/Core/Run/RunSaveSerializer.cs",
        "Assets/_Project/Core/Run/RunState.cs",
        "Assets/_Project/Core.Tests/DeadManZone.Core.Tests.asmdef",
        "Assets/_Project/Core.Tests/EditMode/AuthorityCalculatorTests.cs.meta",
        "Assets/_Project/Core.Tests/EditMode/EmergencyDraftTests.cs.meta",
        "Assets/_Project/Core.Tests/EditMode/ManpowerCalculatorTests.cs.meta",
        "Assets/_Project/Core.Tests/EditMode/MoraleCalculatorTests.cs.meta",
        "Assets/_Project/Core.Tests/EditMode/RunOrchestratorTests.cs"
    )
    Invoke-Git checkout $Bulk -- @pr6
    Invoke-Git commit -m "feat(game): orchestrator tick combat and save integration"
}

if ($StartFrom -le 7) {
    Sync-Worktree "pr/6-orchestration"
    Invoke-Git checkout -B pr/7-data-ui
    $pr7 = @(
        "Assets/_Project/Data",
        "Assets/_Project/Presentation/Board",
        "Assets/_Project/Presentation/DeadManZone.Presentation.asmdef",
        "Assets/_Project/Presentation/Editor",
        "Assets/_Project/Presentation/MainMenu",
        "Assets/_Project/Presentation/Run",
        "Assets/_Project/Scenes",
        "Assets/_Project/Tests.PlayMode/DeadManZone.PlayMode.Tests.asmdef",
        "Assets/_Project/Tests.PlayMode/MainMenuPlayModeTests.cs",
        "Assets/_Project/Core.Tests/EditMode/VerticalSliceRegressionTests.cs"
    )
    Invoke-Git checkout $Bulk -- @pr7
    Invoke-Git commit -m "feat(data+ui): content assets, scenes, and presentation refresh"
}

if ($StartFrom -le 8) {
    $parent = (& git rev-parse pr/7-data-ui).Trim()
    $commit = New-TreeCommit $parent "fd4285a" "fix(tests): green edit/play mode suite and layout alignment"
    Invoke-Git branch -f pr/8-tests $commit
}

Sync-Worktree "pr/7-data-ui"
Write-Host "Done. Branches: pr/1-docs .. pr/8-tests (started from PR $StartFrom)"
Write-Host "PR8 tip: $((git rev-parse pr/8-tests).Trim()) (backup/rework-all: $((git rev-parse backup/rework-all).Trim()))"
