using SharpDX.Direct3D11;
using Device = SharpDX.Direct3D11.Device;
using System.Drawing;
using SharpDX.DXGI;
using Resource = SharpDX.Direct3D11.Resource;
using SharpDX.Windows;
using SharpDX;

namespace DirectXEngine
{
    internal class MainCameraGraphics : Graphics
    {
        public MainCameraGraphics(Camera mainCamera) : base(mainCamera)
        {
            Size size = EngineCore.Current.RenderForm.ClientSize;
            UpdateAspect(size.Width, size.Height);
            EngineCore.Current.FormResized += (size) => UpdateAspect(size.Width, size.Height);
        }

        public override float Aspect => _Aspect;
        private float _Aspect;

        protected override Texture2D UpdateRenderTarget(Viewport viewport)
        {
            int width = viewport.Width;
            int height = viewport.Height;

            EngineCore.Current.SwapChain.ResizeBuffers(1, width, height, Format.Unknown, SwapChainFlags.None);

            return Resource.FromSwapChain<Texture2D>(EngineCore.Current.SwapChain, 0);
        }

        private void UpdateAspect(float width, float height) => _Aspect = width / height;
    }
}
