using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DirectXEngine
{
    public class GameObject
    {
        public GameObject()
        {
            
        }

        internal GameObject(bool isInstantiated)
        {
            IsInstantiated = isInstantiated;
        }

        public Transform Transform { get; private set; } = new Transform();
        public string Name
        {
            get => _Name;
            set
            {
                ExceptionHelper.ThrowIfNull(value);
                _Name = value;
            }
        }
        public bool IsInstantiated { get; private set; } = false;
        private string _Name = string.Empty;
        private List<Component> _Components = new List<Component>();
        private List<Startable> _Startables = new List<Startable>();
        private List<Updatable> _Updatables = new List<Updatable>();

        internal virtual GameObject Copy(bool isInstantiated)
        {
            GameObject gameObjectCopy = new GameObject();
            gameObjectCopy.Transform = Transform.Copy();
            gameObjectCopy._Name = _Name;

            foreach (Component component in _Components)
            {
                Component componentCopy = component.Copy(gameObjectCopy);
                gameObjectCopy._Components.Add(componentCopy);
                gameObjectCopy.UpdateComponentsOnAdd(componentCopy);

                if (componentCopy is Updatable updatable)
                {
                    _Updatables.Add(updatable);
                    _Startables.Add(updatable);
                    continue;
                }

                if (componentCopy is Startable startable)
                    _Startables.Add(startable);
            }

            gameObjectCopy.IsInstantiated = isInstantiated;

            return gameObjectCopy;
        }

        internal void InvokeOnInstantiate()
        {
            for (int i = 0; i < _Components.Count; i++)
            {
                if (i >= _Components.Count)
                    return;

                _Components[i].InvokeOnInstantiate();
            }
        }

        internal void InvokeOnStart()
        {
            for (int i = 0; i < _Startables.Count; i++)
            {
                if (i >= _Startables.Count)
                    return;

                _Startables[i].InvokeOnStart();
            }
        }

        internal void InvokeOnEnd()
        {
            for (int i = 0; i < _Startables.Count; i++)
            {
                if (i >= _Startables.Count)
                    return;

                _Startables[i].InvokeOnEnd();
            }
        }

        internal void InvokeOnUpdate()
        {
            for (int i = 0; i < _Updatables.Count; i++)
            {
                if (i >= _Updatables.Count)
                    return;

                _Updatables[i].InvokeUpdate();
            }
        }

        public static GameObject Create(GameObjectType type)
        {
            switch (type)
            {
                case GameObjectType.Plane: return CreatePlane();
                case GameObjectType.Cube: return CreateCube();
                case GameObjectType.DirectionalLight:
                    GameObject light = Scene.Current.Instantiate();
                    light.AddComponent<DirectionalLight>();
                    return light;
                case GameObjectType.Spotlight:
                    GameObject spotlight = Scene.Current.Instantiate();
                    spotlight.AddComponent<Spotlight>();
                    return spotlight;
                default: return new GameObject();
            }
        }

        public T GetComponent<T>() where T : class
        {
            T Component = _Components.FirstOrDefault(X => X is T) as T;
            ExceptionHelper.ThrowByCondition(Component, "", e => e == null);
            return Component;
        }

        public bool TryGetComponent<T>(out T component) where T : class
        {
            component = null;
            try
            {
                component = GetComponent<T>();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public List<T> GetComponents<T>() where T : class => _Components.FindAll(X => X is T).Cast<T>().ToList();

        public Component AddComponent(Type componentType)
        {
            ExceptionHelper.ThrowByCondition(!componentType.IsSubclassOf(typeof(Component)));
            ExceptionHelper.ThrowByCondition(_Components.Find(X => X.GetType().Equals(componentType)) != null);

            Component component = (Component)Activator.CreateInstance(componentType, this);
            _Components.Add(component);

            UpdateComponentsOnAdd(component);

            return component;
        }

        public T AddComponent<T>() where T : Component
        {
            Type componentType = typeof(T);
            
            ExceptionHelper.ThrowByCondition(_Components.Find(X => X.GetType().Equals(componentType)) != null);

            T component = (T)Activator.CreateInstance(componentType, this);
            _Components.Add(component);

            UpdateComponentsOnAdd(component);

            return component;
        }

        private void UpdateComponentsOnAdd(Component component)
        {
            if (component is Updatable updatable)
            {
                _Updatables.Add(updatable);
                _Startables.Add(updatable);
                
                if (IsInstantiated)
                    updatable.InvokeOnStart();
                return;
            }

            if (component is Startable startable)
            {
                _Startables.Add(startable);
                if (IsInstantiated)
                    startable.InvokeOnStart();
            }
        }

        private static GameObject CreatePlane()
        {
            GameObject plane = Scene.Current.Instantiate();
            MeshRenderer meshRenderer = plane.AddComponent<MeshRenderer>();
            Mesh mesh = new Mesh
            {
                Vertices = new Vector3[]
                {
                    new Vector3(0.5f, -0.5f, 0),
                    new Vector3(-0.5f, -0.5f, 0),
                    new Vector3(-0.5f, 0.5f, 0),
                    new Vector3(0.5f, 0.5f, 0),
                },
                Triangles = new int[]
                {
                    0, 1, 2, 0, 2, 3
                },
                Normals = new Vector3[]
                {
                    Vector3.BackwardLH,
                    Vector3.BackwardLH,
                    Vector3.BackwardLH,
                    Vector3.BackwardLH,
                }
            };
            meshRenderer.Mesh = mesh;         
            return plane;
        }

        private static GameObject CreateCube()
        {
            GameObject cube = Scene.Current.Instantiate();
            MeshRenderer meshRenderer = cube.AddComponent<MeshRenderer>();
            Mesh mesh = new Mesh
            {
                Vertices = new Vector3[]
                {
                    new Vector3(0.5f, -0.5f, -0.5f),
                    new Vector3(-0.5f, -0.5f, -0.5f),
                    new Vector3(-0.5f, 0.5f, -0.5f),
                    new Vector3(0.5f, 0.5f, -0.5f),
                    new Vector3(0.5f, -0.5f, 0.5f),
                    new Vector3(-0.5f, -0.5f, 0.5f),
                    new Vector3(-0.5f, 0.5f, 0.5f),
                    new Vector3(0.5f, 0.5f, 0.5f),
                },
                Triangles = new int[]
                {
                    0, 1, 3,
                    1, 2, 3,
                    1, 5, 2,
                    2, 5, 6,
                    5, 7, 6,
                    5, 4, 7,
                    4, 3, 7,
                    4, 0, 3,
                    3, 2, 6,
                    3, 6, 7,
                    0, 5, 1,
                    4, 5, 0
                },
            };
            meshRenderer.UseFlatShading = true;
            meshRenderer.Mesh = mesh;

            return cube;
        }
    }
}
