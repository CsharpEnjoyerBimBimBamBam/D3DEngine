namespace DirectXEngine
{
    internal class MainCamera : Camera
    {
        public MainCamera(bool isInstantiated) : base(isInstantiated, false)
        {
            Graphics = new MainCameraGraphics(this, EngineCore.Current.DepthView, EngineCore.Current.RenderView);
            UpdateFieldOfView(60);
        }

        internal override Graphics Graphics { get; }
    }
}
