using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace DirectXEngine
{
    public class Scene
    {
        internal Scene()
        {
            EngineCore.Current.FrameRenderStart += InvokeGameObjectsEvents;
        }

        public static Scene Current { get; } = new Scene();
        private List<GameObject> _GameObjects = new List<GameObject>();

        public GameObject Instantiate(GameObject original)
        {
            GameObject copy = original.Copy(true);
            copy.InvokeOnInstantiate();
            copy.InvokeOnStart();
            _GameObjects.Add(copy);
            return copy;
        }

        public T Instantiate<T>(T original) where T : GameObject => (T)Instantiate((GameObject)original);

        public GameObject Instantiate()
        {
            GameObject gameObject = new GameObject(true);
            _GameObjects.Add(gameObject);
            return gameObject;
        }

        public T Instantiate<T>() where T : GameObject
        {
            T gameObject = (T)Activator.CreateInstance(typeof(T), true);
            _GameObjects.Add(gameObject);
            return gameObject;
        }

        public GameObject[] FindGameObjectsOfType<T>() where T : GameObject => _GameObjects.Where(x => x is T).ToArray();

        private void InvokeGameObjectsEvents()
        {
            _GameObjects.ForEach(x => x.InvokeOnUpdate());
            _GameObjects.ForEach(x => x.InvokeOnEnd());
        }
    }
}
