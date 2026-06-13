using System.Collections.Generic;
using UnityEngine;

namespace DeadManZone.Presentation.Combat.Arena
{
    public sealed class CombatUnitActorPool
    {
        private readonly Transform _root;
        private readonly Stack<CombatUnitActor> _available = new();

        public CombatUnitActorPool(Transform root) => _root = root;

        public CombatUnitActor Rent()
        {
            while (_available.Count > 0)
            {
                var actor = _available.Pop();
                if (actor == null)
                    continue;

                actor.gameObject.SetActive(true);
                return actor;
            }

            var go = new GameObject("CombatUnitActor");
            go.transform.SetParent(_root, false);
            return go.AddComponent<CombatUnitActor>();
        }

        public void Clear() => _available.Clear();

        public void Release(CombatUnitActor actor)
        {
            if (actor == null)
                return;

            actor.ResetForPool();
            actor.transform.SetParent(_root, false);
            _available.Push(actor);
        }

        public void ReleaseAll(IEnumerable<CombatUnitActor> actors)
        {
            foreach (var actor in actors)
                Release(actor);
        }
    }
}
