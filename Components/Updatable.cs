using System.Collections;
using System.Collections.Generic;

namespace DirectXEngine
{
    public abstract class Updatable : Startable
    {
        protected Updatable(GameObject attachedGameObject) : base(attachedGameObject)
        {
            
        }

        private List<IEnumerator> _UpdateCoroutines = new List<IEnumerator>();
        private List<IEnumerator> _FixedUpdateCoroutines = new List<IEnumerator>();

        internal void InvokeUpdate()
        {
            UpdateCoroutines(_UpdateCoroutines);
            OnUpdate();
        }

        internal void InvokeFixedUpdate()
        {
            UpdateCoroutines(_FixedUpdateCoroutines);
            OnFixedUpdate();
        }

        protected virtual void OnUpdate() { }

        protected virtual void OnFixedUpdate() { }

        protected void StartUpdateCoroutine(IEnumerator enumerator) => _UpdateCoroutines.Add(enumerator);

        protected void StartFixedUpdateCoroutine(IEnumerator enumerator) => _FixedUpdateCoroutines.Add(enumerator);

        private void UpdateCoroutines(List<IEnumerator> coroutines)
        {
            for (int i = 0; i < coroutines.Count; i++)
                UpdateCoroutine(coroutines, i);
        }

        private void UpdateCoroutine(List<IEnumerator> coroutines, int index)
        {
            IEnumerator enumerator = coroutines[index];

            Coroutine current = enumerator.Current as Coroutine;

            if (current != null && !current.CanMoveNext)
                return;

            if (!enumerator.MoveNext())
                coroutines.RemoveAt(index);
        }
    }
}
