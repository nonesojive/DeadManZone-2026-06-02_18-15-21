# Crops Grok Batch 2 sheets into 256x256 neutral shop icons.
Add-Type -AssemblyName System.Drawing

$assetsFolder = $PSScriptRoot
foreach ($null in 1..4) { $assetsFolder = Split-Path $assetsFolder -Parent }
$Batch2 = Join-Path $assetsFolder "Grok Images\Isometric Batch 2"
$IconDir = Join-Path $assetsFolder "_Project\Art\Neutral\Renders\Icons"
$CellDir = Join-Path $assetsFolder "_Project\Art\Neutral\Renders\Cells"

$RosterFile = Join-Path $Batch2 "grok-image-2eb75a93-e52d-4847-ae43-03394588e5fd.jpg"
$VehicleFile = Join-Path $Batch2 "grok-image-129be410-7172-41f3-950b-9e1a668f383c.jpg"

$PieceIds = @(
    "conscript_rifleman",
    "grenade_thrower",
    "field_medic",
    "armored_transport",
    "mobile_cannon"
)

function Test-BackgroundPixel([System.Drawing.Color]$c) {
    $gray = ($c.R + $c.G + $c.B) / 3.0
    $spread = [Math]::Max([Math]::Abs($c.R - $c.G), [Math]::Max([Math]::Abs($c.G - $c.B), [Math]::Abs($c.R - $c.B)))
    return ($spread -lt 15) -and ($gray -gt 55) -and ($gray -lt 150)
}

function Remove-GrayBackground([System.Drawing.Bitmap]$bmp) {
    for ($y = 0; $y -lt $bmp.Height; $y++) {
        for ($x = 0; $x -lt $bmp.Width; $x++) {
            $p = $bmp.GetPixel($x, $y)
            if (Test-BackgroundPixel $p) {
                $bmp.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(0, 0, 0, 0))
            }
        }
    }
}

function Get-OpaqueBounds([System.Drawing.Bitmap]$bmp) {
    $minX = $bmp.Width; $minY = $bmp.Height; $maxX = 0; $maxY = 0
    for ($y = 0; $y -lt $bmp.Height; $y++) {
        for ($x = 0; $x -lt $bmp.Width; $x++) {
            if ($bmp.GetPixel($x, $y).A -lt 25) { continue }
            if ($x -lt $minX) { $minX = $x }
            if ($y -lt $minY) { $minY = $y }
            if ($x -gt $maxX) { $maxX = $x }
            if ($y -gt $maxY) { $maxY = $y }
        }
    }
    if ($maxX -le $minX) { return @{ X = 0; Y = 0; W = $bmp.Width; H = $bmp.Height } }
    return @{ X = $minX; Y = $minY; W = ($maxX - $minX + 1); H = ($maxY - $minY + 1) }
}

function Export-Icon([System.Drawing.Bitmap]$source, [int]$sliceIndex, [int]$sliceCount, [string]$pieceId, [int]$outputSize) {
    $sliceW = [int][Math]::Floor($source.Width / $sliceCount)
    $sliceX = $sliceIndex * $sliceW
    # Asymmetric inset: trim harder toward neighbors (fixes medic / vehicle bleed).
    # Keep insets modest — aggressive trimming clips unit silhouettes (crop manually if neighbors bleed).
    $leftInset = if ($sliceIndex -eq 0) { 0.04 } else { 0.08 }
    $rightInset = if ($sliceIndex -ge ($sliceCount - 1)) { 0.04 } else { 0.12 }
    $innerX = $sliceX + [int][Math]::Round($sliceW * $leftInset)
    $innerW = [Math]::Max($sliceW - [int][Math]::Round($sliceW * ($leftInset + $rightInset)), [int]($sliceW * 0.45))
    $crop = New-Object System.Drawing.Bitmap $innerW, $source.Height
    $g = [System.Drawing.Graphics]::FromImage($crop)
    $g.DrawImage($source, 0, 0, (New-Object System.Drawing.Rectangle $innerX, 0, $innerW, $source.Height), [System.Drawing.GraphicsUnit]::Pixel)
    $g.Dispose()

    Remove-GrayBackground $crop
    $b = Get-OpaqueBounds $crop
    $trim = New-Object System.Drawing.Bitmap $b.W, $b.H
    $g2 = [System.Drawing.Graphics]::FromImage($trim)
    $g2.DrawImage($crop, 0, 0, (New-Object System.Drawing.Rectangle $b.X, $b.Y, $b.W, $b.H), [System.Drawing.GraphicsUnit]::Pixel)
    $g2.Dispose()
    $crop.Dispose()

    $size = $outputSize
    $fill = 0.86
    $target = $size * $fill
    $scale = $target / [Math]::Max($trim.Width, $trim.Height)
    $drawW = [int][Math]::Round($trim.Width * $scale)
    $drawH = [int][Math]::Round($trim.Height * $scale)
    $offsetX = [int](($size - $drawW) / 2)
    $offsetY = [int](($size - $drawH) / 2)

    $out = New-Object System.Drawing.Bitmap $size, $size
    $g3 = [System.Drawing.Graphics]::FromImage($out)
    $g3.Clear([System.Drawing.Color]::FromArgb(0, 0, 0, 0))
    $g3.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g3.DrawImage($trim, $offsetX, $offsetY, $drawW, $drawH)
    $g3.Dispose()
    $trim.Dispose()

    return $out
}

function Save-Png([System.Drawing.Bitmap]$bmp, [string]$path) {
    $dir = Split-Path $path -Parent
    New-Item -ItemType Directory -Force -Path $dir | Out-Null
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Host "Wrote $path"
}

# 1x1 infantry get per-cell board sprites; 1x2 vehicles use footprint icon on the board.
$SingleCellPieces = @("conscript_rifleman", "grenade_thrower", "field_medic")

if (-not (Test-Path $RosterFile)) { throw "Missing roster: $RosterFile" }
New-Item -ItemType Directory -Force -Path $IconDir | Out-Null
New-Item -ItemType Directory -Force -Path $CellDir | Out-Null

$roster = [System.Drawing.Image]::FromFile($RosterFile)
$vehicle = $null
if (Test-Path $VehicleFile) { $vehicle = [System.Drawing.Image]::FromFile($VehicleFile) }

for ($i = 0; $i -lt $PieceIds.Length; $i++) {
    $id = $PieceIds[$i]
    if ($id -eq "armored_transport" -and $vehicle) {
        $icon = Export-Icon $vehicle 0 2 $id 256
    }
    elseif ($id -eq "mobile_cannon" -and $vehicle) {
        $icon = Export-Icon $vehicle 1 2 $id 256
    }
    else {
        $icon = Export-Icon $roster $i $PieceIds.Length $id 256
    }

    Save-Png $icon (Join-Path $IconDir ($id + "_icon.png"))

    if ($SingleCellPieces -contains $id) {
        $cellIcon = Export-Icon $roster $i $PieceIds.Length $id 128
        Save-Png $cellIcon (Join-Path $CellDir ($id + "_0_0.png"))
    }
}

$roster.Dispose()
if ($vehicle) { $vehicle.Dispose() }
Write-Host "Done. In Unity: DeadManZone -> Art -> Assign Neutral Icons From Renders"
Write-Host "Optional: DeadManZone -> Art -> Assign Neutral Cell Sprites From Renders"
