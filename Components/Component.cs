using System;
using System.Collections;
using System.Text.Json.Serialization;

namespace DirectXEngine
{
    public abstract class Component : Prefab
    {
        public Component(GameObject attachedGameObject)
        {
            ExceptionHelper.ThrowIfNull(attachedGameObject);
            GameObject = attachedGameObject;
        }

        [JsonIgnore] public Transform Transform => GameObject.Transform;
        [JsonIgnore] public GameObject GameObject { get; private set; }

        internal Component Clone(GameObject attachedGameObject)
        {
            Component componentCopy = this is ICloneableComponent clonable ? clonable.Clone() : MemberwiseClone() as Component;
            componentCopy.GameObject = attachedGameObject;
            return componentCopy;
        }

        internal void InvokeOnInstantiate() => OnInstantiate();

        internal void InvokeOnDestroy() => OnDestroy();

        internal void InvokeOnRemove() => OnRemove();

        protected virtual void OnInstantiate() { }

        protected virtual void OnDestroy() { }

        protected virtual void OnRemove() { }
    }
}
