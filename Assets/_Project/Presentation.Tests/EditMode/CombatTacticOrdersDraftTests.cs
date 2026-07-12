using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Presentation.Combat.Arena;
using NUnit.Framework;

namespace DeadManZone.Presentation.Tests
{
    /// <summary>
    /// The tactics window's order sheet must defer every verdict to the Core
    /// TacticPauseValidator and build the exact PhaseCommand shapes the live flow
    /// submits (TacticPausePanel.SubmitAndContinue + TargetCell for targeted abilities).
    /// </summary>
    public sealed class CombatTacticOrdersDraftTests
    {
        private static readonly TacticType[] IronmarchTactics =
        {
            TacticType.StandGround, TacticType.Advance, TacticType.DisciplinedFire
        };

        private static AvailableCommand Ability(GrantedAbility ability, int checkpointIndex, string source) =>
            new()
            {
                Type = CommandType.UseAbility,
                Ability = ability,
                SourcePieceId = source,
                RequisitionCost = CombatAbilityExecutor.GetAuthorityCost(ability, checkpointIndex)
            };

        private static CombatTacticOrdersDraft NewDraft(int authority, int checkpointIndex = 0) =>
            new(TacticType.DisciplinedFire, authority, checkpointIndex,
                hasCommandPiece: false, startingTactics: IronmarchTactics);

        [Test]
        public void QueueAbilities_TracksValidatorCosts_AndRemainingAuthority()
        {
            var draft = NewDraft(authority: 8);

            Assert.IsTrue(draft.TryQueueAbility(
                Ability(GrantedAbility.GrenadeLob, 0, "p3d_unit_3"), new GridCoord(12, 2), out _));
            Assert.IsTrue(draft.TryQueueAbility(
                Ability(GrantedAbility.ShieldAllies, 0, "p3d_unit_2"), null, out _));

            // GrenadeLob at pause 0 costs 2, ShieldAllies 2 (CombatAbilityExecutor authority table).
            Assert.AreEqual(4, draft.TotalCost);
            Assert.AreEqual(4, draft.AuthorityRemaining);
            Assert.IsTrue(draft.Validate(out _));
        }

        [Test]
        public void QueueAbility_OverBudget_RejectedByValidator_QueueUnchanged()
        {
            var draft = NewDraft(authority: 3);
            Assert.IsTrue(draft.TryQueueAbility(
                Ability(GrantedAbility.GrenadeLob, 0, "p3d_unit_3"), new GridCoord(12, 2), out _));

            Assert.IsFalse(draft.TryQueueAbility(
                Ability(GrantedAbility.ShieldAllies, 0, "p3d_unit_2"), null, out string reason));
            Assert.AreEqual("Insufficient Authority", reason);
            Assert.AreEqual(1, draft.Queued.Count);
            Assert.AreEqual(1, draft.AuthorityRemaining);
        }

        [Test]
        public void QueueAbility_Duplicate_Rejected()
        {
            var draft = NewDraft(authority: 8);
            Assert.IsTrue(draft.TryQueueAbility(
                Ability(GrantedAbility.ShieldAllies, 0, "p3d_unit_2"), null, out _));

            Assert.IsFalse(draft.TryQueueAbility(
                Ability(GrantedAbility.ShieldAllies, 0, "p3d_unit_2"), null, out string reason));
            Assert.AreEqual("Ability already queued", reason);
        }

        [Test]
        public void RemoveQueued_RefundsCost()
        {
            var draft = NewDraft(authority: 4);
            draft.TryQueueAbility(Ability(GrantedAbility.GrenadeLob, 0, "p3d_unit_3"), new GridCoord(12, 2), out _);
            draft.TryQueueAbility(Ability(GrantedAbility.ShieldAllies, 0, "p3d_unit_2"), null, out _);
            Assert.AreEqual(0, draft.AuthorityRemaining);

            draft.RemoveQueuedAt(0);

            Assert.AreEqual(1, draft.Queued.Count);
            Assert.AreEqual(2, draft.AuthorityRemaining);
            Assert.IsFalse(draft.IsAbilityQueued(GrantedAbility.GrenadeLob));
        }

        [Test]
        public void SelectTactic_LockedOrIllegal_RejectedAndSelectionUnchanged()
        {
            var draft = NewDraft(authority: 8);

            // Not in the faction's starting tactics.
            Assert.IsFalse(draft.TrySelectTactic(TacticType.ProtectSupport, out string reason));
            Assert.AreEqual("Tactic not unlocked", reason);
            Assert.AreEqual(TacticType.DisciplinedFire, draft.SelectedTactic);

            // Unlocked switch goes through.
            Assert.IsTrue(draft.TrySelectTactic(TacticType.Advance, out _));
            Assert.AreEqual(TacticType.Advance, draft.SelectedTactic);
        }

        [Test]
        public void MidPauseTacticSwitch_CostsOneAuthority_AtCheckpointOne()
        {
            var draft = NewDraft(authority: 1, checkpointIndex: 1);
            Assert.AreEqual(0, draft.TotalCost); // staying on the active tactic is free

            Assert.IsTrue(draft.TrySelectTactic(TacticType.StandGround, out _));
            Assert.AreEqual(1, draft.TotalCost); // switch at pause 1 costs 1 (validator rule)

            // The switch consumed the whole budget; any ability must now be rejected.
            Assert.IsFalse(draft.TryQueueAbility(
                Ability(GrantedAbility.ShieldAllies, 1, "p3d_unit_2"), null, out string reason));
            Assert.AreEqual("Insufficient Authority", reason);
        }

        [Test]
        public void BuildCommands_MirrorsLiveFlowShape_SetTacticFirstThenAbilitiesWithTargets()
        {
            var draft = NewDraft(authority: 8);
            draft.TrySelectTactic(TacticType.Advance, out _);
            var target = new GridCoord(12, 2);
            draft.TryQueueAbility(Ability(GrantedAbility.GrenadeLob, 0, "p3d_unit_3"), target, out _);

            var commands = draft.BuildCommands();

            Assert.AreEqual(2, commands.Count);
            Assert.AreEqual(CommandType.SetTactic, commands[0].Type);
            Assert.AreEqual(TacticType.Advance, commands[0].Tactic);
            Assert.AreEqual(0, commands[0].AfterCheckpoint);
            Assert.AreEqual("player_tactic", commands[0].SourcePieceId); // same marker the live panel submits

            Assert.AreEqual(CommandType.UseAbility, commands[1].Type);
            Assert.AreEqual(GrantedAbility.GrenadeLob, commands[1].Ability);
            Assert.AreEqual("p3d_unit_3", commands[1].SourcePieceId);
            Assert.AreEqual(2, commands[1].Cost);
            Assert.IsTrue(commands[1].TargetCell.HasValue);
            Assert.AreEqual(target, commands[1].TargetCell.Value);
        }
    }
}
