using SharpDX.Direct3D11;
using Device = SharpDX.Direct3D11.Device;
using System.Drawing;

namespace DirectXEngine
{
    internal class MainCameraGraphics : Graphics
    {
        public MainCameraGraphics(Camera mainCamera, DepthStencilView depthView, RenderTargetView renderView) : base(mainCamera, depthView, renderView)
        {
            UpdateViewport();
            EngineCore.Current.FormResized += (size) => UpdateViewport();
        }

        public override Size Resolution 
        { 
            get => EngineCore.Current.RenderFormResolution; 
            set => EngineCore.Current.RenderFormResolution = value;
        }

        protected override DepthStencilView DepthView => EngineCore.Current.DepthView;
        protected override RenderTargetView RenderView => EngineCore.Current.RenderView;
    }
}
