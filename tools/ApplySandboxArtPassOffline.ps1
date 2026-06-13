# Applies sandbox art pass without Unity batchmode (icons + piece YAML references).
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$piecesDir = Join-Path $root "Assets\_Project\Data\Resources\DeadManZone\Pieces"
$catalogPath = Join-Path $root "Assets\_Project\Data\Resources\DeadManZone\SandboxArtCatalog.asset"
$catalogMetaPath = "$catalogPath.meta"
$catalogScriptGuid = "25b2cb9b20ab48249b87cc1cb432f6fa"
$iconSize = 256

function New-GuidHex { return ([guid]::NewGuid().ToString("N")) }

function Get-AssetGuid([string]$assetPath) {
    $meta = "$assetPath.meta"
    if (-not (Test-Path $meta)) { throw "Missing meta: $meta" }
    foreach ($line in Get-Content $meta) {
        if ($line -match '^guid: ([0-9a-f]+)$') { return $Matches[1] }
    }
    throw "No guid in $meta"
}

function PrefabRef([string]$relativeAssetPath) {
    if ([string]::IsNullOrWhiteSpace($relativeAssetPath)) { return "{fileID: 0}" }
    $full = Join-Path $root ($relativeAssetPath -replace '/', '\')
    $guid = Get-AssetGuid $full
    return "{fileID: 100100000, guid: $guid, type: 3}"
}

function SpriteRef([string]$relativeAssetPath) {
    $full = Join-Path $root ($relativeAssetPath -replace '/', '\')
    $guid = Get-AssetGuid $full
    return "{fileID: 21300000, guid: $guid, type: 3}"
}

function Ensure-SpriteMeta([string]$assetPath, [string]$guid) {
    $meta = "$assetPath.meta"
    if (Test-Path $meta) { return }
    @"
fileFormatVersion: 2
guid: $guid
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
  isReadable: 0
  streamingMipmaps: 0
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {x: 0.5, y: 0.5}
  spritePixelsToUnits: 100
  spriteBorder: {x: 0, y: 0, z: 0, w: 0}
  alphaUsage: 1
  alphaIsTransparency: 1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  maxTextureSize: 256
  textureCompression: 0
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 256
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    physicsShape: []
    bones: []
    spriteID: 5e97eb03825dee720800000000000000
    internalID: 0
    vertices: []
    indices:
    edges: []
    weights: []
    secondaryTextures: []
  assetBundleName:
  assetBundleVariant:
"@ | Set-Content -Path $meta -Encoding UTF8
}

function Write-PlaceholderPng([string]$absolutePath, [int]$paletteIndex) {
    $dir = Split-Path $absolutePath -Parent
    if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
    $template = Join-Path $root "Assets\BunkerSurvivalUI\Sprites\Icons\icon_toolbox.png"
    if (-not (Test-Path $template)) { throw "Template icon missing: $template" }
    Copy-Item $template $absolutePath -Force
}

function Ensure-Icon([string]$iconPath, [int]$index) {
    $absolute = Join-Path $root ($iconPath -replace '/', '\')
    if (-not (Test-Path $absolute)) {
        Write-PlaceholderPng $absolute $index
    }
    if (-not (Test-Path "$absolute.meta")) {
        Ensure-SpriteMeta $absolute (New-GuidHex)
    }
}

function Set-PieceArt([string]$pieceId, [string]$iconPath, [string]$prefabPath, [double]$scale, [double]$height) {
    $asset = Join-Path $piecesDir "$pieceId.asset"
    if (-not (Test-Path $asset)) { throw "Missing piece: $asset" }
    $content = Get-Content $asset -Raw
    $iconRef = SpriteRef $iconPath
    $prefabRef = PrefabRef $prefabPath

    $content = [regex]::Replace($content, 'icon: \{fileID: [^}]+\}', "icon: $iconRef")
    $content = [regex]::Replace($content, 'combatArenaPrefab: \{fileID: [^}]+\}', "combatArenaPrefab: $prefabRef")
    $content = [regex]::Replace($content, 'combatArenaModelScale: [0-9.]+', "combatArenaModelScale: $scale")
    $content = [regex]::Replace($content, 'combatArenaModelHeight: [0-9.]+', "combatArenaModelHeight: $height")
    Set-Content -Path $asset -Value $content -Encoding UTF8 -NoNewline
}

$entries = @(
    @{ Id = "conscript_rifleman"; Prefab = "Assets/Toon_Soldiers/ToonSoldiers_WW2/prefabs/TSww2_German_infantry.prefab"; Icon = "Assets/_Project/Art/Neutral/Renders/Icons/conscript_rifleman_icon.png"; Scale = 1; Height = 1.6 }
    @{ Id = "grenade_thrower"; Prefab = "Assets/Toon_Soldiers/ToonSoldiers_WW2/prefabs/TSww2_German_support.prefab"; Icon = "Assets/_Project/Art/Neutral/Renders/Icons/grenade_thrower_icon.png"; Scale = 1; Height = 1.6 }
    @{ Id = "field_medic"; Prefab = "Assets/Toon_Soldiers/ToonSoldiers_WW2/prefabs/TSww2_German_medic.prefab"; Icon = "Assets/_Project/Art/Neutral/Renders/Icons/field_medic_icon.png"; Scale = 1; Height = 1.6 }
    @{ Id = "armored_transport"; Prefab = "Assets/RTS_Modern_Combat_Vehicle_Pack_Free/ATV_N1/0_Prefabs/ATV_N1_Color_0_Prefab.prefab"; Icon = "Assets/_Project/Art/Neutral/Renders/Icons/armored_transport_icon.png"; Scale = 0.9; Height = 1.2 }
    @{ Id = "mobile_cannon"; Prefab = "Assets/RTS_Modern_Combat_Vehicle_Pack_Free/MSH_N2/0_Prefabs/MSH_N2_Color_0_Prefab.prefab"; Icon = "Assets/_Project/Art/Neutral/Renders/Icons/mobile_cannon_icon.png"; Scale = 0.85; Height = 1.4 }
    @{ Id = "neutral_supply_depot"; Prefab = "Assets/_Project/Presentation/Combat/Arena/Prefabs/Buildings/ArenaBuilding_SupplyDepot.prefab"; Icon = "Assets/BunkerSurvivalUI/Sprites/Icons/icon_fuel_canister.png"; Scale = 1; Height = 0 }
    @{ Id = "neutral_field_gun"; Prefab = "Assets/_Project/Presentation/Combat/Arena/Prefabs/Buildings/ArenaBuilding_FieldGun.prefab"; Icon = "Assets/BunkerSurvivalUI/Sprites/Icons/icon_generator_part.png"; Scale = 1; Height = 0 }
    @{ Id = "shock_trooper"; Prefab = "Assets/Toon_Soldiers/ToonSoldiers_WW2/prefabs/TSww2_German_officer.prefab"; Icon = "Assets/_Project/Art/Sandbox/Renders/Icons/shock_trooper_icon.png"; Scale = 1; Height = 1.6 }
    @{ Id = "neutral_mortar_team"; Prefab = "Assets/Toon_Soldiers/ToonSoldiers_WW2/prefabs/TSww2_German_support.prefab"; Icon = "Assets/_Project/Art/Sandbox/Renders/Icons/neutral_mortar_team_icon.png"; Scale = 1; Height = 1.6 }
    @{ Id = "marksman_squad"; Prefab = "Assets/Toon_Soldiers/ToonSoldiers_WW2/prefabs/TSww2_German_sniper.prefab"; Icon = "Assets/_Project/Art/Sandbox/Renders/Icons/marksman_squad_icon.png"; Scale = 1; Height = 1.6 }
    @{ Id = "ironmarch_hq"; Prefab = "Assets/_Project/Presentation/Combat/Arena/Prefabs/Buildings/ArenaBuilding_Hq.prefab"; Icon = "Assets/BunkerSurvivalUI/Sprites/Icons/icon_bunker_map.png"; Scale = 1; Height = 0 }
    @{ Id = "rifle_squad"; Prefab = "Assets/Toon_Soldiers/ToonSoldiers_WW2/prefabs/TSww2_German_infantry.prefab"; Icon = "Assets/_Project/Art/Sandbox/Renders/Icons/rifle_squad_icon.png"; Scale = 1; Height = 1.6 }
    @{ Id = "diesel_walker"; Prefab = "Assets/RTS_Modern_Combat_Vehicle_Pack_Free/FA_N26/0_Prefabs/FA_N26_Color_0_Prefab.prefab"; Icon = "Assets/_Project/Art/Sandbox/Renders/Icons/diesel_walker_icon.png"; Scale = 0.9; Height = 1.4 }
    @{ Id = "radio_array"; Prefab = ""; Icon = "Assets/BunkerSurvivalUI/Sprites/Icons/icon_emergency_radio.png"; Scale = 1; Height = 0 }
    @{ Id = "mg_team"; Prefab = "Assets/Toon_Soldiers/ToonSoldiers_WW2/prefabs/TSww2_German_support.prefab"; Icon = "Assets/_Project/Art/Sandbox/Renders/Icons/mg_team_icon.png"; Scale = 1; Height = 1.6 }
    @{ Id = "field_gun_nest"; Prefab = "Assets/_Project/Presentation/Combat/Arena/Prefabs/Buildings/ArenaBuilding_FieldGun.prefab"; Icon = "Assets/BunkerSurvivalUI/Sprites/Icons/icon_generator_part.png"; Scale = 1; Height = 0 }
    @{ Id = "supply_depot"; Prefab = "Assets/_Project/Presentation/Combat/Arena/Prefabs/Buildings/ArenaBuilding_SupplyDepot.prefab"; Icon = "Assets/BunkerSurvivalUI/Sprites/Icons/icon_fuel_canister.png"; Scale = 1; Height = 0 }
    @{ Id = "field_workshop"; Prefab = "Assets/_Project/Presentation/Combat/Arena/Prefabs/Buildings/ArenaBuilding_SupplyDepot.prefab"; Icon = "Assets/BunkerSurvivalUI/Sprites/Icons/icon_toolbox.png"; Scale = 1; Height = 0 }
    @{ Id = "mobile_artillery"; Prefab = "Assets/RTS_Modern_Combat_Vehicle_Pack_Free/MSH_N2/0_Prefabs/MSH_N2_Color_1_Prefab.prefab"; Icon = "Assets/_Project/Art/Sandbox/Renders/Icons/mobile_artillery_icon.png"; Scale = 0.85; Height = 1.4 }
    @{ Id = "ironmarch_heavy_tank"; Prefab = "Assets/RTS_Modern_Combat_Vehicle_Pack_Free/FA_N26/0_Prefabs/FA_N26_Color_1_Prefab.prefab"; Icon = "Assets/_Project/Art/Sandbox/Renders/Icons/ironmarch_heavy_tank_icon.png"; Scale = 0.95; Height = 1.5 }
    @{ Id = "ironmarch_mortar"; Prefab = "Assets/Toon_Soldiers/ToonSoldiers_WW2/prefabs/TSww2_German_support.prefab"; Icon = "Assets/_Project/Art/Sandbox/Renders/Icons/ironmarch_mortar_icon.png"; Scale = 1; Height = 1.6 }
    @{ Id = "ironmarch_engineer"; Prefab = "Assets/Toon_Soldiers/ToonSoldiers_WW2/prefabs/TSww2_German_medic.prefab"; Icon = "Assets/_Project/Art/Sandbox/Renders/Icons/ironmarch_engineer_icon.png"; Scale = 1; Height = 1.6 }
    @{ Id = "ironmarch_breacher"; Prefab = "Assets/Toon_Soldiers/ToonSoldiers_WW2/prefabs/TSww2_German_officer.prefab"; Icon = "Assets/_Project/Art/Sandbox/Renders/Icons/ironmarch_breacher_icon.png"; Scale = 1; Height = 1.6 }
    @{ Id = "ironmarch_sniper"; Prefab = "Assets/Toon_Soldiers/ToonSoldiers_WW2/prefabs/TSww2_German_sniper.prefab"; Icon = "Assets/_Project/Art/Sandbox/Renders/Icons/ironmarch_sniper_icon.png"; Scale = 1; Height = 1.6 }
    @{ Id = "ironmarch_defender"; Prefab = "Assets/Toon_Soldiers/ToonSoldiers_WW2/prefabs/TSww2_German_infantry.prefab"; Icon = "Assets/_Project/Art/Sandbox/Renders/Icons/ironmarch_defender_icon.png"; Scale = 1; Height = 1.6 }
)

for ($i = 0; $i -lt $entries.Count; $i++) {
    Ensure-Icon $entries[$i].Icon $i
    Set-PieceArt $entries[$i].Id $entries[$i].Icon $entries[$i].Prefab $entries[$i].Scale $entries[$i].Height
}

$catalogGuid = if (Test-Path $catalogMetaPath) { Get-AssetGuid $catalogPath } else { New-GuidHex }
if (-not (Test-Path $catalogMetaPath)) {
    @"
fileFormatVersion: 2
guid: $catalogGuid
NativeFormatImporter:
  externalObjects: {}
  mainObjectFileID: 11400000
  userData:
  assetBundleName:
  assetBundleVariant:
"@ | Set-Content -Path $catalogMetaPath -Encoding UTF8
}

$entryYaml = ($entries | ForEach-Object {
    $snapshot = $_.Icon -like "*Sandbox/Renders*"
    @"
  - pieceId: $($_.Id)
    iconAssetPath: $($_.Icon)
    combatArenaPrefabPath: $($_.Prefab)
    combatArenaModelScale: $($_.Scale)
    combatArenaModelHeight: $($_.Height)
    snapshotIconFromPrefab: $(if ($snapshot) { 1 } else { 0 })
"@
}) -join "`n"

@"
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: $catalogScriptGuid, type: 3}
  m_Name: SandboxArtCatalog
  m_EditorClassIdentifier:
  entries:
$entryYaml
"@ | Set-Content -Path $catalogPath -Encoding UTF8 -NoNewline

Write-Host "Applied sandbox art to $($entries.Count) pieces and wrote catalog."
