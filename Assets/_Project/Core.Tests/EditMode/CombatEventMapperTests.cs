using System.Collections.Generic;
using DeadManZone.Core.Combat;
using DeadManZone.Core.Run;
using NUnit.Framework;

namespace DeadManZone.Core.Tests
{
    public sealed class CombatEventMapperTests
    {
        [Test]
        public void FromRecords_MapsAllFields()
        {
            var records = new List<CombatEventRecord>
            {
                new()
                {
                    Segment = 2,
                    Tick = 5,
                    ActorId = "rifle_1",
                    ActionType = "damage",
                    TargetId = "enemy_1",
                    Value = 12
                }
            };

            var events = new List<CombatEvent>(CombatEventMapper.FromRecords(records));

            Assert.AreEqual(1, events.Count);
            Assert.AreEqual(2, events[0].Segment);
            Assert.AreEqual(5, events[0].Tick);
            Assert.AreEqual("rifle_1", events[0].ActorId);
            Assert.AreEqual("damage", events[0].ActionType);
            Assert.AreEqual("enemy_1", events[0].TargetId);
            Assert.AreEqual(12, events[0].Value);
        }
    }
}
