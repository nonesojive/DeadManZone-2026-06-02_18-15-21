# ponytail: smallest runnable check — fails if tiles missing or fully opaque.
Add-Type -AssemblyName System.Drawing

$dir = $PSScriptRoot
$missing = @()
$badAlpha = @()

for ($i = 1; $i -le 18; $i++) {
    $path = Join-Path $dir ("tile{0}.png" -f $i)
    if (-not (Test-Path $path)) {
        $missing += $i
        continue
    }

    $bmp = [System.Drawing.Bitmap]::FromFile($path)
    try {
        $transparent = 0
        for ($y = 0; $y -lt $bmp.Height; $y++) {
            for ($x = 0; $x -lt $bmp.Width; $x++) {
                if ($bmp.GetPixel($x, $y).A -lt 16) { $transparent++ }
            }
        }
        if ($transparent -eq 0) { $badAlpha += $i }
    }
    finally {
        $bmp.Dispose()
    }
}

if ($missing.Count -gt 0) { throw "Missing tiles: $($missing -join ', ')" }
if ($badAlpha.Count -gt 0) { throw "Tiles without transparency: $($badAlpha -join ', ')" }

Write-Host "verify_grok_tiles: OK (18 tiles, all have alpha holes)"
