using System;
using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core;
using UnityEditor;
using UnityEngine;

namespace DeadManZone.Data.Editor
{
    /// <summary>
    /// 2026-07-15 faction-roster-v1 (Wave 2): the single menu item that generates the FULL
    /// 103-piece / 8-faction roster in one pass — Neutral + IronMarch (already owned by
    /// <see cref="IronmarchUnionContentFactory"/>) plus the 7 remaining factions' content
    /// factories, the Critical-Mass database (picks up the 7 new faction CM rules), and the
    /// live 10-fight enemy ladder (<see cref="IronmarchEnemyFactory"/>, unchanged — it only
    /// ever needed Neutral/IronMarch pieces, which remain present in the combined roster).
    ///
    /// IMPORTANT ordering lesson (found the hard way while landing this factory): pieces and
    /// factions are handled as two FULLY SEPARATE delete-flush-create-flush-reresolve stages,
    /// each self-contained. Earlier drafts deleted BOTH folders up front, then created both —
    /// if piece creation threw for any reason (including an unrelated transient race from an
    /// overlapping invocation), the method aborted having already wiped the Factions folder
    /// but never having recreated it, permanently orphaning the ContentDatabase's faction
    /// references until a fully-clean run happened to complete end to end. Keeping each type's
    /// delete immediately before ITS OWN recreation (mirroring IronmarchUnionContentFactory's
    /// original single-type pattern) means a failure on one side never touches the other.
    /// </summary>
    public static class AllFactionsContentFactory
    {
        private const string Root = "Assets/_Project/Data/Resources/DeadManZone";
        private const string PiecesRoot = Root + "/Pieces";
        private const string FactionsRoot = Root + "/Factions";
        private const string EnemiesRoot = Root + "/Enemies";

        private static readonly string[] FactionIdOrder =
        {
            FactionIds.IronmarchUnion, FactionIds.DustScourge, FactionIds.CartelOfEchoes,
            FactionIds.OathbornAccord, FactionIds.ParadoxEngine, FactionIds.BlightbornPact,
            FactionIds.CrimsonAssembly, FactionIds.AshenCovenant
        };

        [MenuItem(DeadManZoneEditorMenus.Content + "Generate Full Roster (All 8 Factions)")]
        public static void Generate()
        {
            EnsureFolder(Root);
            EnsureFolder(PiecesRoot);
            EnsureFolder(FactionsRoot);
            EnsureFolder(EnemiesRoot);

            var pieceArray = GenerateAllPieces();
            var factions = GenerateAllFactions();

            // Wave 5 (2026-07-17): IronMarch's array MUST come first — ContentDatabase.
            // GetEnemyTemplate(int) (the single-arg legacy lookup, still used by
            // GetEnemyTemplateForDifficulty's pre-M2-save fallback and by BalancePassTests'/
            // VerticalSliceRegressionTests' IronMarch-specific goldens) is a plain
            // FirstOrDefault over fightNumber with no faction filter, so whichever faction's
            // fight_N template lands first in this combined array is what it resolves to.
            // Every other call site that needs a SPECIFIC faction's template already uses the
            // faction-aware ContentDatabase.GetEnemyTemplate(int, string) overload.
            var enemies = IronmarchEnemyFactory.CreateAll(pieceArray)
                .Concat(DustScourgeEnemyFactory.CreateAll(pieceArray))
                .Concat(CartelOfEchoesEnemyFactory.CreateAll(pieceArray))
                .Concat(OathbornAccordEnemyFactory.CreateAll(pieceArray))
                .Concat(ParadoxEngineEnemyFactory.CreateAll(pieceArray))
                .Concat(BlightbornPactEnemyFactory.CreateAll(pieceArray))
                .Concat(CrimsonAssemblyEnemyFactory.CreateAll(pieceArray))
                .Concat(AshenCovenantEnemyFactory.CreateAll(pieceArray))
                .ToArray();

            // Written TWICE deliberately, with a full flush between: observed in testing, a
            // single SerializedObject write of the ContentDatabase's `factions` array right
            // after 8 FactionSO assets were freshly (re)created could embed a stale/interim
            // object reference for one or more slots even though a same-frame path-based
            // re-resolve (GenerateAllFactions above) already reported the correct data — i.e.
            // the object references handed to the writer were fine, but what got serialized to
            // ContentDatabase.asset's own YAML wasn't. Re-resolving fresh from disk and writing
            // again after a synchronous flush is what actually makes it stick.
            DemoContentDatabaseWriter.Write(pieceArray, factions, enemies);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var pieceArrayFinal = ReResolvePieces(pieceArray);
            var factionsFinal = ReResolveFactions(factions);
            DemoContentDatabaseWriter.Write(pieceArrayFinal, factionsFinal, enemies);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Pick up the 7 new faction Critical-Mass rules added to CriticalMassDefaultRules.
            CriticalMassDatabaseGenerator.Generate();

            // Re-assign placeholder shop icons: the delete-all above wipes every piece's icon
            // reference, which is exactly how the owner's first Oathborn run ended up with
            // blank shop cards (2026-07-17). Generator is idempotent — existing PNGs are
            // reused, only the SO references are re-linked.
            PlaceholderIconGenerator.Generate();

            Debug.Log($"Full faction roster generated: {pieceArrayFinal.Length} pieces, {factionsFinal.Length} factions, {enemies.Length} enemy templates.");
        }

        /// <summary>Stage 1, fully self-contained: delete every PieceDefinitionSO, flush, create
        /// all 103 from the 8 factories, flush again, then re-resolve every one strictly by its
        /// known asset path (never trust the in-memory return values — see class doc comment).</summary>
        private static PieceDefinitionSO[] GenerateAllPieces()
        {
            DeleteAllOfType<PieceDefinitionSO>(PiecesRoot);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            // SaveAssets after EVERY factory, not once at the end. Observed: creating all 84
            // new-faction pieces across 7 factories and flushing once left every one of them
            // serialized with default (blank) fields, while the IronMarch factory — which
            // flushes internally right after its own creations — always persisted correctly.
            // Isolated single-factory runs with an immediate SaveAssets also persist correctly.
            // Per-factory flush mirrors the one pattern proven to survive this AssetDatabase
            // behavior.
            var pieces = new List<PieceDefinitionSO>();
            void AddAndFlush(IEnumerable<PieceDefinitionSO> created)
            {
                pieces.AddRange(created);
                AssetDatabase.SaveAssets();
            }

            AddAndFlush(IronmarchUnionContentFactory.CreatePieces()); // Neutral (7) + IronMarch (12) = 19
            AddAndFlush(DustScourgeContentFactory.CreatePieces());    // 12
            AddAndFlush(CartelOfEchoesContentFactory.CreatePieces()); // 12
            AddAndFlush(OathbornAccordContentFactory.CreatePieces()); // 12
            AddAndFlush(ParadoxEngineContentFactory.CreatePieces());  // 12
            AddAndFlush(BlightbornPactContentFactory.CreatePieces()); // 12
            AddAndFlush(CrimsonAssemblyContentFactory.CreatePieces());// 12
            AddAndFlush(AshenCovenantContentFactory.CreatePieces());  // 12

            // Do NOT read ids (or validate) off the in-memory returns before the flush —
            // freshly created SO instances can be stale/blank until the synchronous import
            // completes (observed: last factory's pieces validating with empty ids). Flush
            // first, then resolve strictly from disk and validate what's actually there.
            int expectedCount = pieces.Count;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var resolved = AssetDatabase.FindAssets("t:PieceDefinitionSO", new[] { PiecesRoot })
                .Select(g => AssetDatabase.LoadAssetAtPath<PieceDefinitionSO>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(p => p != null)
                .OrderBy(p => p.id, StringComparer.Ordinal)
                .ToArray();
            if (resolved.Length != expectedCount)
                throw new InvalidOperationException(
                    $"Expected {expectedCount} piece assets after generation, found {resolved.Length} on disk.");

            ValidatePieceRoster(resolved);
            return resolved;
        }

        /// <summary>Stage 2, fully self-contained: delete every FactionSO, flush, create all 8
        /// from the 8 factories, flush again, then re-resolve every one strictly by its known
        /// asset path.</summary>
        private static FactionSO[] GenerateAllFactions()
        {
            DeleteAllOfType<FactionSO>(FactionsRoot);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            IronmarchUnionContentFactory.SaveIronmarchFaction();
            DustScourgeContentFactory.SaveFaction();
            CartelOfEchoesContentFactory.SaveFaction();
            OathbornAccordContentFactory.SaveFaction();
            ParadoxEngineContentFactory.SaveFaction();
            BlightbornPactContentFactory.SaveFaction();
            CrimsonAssemblyContentFactory.SaveFaction();
            AshenCovenantContentFactory.SaveFaction();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var resolved = new FactionSO[FactionIdOrder.Length];
            for (int i = 0; i < FactionIdOrder.Length; i++)
            {
                string path = $"{FactionsRoot}/{FactionIdOrder[i]}.asset";
                resolved[i] = AssetDatabase.LoadAssetAtPath<FactionSO>(path);
                if (resolved[i] == null)
                    throw new InvalidOperationException($"Missing faction asset at {path} after generation.");
                if (resolved[i].factionId != FactionIdOrder[i])
                    throw new InvalidOperationException(
                        $"Faction asset at {path} has factionId '{resolved[i].factionId}', expected '{FactionIdOrder[i]}' — stale/ghost object reference.");
            }

            return resolved;
        }

        /// <summary>Re-fetches every piece strictly by its known asset path, discarding whatever
        /// in-memory references were passed in. Used for the second write pass in <see cref="Generate"/>.</summary>
        private static PieceDefinitionSO[] ReResolvePieces(PieceDefinitionSO[] pieces)
        {
            var resolved = new PieceDefinitionSO[pieces.Length];
            for (int i = 0; i < pieces.Length; i++)
            {
                string path = $"{PiecesRoot}/{pieces[i].id}.asset";
                resolved[i] = AssetDatabase.LoadAssetAtPath<PieceDefinitionSO>(path);
                if (resolved[i] == null)
                    throw new InvalidOperationException($"Missing piece asset at {path} during re-resolve pass.");
            }
            return resolved;
        }

        /// <summary>Re-fetches every faction strictly by its known asset path, discarding whatever
        /// in-memory references were passed in. Used for the second write pass in <see cref="Generate"/>.</summary>
        private static FactionSO[] ReResolveFactions(FactionSO[] factions)
        {
            var resolved = new FactionSO[FactionIdOrder.Length];
            for (int i = 0; i < FactionIdOrder.Length; i++)
            {
                string path = $"{FactionsRoot}/{FactionIdOrder[i]}.asset";
                resolved[i] = AssetDatabase.LoadAssetAtPath<FactionSO>(path);
                if (resolved[i] == null)
                    throw new InvalidOperationException($"Missing faction asset at {path} during re-resolve pass.");
                if (resolved[i].factionId != FactionIdOrder[i])
                    throw new InvalidOperationException(
                        $"Faction asset at {path} has factionId '{resolved[i].factionId}', expected '{FactionIdOrder[i]}' during re-resolve pass.");
            }
            return resolved;
        }

        private static void ValidatePieceRoster(PieceDefinitionSO[] pieces)
        {
            const int expectedCount = 103;
            if (pieces.Length != expectedCount)
                throw new InvalidOperationException($"Expected {expectedCount} pieces but generated {pieces.Length}.");

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var piece in pieces)
            {
                if (piece == null || string.IsNullOrWhiteSpace(piece.id))
                    throw new InvalidOperationException("Generated a piece with a missing id.");

                if (!seen.Add(piece.id))
                    throw new InvalidOperationException($"Duplicate piece id generated: {piece.id}");
            }
        }

        private static void DeleteAllOfType<T>(string folder) where T : UnityEngine.Object
        {
            string typeFilter = $"t:{typeof(T).Name}";
            string[] existing = AssetDatabase.FindAssets(typeFilter, new[] { folder });
            for (int i = 0; i < existing.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(existing[i]);
                AssetDatabase.DeleteAsset(path);
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            var folder = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}
