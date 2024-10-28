using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using Device = SharpDX.Direct3D11.Device;
using Buffer = SharpDX.Direct3D11.Buffer;
using Resource = SharpDX.Direct3D11.Resource;
using MapFlags = SharpDX.Direct3D11.MapFlags;
using System.Collections.Generic;
using SharpDX.DXGI;
using System.Windows.Forms;
using SharpDX;
using System.Drawing;
using SharpDX.Mathematics.Interop;
using System.Runtime.Remoting.Contexts;
using SharpDX.Windows;
using System;

namespace DirectXEngine
{
    internal class Graphics
    {
        internal Graphics(Camera camera)
        {
            _DeviceContext = _Device.ImmediateContext;
            _InputAssembler = _DeviceContext.InputAssembler;
            _Camera = camera;
            UpdateViewport();
            UpdateViews();
            UpdateDepthStencilState();
            UpdateRasterizerState();
        }

        internal Graphics(Camera camera, DepthStencilView depthView, RenderTargetView renderView)
        {
            _DeviceContext = _Device.ImmediateContext;
            _InputAssembler = _DeviceContext.InputAssembler;
            _Camera = camera;
            DepthView = depthView;
            RenderView = renderView;
            UpdateDepthStencilState();
            UpdateRasterizerState();
        }

        public Texture2D DepthBuffer
        {
            get
            {
                //Texture2DDescription description = new Texture2DDescription
                //{
                //    Width = _Resolution.Width,
                //    Height = _Resolution.Height,
                //    MipLevels = 1,
                //    ArraySize = 1,
                //    Format = Format.R32_Typeless,
                //    SampleDescription = new SampleDescription(1, 0),
                //    Usage = ResourceUsage.Default,
                //    BindFlags = BindFlags.DepthStencil | BindFlags.ShaderResource,
                //    CpuAccessFlags = CpuAccessFlags.None,
                //    OptionFlags = ResourceOptionFlags.None
                //};
                //
                //Texture2D depthBuffer = new Texture2D(_Device, description);
                //Resource depthBufferResource = DepthView.Resource;
                return DepthView.ResourceAs<Texture2D>();
                //_DeviceContext.CopyResource(depthBufferResource, depthBuffer);
                //depthBufferResource.Dispose();
                //
                //return depthBuffer;
            }
        }
        public Texture2D Frame => RenderView.ResourceAs<Texture2D>();
        public virtual Size Resolution
        {
            get => _Resolution;
            set
            {
                if (value == _Resolution)
                    return;

                ExceptionHelper.ThrowIfOutOfRange(value.Width, 0, double.PositiveInfinity);
                ExceptionHelper.ThrowIfOutOfRange(value.Height, 0, double.PositiveInfinity);
                _Resolution = value;
                UpdateViewport();
                UpdateViews();
            }
        }
        public float DefaultDepth
        {
            get => _DefaultDepth;
            set
            {
                ExceptionHelper.ThrowIfOutOfRange01(value);
                _DefaultDepth = value;
            }
        }
        public DepthStencilStateDescription DepthStateDescription
        {
            get => _DepthStateDescription;
            set
            {
                _DepthStateDescription = value;
                UpdateDepthStencilState();
            }
        }
        public RasterizerStateDescription RasterizerDescription
        {
            get => _RasterizerDescription;
            set
            {
                _RasterizerDescription = value;
                UpdateRasterizerState();
            }
        }
        public OutputMode OutputMode = OutputMode.RenderTargetDepthBuffer;
        public float Aspect { get; private set; }
        protected virtual DepthStencilView DepthView { get; private set; }
        protected virtual RenderTargetView RenderView { get; private set; }
        private DepthStencilStateDescription _DepthStateDescription = DepthStencilStateDescription.Default();
        private RasterizerStateDescription _RasterizerDescription = RasterizerStateDescription.Default();
        private DepthStencilState _DepthState;
        private RasterizerState _RasterizerState;
        private Camera _Camera;
        private Size _Resolution = new Size(800, 800);
        private Viewport _Viewport;
        private float _DefaultDepth = 1f;
        private static Device _Device => EngineCore.Current.Device;
        private DeviceContext _DeviceContext;
        private Texture2D _RenderTarget;
        private Texture2D _DepthBuffer;
        private InputAssemblerStage _InputAssembler;
        private static List<RendererData> _Renderers = new List<RendererData>();

        internal void UpdateViews(DepthStencilView depthView, RenderTargetView renderView)
        {
            DepthView = depthView;
            RenderView = renderView;
        }

        internal static RendererData UpdateRenderer(Renderer renderer, RendererGraphicsSettings settings)
        {
            RendererData rendererData = _Renderers.Find(X => X.Renderer == renderer);
            RendererData updatedRendererData = UpdateRenderer(renderer);

            settings.InitializeBuffers(_Device);
            settings.DisposeBuffersData();

            if (rendererData == null)
                rendererData = updatedRendererData;
            
            rendererData.Settings = settings;
            return rendererData;
        }

        internal static RendererData UpdateRenderer(Renderer renderer)
        {
            Material material = renderer.Material;
            Shader shader = material.Shader;

            int constantBufferSize = shader.ConstantBufferSize;

            RendererData rendererData = _Renderers.Find(X => X.Renderer == renderer);
            if (rendererData == null)
            {
                rendererData = new RendererData
                {
                    Renderer = renderer,
                    ConstantData = new ShaderConstantData(_Device, shader, constantBufferSize)
                };

                _Renderers.Add(rendererData);
                return rendererData;
            }

            rendererData.ConstantData.Dispose();
            return rendererData;
        }

        internal void DrawAll()
        {
            OnDraw();

            foreach (RendererData data in _Renderers)
                Draw(data);
        }

        internal void DrawAll<T>() where T : Renderer
        {
            OnDraw();

            foreach (RendererData data in _Renderers)
            {
                if (data is not T)
                    continue;

                Draw(data);
            }
        }

        internal void DrawAll<T>(Shader shader, byte[] constantBufferData) where T : Renderer
        {
            OnDraw();

            ShaderConstantData shaderData = new ShaderConstantData(_Device, shader, constantBufferData);

            SetShaderConstantData(shaderData);

            foreach (RendererData data in _Renderers)
                Draw<T>(data);

            shaderData.Dispose();
        }

        internal void DrawAll(Dictionary<Renderer, ManualDrawDescription> rendererDescriptions)
        {
            OnDraw();

            foreach (RendererData data in _Renderers)
            {
                if (!rendererDescriptions.TryGetValue(data.Renderer, out ManualDrawDescription description))
                    continue;

                ShaderConstantData shaderData = new ShaderConstantData(_Device, description.Shader, description.ConstantBufferData);
                
                SetShaderConstantData(shaderData);
                data.Settings.Draw(_DeviceContext);
                shaderData.Dispose();
            }
        }

        private void Draw<T>(RendererData data) where T : Renderer
        {
            if (data.Renderer is not T)
                return;
            data.Settings.Draw(_DeviceContext);
        }

        private void OnDraw()
        {
            ClearViews();
            SetOutput();
        }

        private void ClearViews()
        {
            _DeviceContext.ClearDepthStencilView(DepthView, DepthStencilClearFlags.Depth, _DefaultDepth, 0);
            _DeviceContext.ClearRenderTargetView(RenderView, _Camera.SkyColor);
        }

        private void SetOutput()
        {
            switch (OutputMode)
            {
                case OutputMode.RenderTargetDepthBuffer:
                    _DeviceContext.OutputMerger.SetTargets(DepthView, RenderView);
                    break;
                case OutputMode.RenderTarget:
                    DepthStencilView depthView = null;
                    _DeviceContext.OutputMerger.SetTargets(depthView, RenderView);
                    break;
                case OutputMode.DepthBuffer:
                    RenderTargetView renderView = null;
                    _DeviceContext.OutputMerger.SetTargets(DepthView, renderView);
                    break;
            }

            _DeviceContext.OutputMerger.SetDepthStencilState(_DepthState);
            _DeviceContext.Rasterizer.SetViewport(_Viewport);
            _DeviceContext.Rasterizer.State = _RasterizerState;
        }

        private void SetShaderConstantData(ShaderConstantData data)
        {
            _InputAssembler.InputLayout = data.Layout;
            _InputAssembler.PrimitiveTopology = data.Topology;
            _DeviceContext.VertexShader.Set(data.VertexShader);
            _DeviceContext.PixelShader.Set(data.PixelShader);
            Buffer constantBuffer = data.ConstantBuffer;
            _DeviceContext.VertexShader.SetConstantBuffer(0, constantBuffer);
            _DeviceContext.PixelShader.SetConstantBuffer(0, constantBuffer);
        }

        private void Draw(RendererData rendererData)
        {
            Renderer renderer = rendererData.Renderer;
            ShaderConstantData constantData = rendererData.ConstantData;
            SetShaderConstantData(constantData);

            ShaderDynamicResources shaderResources = renderer.GetResourcesInternal(_Camera);

            Buffer constantBuffer = constantData.ConstantBuffer;
            _DeviceContext.UpdateSubresource(shaderResources.ConstantBufferData, constantBuffer);

            SetShaderResources(shaderResources.Resources);
            SetShaderSamplers(shaderResources.Samplers);
            
            rendererData.Settings.Draw(_DeviceContext);
        }

        private void SetShaderResources(ShaderResource[] shaderResources)
        {
            foreach (ShaderResource shaderResource in shaderResources)
                shaderResource.Set(_Device);
        }

        private void SetShaderSamplers(ShaderSampler[] samplers)
        {
            foreach (ShaderSampler sampler in samplers)
                sampler.Set(_Device);
        }

        protected void UpdateViewport()
        {
            int width = Resolution.Width;
            int height = Resolution.Height;

            _Viewport = new Viewport(0, 0, width, height, 0.0f, 1.0f);
            Aspect = width / (float)height;
        }

        private void UpdateViews()
        {
            RenderTargetView renderView = RenderView;
            DepthStencilView depthView = DepthView;

            Utilities.Dispose(ref _RenderTarget);
            Utilities.Dispose(ref _DepthBuffer);
            Utilities.Dispose(ref renderView);
            Utilities.Dispose(ref depthView);
            
            int width = _Viewport.Width;
            int height = _Viewport.Height;
            
            _DepthBuffer = new Texture2D(_Device, new Texture2DDescription()
            {
                Format = Format.D32_Float,
                ArraySize = 1,
                MipLevels = 1,
                Width = width,
                Height = height,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.DepthStencil,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            });
            
            DepthView = new DepthStencilView(_Device, _DepthBuffer);
            
            _RenderTarget = new Texture2D(_Device, new Texture2DDescription
            {
                Format = Format.R8G8B8A8_UNorm,
                ArraySize = 1,
                MipLevels = 1,
                Width = width,
                Height = height,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.RenderTarget,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            });

            RenderView = new RenderTargetView(_Device, _RenderTarget);
        }

        private void UpdateDepthStencilState()
        {
            Utilities.Dispose(ref _DepthState);
            _DepthState = new DepthStencilState(_Device, _DepthStateDescription);
        }

        private void UpdateRasterizerState()
        {
            Utilities.Dispose(ref _RasterizerState);
            _RasterizerState = new RasterizerState(_Device, _RasterizerDescription);
        }
    }
}
