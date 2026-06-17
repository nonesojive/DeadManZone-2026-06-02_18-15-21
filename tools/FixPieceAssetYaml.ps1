$ErrorActionPreference = "Stop"
$dir = Join-Path (Split-Path -Parent $PSScriptRoot) "Assets\_Project\Data\Resources\DeadManZone\Pieces"
Get-ChildItem $dir -Filter "*.asset" | ForEach-Object {
    $text = [IO.File]::ReadAllText($_.FullName)
    $orig = $text
    $text = $text -replace "  synergyTags:\r?\n  \[\]", "  synergyTags: []"
    $text = $text -replace "  abilityTags:\r?\n  \[\]", "  abilityTags: []"
    $text = $text -replace "  flavorTags:\r?\n  \[\]", "  flavorTags: []"
    if ($text -ne $orig) {
        [IO.File]::WriteAllText($_.FullName, $text)
        Write-Host "Fixed $($_.Name)"
    }
}
