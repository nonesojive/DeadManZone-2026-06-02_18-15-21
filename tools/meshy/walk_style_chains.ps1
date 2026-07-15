# Walk 10 style bake-off Meshy chains. Requires MESHY_API_KEY in env.
$ErrorActionPreference = "Stop"
$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
if (-not $root) { $root = (Get-Location).Path }
Set-Location $root
if (-not $env:MESHY_API_KEY) {
  $env:MESHY_API_KEY = [Environment]::GetEnvironmentVariable('MESHY_API_KEY','User')
}
$py = "python"
$client = "tools/meshy/meshy_client.py"
$refs = "tools/meshy/units/conscript_rifleman/refs/styles"
$outRoot = "tools/meshy/units/conscript_rifleman/styles"
$jobDoc = "docs/meshy-styles-jobs-2026-07.md"
$ids = @(
  "s01_cel","s02_vector","s03_lowpoly2d","s04_cutout","s05_stylized3d",
  "s06_plastic_toy","s07_voxel","s08_woodblock","s09_comic_noir","s10_stopmo"
)

function Invoke-Meshy([string[]]$Args) {
  & $py $client @Args
  if ($LASTEXITCODE -ne 0) { throw "meshy_client failed: $Args" }
}

# Phase 1: queue all image3d
$image3d = @{}
foreach ($id in $ids) {
  $img = Join-Path $refs "$id.png"
  Write-Host "QUEUE image3d $id"
  $tid = (Invoke-Meshy @("image3d","--image",$img,"--polycount","12000")).Trim()
  $image3d[$id] = $tid
  Write-Host "  -> $tid"
}

# Phase 2: walk each chain
$results = @{}
foreach ($id in $ids) {
  Write-Host "==== $id ===="
  $i3 = $image3d[$id]
  Invoke-Meshy @("wait","image3d",$i3,"--timeout-minutes","45")
  $remesh = (Invoke-Meshy @("remesh",$i3,"--polycount","12000")).Trim()
  Invoke-Meshy @("wait","remesh",$remesh,"--timeout-minutes","45")
  $rig = (Invoke-Meshy @("rig",$remesh,"--height","1.8")).Trim()
  Invoke-Meshy @("wait","rig",$rig,"--timeout-minutes","45")
  $idle = (Invoke-Meshy @("animate",$rig,"0")).Trim()
  $die  = (Invoke-Meshy @("animate",$rig,"8")).Trim()
  Invoke-Meshy @("wait","anim",$idle,"--timeout-minutes","45")
  Invoke-Meshy @("wait","anim",$die,"--timeout-minutes","45")

  $dest = Join-Path $outRoot $id
  New-Item -ItemType Directory -Force -Path $dest | Out-Null
  # download idle/die anims + walk from rig
  Invoke-Meshy @("download","anim",$idle,"--out",$dest,"--prefix","idle_","--filter","glb")
  Invoke-Meshy @("download","anim",$die,"--out",$dest,"--prefix","die_","--filter","glb")
  Invoke-Meshy @("download","rig",$rig,"--out",$dest,"--prefix","rig_","--filter","walking_glb")

  # normalize names
  Get-ChildItem $dest -Filter "idle_*.glb" | Select-Object -First 1 | ForEach-Object { Move-Item $_.FullName (Join-Path $dest "idle.glb") -Force }
  Get-ChildItem $dest -Filter "die_*.glb" | Select-Object -First 1 | ForEach-Object { Move-Item $_.FullName (Join-Path $dest "die.glb") -Force }
  $walk = Get-ChildItem $dest -Filter "rig_*.glb" | Where-Object { $_.Name -match "walk" } | Select-Object -First 1
  if ($walk) { Move-Item $walk.FullName (Join-Path $dest "walk.glb") -Force }

  $results[$id] = [pscustomobject]@{ image3d=$i3; remesh=$remesh; rig=$rig; idle=$idle; die=$die }
  Write-Host "DONE $id"
}

# Rewrite job doc table rows
$lines = Get-Content $jobDoc
$out = foreach ($line in $lines) {
  $matched = $false
  foreach ($id in $ids) {
    if ($line -match "^\| $id \|") {
      $r = $results[$id]
      $style = ($line -split '\|')[2].Trim()
      $note = if ($line -match 'pack risk') { ' pack risk' } else { '' }
      "| $id | $style | $($r.image3d) | $($r.remesh) | $($r.rig) | $($r.idle) | $($r.die) |$note |"
      $matched = $true
      break
    }
  }
  if (-not $matched) { $line }
}
$out | Set-Content $jobDoc -Encoding utf8
Add-Content $jobDoc "`n**All 10 style chains complete $(Get-Date -Format o).**`n"
Write-Host "ALL COMPLETE"
