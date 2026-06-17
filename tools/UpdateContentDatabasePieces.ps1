$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$piecesDir = Join-Path $root "Assets\_Project\Data\Resources\DeadManZone\Pieces"
$dbPath = Join-Path $root "Assets\_Project\Data\Resources\DeadManZone\ContentDatabase.asset"

$pieceIds = @(
    "ironmarch_hq","rifle_squad","diesel_walker","radio_array","mg_team","field_gun_nest","supply_depot","field_workshop","mobile_artillery",
    "ironmarch_heavy_tank","ironmarch_mortar","ironmarch_engineer","ironmarch_breacher","ironmarch_sniper","ironmarch_defender",
    "conscript_rifleman","grenade_thrower","field_medic","armored_transport","mobile_cannon","neutral_supply_depot","neutral_field_gun","shock_trooper","neutral_mortar_team","marksman_squad",
    "dust_hq","sand_raider","scrap_rig","toxin_launcher",
    "echo_hq","phantom_agent","signal_relay","resonance_cannon",
    "crimson_elite","crimson_tank","crimson_artillery",
    "wraith_stalker","wraith_phantom","wraith_bombard"
)

$refs = foreach ($id in $pieceIds) {
    $meta = Get-Content (Join-Path $piecesDir "$id.asset.meta") -Raw
    if ($meta -notmatch 'guid: ([0-9a-f]+)') { throw "Missing guid for $id" }
    "  - {fileID: 11400000, guid: $($Matches[1]), type: 2}"
}

$content = Get-Content $dbPath -Raw
$start = $content.IndexOf("  pieces:")
$end = $content.IndexOf("  factions:")
$before = $content.Substring(0, $start)
$after = $content.Substring($end)
$newPieces = "  pieces:`n" + ($refs -join "`n") + "`n"
($before + $newPieces + $after) | Set-Content $dbPath -Encoding UTF8 -NoNewline
Write-Host "Updated ContentDatabase with $($pieceIds.Count) pieces."
