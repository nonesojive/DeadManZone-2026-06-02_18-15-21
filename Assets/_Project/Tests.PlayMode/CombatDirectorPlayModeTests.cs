using System.Collections;
using DeadManZone.Core.Combat;
using DeadManZone.Presentation.Combat;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DeadManZone.PlayMode.Tests
{
    public sealed class CombatDirectorPlayModeTests
    {
        private GameObject _root;

        [TearDown]
        public void TearDown()
        {
            if (_root != null)
                Object.DestroyImmediate(_root);
        }

        [UnityTest]
        public IEnumerator PlayLog_ReplaysEventsInOrder()
        {
            _root = new GameObject("CombatDirectorRoot");
            var director = _root.AddComponent<CombatDirector>();
            director.SetSecondsPerTickForTests(0f);

            var log = new CombatEventLog();
            log.Append(0, 0, "a", "move", "x", 0);
            log.Append(0, 1, "a", "damage", "x", 2);

            int replayed = 0;
            director.EventReplayed += _ => replayed++;
            director.PlayLog(log, segment: 0);

            yield return new WaitUntil(() => !director.IsPlaying);

            Assert.AreEqual(2, replayed);
            Assert.IsFalse(director.IsPlaying);
        }
    }
}
