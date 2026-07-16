using System.Collections.Generic;
using System.Linq;
using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Common;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    /// <summary>2026-07-15 faction-roster-v1 §1.8/§2.5 transport tentpole (Armored Ark). Covers
    /// the pure data-manipulation seam only — tick-loop orchestration (movement toward the
    /// target, unload-on-arrival, spill-on-destruction, logging) is covered by
    /// TickCombatRunTransportTests.cs.</summary>
    public sealed class TransportRulesTests
    {
        private static PieceDefinition TransportDefinition() => new()
        {
            Id = "armored_ark",
            Category = PieceCategory.Unit,
            Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
            MaxHp = 200,
            IsTransport = true,
            TransportCapacity = 2
        };

        private static PieceDefinition CargoDefinition() => new()
        {
            Id = "truncheon_line",
            Category = PieceCategory.Unit,
            Shape = new PieceShape(new[] { new GridCoord(0, 0) }),
            MaxHp = 40,
            MaxMorale = 40
        };

        [Test]
        public void ResolveCargo_ReturnsOnlyEmbarkedListedIds()
        {
            var transport = new CombatantState
            {
                InstanceId = "ark",
                Definition = TransportDefinition(),
                AnchorPosition = new GridCoord(0, 0),
                IsTransport = true,
                EmbarkedCargoIds = new List<string> { "cargo_1" }
            };
            var embarkedListed = new CombatantState { InstanceId = "cargo_1", Definition = CargoDefinition(), IsEmbarked = true };
            var alreadyUnloaded = new CombatantState { InstanceId = "cargo_2", Definition = CargoDefinition(), IsEmbarked = false };

            var cargo = TransportRules.ResolveCargo(transport, new List<CombatantState> { embarkedListed, alreadyUnloaded });

            Assert.AreEqual(1, cargo.Count);
            Assert.AreEqual("cargo_1", cargo[0].InstanceId);
        }

        [Test]
        public void ResolveCargo_NoEmbarkedCargo_ReturnsEmpty()
        {
            var transport = new CombatantState { InstanceId = "ark", Definition = TransportDefinition(), IsTransport = true };
            Assert.IsEmpty(TransportRules.ResolveCargo(transport, System.Array.Empty<CombatantState>()));
        }

        [Test]
        public void Disembark_MovesCargoToCarrierAnchorAndClearsEmbarked()
        {
            var transport = new CombatantState
            {
                InstanceId = "ark",
                Definition = TransportDefinition(),
                AnchorPosition = new GridCoord(3, 4),
                IsTransport = true
            };
            var cargo = new CombatantState
            {
                InstanceId = "cargo_1",
                Definition = CargoDefinition(),
                AnchorPosition = new GridCoord(0, 0),
                IsEmbarked = true,
                ShapeOffsets = new List<GridCoord> { new(0, 0) }
            };

            TransportRules.Disembark(cargo, transport);

            Assert.IsFalse(cargo.IsEmbarked, "cargo must never die inside — disembark always clears the flag");
            Assert.AreEqual(transport.AnchorPosition, cargo.AnchorPosition);
        }
    }
}
