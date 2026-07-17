using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Core.Run;
using DeadManZone.Core.Tags;

namespace DeadManZone.Core.Combat
{
    public sealed class TickCombatRun
    {
        private readonly CommandProcessor _commandProcessor = new();
        private readonly BoardState _playerBoard;
        private readonly BoardState _playerHqBoard;
        private readonly int _playerArtilleryCount;
        private readonly BattlefieldState _battlefield;
        private readonly BattlefieldLayout _layout;
        private readonly Rng _rng;
        private readonly List<CombatantState> _playerCombatants;
        private readonly List<CombatantState> _enemyCombatants;
        private readonly TacticState _tactics = new();
        private readonly CombatEventLog _log = new();
        private CombatOccupancyGrid _occupancyGrid = new();
        private bool _awaitingCommand;
        private bool _awaitingOpeningCommand;
        private bool _protectSupportArmorGranted;

        // 2026-07-15 faction-roster-v1 §1.7/§2.6/§4 Paradox's The Second Hand: per-instance
        // pause thresholds (a fielded AddsPauseWindow piece appends one to the shared default).
        private readonly float[] _pauseThresholds;

        // §1.8/§2.5 Oathborn's Armored Ark: transport instance id -> opening-window target cell.
        private readonly Dictionary<string, GridCoord> _transportTargets = new();

        // §2.7/§4 Blightborn's Yellow Autumn: earlier ambient-gas start + hijacking-side immunity.
        // Tracked per side (not just "player") so an enemy-fielded Yellow Autumn (later enemy
        // rotation waves) is immune on its own side, not the player's.
        private readonly bool _playerAmbientGasHijack;
        private readonly bool _enemyAmbientGasHijack;
        private readonly int _effectiveGasStartTick;

        public BoardState PlayerBoard => _playerBoard;
        public int Authority { get; private set; }
        public int Requisition => Authority;
        public int CheckpointsFired { get; private set; }
        public int GlobalTick { get; private set; }
        public PauseTriggerContext LastPauseTrigger { get; private set; }
        public CombatEventLog Log => _log;
        public bool IsFightOver { get; private set; }
        public bool PlayerWon { get; private set; }
        public bool IsDraw { get; private set; }

        public bool AwaitingCommand => !IsFightOver && _awaitingCommand;

        /// <summary>Index of the pause currently awaiting commands, or -1. Equals
        /// CheckpointsFired for any non-opening pause — identical to the old hardcoded "1" for
        /// every fight with only the default single mid-fight pause, and generalizes correctly
        /// once a third window (§1.7 The Second Hand) is in play.</summary>
        public int CurrentPauseIndex => AwaitingCommand
            ? _awaitingOpeningCommand ? 0 : CheckpointsFired
            : -1;

        public TacticType PlayerTactic => _tactics.PlayerTactic;

        public IReadOnlyList<CombatantState> PlayerCombatantsForTests => _playerCombatants;

        public IReadOnlyList<CombatantState> EnemyCombatantsForTests => _enemyCombatants;

        /// <summary>Cells the ability executor will honor as explicit targets right now —
        /// live enemy anchors, per <see cref="CombatAbilityExecutor.IsValidTargetCell"/>.
        /// Surfaced to the pause UI via <see cref="Run.CombatPauseContext.EnemyTargetCells"/>
        /// so the target picker validates against the same rule execution applies.</summary>
        public IReadOnlyList<GridCoord> GetLiveEnemyTargetCells() =>
            _enemyCombatants
                .Where(e => e.IsActive)
                .Select(e => e.AnchorPosition)
                .Distinct()
                .ToList();

        public IReadOnlyDictionary<GridCoord, string> OccupancySnapshotForTests => _occupancyGrid.Snapshot();

        private TickCombatRun(
            BoardState playerBoard,
            BoardState enemyBoard,
            int seed,
            int authority,
            BuildBoardSet playerBuildBoards,
            IReadOnlyList<ICombatRuleModifier> modifiers,
            bool suppressEnemyFightStartEngines)
        {
            _playerBoard = playerBoard;
            _playerHqBoard = playerBuildBoards?.Hq;
            // Grand Battery's Rolling Barrage scales with this (2026-07-15 faction-roster-v1
            // §2.2) — a fixed fight-start count, same reuse of BuildBoardTagCounter as the
            // Marksman-Doctrine Officer's BoardPerTagCount sniper-count aura.
            _playerArtilleryCount = BuildBoardTagCounter.Count(playerBuildBoards, GameTagIds.Artillery);
            _battlefield = BattlefieldState.FromBoards(playerBoard, enemyBoard);
            _layout = _battlefield.Layout;
            _rng = new Rng(seed);
            Authority = authority;
            _playerCombatants = SpawnCombatants(playerBoard, CombatSide.Player, 0);
            _enemyCombatants = SpawnCombatants(enemyBoard, CombatSide.Enemy, _layout.EnemyOriginX);

            // §1.7/§2.6 The Second Hand: scan HQ + combat board (mirrors CommandProcessor.
            // GetAvailableCommands' own HQ-ability scan) for a fielded AddsPauseWindow piece.
            var playerPiecesForThresholds = _playerHqBoard == null
                ? playerBoard.Pieces
                : playerBoard.Pieces.Concat(_playerHqBoard.Pieces);
            bool addsThirdWindow = playerPiecesForThresholds.Any(p => p.Definition.AddsPauseWindow);
            _pauseThresholds = addsThirdWindow
                ? CombatPacingConfig.PauseThresholds.Append(CombatPacingConfig.ThirdPauseWindowThreshold).ToArray()
                : CombatPacingConfig.PauseThresholds;

            // §2.7 Yellow Autumn: earlier ambient-gas start + hijacking-side immunity for the
            // whole fight once fielded (checked once here, not re-evaluated per tick). Either
            // side hijacking pulls the start earlier for everyone; each side is only immune
            // if IT fielded the piece.
            _playerAmbientGasHijack = _playerCombatants.Any(c => c.Definition.HijacksAmbientGas);
            _enemyAmbientGasHijack = _enemyCombatants.Any(c => c.Definition.HijacksAmbientGas);
            _effectiveGasStartTick = GasHijackRules.GetEffectiveGasStartTick(_playerAmbientGasHijack || _enemyAmbientGasHijack);

            var playerSynergySnapshot = PieceAbilityEngine.EvaluateFightStart(playerBoard, playerBuildBoards);
            var playerCriticalMassSnapshot = CriticalMassEngine.Evaluate(playerBoard);
            CriticalMassEngine.ApplyToCombatants(playerCriticalMassSnapshot, _playerCombatants);
            PieceAbilityEngine.ApplyToCombatants(playerSynergySnapshot, _playerCombatants);

            // Easy Fight Options (M2) field a green enemy force: the ENEMY side's
            // fight-start engines (synergy auras, critical mass) are skipped entirely.
            // Player side and tactic buffs are untouched; Normal/Hard/bosses pass false.
            if (!suppressEnemyFightStartEngines)
            {
                var enemySynergySnapshot = PieceAbilityEngine.EvaluateFightStart(enemyBoard);
                var enemyCriticalMassSnapshot = CriticalMassEngine.Evaluate(enemyBoard);
                CriticalMassEngine.ApplyToCombatants(enemyCriticalMassSnapshot, _enemyCombatants);
                PieceAbilityEngine.ApplyToCombatants(enemySynergySnapshot, _enemyCombatants);
            }

            ApplyTacticDamageBuffs();

            // Rule modifiers (boss Twists now, Battle Conditions in M2) apply AFTER the
            // standard fight-start engines so a twist reads final fight-start state. The
            // restore path passes the same modifiers into Start, keeping replays identical.
            if (modifiers != null)
            {
                foreach (var modifier in modifiers)
                    modifier?.OnFightStart(_playerCombatants, _enemyCombatants);
            }

            _awaitingOpeningCommand = true;
            _awaitingCommand = true;
            LastPauseTrigger = new PauseTriggerContext
            {
                CheckpointIndex = 0,
                TriggeredBy = CombatSide.Player,
                Threshold = 1f
            };
        }

        public static TickCombatRun Start(
            BoardState playerBoard,
            BoardState enemyBoard,
            int seed,
            int authority = 0,
            BuildBoardSet playerBuildBoards = null,
            IReadOnlyList<ICombatRuleModifier> modifiers = null,
            bool suppressEnemyFightStartEngines = false) =>
            new TickCombatRun(
                playerBoard,
                enemyBoard,
                seed,
                authority,
                playerBuildBoards,
                modifiers,
                suppressEnemyFightStartEngines);

        public CombatAdvanceResult Continue(IReadOnlyList<PhaseCommand> commands)
        {
            if (IsFightOver)
                return CompleteResult();

            if (_awaitingCommand)
            {
                ApplyCommands(commands, CurrentPauseIndex);
                _awaitingCommand = false;
                if (_awaitingOpeningCommand)
                {
                    _awaitingOpeningCommand = false;
                    if (TryEndFight(CheckpointsFired))
                        return CompleteResult();
                }
                else if (TryEndFight(CheckpointsFired))
                {
                    return CompleteResult();
                }
            }

            int segment = CheckpointsFired;
            RunUntilPauseOrEnd(segment);
            return IsFightOver ? CompleteResult(segment) : AwaitingResult(segment);
        }

        public void FastForwardToCheckpoint(int checkpointsFired, IReadOnlyList<PhaseCommand> submittedCommands)
        {
            if (checkpointsFired <= 0 && !_awaitingOpeningCommand)
                return;

            if (_awaitingOpeningCommand)
                Continue(FilterCommands(submittedCommands, 0));

            while (!IsFightOver && _awaitingCommand && CheckpointsFired < checkpointsFired)
                Continue(FilterCommands(submittedCommands, CurrentPauseIndex));
        }

        /// <summary>Replays saved pause commands until the sim matches a persisted mid-fight snapshot.</summary>
        public void FastForwardFromSave(
            int checkpointsFired,
            bool savedAwaitingCommand,
            IReadOnlyList<PhaseCommand> submittedCommands)
        {
            FastForwardToCheckpoint(checkpointsFired, submittedCommands);

            while (!IsFightOver && !savedAwaitingCommand && AwaitingCommand)
            {
                var commands = FilterCommands(submittedCommands, CurrentPauseIndex);
                if (commands.Count == 0)
                    break;

                Continue(commands);
            }
        }

        public CombatAdvanceResult BuildCompletionResultIfOver() =>
            IsFightOver ? CompleteResult() : null;

        private void RunUntilPauseOrEnd(int segment)
        {
            while (!IsFightOver)
            {
                if (GlobalTick >= CombatPacingConfig.MaxFightTicks)
                {
                    EndAsDraw(segment);
                    return;
                }

                TryMoveSide(_playerCombatants, _enemyCombatants, segment);
                TryMoveSide(_enemyCombatants, _playerCombatants, segment);
                if (TryEndFight(segment))
                    return;

                ApplyHealPulses(segment);

                if (GlobalTick >= _effectiveGasStartTick)
                {
                    ApplyGas(segment);
                    if (TryEndFight(segment))
                        return;
                }

                ResolveAttacks(_playerCombatants, _enemyCombatants, _tactics.PlayerTactic, _tactics.PlayerDamageBuff, segment, CheckpointsFired);
                if (TryEndFight(segment))
                    return;

                ResolveAttacks(_enemyCombatants, _playerCombatants, _tactics.EnemyTactic, _tactics.EnemyDamageBuff, segment, CheckpointsFired);
                if (TryEndFight(segment))
                    return;

                TickSuppressionDurations();

                GlobalTick++;
                if (TryFireCheckpoint(segment))
                    return;
            }
        }

        private bool TryFireCheckpoint(int segment)
        {
            var thresholds = _pauseThresholds;
            if (CheckpointsFired >= thresholds.Length)
                return false;

            var player = ArmyHealthTracker.Evaluate(_playerCombatants);
            var enemy = ArmyHealthTracker.Evaluate(_enemyCombatants);
            float lowest = System.Math.Min(player.Fraction, enemy.Fraction);

            int consumed = 0;
            float lastThreshold = 0f;
            while (CheckpointsFired + consumed < thresholds.Length &&
                   lowest <= thresholds[CheckpointsFired + consumed])
            {
                lastThreshold = thresholds[CheckpointsFired + consumed];
                consumed++;
            }

            if (consumed == 0)
                return false;

            var triggeredBy = player.Fraction <= enemy.Fraction ? CombatSide.Player : CombatSide.Enemy;
            _log.Append(segment, GlobalTick, "combat", "checkpoint", triggeredBy.ToString(), (int)(lastThreshold * 100));

            // Pause-granted armor (ShieldAllies) expires at every pause boundary,
            // whether or not the player submits commands here. Fight-start armor
            // (ArmorBuffSteps) is permanent and never touched. Enemy units have no
            // command path, so only the player side can carry pause-scoped armor.
            foreach (var combatant in _playerCombatants)
                combatant.PauseArmorBuffSteps = 0;

            CheckpointsFired += consumed;
            _awaitingCommand = true;
            LastPauseTrigger = new PauseTriggerContext
            {
                CheckpointIndex = 1,
                TriggeredBy = triggeredBy,
                Threshold = lastThreshold
            };
            return true;
        }

        private void EndAsDraw(int segment)
        {
            IsFightOver = true;
            IsDraw = true;
            PlayerWon = false;
            _log.Append(segment, GlobalTick, "combat", "fight_end", "draw", 0);
        }

        private void TryMoveSide(
            IReadOnlyList<CombatantState> movers,
            IReadOnlyList<CombatantState> targets,
            int segment)
        {
            var aliveTargets = targets.Where(t => t.IsActive).ToList();
            if (aliveTargets.Count == 0)
                return;

            foreach (var mover in movers.Where(m => m.IsActive && m.EffectiveMovementSpeed != 0).OrderBy(m => m.InstanceId))
            {
                if (mover.EffectiveMovementSpeed == 0)
                    continue;

                int moveChargePerTick = CombatMovementSpeed.GetChargePerTick(
                    mover.EffectiveMovementSpeed,
                    mover.Side == CombatSide.Player ? _tactics.PlayerTactic : _tactics.EnemyTactic);
                if (mover.MoveChargePercentBonus != 0)
                {
                    int percentMultiplier = System.Math.Max(0, 100 + mover.MoveChargePercentBonus);
                    moveChargePerTick = moveChargePerTick * percentMultiplier / 100;
                }

                moveChargePerTick = MovementSlowRules.ApplyMovementSlow(
                    moveChargePerTick,
                    MovementSlowRules.IsSlowed(mover, targets));

                moveChargePerTick = SuppressionRules.ApplyMovementSuppression(moveChargePerTick, mover.IsSuppressed);

                mover.MoveCharge += moveChargePerTick;

                // §1.8/§2.5 Armored Ark: a transport with an assigned opening-window target
                // drives straight there instead of engaging — "choice of WHERE, not when".
                var transportGoal = default(GridCoord);
                bool isTransportRun = mover.IsTransport && _transportTargets.TryGetValue(mover.InstanceId, out transportGoal);
                GridCoord goal;
                if (isTransportRun)
                {
                    goal = transportGoal;
                }
                else
                {
                    goal = RoleEngagement.ComputeGoal(mover, movers, aliveTargets, _layout);
                    if (!CombatMovementRules.ShouldAttemptMove(mover, aliveTargets, goal))
                        continue;
                }

                if (IsGoalBlockedByFriendly(goal, mover.InstanceId, movers))
                    continue;

                var next = ShapePathfinder.FindStep(
                    mover.AnchorPosition,
                    goal,
                    mover.ShapeOffsets,
                    mover.InstanceId,
                    _occupancyGrid,
                    _layout,
                    spawnAnchorY: mover.SpawnAnchorY,
                    preferLaneHold: RoleEngagement.IsFrontlineMover(mover));
                if (next == null || next.Value.Equals(mover.AnchorPosition))
                {
                    if (isTransportRun && mover.AnchorPosition.Equals(transportGoal))
                        ArriveTransport(mover, segment);
                    continue;
                }

                int cost = CombatMovement.GetStepChargeCost(mover.AnchorPosition, next.Value, _layout);
                if (mover.MoveCharge < cost)
                    continue;

                mover.MoveCharge -= cost;
                _occupancyGrid.Move(mover.InstanceId, next.Value, mover.ShapeOffsets);
                mover.AnchorPosition = next.Value;
                mover.RecomputeOccupiedCells();
                _log.Append(
                    segment,
                    GlobalTick,
                    mover.InstanceId,
                    "move",
                    $"{next.Value.X},{next.Value.Y}",
                    0);

                if (isTransportRun && mover.AnchorPosition.Equals(transportGoal))
                    ArriveTransport(mover, segment);
            }
        }

        /// <summary>Transport reached its opening-window target: unload cargo and stop trying
        /// to path further (§2.5 Armored Ark — unload happens exactly once, on arrival).</summary>
        private void ArriveTransport(CombatantState transport, int segment)
        {
            if (!_transportTargets.Remove(transport.InstanceId))
                return;

            UnloadTransport(transport, segment);
        }

        private void ApplyGas(int segment)
        {
            // Routed units fled the field, so gas can't touch them.
            foreach (var combatant in _playerCombatants.Concat(_enemyCombatants).Where(c => c.IsActive).OrderBy(c => c.InstanceId))
            {
                if (GasDamageSystem.IsMitigated(combatant.Definition))
                    continue;

                // §2.7 Yellow Autumn: "your units are immune to it" — the hijacking side only.
                if (_playerAmbientGasHijack && combatant.Side == CombatSide.Player)
                    continue;
                if (_enemyAmbientGasHijack && combatant.Side == CombatSide.Enemy)
                    continue;

                int damage = GasDamageSystem.GetDamage(
                    combatant.AnchorPosition,
                    GlobalTick - _effectiveGasStartTick,
                    _layout);
                combatant.CurrentHp -= damage;
                _log.Append(segment, GlobalTick, "gas", "gas_damage", combatant.InstanceId, damage);
                if (!combatant.IsAlive)
                    LogDestroyed(segment, combatant.InstanceId, "gas");
            }
        }

        /// <summary>§4 (🟡 in-combat healing): pulses HealPulseAmount to nearby active allies on
        /// a tick cadence, capped at MaxHp. Sim-wide — enemy healers (if ever fielded) work the
        /// same way.</summary>
        private void ApplyHealPulses(int segment)
        {
            ApplyHealPulsesForSide(_playerCombatants, segment);
            ApplyHealPulsesForSide(_enemyCombatants, segment);
        }

        private void ApplyHealPulsesForSide(IReadOnlyList<CombatantState> side, int segment)
        {
            foreach (var healer in side.Where(c => c.IsActive && c.Definition.HealPulseAmount > 0).OrderBy(c => c.InstanceId))
            {
                if (!HealPulseRules.IsPulseTick(healer, GlobalTick))
                    continue;

                foreach (var target in HealPulseRules.GetHealTargets(healer, side).OrderBy(t => t.InstanceId).ToList())
                {
                    int amount = HealPulseRules.GetHealAmount(healer, target);
                    if (amount <= 0)
                        continue;

                    target.CurrentHp += amount;
                    _log.Append(segment, GlobalTick, healer.InstanceId, "heal", target.InstanceId, amount);
                }
            }
        }

        /// <summary>§1.8 Suppression tentpole: ticks every combatant's suppression duration
        /// down once per sim tick.</summary>
        private void TickSuppressionDurations()
        {
            foreach (var combatant in _playerCombatants)
                SuppressionRules.TickDown(combatant);
            foreach (var combatant in _enemyCombatants)
                SuppressionRules.TickDown(combatant);
        }

        /// <summary>§2.5 Armored Ark: unload embarked cargo onto the field at the transport's
        /// current anchor. The good outcome — no morale shock (that's spill-on-destruction only).</summary>
        private void UnloadTransport(CombatantState transport, int segment)
        {
            var side = transport.Side == CombatSide.Player ? _playerCombatants : _enemyCombatants;
            foreach (var cargo in TransportRules.ResolveCargo(transport, side))
            {
                TransportRules.Disembark(cargo, transport);
                _occupancyGrid.Place(cargo.InstanceId, cargo.AnchorPosition, cargo.ShapeOffsets);
                _log.Append(segment, GlobalTick, transport.InstanceId, "transport_unload", cargo.InstanceId, 0);
                _log.Append(segment, GlobalTick, cargo.InstanceId, "move", $"{cargo.AnchorPosition.X},{cargo.AnchorPosition.Y}", 0);
            }

            transport.EmbarkedCargoIds = System.Array.Empty<string>();
        }

        /// <summary>§2.5 Armored Ark: "if destroyed in transit, cargo spills out at the wreck
        /// with a morale shock — never dies inside." Called from LogDestroyed before the death
        /// shock radius pulse (the spilled cargo is now on the field and eligible for it too).</summary>
        private void SpillTransportCargo(CombatantState transport, int segment)
        {
            var side = transport.Side == CombatSide.Player ? _playerCombatants : _enemyCombatants;
            foreach (var cargo in TransportRules.ResolveCargo(transport, side))
            {
                TransportRules.Disembark(cargo, transport);
                _occupancyGrid.Place(cargo.InstanceId, cargo.AnchorPosition, cargo.ShapeOffsets);
                _log.Append(segment, GlobalTick, transport.InstanceId, "transport_spill", cargo.InstanceId, 0);
                _log.Append(segment, GlobalTick, cargo.InstanceId, "move", $"{cargo.AnchorPosition.X},{cargo.AnchorPosition.Y}", 0);
                ApplyMoraleDamage(segment, cargo, TransportRules.SpillMoraleShock, transport.InstanceId);
            }

            transport.EmbarkedCargoIds = System.Array.Empty<string>();
        }

        private void ResolveAttacks(
            IList<CombatantState> attackers,
            IList<CombatantState> defenders,
            TacticType tactic,
            int damageBuff,
            int segment,
            int tacticsCheckpointIndex)
        {
            // §2.7 Duchess of Sighs: rare-only gas→morale fusion, checked once per side per
            // volley rather than per attacker (army-wide granted rule).
            bool gasMoraleFusion = attackers.Any(c => c.IsActive && c.Definition.GasDealsMoraleDamage);

            foreach (var actor in attackers.Where(a => a.IsActive && a.CanAttack).OrderBy(a => a.InstanceId))
            {
                if (actor.CooldownRemaining > 0)
                {
                    actor.CooldownRemaining--;
                    continue;
                }

                var target = TacticTargeting.SelectTarget(actor, defenders.ToList(), tactic, tacticsCheckpointIndex);
                if (target == null)
                    continue;

                int distance = CombatRange.Distance(actor.AnchorPosition, target.AnchorPosition);
                int accuracyMod = AccuracyModifierCollector.Collect(actor, target, tactic);
                var outcome = CombatAccuracyResolver.Resolve(
                    _rng,
                    actor.Definition,
                    target.Definition,
                    distance,
                    accuracyMod,
                    actor.DamageBonus + damageBuff + LowStateRules.GetDamageBonus(actor),
                    target.TotalArmorSteps,
                    actor.DamagePercentBonus,
                    actor.AccuracyPercentBonus,
                    actor.Definition.AttackRange,
                    actor.AttackRangeSteps);

                string actionType = outcome.Kind switch
                {
                    CombatAttackOutcomeKind.Hit => "damage",
                    CombatAttackOutcomeKind.Graze => "graze",
                    _ => "miss"
                };

                _log.Append(segment, GlobalTick, actor.InstanceId, actionType, target.InstanceId, outcome.Damage);
                if (outcome.Damage > 0)
                {
                    target.CurrentHp -= outcome.Damage;
                    actor.DamageDealtThisFight += outcome.Damage;
                    target.DamageTakenThisFight += outcome.Damage;
                    if (!target.IsAlive)
                    {
                        LogDestroyed(segment, target.InstanceId, actor.InstanceId);
                    }
                    else
                    {
                        if (actor.Definition.TerrorDamage > 0)
                            ApplyMoraleDamage(segment, target, actor.Definition.TerrorDamage, actor.InstanceId);

                        // §1.8 Suppression tentpole: on-hit only, the game's ONLY enemy-facing
                        // debuff family (border rule).
                        if (actor.Definition.AppliesSuppressionOnHit)
                        {
                            SuppressionRules.Apply(target, actor.SuppressionDurationBonusTicks);
                            int suppressionDuration = SuppressionRules.SuppressionDurationTicks
                                + System.Math.Max(0, actor.SuppressionDurationBonusTicks);
                            _log.Append(segment, GlobalTick, actor.InstanceId, "suppress", target.InstanceId, suppressionDuration);
                        }

                        // §2.7 Duchess of Sighs: "your gas damage also deals equal morale
                        // damage" — attack-sourced gas only (not the ambient GasDamageSystem).
                        if (gasMoraleFusion && actor.Definition.AttackType == AttackType.Gas)
                            ApplyMoraleDamage(segment, target, outcome.Damage, actor.InstanceId);
                    }
                }

                int attackSpeedSteps = actor.AttackSpeedSteps
                    + LowStateRules.GetAttackSpeedSteps(actor)
                    - (actor.IsSuppressed ? SuppressionRules.SuppressionAttackSpeedStepDown : 0);
                actor.CooldownRemaining = CombatAttackSpeed.GetEffectiveCooldown(
                    actor.Definition.CooldownTicks,
                    actor.Definition.AttackSpeed,
                    attackSpeedSteps);
            }
        }

        private void ApplyCommands(IReadOnlyList<PhaseCommand> commands, int checkpointIndex)
        {
            var pauseCommands = FilterCommands(commands, checkpointIndex);
            if (pauseCommands.Count == 0)
                return;

            int authority = Authority;
            _commandProcessor.TryApplyBatch(
                pauseCommands,
                _playerBoard,
                ref authority,
                _tactics,
                _playerCombatants,
                _enemyCombatants,
                _log,
                checkpointIndex,
                CheckpointsFired,
                GlobalTick,
                hqBoard: _playerHqBoard,
                artilleryCount: _playerArtilleryCount,
                transportTargetSink: _transportTargets);
            Authority = authority;

            // Ability/strike kills inside the batch don't touch the occupancy grid
            // (only tick-loop kills route through LogDestroyed) — rebuild so dead
            // units stop blocking pathfinding for the rest of the fight.
            RebuildOccupied();
        }

        private bool TryEndFight(int segment)
        {
            var (fightOver, playerWon, isDraw) = CombatWinChecker.Evaluate(_playerCombatants, _enemyCombatants);
            if (!fightOver)
                return false;

            IsFightOver = true;
            PlayerWon = playerWon;
            IsDraw = isDraw;
            string outcome = isDraw ? "draw" : playerWon ? "victory" : "defeat";
            _log.Append(segment, GlobalTick, "combat", "fight_end", outcome, 0);
            return true;
        }

        private void LogDestroyed(int segment, string victimId, string sourceId)
        {
            _occupancyGrid.Remove(victimId);
            _log.Append(segment, GlobalTick, victimId, "destroyed", sourceId, 0);

            // §2.5 Armored Ark: "if destroyed in transit, cargo spills out at the wreck ...
            // never dies inside." Must run before the death-shock pulse below so spilled cargo
            // is on the field (and IsActive) in time to be eligible for/immune from it exactly
            // like any other unit that was already there.
            var victim = _playerCombatants.Concat(_enemyCombatants).FirstOrDefault(c => c.InstanceId == victimId);
            if (victim != null && victim.IsTransport && victim.EmbarkedCargoIds.Count > 0)
                SpillTransportCargo(victim, segment);

            ApplyDeathShock(segment, victimId);
        }

        /// <summary>Deaths — never routs — shock the dead unit's living unbroken allies
        /// within <see cref="MoraleRules.DeathShockRadius"/> (ADR-0005). Shock-routs don't
        /// shock again; only deaths do. §2.9 Ashen death-shock inversion: an Ashen death
        /// GRANTS morale to those allies instead of draining it (MoraleRules.IsDeathShockInverted).</summary>
        private void ApplyDeathShock(int segment, string deadInstanceId)
        {
            var dead = _playerCombatants.Concat(_enemyCombatants)
                .FirstOrDefault(c => c.InstanceId == deadInstanceId);
            if (dead == null)
                return;

            bool inverted = MoraleRules.IsDeathShockInverted(dead.Definition.FactionId);
            var allies = dead.Side == CombatSide.Player ? _playerCombatants : _enemyCombatants;
            foreach (var ally in allies
                         .Where(a => a.IsActive
                             && CombatRange.Distance(dead.AnchorPosition, a.AnchorPosition) <= MoraleRules.DeathShockRadius)
                         .OrderBy(a => a.InstanceId)
                         .ToList())
            {
                if (inverted)
                    ApplyMoraleGain(segment, ally, MoraleRules.DeathShockDamage, dead.InstanceId);
                else
                    ApplyMoraleDamage(segment, ally, MoraleRules.DeathShockDamage, dead.InstanceId);
            }
        }

        private void ApplyMoraleDamage(int segment, CombatantState target, int amount, string sourceId)
        {
            if (!target.CanBreak || !target.IsActive || amount <= 0)
                return;

            amount = MoraleRules.ApplyResistance(amount, target.MoraleDamageResistancePercent);
            if (amount <= 0)
                return;

            target.CurrentMorale -= amount;
            _log.Append(segment, GlobalTick, sourceId, "morale_damage", target.InstanceId, amount);
            if (target.CurrentMorale > 0)
                return;

            // Break: the unit routs — flees the field, out of the fight, not dead. It stays
            // in the combatant lists (routed player units survive the fight, ADR-0005).
            target.IsBroken = true;
            _occupancyGrid.Remove(target.InstanceId);
            _log.Append(segment, GlobalTick, target.InstanceId, "rout", sourceId, 0);
        }

        /// <summary>§2.9 Ashen death-shock inversion: the gain-side twin of ApplyMoraleDamage.
        /// No resistance modifier (there's nothing to resist about a gift) and no break check
        /// (gains never rout anyone); clamps at MaxMorale.</summary>
        private void ApplyMoraleGain(int segment, CombatantState target, int amount, string sourceId)
        {
            if (!target.CanBreak || !target.IsActive || amount <= 0)
                return;

            int before = target.CurrentMorale;
            target.CurrentMorale = System.Math.Min(target.Definition.MaxMorale, target.CurrentMorale + amount);
            int applied = target.CurrentMorale - before;
            if (applied <= 0)
                return;

            _log.Append(segment, GlobalTick, sourceId, "morale_gain", target.InstanceId, applied);
        }

        private CombatAdvanceResult AwaitingResult(int segment) =>
            new CombatAdvanceResult
            {
                Status = CombatAdvanceStatus.AwaitingCommand,
                SegmentIndex = segment,
                PauseTrigger = LastPauseTrigger,
                EventLog = _log
            };

        private CombatAdvanceResult CompleteResult() => CompleteResult(CheckpointsFired);

        private CombatAdvanceResult CompleteResult(int segment)
        {
            var (total, lost) = ComputePlayerLossStats();
            var survivors = _playerCombatants
                .Where(c => c.IsAlive && c.Definition.MaxHp > 0)
                .Select(c => c.InstanceId)
                .ToList();
            int enemyKilled = _enemyCombatants.Count(c => c.Definition.MaxHp > 0 && !c.IsAlive);
            int enemyRouted = _enemyCombatants.Count(c => c.Definition.MaxHp > 0 && c.IsAlive && c.IsBroken);

            return new CombatAdvanceResult
            {
                Status = CombatAdvanceStatus.Completed,
                SegmentIndex = segment,
                PlayerWon = PlayerWon,
                IsDraw = IsDraw,
                EventLog = _log,
                PlayerCombatantsTotal = total,
                PlayerCombatantsLost = lost,
                EnemyKilled = enemyKilled,
                EnemyRouted = enemyRouted,
                SurvivingPlayerCombatantIds = survivors,
                PlayerCombatantsAtEnd = _playerCombatants,
                BattleReport = BattleReportBuilder.Build(
                    _playerCombatants,
                    PlayerWon,
                    IsDraw,
                    // field_hospital is Building-primary => always resolves to the HQ board
                    // (BoardPlacementRules.ResolveTargetBoard), not the combat board.
                    ManpowerCalculator.ComputeCasualties(_playerCombatants, _playerHqBoard),
                    suppliesEarned: 0,
                    enemyKilled: enemyKilled,
                    enemyRouted: enemyRouted)
            };
        }

        private (int total, int lost) ComputePlayerLossStats()
        {
            int total = 0;
            int lost = 0;

            foreach (var combatant in _playerCombatants)
            {
                if (combatant.Definition.MaxHp <= 0)
                    continue;

                total++;
                if (!combatant.IsAlive)
                    lost++;
            }

            return (total, lost);
        }

        private void RebuildOccupied()
        {
            _occupancyGrid = new CombatOccupancyGrid();
            foreach (var combatant in _playerCombatants.Concat(_enemyCombatants).Where(c => c.IsActive))
            {
                if (combatant.ShapeOffsets == null || combatant.ShapeOffsets.Count == 0)
                    continue;

                _occupancyGrid.Place(
                    combatant.InstanceId,
                    combatant.AnchorPosition,
                    combatant.ShapeOffsets);
            }
        }

        private static bool IsGoalBlockedByFriendly(
            GridCoord goal,
            string moverInstanceId,
            IReadOnlyList<CombatantState> allies)
        {
            foreach (var ally in allies)
            {
                if (!ally.IsActive || ally.InstanceId == moverInstanceId)
                    continue;

                if (ally.AnchorPosition.Equals(goal))
                    return true;
            }

            return false;
        }

        private List<CombatantState> SpawnCombatants(BoardState board, CombatSide side, int xOffset)
        {
            int halfWidth = board.Layout.Width;
            var combatants = new List<CombatantState>();
            var anchorByInstanceId = new Dictionary<string, GridCoord>();

            foreach (var piece in board.Pieces
                         .OrderBy(p => p.InstanceId)
                         .Where(p => p.Definition.MaxHp > 0))
            {
                int rotationIndex = (int)piece.Rotation / 90;
                int localX = side == CombatSide.Enemy
                    ? BattlefieldLayout.MirrorEnemyAnchorX(
                        piece.Anchor.X,
                        piece.Definition.Shape,
                        halfWidth,
                        piece.Rotation)
                    : piece.Anchor.X;
                var anchor = new GridCoord(xOffset + localX, piece.Anchor.Y);
                anchorByInstanceId[piece.InstanceId] = anchor;
                var offsets = CombatFootprint.ComputeOffsets(piece.Definition.Shape, rotationIndex);
                var combatant = new CombatantState
                {
                    InstanceId = piece.InstanceId,
                    Side = side,
                    Definition = piece.Definition,
                    CurrentHp = piece.Definition.MaxHp,
                    CurrentMorale = piece.Definition.MaxMorale,
                    CooldownRemaining = 0,
                    AnchorPosition = anchor,
                    SpawnAnchorY = piece.Anchor.Y,
                    ShapeOffsets = offsets,
                    MoraleDamageResistancePercent = piece.Definition.MoraleDamageResistancePercent,
                    // §1.8/§2.5 Armored Ark: cargo spawns embarked (off-field) instead of
                    // independently when Build-phase-loaded (PlacedPiece.CarrierInstanceId).
                    IsTransport = piece.Definition.IsTransport,
                    IsEmbarked = !string.IsNullOrEmpty(piece.CarrierInstanceId),
                    CarrierInstanceId = piece.CarrierInstanceId
                };
                combatants.Add(combatant);
            }

            // Re-anchor embarked cargo to its carrier's spawn anchor (cosmetic — where it'll
            // appear on unload/spill) and keep it OFF the occupancy grid: it's inside the
            // transport, not on the field.
            foreach (var cargo in combatants.Where(c => c.IsEmbarked))
            {
                if (cargo.CarrierInstanceId != null && anchorByInstanceId.TryGetValue(cargo.CarrierInstanceId, out var carrierAnchor))
                    cargo.AnchorPosition = carrierAnchor;
                cargo.RecomputeOccupiedCells();
            }

            foreach (var transport in combatants.Where(c => c.IsTransport))
            {
                transport.EmbarkedCargoIds = combatants
                    .Where(c => c.CarrierInstanceId == transport.InstanceId)
                    .Select(c => c.InstanceId)
                    .ToList();
            }

            foreach (var combatant in combatants.Where(c => !c.IsEmbarked))
            {
                combatant.RecomputeOccupiedCells();
                _occupancyGrid.Place(combatant.InstanceId, combatant.AnchorPosition, combatant.ShapeOffsets);
            }

            return combatants;
        }

        private void ApplyTacticDamageBuffs()
        {
            _tactics.PlayerDamageBuff = TacticEffects.GetDamageBuff(_tactics.PlayerTactic);
            _tactics.EnemyDamageBuff = TacticEffects.GetDamageBuff(_tactics.EnemyTactic);

            // Fight-start armor (ArmorBuffSteps) is permanent, so the ProtectSupport
            // grant must be idempotent — a second SetPlayerTactic(ProtectSupport)
            // (e.g. on the save-restore path) would otherwise double-grant forever.
            if (_tactics.PlayerTactic != TacticType.ProtectSupport || _protectSupportArmorGranted)
                return;

            _protectSupportArmorGranted = true;
            TacticEffects.ApplyProtectSupportBuffs(
                _tactics.PlayerTactic,
                _playerCombatants,
                _playerBoard.Layout);
        }

        public void SetPlayerTactic(TacticType tactic)
        {
            _tactics.PlayerTactic = tactic;
            ApplyTacticDamageBuffs();
        }

        private static IReadOnlyList<PhaseCommand> FilterCommands(
            IReadOnlyList<PhaseCommand> commands,
            int checkpointIndex) =>
            commands?.Where(c => c.AfterCheckpoint == checkpointIndex).ToList()
            ?? (IReadOnlyList<PhaseCommand>)new List<PhaseCommand>();
    }
}
