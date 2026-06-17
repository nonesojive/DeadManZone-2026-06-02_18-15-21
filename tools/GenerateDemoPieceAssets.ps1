# Generates PieceDefinitionSO YAML assets for sandbox roster (run when Unity batchmode is unavailable).
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$piecesDir = Join-Path $root "Assets\_Project\Data\Resources\DeadManZone\Pieces"
$scriptGuid = "d5fb2f91481ca4c4888ee9094ae7faa6"

function New-GuidHex { return ([guid]::NewGuid().ToString("N")) }

function Format-Shape($cells) {
    if ($null -eq $cells) { return "  - {x: 0, y: 0}" }
    if ($cells.Count -eq 2 -and $cells[0] -isnot [System.Array]) {
        return "  - {x: $($cells[0]), y: $($cells[1])}"
    }

    ($cells | ForEach-Object {
        $c = @($_)
        "  - {x: $($c[0]), y: $($c[1])}"
    }) -join "`n"
}

function Cell($x, $y) { ,@($x, $y) }

$single = @(Cell 0 0)
$vertical = @(Cell 0 0; Cell 0 1)
$horizontal = @(Cell 0 0; Cell 1 0)
$transportL = @(Cell 0 0; Cell 0 1; Cell 1 1)
$siege = @(Cell 0 0; Cell 1 0; Cell 2 0; Cell 0 1; Cell 1 1; Cell 2 1)
$square = @(Cell 0 0; Cell 1 0; Cell 0 1; Cell 1 1)
$walker = $square
$hqShape = @(Cell 0 0; Cell 0 1; Cell 0 2; Cell 0 3; Cell 1 1; Cell 1 2; Cell 2 0)

function Write-PieceAsset($id, $fields) {
    $assetPath = Join-Path $piecesDir "$id.asset"
    $metaPath = "$assetPath.meta"
    if (-not (Test-Path $metaPath)) {
        $guid = New-GuidHex
        @"
fileFormatVersion: 2
guid: $guid
NativeFormatImporter:
  externalObjects: {}
  mainObjectFileID: 11400000
  userData:
  assetBundleName:
  assetBundleVariant:
"@ | Set-Content -Path $metaPath -Encoding UTF8
    }

    $shapeYaml = Format-Shape $fields.Shape
    $synergy = if ($fields.SynergyTags.Count -gt 0) {
        "  synergyTags:`n" + (($fields.SynergyTags | ForEach-Object { "  - $_" }) -join "`n")
    } else {
        "  synergyTags: []"
    }
    $ability = if ($fields.AbilityTags.Count -gt 0) {
        "  abilityTags:`n" + (($fields.AbilityTags | ForEach-Object { "  - $_" }) -join "`n")
    } else {
        "  abilityTags: []"
    }

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
  m_Script: {fileID: 11500000, guid: $scriptGuid, type: 3}
  m_Name: $id
  m_EditorClassIdentifier:
  id: $id
  displayName: $($fields.DisplayName)
  category: $($fields.Category)
  shapeCells:
$shapeYaml
  tags: []
  primary: $($fields.Primary)
  combatRole: $($fields.CombatRole)
  systemTag: $($fields.SystemTag)
$synergy
$ability
  flavorTags: []
  maxHp: $($fields.MaxHp)
  baseDamage: $($fields.BaseDamage)
  cooldownTicks: $($fields.CooldownTicks)
  goldCost: $($fields.GoldCost)
  requisitionCost: $($fields.RequisitionCost)
  manpowerCost: $($fields.ManpowerCost)
  musterPerShop: $($fields.MusterPerShop)
  shopModifiers: $($fields.ShopModifiers)
  commandActions: $($fields.CommandActions)
  shopLane: $($fields.ShopLane)
  includeInShopPool: $($fields.IncludeInShopPool)
  salvageChanceBonus: $($fields.SalvageChanceBonus)
  attackSpeed: $($fields.AttackSpeed)
  attackRange: $($fields.AttackRange)
  movementSpeed: $($fields.MovementSpeed)
  armorType: $($fields.ArmorType)
  attackType: $($fields.AttackType)
  grantedAbility: $($fields.GrantedAbility)
  factionId: $($fields.FactionId)
  icon: {fileID: 0}
  categoryTint: {r: $($fields.TintR), g: $($fields.TintG), b: $($fields.TintB), a: 1}
  cellSprites: []
"@ | Set-Content -Path $assetPath -Encoding UTF8
}

$single = @(@(0,0))
$vertical = @(@(0,0), @(0,1))
$horizontal = @(@(0,0), @(1,0))
$transportL = @(@(0,0), @(0,1), @(1,1))
$siege = @(@(0,0), @(1,0), @(2,0), @(0,1), @(1,1), @(2,1))
$square = @(@(0,0), @(1,0), @(0,1), @(1,1))
$walker = $square
$hqShape = @(@(0,0), @(0,1), @(0,2), @(0,3), @(1,1), @(1,2), @(2,0))

function P {
    param($id, $name, $category, $lane, $shape, $primary, $role, $system, $faction,
        $hp=10, $dmg=0, $cd=3, $gold=5, $req=0, $mp=1, $muster=0, $shopMod=0, $cmd=0,
        $ability=0, $salvage=0, $atkSpd=1, $range=1, $move=2, $armor=1, $atkType=1,
        $inShop=1, $synergy=@(), $abilityTags=@(), $tint=@(0.35,0.42,0.55))
    Write-PieceAsset $id @{
        DisplayName=$name; Category=$category; ShopLane=$lane; Shape=$shape
        Primary=$primary; CombatRole=$role; SystemTag=$system; FactionId=$faction
        MaxHp=$hp; BaseDamage=$dmg; CooldownTicks=$cd; GoldCost=$gold; RequisitionCost=$req
        ManpowerCost=$mp; MusterPerShop=$muster; ShopModifiers=$shopMod; CommandActions=$cmd
        GrantedAbility=$ability; SalvageChanceBonus=$salvage; AttackSpeed=$atkSpd
        AttackRange=$range; MovementSpeed=$move; ArmorType=$armor; AttackType=$atkType
        IncludeInShopPool=$inShop; SynergyTags=$synergy; AbilityTags=$abilityTags
        TintR=$tint[0]; TintG=$tint[1]; TintB=$tint[2]
    }
}

$neutralTint = @(0.35,0.42,0.55)
$ironTint = @(0.35,0.42,0.55)
$ironBuild = @(0.48,0.4,0.28)

# Neutral sandbox (10)
P "conscript_rifleman" "Conscript Rifleman" 1 2 $single "infantry" "assault" "combatant" "neutral" 60 12 3 4 0 6 -tint $neutralTint
P "grenade_thrower" "Grenade Thrower" 1 2 $vertical "infantry" "artillery" "combatant" "neutral" 70 24 4 5 0 8 0 0 0 1 0 1 2 1 1 2 1 @() @("grenadier")
P "armored_transport" "Armored Transport" 1 2 $transportL "vehicle" "tank" "combatant" "neutral" 120 8 3 8 0 8 -move 2 -armor 2 -ability 2
P "mobile_cannon" "Mobile Cannon" 2 0 $siege "vehicle" "artillery" "combatant" "neutral" 180 36 3 10 2 10 -atkSpd 0 -range 2 -move 1 -armor 3 -atkType 2 -ability 3
P "field_medic" "Field Medic" 1 1 $single "infantry" "support" "combatant" "neutral" 40 0 3 5 0 4 -atkType 0 -synergy @("medic")
P "neutral_supply_depot" "Neutral Supply Depot" 0 1 $single "building" "utility" "noncombatant" "neutral" 55 0 3 5 0 0 2 2 0 0 0 -salvage 5 -tint $ironBuild
P "neutral_field_gun" "Neutral Field Gun" 0 1 $vertical "building" "artillery" "combatant" "neutral" 120 22 3 7 -atkType 2 -range 1 -tint $ironBuild
P "shock_trooper" "Shock Trooper" 1 2 $single "infantry" "assault" "combatant" "neutral" 85 22 3 6 0 7 -move 3 -armor 2 -atkType 4 -abilityTags @("flamethrower")
P "neutral_mortar_team" "Mortar Team" 1 0 $horizontal "infantry" "artillery" "combatant" "neutral" 75 30 3 6 1 8 -atkSpd 0 -range 2 -atkType 2
P "marksman_squad" "Marksman Squad" 1 0 $single "infantry" "sniper" "combatant" "neutral" 55 26 4 6 0 5 -atkSpd 0 -range 2

# IronMarch (15)
P "ironmarch_hq" "IronMarch High Command" 0 1 $hqShape "building" "headquarters" "hq" "iron_vanguard" 80 0 3 0 0 8 -inShop 0 -tint $ironBuild
P "rifle_squad" "Rifle Squad" 1 2 $single "infantry" "assault" "combatant" "iron_vanguard" 100 20 3 5 0 10
P "diesel_walker" "Diesel Walker" 1 2 $walker "vehicle" "tank" "combatant" "iron_vanguard" 250 32 5 12 0 1 -move 1 -armor 3 -atkType 2
P "radio_array" "Radio Array" 0 1 $single "building" "utility" "noncombatant" "iron_vanguard" 120 0 3 7 -shopMod 4 -tint $ironBuild
P "mg_team" "MG Team" 1 2 $horizontal "infantry" "assault" "combatant" "iron_vanguard" 120 24 4 8 0 12 -atkSpd 2 -armor 2 -atkType 3
P "field_gun_nest" "Field Gun Nest" 0 1 $vertical "building" "artillery" "combatant" "iron_vanguard" 180 24 3 9 -range 2 -atkType 2 -tint $ironBuild
P "supply_depot" "Supply Depot" 0 1 $single "building" "utility" "noncombatant" "iron_vanguard" 50 0 3 6 0 0 3 2 -tint $ironBuild
P "field_workshop" "Field Workshop" 0 1 $single "building" "utility" "noncombatant" "iron_vanguard" 120 0 3 7 0 1 2 8 -tint $ironBuild
P "mobile_artillery" "Mobile Artillery" 2 0 $horizontal "vehicle" "artillery" "combatant" "iron_vanguard" 160 40 3 10 2 1 -range 2 -atkType 2
P "ironmarch_heavy_tank" "IronMarch Heavy Tank" 1 2 $square "vehicle" "tank" "combatant" "iron_vanguard" 320 36 6 14 0 14 -move 1 -armor 3 -atkType 2
P "ironmarch_mortar" "IronMarch Mortar" 1 0 $vertical "infantry" "artillery" "combatant" "iron_vanguard" 90 34 5 8 1 1 -atkSpd 0 -range 2 -atkType 2
P "ironmarch_engineer" "Combat Engineer" 1 1 $single "infantry" "support" "combatant" "iron_vanguard" 80 8 3 6 0 6 -atkType 5 -armor 2 -synergy @("mechanic")
P "ironmarch_breacher" "Assault Breacher" 1 2 $single "infantry" "assault" "combatant" "iron_vanguard" 110 26 3 7 0 8 -move 3 -armor 2 -atkType 4 -abilityTags @("flamethrower")
P "ironmarch_sniper" "Marksman Detachment" 1 0 $single "infantry" "sniper" "combatant" "iron_vanguard" 70 28 4 7 0 6 -atkSpd 0 -range 2
P "ironmarch_defender" "Bulwark Squad" 1 1 $horizontal "infantry" "defender" "combatant" "iron_vanguard" 140 14 3 6 0 10 -move 1 -armor 3 -atkType 5

# Other factions (unchanged counts)
P "dust_hq" "Nomad Command" 0 1 $horizontal "building" "headquarters" "hq" "dust_scourge" 220 0 3 0 0 0 -inShop 0 -tint @(0.52,0.39,0.18)
P "sand_raider" "Sand Raider" 1 2 $single "infantry" "assault" "combatant" "dust_scourge" 90 24 2 6 -move 3 -atkType 5
P "scrap_rig" "Scrap Rig" 1 2 $horizontal "vehicle" "tank" "combatant" "dust_scourge" 160 16 3 7 -armor 2 -atkType 3
P "toxin_launcher" "Toxin Launcher" 2 0 $single "vehicle" "artillery" "combatant" "dust_scourge" 100 32 3 9 2 1 -ability 1 -atkType 6

P "echo_hq" "Echo Nexus" 0 1 $horizontal "building" "headquarters" "hq" "cartel_of_echoes" 200 0 3 0 0 0 -inShop 0
P "phantom_agent" "Phantom Agent" 1 2 $single "infantry" "sniper" "combatant" "cartel_of_echoes" 70 24 2 7 -abilityTags @("stealth") -atkType 2
P "signal_relay" "Signal Relay" 0 1 $single "building" "utility" "noncombatant" "cartel_of_echoes" 110 0 3 6 0 0 1 4 -tint $ironBuild
P "resonance_cannon" "Resonance Cannon" 2 0 $horizontal "vehicle" "artillery" "combatant" "cartel_of_echoes" 130 40 3 10 2 1 -range 2 -atkType 2

P "crimson_elite" "Crimson Elite" 1 2 $single "infantry" "assault" "combatant" "crimson_legion" 120 24 3 0 -atkType 5 -armor 2
P "crimson_tank" "Crimson Tank" 1 2 $walker "vehicle" "tank" "combatant" "crimson_legion" 280 40 3 0 -armor 3 -atkType 2
P "crimson_artillery" "Crimson Battery" 0 1 $horizontal "building" "artillery" "combatant" "crimson_legion" 200 32 3 0 -range 2 -atkType 2 -tint $ironBuild

P "wraith_stalker" "Wraith Stalker" 1 2 $single "infantry" "sniper" "combatant" "ash_wraiths" 80 32 3 0 -abilityTags @("stealth") -atkType 2
P "wraith_phantom" "Ash Phantom" 1 2 $single "infantry" "assault" "combatant" "ash_wraiths" 100 24 3 0 -atkType 3
P "wraith_bombard" "Grave Bombard" 2 0 $horizontal "vehicle" "artillery" "combatant" "ash_wraiths" 150 40 3 0 -range 2 -atkType 2

Write-Host "Generated sandbox piece assets in $piecesDir"
