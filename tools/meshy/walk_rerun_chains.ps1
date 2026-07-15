# Walk Phase 0 cel-rerun Meshy chains for 3 variants.
$ErrorActionPreference = "Stop"
$env:MESHY_API_KEY = [Environment]::GetEnvironmentVariable('MESHY_API_KEY','User')
if (-not $env:MESHY_API_KEY) { throw "MESHY_API_KEY missing" }

$Here = Split-Path -Parent $MyInvocation.MyCommand.Path
$Repo = Resolve-Path (Join-Path $Here "..\..")
$Client = Join-Path $Here "meshy_client.py"
$Log = Join-Path $Here "walk_rerun_chains.log"

function Log($msg) {
    $line = "$(Get-Date -Format 'HH:mm:ss') $msg"
    Add-Content -Path $Log -Value $line
    Write-Output $line
}

function Invoke-Meshy {
    param([string[]]$MeshyArgs)
    $out = & python $Client @MeshyArgs 2>&1
    if ($LASTEXITCODE -ne 0) { throw "meshy_client failed: $($MeshyArgs -join ' ') -> $out" }
    return ($out | Select-Object -Last 1).ToString().Trim()
}

$variants = [ordered]@{
    cel_mid = "019f6331-e7bd-717e-a4e8-cb68fcadd3fe"
    cel_stocky_v2 = "019f6331-eb83-7e1b-b39e-86502aa3d89a"
    cel_real_v2 = "019f6331-e498-7e1a-a8d2-a375906b00f3"
}

foreach ($name in $variants.Keys) {
    $img3d = $variants[$name]
    Log "=== $name image3d $img3d ==="
    Invoke-Meshy -MeshyArgs @("wait","image3d",$img3d) | Out-Null
    $remesh = Invoke-Meshy -MeshyArgs @("remesh",$img3d,"--polycount","12000")
    Log "$name remesh $remesh"
    Invoke-Meshy -MeshyArgs @("wait","remesh",$remesh) | Out-Null
    $rig = Invoke-Meshy -MeshyArgs @("rig",$remesh,"--height","1.8")
    Log "$name rig $rig"
    Invoke-Meshy -MeshyArgs @("wait","rig",$rig) | Out-Null
    $idle = Invoke-Meshy -MeshyArgs @("animate",$rig,"0")
    $die = Invoke-Meshy -MeshyArgs @("animate",$rig,"8")
    Log "$name anim idle $idle die $die"
    Invoke-Meshy -MeshyArgs @("wait","anim",$idle) | Out-Null
    Invoke-Meshy -MeshyArgs @("wait","anim",$die) | Out-Null
    $outDir = Join-Path $Repo "tools\meshy\units\conscript_rifleman\rerun\$name"
    New-Item -ItemType Directory -Force -Path $outDir | Out-Null
    Invoke-Meshy -MeshyArgs @("download","anim",$idle,"--out",$outDir,"--prefix","idle_","--filter","glb") | Out-Null
    Invoke-Meshy -MeshyArgs @("download","anim",$die,"--out",$outDir,"--prefix","die_","--filter","glb") | Out-Null
    Invoke-Meshy -MeshyArgs @("download","rig",$rig,"--out",$outDir,"--prefix","rig_","--filter","walk") | Out-Null
    Get-ChildItem $outDir -Filter "idle_*.glb" | ForEach-Object { Copy-Item $_.FullName (Join-Path $outDir "idle.glb") -Force }
    Get-ChildItem $outDir -Filter "die_*.glb" | ForEach-Object { Copy-Item $_.FullName (Join-Path $outDir "die.glb") -Force }
    Get-ChildItem $outDir -Filter "*walk*.glb" | Select-Object -First 1 | ForEach-Object { Copy-Item $_.FullName (Join-Path $outDir "walk.glb") -Force }
    Log "$name DONE -> $outDir"
}

Log "ALL RERUN CHAINS COMPLETE"
