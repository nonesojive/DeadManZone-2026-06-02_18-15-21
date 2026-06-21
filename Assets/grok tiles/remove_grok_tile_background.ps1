# Removes outer dark gutters + inner hollow blacks from split Grok tile PNGs.
Add-Type -AssemblyName System.Drawing

$EdgeThreshold = 42
$InnerThreshold = 20
$dir = $PSScriptRoot

function Test-DarkPixel([System.Drawing.Color]$c, [int]$threshold) {
    $max = [Math]::Max($c.R, [Math]::Max($c.G, $c.B))
    return ($max -le $threshold)
}

function Remove-GrokTileBackground([System.Drawing.Bitmap]$bmp) {
    $w = $bmp.Width
    $h = $bmp.Height
    $remove = New-Object 'bool[,]' $h, $w

    for ($y = 0; $y -lt $h; $y++) {
        for ($x = 0; $x -lt $w; $x++) {
            if (Test-DarkPixel $bmp.GetPixel($x, $y) $InnerThreshold) {
                $remove[$y, $x] = $true
            }
        }
    }

    $queue = New-Object System.Collections.Generic.Queue[object]
    function Enqueue-IfDark([int]$x, [int]$y) {
        if ($x -lt 0 -or $y -lt 0 -or $x -ge $w -or $y -ge $h) { return }
        if ($remove[$y, $x]) { return }
        if (-not (Test-DarkPixel $bmp.GetPixel($x, $y) $EdgeThreshold)) { return }
        $remove[$y, $x] = $true
        $queue.Enqueue(@($x, $y))
    }

    for ($x = 0; $x -lt $w; $x++) {
        Enqueue-IfDark $x 0
        Enqueue-IfDark $x ($h - 1)
    }
    for ($y = 0; $y -lt $h; $y++) {
        Enqueue-IfDark 0 $y
        Enqueue-IfDark ($w - 1) $y
    }

    while ($queue.Count -gt 0) {
        $p = $queue.Dequeue()
        Enqueue-IfDark ($p[0] - 1) $p[1]
        Enqueue-IfDark ($p[0] + 1) $p[1]
        Enqueue-IfDark $p[0] ($p[1] - 1)
        Enqueue-IfDark $p[0] ($p[1] + 1)
    }

    for ($y = 0; $y -lt $h; $y++) {
        for ($x = 0; $x -lt $w; $x++) {
            if ($remove[$y, $x]) {
                $bmp.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(0, 0, 0, 0))
            }
        }
    }
}

for ($i = 1; $i -le 18; $i++) {
    $path = Join-Path $dir ("tile{0}.png" -f $i)
    if (-not (Test-Path $path)) { throw "Missing $path" }
    $bmp = [System.Drawing.Bitmap]::FromFile($path)
    try {
        Remove-GrokTileBackground $bmp
        $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
        Write-Host "Processed tile$i"
    }
    finally {
        $bmp.Dispose()
    }
}

Write-Host "Done. Run verify_grok_tiles.ps1 to confirm."
