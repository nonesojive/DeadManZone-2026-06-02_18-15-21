using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using DeadManZone.Core.Tags;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class CombatantStateTests
    {
        [Test]
        public void CombatantState_HasTag_UsesCategorizedSystemTag()
        {
            var definition = new PieceDefinition
            {
                Id = "rifle",
                DisplayName = "Rifle",
                Category = PieceCategory.Unit,
                Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
                Primary = GameTagIds.Infantry,
                CombatRole = GameTagIds.Assault,
                SystemTag = GameTagIds.Combatant,
                Tags = System.Array.Empty<string>(),
                MaxHp = 10,
                BaseDamage = 2
            };

            var combatant = new CombatantState
            {
                InstanceId = "rifle_1",
                Side = CombatSide.Player,
                Definition = definition,
                CurrentHp = definition.MaxHp,
                Position = new GridCoord(0, 0)
            };

            Assert.IsTrue(combatant.HasTag(GameTagIds.Combatant));
        }
    }
}
