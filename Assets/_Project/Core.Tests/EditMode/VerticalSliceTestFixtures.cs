using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tags;
using DeadManZone.Data;
using DeadManZone.Game;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    /// <summary>Shared boards and commands for vertical-slice regression tests.</summary>
    public static class VerticalSliceTestFixtures
    {
        public const int RegressionRunSeed = 24_680;
        public const int RegressionRequisition = 8;

        public static BuildBoardSet BuildGauntletBoards(ContentDatabase database)
        {
            var faction = database.GetFaction(FactionIds.IronmarchUnion);
            Assert.NotNull(faction, "ironmarch_union faction required for regression tests.");

            // 2026-07-15 faction-roster-v1: ids updated (mortars→field_mortar_team,
            // enlisted→shock_sergeant, transport→iron_guard, conscript→conscript_rifles,
            // horse→breakthrough_tank, bulwark→iron_guard). All footprints kept 1:1 with
            // the pieces they replace, so these anchors still land unchanged.
            var combat = new BoardState(faction.CreateCombatBoardLayout());
            Place(combat, database, "field_mortar_team", new GridCoord(0, 0), "mortars_1");
            Place(combat, database, "shock_sergeant", new GridCoord(3, 0), "enlisted_1");
            Place(combat, database, "iron_guard", new GridCoord(4, 2), "transport_1");
            Place(combat, database, "field_medic", new GridCoord(4, 5), "medic_1");
            Place(combat, database, "conscript_rifles", new GridCoord(5, 4), "conscript_1");
            Place(combat, database, "breakthrough_tank", new GridCoord(0, 3), "horse_1");
            Place(combat, database, "iron_guard", new GridCoord(5, 0), "bulwark_1");

            var hq = new BoardState(faction.CreateHqBoardLayout());
            Place(hq, database, "command_outpost", new GridCoord(1, 0), "outpost_1");

            return new BuildBoardSet { Combat = combat, Hq = hq };
        }

        public static void SaveGauntletToOrchestrator(RunOrchestrator orchestrator, ContentDatabase database)
        {
            var boards = BuildGauntletBoards(database);
            orchestrator.SaveCombatBoard(boards.Combat);
            orchestrator.SaveHqBoard(boards.Hq);
        }

        /// <summary>Aggregate board for combat command helpers that scan all placed pieces.</summary>
        public static BoardState BuildGauntletBoard(ContentDatabase database) =>
            BuildGauntletBoards(database).ToAggregateBoard();

        public static List<PhaseCommand> BuildAggressiveCommands(BoardState board)
        {
            var commands = new List<PhaseCommand>();
            string commandId = board.Pieces.FirstOrDefault(p => p.Definition.Id == "command_outpost")?.InstanceId;

            commands.Add(new PhaseCommand
            {
                AfterCheckpoint = 0,
                Type = CommandType.SetTactic,
                Tactic = TacticType.Advance,
                SourcePieceId = commandId ?? "player_tactic"
            });
            commands.Add(new PhaseCommand
            {
                AfterCheckpoint = 1,
                Type = CommandType.SetTactic,
                Tactic = TacticType.Advance,
                SourcePieceId = commandId ?? "player_tactic"
            });

            return commands;
        }

        public static List<PhaseCommand> BuildAggressiveCommandsForCheckpoint(
            BoardState board,
            int checkpointIndex,
            int availableAuthority)
        {
            var commands = new List<PhaseCommand>();
            int remaining = availableAuthority;

            foreach (var command in BuildAggressiveCommands(board))
            {
                if (command.AfterCheckpoint != checkpointIndex)
                    continue;

                if (command.Type == CommandType.SetTactic)
                {
                    commands.Add(command);
                    continue;
                }

                if (command.Type != CommandType.UseAbility)
                    continue;

                int cost = CombatAbilityExecutor.GetAuthorityCost(command.Ability, checkpointIndex);
                if (cost > remaining)
                    continue;

                commands.Add(command);
                remaining -= cost;
            }

            return commands;
        }

        private static void TryAddAbility(
            List<PhaseCommand> commands,
            BoardState board,
            string pieceId,
            GrantedAbility ability,
            int checkpointIndex)
        {
            var piece = board.Pieces.FirstOrDefault(p => p.Definition.Id == pieceId);
            if (piece == null)
                return;

            commands.Add(new PhaseCommand
            {
                AfterCheckpoint = checkpointIndex,
                Type = CommandType.UseAbility,
                Ability = ability,
                SourcePieceId = piece.InstanceId
            });
        }

        private static void Place(
            BoardState board,
            ContentDatabase database,
            string pieceId,
            GridCoord anchor,
            string instanceId)
        {
            var piece = WithLegacySynergyFallbackAbilities(database.Pieces.First(p => p.id == pieceId).ToCore());
            var result = board.TryPlace(piece, anchor, instanceId);
            Assert.IsTrue(result.Success, $"Failed to place {pieceId} at {anchor}: {result.Reason}");
        }

        private static PieceDefinition WithLegacySynergyFallbackAbilities(PieceDefinition piece)
        {
            if (piece.SynergyTags == null || piece.SynergyTags.Count == 0)
                return piece;
            if (piece.Abilities != null && piece.Abilities.Count > 0)
                return piece;

            var abilities = new List<PieceAbilityDefinition>();
            foreach (var tag in piece.SynergyTags)
            {
                switch (tag)
                {
                    case GameTagIds.Medic:
                        abilities.Add(new PieceAbilityDefinition
                        {
                            Id = "legacy_medic_adjacent_infantry_armor_plus_one",
                            Trigger = PieceAbilityTrigger.AdjacentAura,
                            NeighborFilter = new NeighborFilter { PrimaryTagId = GameTagIds.Infantry },
                            Stat = SynergyStat.ArmorType,
                            ModType = SynergyModType.Flat,
                            Magnitude = 1
                        });
                        break;
                    case GameTagIds.Command:
                        abilities.Add(new PieceAbilityDefinition
                        {
                            Id = "legacy_command_adjacent_artillery_damage_plus_two",
                            Trigger = PieceAbilityTrigger.AdjacentAura,
                            NeighborFilter = new NeighborFilter { CombatRoleTagId = GameTagIds.Artillery },
                            Stat = SynergyStat.Damage,
                            ModType = SynergyModType.Flat,
                            Magnitude = 2
                        });
                        break;
                    case GameTagIds.Echo:
                        abilities.Add(new PieceAbilityDefinition
                        {
                            Id = "legacy_echo_adjacent_stealth_damage_plus_one",
                            Trigger = PieceAbilityTrigger.AdjacentAura,
                            NeighborFilter = new NeighborFilter { AbilityTagId = GameTagIds.Stealth },
                            Stat = SynergyStat.Damage,
                            ModType = SynergyModType.Flat,
                            Magnitude = 1
                        });
                        break;
                    case GameTagIds.Inspiring:
                        abilities.Add(new PieceAbilityDefinition
                        {
                            Id = "legacy_inspiring_adjacent_move_charge_plus_five",
                            Trigger = PieceAbilityTrigger.AdjacentAura,
                            NeighborFilter = NeighborFilter.Any,
                            Stat = SynergyStat.MoveChargePercent,
                            ModType = SynergyModType.Flat,
                            Magnitude = 5
                        });
                        break;
                }
            }

            return abilities.Count == 0
                ? piece
                : TestPieces.With(piece, abilities: abilities);
        }
    }
}
