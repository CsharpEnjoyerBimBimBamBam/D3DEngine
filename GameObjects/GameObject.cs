using Microsoft.SqlServer.Server;
using SharpDX;
using SharpDX.Direct2D1;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace DirectXEngine
{
    [Serializable]
    [JsonConverter(typeof(GameObjectConverter))]
    public class GameObject : Prefab
    {
        public GameObject()
        {
            _Transform = AddComponent<Transform>();
        }

        internal GameObject(bool isInstantiated)
        {
            IsInstantiated = isInstantiated;
            _Transform = AddComponent<Transform>();
        }

        internal GameObject(bool isInstantiated, bool addTransform)
        {
            IsInstantiated = isInstantiated;
            if (addTransform)
                _Transform = AddComponent<Transform>();
        }

        [JsonIgnore]
        public Transform Transform
        {
            get
            {
                if (_Transform == null)
                    _Transform = GetComponent<Transform>();
                return _Transform;
            }
        }
        public string Name
        {
            get => _Name;
            set
            {
                ExceptionHelper.ThrowIfNull(value);
                _Name = value;
            }
        }
        public bool Enabled 
        {
            get => _Enabled;
            set => _Enabled = value;
        }
        [JsonIgnore]
        public bool IsInstantiated { get; } = false;
        private bool _Enabled = true;
        private Transform _Transform;
        private string _Name = string.Empty;
        [SerializeMemberAttribute] private List<Component> _Components = new List<Component>();
        private List<Startable> _Startables = new List<Startable>();
        private List<Updatable> _Updatables = new List<Updatable>();
        private const string _ComponentExistException = "Game object already have a component";
        private const string _ComponentTypeException = "Type must be subclass of Component";

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
                case GameObjectType.PointLight:
                    GameObject pointLight = Scene.Current.Instantiate();
                    pointLight.AddComponent<PointLight>();
                    return pointLight;
                default: return new GameObject();
            }
        }

        public T GetComponent<T>() where T : class => _Components[GetComponentIndex<T>()] as T;

        public bool TryGetComponent<T>(out T component) where T : class
        {
            component = _Components.FirstOrDefault(X => X is T) as T;         
            return component != null;
        }

        public IEnumerable<T> GetComponents<T>() where T : class => _Components.FindAll(X => X is T).Cast<T>();

        public Component AddComponent(Type componentType)
        {
            ExceptionHelper.ThrowByCondition(!componentType.IsSubclassOf(typeof(Component)), _ComponentTypeException);
            CheckIfComponentAdded(componentType);

            Component component = (Component)Activator.CreateInstance(componentType, this);

            UpdateComponentOnAdd(component);

            return component;
        }

        public T AddComponent<T>() where T : Component
        {
            Type componentType = typeof(T);

            CheckIfComponentAdded(componentType);

            T component = (T)Activator.CreateInstance(componentType, this);

            UpdateComponentOnAdd(component);

            return component;
        }

        public void RemoveComponent<T>() where T : class
        {
            int index = GetComponentIndex<T>();
            _Components[index].InvokeOnRemove();
            _Components.RemoveAt(index);
        }

        internal GameObject Copy()
        {
            GameObject copy = CopyWithoutChildrens();
            CopyChildrens(copy.Transform);
            return copy;
        }

        internal void InvokeOnInstantiate() => InvokeEvents(_Components, (updatable) => updatable.InvokeOnInstantiate());

        internal void InvokeOnStart() => InvokeEvents(_Startables, (updatable) => updatable.InvokeOnStart());

        internal void InvokeOnEnd() => InvokeEvents(_Startables, (updatable) => updatable.InvokeOnEnd());

        internal void InvokeOnUpdate() => InvokeEvents(_Updatables, (updatable) => updatable.InvokeUpdate());

        internal void InvokeOnDestroy() => InvokeEvents(_Components, (component) =>  component.InvokeOnDestroy());

        protected virtual GameObject CopyWithoutChildrens()
        {
            GameObject gameObjectCopy = Scene.Current.Instantiate(false);
            gameObjectCopy._Name = _Name;
            
            foreach (Component component in _Components)
            {
                Component componentCopy = component.Clone(gameObjectCopy);
                gameObjectCopy.UpdateComponentOnAdd(componentCopy);
            }

            gameObjectCopy.InvokeOnInstantiate();
            return gameObjectCopy;
        }

        private void CopyChildrens(Transform parent)
        {
            InvokeOnChildrens(children =>
            {
                GameObject childrenCopy = children.CopyWithoutChildrens();
                childrenCopy.Transform.Parent = parent;
                children.CopyChildrens(childrenCopy.Transform);
            });
        }

        private void InvokeOnChildrens(Action<GameObject> action) =>
            Transform.Childrens.SafetyForEach(transform => action.Invoke(transform.GameObject));

        private void InvokeEvents<T>(List<T> components, Action<T> action) where T : Component => 
            components.SafetyForEach(x => action(x));

        private void CheckIfComponentAdded(Type componentType) =>
            ExceptionHelper.ThrowByCondition(_Components.Find(X => X.GetType().Equals(componentType)) != null, _ComponentExistException + $" {componentType.Name}");

        private int GetComponentIndex<T>() where T : class
        {
            int componentIndex = _Components.FindIndex(X => X is T);
            ExceptionHelper.ThrowByCondition(componentIndex == -1, $"Game object have no component {typeof(T).Name}");
            return componentIndex;
        }

        private void UpdateComponentOnAdd(Component component)
        {
            _Components.Add(component);

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
