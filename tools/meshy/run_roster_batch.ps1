# DeadManZone - Neutral + IronMarch v1 roster: full Meshy model batch.
# Run AFTER refs are gated by refcheck and promoted to units/<piece>/ref.png.
# COSTS ~30-40 credits per humanoid unit, less for --vehicle (no rig/anim).
# Resume-safe: generate_unit.py keeps pipeline_state.json per unit.
#
#   powershell -ExecutionPolicy Bypass -File tools/meshy/run_roster_batch.ps1
#   (add -DryRun to walk the chain without spending credits)

param([switch]$DryRun)

$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$dry = if ($DryRun) { "--dry-run" } else { "" }

# conscript_rifles reuses the canonical s09 comic-noir ref/model.
$humanoid = @(
    "militia_squad", "field_medic",                       # neutral
    "conscript_rifles", "line_grenadiers", "field_mortar_team",
    "sharpshooter", "iron_guard", "forward_observer",
    "shock_sergeant", "marksman_doctrine_officer"          # ironmarch
)
$static = @(
    "machine_gun_nest", "trench_works",                    # neutral structures
    "breakthrough_tank", "grand_battery"                   # ironmarch vehicle/structure
)

foreach ($u in $humanoid) {
    $ref = Join-Path $here "units/$u/ref.png"
    if (-not (Test-Path $ref)) { Write-Host "SKIP $u (no gated ref.png)" -ForegroundColor Yellow; continue }
    Write-Host "=== $u (humanoid) ===" -ForegroundColor Cyan
    python (Join-Path $here "generate_unit.py") $u $dry
    if ($LASTEXITCODE -ne 0) { Write-Host "FAILED $u - rerun this script to resume" -ForegroundColor Red; exit 1 }
}

foreach ($u in $static) {
    $ref = Join-Path $here "units/$u/ref.png"
    if (-not (Test-Path $ref)) { Write-Host "SKIP $u (no gated ref.png)" -ForegroundColor Yellow; continue }
    Write-Host "=== $u (static, --vehicle) ===" -ForegroundColor Cyan
    python (Join-Path $here "generate_unit.py") $u --vehicle $dry
    if ($LASTEXITCODE -ne 0) { Write-Host "FAILED $u - rerun this script to resume" -ForegroundColor Red; exit 1 }
}

Write-Host "Batch complete. Manual Unity steps: import GLBs, DMZ/UnitCelInk (black outline, texture_0), height-normalize 1.3xCELL." -ForegroundColor Green
