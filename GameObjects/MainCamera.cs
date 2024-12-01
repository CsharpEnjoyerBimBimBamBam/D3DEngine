namespace DirectXEngine
{
    internal class MainCamera : Camera
    {
        public MainCamera(bool isInstantiated) : base(isInstantiated, false)
        {
            Graphics = new MainCameraGraphics(this);
            UpdateFieldOfView(60);
        }

        internal override Graphics Graphics { get; }
    }
}
