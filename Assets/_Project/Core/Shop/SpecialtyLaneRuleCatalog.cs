using System;
using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Content;
using DeadManZone.Core.Tags;

namespace DeadManZone.Core.Shop
{
    public sealed class SpecialtyLaneContext
    {
        private static readonly IReadOnlyList<string> EmptyTags = Array.Empty<string>();

        public IReadOnlyList<string> PreferredCombatRoles { get; init; } = EmptyTags;
        public IReadOnlyList<string> PreferredSynergyTags { get; init; } = EmptyTags;
        public bool PreferBuildings { get; init; }
        public bool PreferVehicles { get; init; }

        public bool IsWildcard =>
            PreferredCombatRoles.Count == 0
            && PreferredSynergyTags.Count == 0
            && !PreferBuildings
            && !PreferVehicles;
    }

    /// <summary>Board-composition rules that bias specialty lane shop offers.</summary>
    public static class SpecialtyLaneRuleCatalog
    {
        private const int MinimumInfantryCount = 2;
        private const int MinimumBuildingCount = 2;

        private static readonly string[] AssaultTankRoles =
        {
            GameTagIds.Assault,
            GameTagIds.Tank
        };

        private static readonly string[] SupportRole =
        {
            GameTagIds.Support
        };

        private static readonly string[] SpotterSynergy =
        {
            GameTagIds.Spotter
        };

        public static SpecialtyLaneContext Resolve(BoardState board, ContentRegistry registry)
        {
            _ = registry;

            if (board == null)
                return WildcardContext();

            int infantryCount = CountPieces(board, GameTagIds.Infantry, countByPrimaryTag: true);
            if (infantryCount < MinimumInfantryCount)
            {
                return new SpecialtyLaneContext
                {
                    PreferredCombatRoles = AssaultTankRoles
                };
            }

            if (!HasPieceWithTag(board, GameTagIds.Artillery, countByCombatRole: true))
            {
                return new SpecialtyLaneContext
                {
                    PreferredCombatRoles = SupportRole,
                    PreferredSynergyTags = SpotterSynergy
                };
            }

            int buildingCount = CountBuildings(board);
            if (buildingCount < MinimumBuildingCount)
            {
                return new SpecialtyLaneContext
                {
                    PreferBuildings = true
                };
            }

            if (CountPieces(board, GameTagIds.Vehicle, countByPrimaryTag: true) == 0)
            {
                return new SpecialtyLaneContext
                {
                    PreferVehicles = true
                };
            }

            return WildcardContext();
        }

        public static bool TryResolveSpecialty(string combatRole, out ShopLane lane)
        {
            lane = default;
            return false;
        }

        public static IEnumerable<PieceDefinition> FilterPool(
            IEnumerable<PieceDefinition> pool,
            SpecialtyLaneContext context)
        {
            if (pool == null)
                return Array.Empty<PieceDefinition>();

            if (context == null || context.IsWildcard)
                return pool;

            var filtered = pool.Where(piece => MatchesPreferences(piece, context)).ToList();
            return filtered.Count > 0 ? filtered : pool;
        }

        public static bool MatchesPreferences(PieceDefinition piece, SpecialtyLaneContext context)
        {
            if (piece == null || context == null || context.IsWildcard)
                return true;

            for (int i = 0; i < context.PreferredCombatRoles.Count; i++)
            {
                if (PieceTagQueries.HasTag(piece, context.PreferredCombatRoles[i]))
                    return true;
            }

            for (int i = 0; i < context.PreferredSynergyTags.Count; i++)
            {
                if (PieceTagQueries.HasSynergyTag(piece, context.PreferredSynergyTags[i]))
                    return true;
            }

            if (context.PreferBuildings && IsBuildingOffer(piece))
                return true;

            if (context.PreferVehicles && IsVehicleOrSupplyOffer(piece))
                return true;

            return false;
        }

        private static SpecialtyLaneContext WildcardContext() => new();

        private static int CountPieces(BoardState board, string tagId, bool countByPrimaryTag)
        {
            int count = 0;
            foreach (var piece in board.Pieces)
            {
                if (countByPrimaryTag
                    ? PieceTagQueries.HasPrimaryTag(piece.Definition, tagId) || PieceTagQueries.HasTag(piece.Definition, tagId)
                    : PieceTagQueries.HasTag(piece.Definition, tagId))
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountBuildings(BoardState board)
        {
            int count = 0;
            foreach (var piece in board.Pieces)
            {
                if (IsBuildingOnBoard(piece.Definition))
                    count++;
            }

            return count;
        }

        private static bool HasPieceWithTag(BoardState board, string tagId, bool countByCombatRole)
        {
            foreach (var piece in board.Pieces)
            {
                if (countByCombatRole
                    ? PieceTagQueries.HasCombatRoleTag(piece.Definition, tagId) || PieceTagQueries.HasTag(piece.Definition, tagId)
                    : PieceTagQueries.HasTag(piece.Definition, tagId))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsBuildingOnBoard(PieceDefinition piece) =>
            piece.Category is PieceCategory.Building
            || PieceTagQueries.HasPrimaryTag(piece, GameTagIds.Building)
            || PieceTagQueries.HasTag(piece, GameTagIds.Building);

        private static bool IsBuildingOffer(PieceDefinition piece) =>
            piece.Category is PieceCategory.Building or PieceCategory.Hybrid
            || PieceTagQueries.HasPrimaryTag(piece, GameTagIds.Building)
            || PieceTagQueries.HasTag(piece, GameTagIds.Building)
            || PieceTagQueries.HasTag(piece, GameTagIds.Utility);

        private static bool IsVehicleOrSupplyOffer(PieceDefinition piece) =>
            PieceTagQueries.HasPrimaryTag(piece, GameTagIds.Vehicle)
            || PieceTagQueries.HasTag(piece, GameTagIds.Vehicle)
            || PieceTagQueries.HasSynergyTag(piece, GameTagIds.Supplier)
            || PieceTagQueries.HasSynergyTag(piece, GameTagIds.SupplyLine);
    }
}
