using System.Collections;
using DeadManZone.Core.Combat;
using DeadManZone.Presentation.Combat;
using DeadManZone.Presentation.Combat.Arena;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace DeadManZone.PlayMode.Tests
{
    public sealed class ArmyHealthBarPlayModeTests
    {
        private GameObject _directorGo;
        private GameObject _barGo;
        private GameObject _presenterGo;

        [TearDown]
        public void TearDown()
        {
            if (_directorGo != null)
                Object.DestroyImmediate(_directorGo);

            if (_barGo != null)
                Object.DestroyImmediate(_barGo);

            if (_presenterGo != null)
                Object.DestroyImmediate(_presenterGo);
        }

        [UnityTest]
        public IEnumerator BarsDescendDuringReplay()
        {
            _directorGo = new GameObject("CombatDirectorRoot");
            var director = _directorGo.AddComponent<CombatDirector>();
            director.SetSecondsPerTickForTests(0f);

            _barGo = new GameObject("PlayerBar", typeof(RectTransform));
            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fillGo.transform.SetParent(_barGo.transform, false);
            var fillImage = fillGo.GetComponent<Image>();
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillAmount = 1f;

            var view = _barGo.AddComponent<ArmyHealthBarView>();
            view.InitializeForTests(fillImage);

            _presenterGo = new GameObject("HealthBarPresenter");
            var presenter = _presenterGo.AddComponent<ArmyHealthBarPresenter>();
            presenter.InitializeForTests(view, enemy: null);
            presenter.RegisterUnitForTests("p1", CombatSide.Player, maxHp: 100);
            director.EventReplayed += presenter.HandleReplayEvent;

            var log = new CombatEventLog();
            log.Append(0, 0, "e1", "damage", "p1", 50);

            director.PlayLog(log, segment: 0);
            yield return new WaitUntil(() => !director.IsPlaying);

            Assert.AreEqual(0.5f, presenter.GetTrackedFractionForTests(CombatSide.Player), 0.0001f);
            Assert.AreEqual(0.5f, view.DisplayedFraction, 0.0001f);
        }
    }
}
