namespace DirectXEngine
{
    public abstract class Updatable : Startable
    {
        protected Updatable(GameObject attachedGameObject) : base(attachedGameObject)
        {
            
        }

        internal void InvokeUpdate() => OnUpdate();

        protected virtual void OnUpdate() { }
    }
}
