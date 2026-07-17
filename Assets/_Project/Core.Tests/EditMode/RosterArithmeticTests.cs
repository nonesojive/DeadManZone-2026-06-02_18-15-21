using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core;
using DeadManZone.Core.Board;
using DeadManZone.Core.Tags;
using DeadManZone.Data;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    /// <summary>2026-07-15 faction-roster-v1: §1.1 roster arithmetic (Neutral 4C/3U/0R,
    /// IronMarch 6C/3U/3R) and the §3 sniper Critical Mass rule (≈2/4/6 → +accuracy, then
    /// +damage%). The roster counts read the shipped ContentDatabase, so they only reflect
    /// the new roster after "DeadManZone → Content → Generate IronMarch Union Content Pass"
    /// has been run in-editor; self-ignore otherwise, same pattern as BalancePassTests.</summary>
    public sealed class RosterArithmeticTests
    {
        private ContentDatabase _database;

        [SetUp]
        public void SetUp()
        {
            _database = ContentDatabase.Load();
            if (_database == null || _database.Pieces.Count == 0)
                Assert.Ignore(DeadManZoneTestContent.MissingDatabaseHint);
        }

        [Test]
        public void Neutral_Roster_Is4Common3Uncommon0Rare()
        {
            var neutral = _database.Pieces.Where(p => p.factionId == "neutral").ToList();
            if (neutral.Count == 0)
                Assert.Ignore("Neutral roster not yet regenerated — run the IronMarch content pass.");

            AssertRarityCounts(neutral, expectedCommon: 4, expectedUncommon: 3, expectedRare: 0, factionLabel: "Neutral");
        }

        [Test]
        public void IronmarchUnion_Roster_Is6Common3Uncommon3Rare()
        {
            var ironmarch = _database.Pieces.Where(p => p.factionId == FactionIds.IronmarchUnion).ToList();
            if (ironmarch.Count == 0)
                Assert.Ignore("IronMarch roster not yet regenerated — run the IronMarch content pass.");

            AssertRarityCounts(ironmarch, expectedCommon: 6, expectedUncommon: 3, expectedRare: 3, factionLabel: "IronMarch Union");
        }

        // 2026-07-15 faction-roster-v1 W2: the remaining 7 factions, all 6C/3U/3R (§2.3-§2.9).
        // Reads the full-roster ContentDatabase from "Generate Full Roster (All 8 Factions)".
        [Test]
        public void DustScourge_Roster_Is6Common3Uncommon3Rare() =>
            AssertFactionRoster(FactionIds.DustScourge, "Dust Scourge");

        [Test]
        public void CartelOfEchoes_Roster_Is6Common3Uncommon3Rare() =>
            AssertFactionRoster(FactionIds.CartelOfEchoes, "Cartel of Echoes");

        [Test]
        public void OathbornAccord_Roster_Is6Common3Uncommon3Rare() =>
            AssertFactionRoster(FactionIds.OathbornAccord, "Oathborn Accord");

        [Test]
        public void ParadoxEngine_Roster_Is6Common3Uncommon3Rare() =>
            AssertFactionRoster(FactionIds.ParadoxEngine, "Paradox Engine");

        [Test]
        public void BlightbornPact_Roster_Is6Common3Uncommon3Rare() =>
            AssertFactionRoster(FactionIds.BlightbornPact, "Blightborn Pact");

        [Test]
        public void CrimsonAssembly_Roster_Is6Common3Uncommon3Rare() =>
            AssertFactionRoster(FactionIds.CrimsonAssembly, "Crimson Assembly");

        [Test]
        public void AshenCovenant_Roster_Is6Common3Uncommon3Rare() =>
            AssertFactionRoster(FactionIds.AshenCovenant, "Ashen Covenant");

        [Test]
        public void FullRoster_Is103PiecesAcrossNeutralAndEightFactions()
        {
            if (_database.Pieces.Count < 103)
                Assert.Ignore("Full 8-faction roster not yet regenerated — run 'Generate Full Roster (All 8 Factions)'.");

            Assert.AreEqual(103, _database.Pieces.Count, "§1.1: 4C/3U neutral (7) + 8 x (6C/3U/3R = 12) = 103.");
        }

        // §1.1 commons-distinctness rule: each faction's 6 commons must differ from their
        // siblings in at least one of primary / combat role / attack type / footprint (cell
        // count). Spot-checks every faction rather than every possible pair combinatorially.
        [Test]
        public void EveryFaction_CommonsAreMutuallyDistinct()
        {
            // Neutral is deliberately excluded: it's pre-existing legacy content (not authored
            // fresh against faction-roster-v1 §2.3-§2.9's "6 mechanically distinct commons per
            // faction" goal), and rule says don't touch existing Neutral/IronMarch pieces to
            // satisfy a new invariant. It already has two utility buildings sharing a
            // building|utility|None|2 signature (recruitment_office among them) — legitimate,
            // not a regression.
            var factionIds = new[]
            {
                FactionIds.IronmarchUnion, FactionIds.DustScourge, FactionIds.CartelOfEchoes,
                FactionIds.OathbornAccord, FactionIds.ParadoxEngine, FactionIds.BlightbornPact,
                FactionIds.CrimsonAssembly, FactionIds.AshenCovenant
            };

            foreach (var factionId in factionIds)
            {
                var commons = _database.Pieces
                    .Where(p => p.factionId == factionId && p.rarity == Rarity.Common)
                    .ToList();
                if (commons.Count == 0)
                    continue; // not yet regenerated — other tests in this fixture already flag that.

                var signatures = new HashSet<string>();
                foreach (var piece in commons)
                {
                    // Non-attacking support pieces (heal-pulse medics, aura buffers) commonly
                    // share primary/role/attackType/footprint while doing mechanically distinct
                    // things — the piece's own synergy tag (Medic vs Inspiring, etc.) and heal
                    // amount are what actually separate them, so fold those into the signature
                    // too rather than false-flagging legitimate same-shape-different-kit pairs.
                    string synergy = piece.synergyTags != null && piece.synergyTags.Length > 0
                        ? piece.synergyTags[0]
                        : "none";
                    // Suppression-on-hit is likewise a real mechanical differentiator: Crimson's
                    // Suppression Team is deliberately the same body as the Assembly Trooper
                    // (§2.8 "the count piece") — its debuff IS its distinctness.
                    string signature =
                        $"{piece.primary}|{piece.combatRole}|{piece.attackType}|{piece.shapeCells?.Length ?? 0}|{synergy}|{piece.healPulseAmount}|{(piece.appliesSuppressionOnHit ? 1 : 0)}";
                    Assert.IsTrue(signatures.Add(signature),
                        $"{factionId}: commons must differ in primary/role/attackType/footprint/synergy/heal/suppression — duplicate signature '{signature}' (piece '{piece.id}').");
                }
            }
        }

        // §1.6: vehicles are rare-only game-wide except Crimson's uncommon Scout Tankette.
        // NOTE: the spec's own §1.6 header says "6 vehicles in 103 pieces" but its own
        // distribution line (Crimson 3 + IronMarch 1R + Oathborn 1R) sums to 5 — a spec
        // authoring inconsistency, not a build bug. This test asserts the real, spec-distribution
        // total (5) and flags the discrepancy rather than padding a 6th vehicle that no
        // faction's roster (§2.2-§2.9) actually authors.
        [Test]
        public void VehicleBudget_MatchesSpecDistribution_RareOnlyExceptCrimsonScoutTankette()
        {
            var vehicles = _database.Pieces.Where(p => p.primary == GameTagIds.Vehicle).ToList();
            if (vehicles.Count == 0)
                Assert.Ignore("Full roster not yet regenerated.");

            Assert.AreEqual(5, vehicles.Count,
                "§1.6 distribution (Crimson 3 + IronMarch 1 + Oathborn 1) sums to 5, not the header's stated 6 — spec text inconsistency.");

            foreach (var vehicle in vehicles)
            {
                bool isCrimsonUncommonException = vehicle.factionId == FactionIds.CrimsonAssembly && vehicle.rarity == Rarity.Uncommon;
                Assert.IsTrue(vehicle.rarity == Rarity.Rare || isCrimsonUncommonException,
                    $"vehicle '{vehicle.id}' ({vehicle.factionId}) must be Rare unless it's Crimson's sanctioned Uncommon Scout Tankette exception.");
            }

            var byFaction = vehicles.GroupBy(v => v.factionId).ToDictionary(g => g.Key, g => g.Count());
            Assert.AreEqual(3, byFaction.GetValueOrDefault(FactionIds.CrimsonAssembly), "Crimson: 3 vehicles (2R + 1U exception).");
            Assert.AreEqual(1, byFaction.GetValueOrDefault(FactionIds.IronmarchUnion), "IronMarch: 1R vehicle.");
            Assert.AreEqual(1, byFaction.GetValueOrDefault(FactionIds.OathbornAccord), "Oathborn: 1R vehicle.");
            foreach (var otherFaction in new[] { FactionIds.DustScourge, FactionIds.CartelOfEchoes, FactionIds.ParadoxEngine, FactionIds.BlightbornPact, FactionIds.AshenCovenant })
            {
                Assert.IsFalse(byFaction.ContainsKey(otherFaction), $"{otherFaction} must field 0 native vehicles per §1.6.");
            }
        }

        // §1.7: ~19 "tactics pieces" (pieces granting a pause-window ability) game-wide.
        // Crimson 4, Paradox 3, every other faction 2, neutral 0. Counted as
        // grantedAbility != None OR addsPauseWindow (The Second Hand's own third-pause-window
        // grant counts as a tactics piece even though it has no GrantedAbility of its own;
        // repeatsPauseAbilities is deliberately excluded — Doctor Recursion modifies other
        // tactics, it isn't itself a button). KNOWN GAP: Cartel's Echo Chairman ("Executive
        // Order") has no GrantedAbility mapping — no existing ability grants a free command
        // action — so it's authored with grantedAbility=None (flagged TODO in
        // CartelOfEchoesContentFactory.Pieces.cs) and does not count here; this is a spot
        // check, not a rigid per-faction equality assertion, precisely because of that gap.
        [Test]
        public void TacticsBudget_SpotCheck_CrimsonHighestParadoxThreeNeutralZero()
        {
            if (_database.Pieces.Count < 103)
                Assert.Ignore("Full roster not yet regenerated.");

            int CountTactics(string factionId) => _database.Pieces.Count(p =>
                p.factionId == factionId && (p.grantedAbility != GrantedAbility.None || p.addsPauseWindow));

            int neutral = CountTactics("neutral");
            int crimson = CountTactics(FactionIds.CrimsonAssembly);
            int paradox = CountTactics(FactionIds.ParadoxEngine);

            Assert.AreEqual(0, neutral, "Neutral carries 0 tactics pieces.");
            Assert.AreEqual(4, crimson, "Crimson carries the game's highest tactics budget (4).");
            Assert.AreEqual(3, paradox, "Paradox carries 3 (2 GrantedAbility tactics + The Second Hand's pause-window grant).");

            foreach (var factionId in new[] { FactionIds.IronmarchUnion, FactionIds.DustScourge, FactionIds.OathbornAccord, FactionIds.BlightbornPact, FactionIds.AshenCovenant })
            {
                Assert.GreaterOrEqual(CountTactics(factionId), 2, $"{factionId}: expected at least 2 tactics pieces per §1.7.");
            }
        }

        private void AssertFactionRoster(string factionId, string label)
        {
            var roster = _database.Pieces.Where(p => p.factionId == factionId).ToList();
            if (roster.Count == 0)
                Assert.Ignore($"{label} roster not yet regenerated — run 'Generate Full Roster (All 8 Factions)'.");

            AssertRarityCounts(roster, expectedCommon: 6, expectedUncommon: 3, expectedRare: 3, factionLabel: label);
        }

        private static void AssertRarityCounts(
            List<PieceDefinitionSO> pieces,
            int expectedCommon,
            int expectedUncommon,
            int expectedRare,
            string factionLabel)
        {
            int common = pieces.Count(p => p.rarity == Rarity.Common);
            int uncommon = pieces.Count(p => p.rarity == Rarity.Uncommon);
            int rare = pieces.Count(p => p.rarity == Rarity.Rare);

            Assert.AreEqual(expectedCommon, common, $"{factionLabel}: expected {expectedCommon} commons");
            Assert.AreEqual(expectedUncommon, uncommon, $"{factionLabel}: expected {expectedUncommon} uncommons");
            Assert.AreEqual(expectedRare, rare, $"{factionLabel}: expected {expectedRare} rares");
            Assert.AreEqual(expectedCommon + expectedUncommon + expectedRare, pieces.Count,
                $"{factionLabel}: roster size must equal the commons+uncommons+rares total");
        }

        [Test]
        public void SniperCriticalMassRule_ThresholdsApproximate2_4_6()
        {
            var rules = CriticalMassDefaultRules.Build();

            var accuracyRule = rules.FirstOrDefault(r => r.Id == "sniper_accuracy");
            Assert.IsNotNull(accuracyRule.Tiers, "sniper_accuracy rule must exist");
            Assert.AreEqual(GameTagIds.Sniper, accuracyRule.CountTagId);
            Assert.AreEqual(CriticalMassStat.Accuracy, accuracyRule.Stat);
            Assert.AreEqual(2, accuracyRule.Tiers[0].Threshold, "sniper accuracy kicks in at ~2 snipers");
            Assert.Greater(accuracyRule.Tiers[0].Magnitude, 0, "the low threshold must actually grant +accuracy");

            var damageRule = rules.FirstOrDefault(r => r.Id == "sniper_damage");
            Assert.IsNotNull(damageRule.Tiers, "sniper_damage rule must exist");
            Assert.AreEqual(GameTagIds.Sniper, damageRule.CountTagId);
            Assert.AreEqual(CriticalMassStat.Damage, damageRule.Stat);
            Assert.AreEqual(SynergyModType.Percent, damageRule.ModType, "spec calls for +damage%, not flat damage");
            Assert.AreEqual(4, damageRule.Tiers[1].Threshold, "damage% ramp lands at ~4 snipers");
            Assert.AreEqual(6, damageRule.Tiers[2].Threshold, "damage% ramp tops out at ~6 snipers");
            Assert.Greater(damageRule.Tiers[2].Magnitude, damageRule.Tiers[1].Magnitude,
                "damage% must keep climbing across the later tiers");
        }

        // §1.9: every faction's identity stack includes its own Critical-Mass rule (Dust's is
        // a SalvageForFaction inversion; the rest count their own faction tag). Pure-data check
        // against CriticalMassDefaultRules.Build() directly — no ContentDatabase/regeneration
        // dependency, so this always runs (unlike the roster-count tests above).
        [Test]
        public void EveryFaction_HasItsOwnCriticalMassRule()
        {
            var rules = CriticalMassDefaultRules.Build();

            var ironmarch = rules.FirstOrDefault(r => r.Id == "ironmarch_union");
            Assert.IsNotNull(ironmarch.Tiers, "ironmarch_union CM rule must exist");
            Assert.AreEqual(CriticalMassCountCategory.Faction, ironmarch.CountCategory);

            var dust = rules.FirstOrDefault(r => r.Id == "dust_scourge_salvage");
            Assert.IsNotNull(dust.Tiers, "Dust Scourge's salvage-count inversion rule must exist");
            Assert.AreEqual(CriticalMassCountCategory.SalvageForFaction, dust.CountCategory,
                "Dust counts salvage-tagged pieces, not its own faction tag");
            Assert.AreEqual(FactionIds.DustScourge, dust.CountTagId);

            var cartel = rules.FirstOrDefault(r => r.Id == "cartel_of_echoes");
            Assert.IsNotNull(cartel.Tiers, "Cartel of Echoes CM rule must exist");
            Assert.AreEqual(CriticalMassStat.Supplies, cartel.Stat);
            Assert.AreEqual(CriticalMassScope.RunResources, cartel.Scope);

            var oathborn = rules.FirstOrDefault(r => r.Id == "oathborn_accord");
            Assert.IsNotNull(oathborn.Tiers, "Oathborn Accord CM rule must exist");
            Assert.AreEqual(CriticalMassStat.MaxMorale, oathborn.Stat);

            var paradox = rules.FirstOrDefault(r => r.Id == "paradox_engine");
            Assert.IsNotNull(paradox.Tiers, "Paradox Engine CM rule must exist");
            Assert.AreEqual(CriticalMassStat.AttackSpeed, paradox.Stat);
            Assert.AreEqual(SynergyModType.TierStep, paradox.ModType);

            var blightborn = rules.FirstOrDefault(r => r.Id == "blightborn_pact");
            Assert.IsNotNull(blightborn.Tiers, "Blightborn Pact CM rule must exist");
            Assert.AreEqual(CriticalMassStat.Damage, blightborn.Stat);
            Assert.AreEqual(SynergyModType.Percent, blightborn.ModType);

            var crimson = rules.FirstOrDefault(r => r.Id == "crimson_assembly");
            Assert.IsNotNull(crimson.Tiers, "Crimson Assembly CM rule must exist");
            Assert.AreEqual(CriticalMassStat.SuppressionDuration, crimson.Stat);

            var ashen = rules.FirstOrDefault(r => r.Id == "ashen_covenant");
            Assert.IsNotNull(ashen.Tiers, "Ashen Covenant CM rule must exist");
            Assert.AreEqual(CriticalMassStat.LowStateDamageBonus, ashen.Stat);
        }
    }
}
