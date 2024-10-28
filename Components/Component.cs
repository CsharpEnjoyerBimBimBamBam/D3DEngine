using System;

namespace DirectXEngine
{
    public abstract class Component
    {
        public Component(GameObject attachedGameObject)
        {
            ExceptionHelper.ThrowIfNull(attachedGameObject);
            GameObject = attachedGameObject;
        }

        public Transform Transform => GameObject.Transform;
        public GameObject GameObject { get; private set; }

        internal Component Copy(GameObject attachedGameObject)
        {        
            Component componentCopy = this is ICloneableComponent clonable ? clonable.Clone() : MemberwiseClone() as Component;
            componentCopy.GameObject = attachedGameObject;
            return componentCopy;
        }

        internal void InvokeOnInstantiate() => OnInstantiate();

        protected virtual void OnInstantiate() { }
    }
}
