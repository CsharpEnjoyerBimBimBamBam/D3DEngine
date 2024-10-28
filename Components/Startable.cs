namespace DirectXEngine
{
    public abstract class Startable : Component
    {
        protected Startable(GameObject attachedGameObject) : base(attachedGameObject)
        {

        }

        internal void InvokeOnStart() => OnStart();

        internal void InvokeOnEnd() => OnEnd();

        protected virtual void OnStart() { }

        protected virtual void OnEnd() { }
    }
}
