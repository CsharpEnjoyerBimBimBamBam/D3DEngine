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
using Assimp;
using System.Text.RegularExpressions;

namespace DirectXEngine
{
    internal class Graphics
    {
        public Graphics(Camera camera)
        {
            _Camera = camera;
            UpdateViewport();
            UpdateViews();
            UpdateDepthStencilState();
            UpdateRasterizerState();
        }

        public Texture2D DepthBufferTexture => _DepthView.ResourceAs<Texture2D>();
        public Texture2D Frame => _RenderView.ResourceAs<Texture2D>();
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
        public BlendStateDescription BlendStateDescription
        {
            get => _BlendStateDescription;
            set
            {
                _BlendStateDescription = value;
                UpdateBlendState();
            }
        }
        public OutputMode OutputMode = OutputMode.RenderTargetDepthBuffer;
        public virtual float Aspect { get; private set; }
        private DepthStencilView _DepthView;
        private RenderTargetView _RenderView;
        private Texture2D _RenderTarget;
        private Texture2D _DepthBuffer;
        private DepthStencilStateDescription _DepthStateDescription = DepthStencilStateDescription.Default();
        private RasterizerStateDescription _RasterizerDescription = RasterizerStateDescription.Default();
        private BlendStateDescription _BlendStateDescription = BlendStateDescription.Default();
        private DepthStencilState _DepthState;
        private RasterizerState _RasterizerState;
        private BlendState _BlendState;
        private Camera _Camera;
        private Size _Resolution = new Size(1920, 1080);
        private Viewport _Viewport;
        private float _DefaultDepth = 1f;
        private static Device _Device => EngineCore.Current.Device;
        private static DeviceContext _DeviceContext => _Device.ImmediateContext;
        private InputAssemblerStage _InputAssembler => _DeviceContext.InputAssembler;
        private static List<RendererData> _Renderers = new List<RendererData>();

        public static RendererData UpdateRenderer(Renderer renderer, RendererGraphicsSettings settings)
        {
            RendererData rendererData = _Renderers.Find(X => X.Renderer == renderer);
            RendererData updatedRendererData = UpdateRenderer(renderer);

            settings.InitializeBuffers(_Device);
            settings.DisposeBuffersData();

            if (rendererData == null)
                rendererData = updatedRendererData;
            else
                rendererData.Settings.Dispose();

            rendererData.Settings = settings;
            return rendererData;
        }

        public static RendererData UpdateRenderer(Renderer renderer)
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
                    ConstantData = new ShaderConstantData(shader, constantBufferSize)
                };

                _Renderers.Add(rendererData);
                return rendererData;
            }

            rendererData.ConstantData.Dispose();
            rendererData.ConstantData = new ShaderConstantData(shader, constantBufferSize);

            return rendererData;
        }

        public static bool RemoveRenderer(Renderer renderer)
        {
            int index = _Renderers.FindIndex(x => x.Renderer == renderer);

            if (index == -1)
                return false;

            RendererData rendererData = _Renderers[index];
            rendererData.ConstantData?.Dispose();

            _Renderers.RemoveAt(index);
            return true;
        }

        public void CopyDepthBuffer(Texture2D destenation)
        {
            _DeviceContext.CopyResource(_DepthBuffer, destenation);
        }

        public void DrawAll()
        {
            OnDraw();
            
            foreach (RendererData data in _Renderers)
                Draw(data);
        }

        public void DrawAll<T>() where T : Renderer
        {
            OnDraw();

            foreach (RendererData data in _Renderers)
            {
                if (data is not T)
                    continue;

                Draw(data);
            }
        }

        public void DrawAll<T>(Shader shader, byte[] constantBufferData) where T : Renderer
        {
            OnDraw();

            ShaderConstantData shaderData = new ShaderConstantData(shader, constantBufferData);

            SetShaderConstantData(shaderData);

            foreach (RendererData data in _Renderers)
                Draw<T>(data);

            shaderData.Dispose();
        }

        public void DrawAll(Dictionary<Renderer, ManualDrawDescription> rendererDescriptions, DepthStencilView depthView = null, RenderTargetView renderView = null)
        {
            depthView ??= _DepthView;
            renderView ??= _RenderView;

            ClearViews(depthView, renderView);
            SetOutput(depthView, renderView);

            foreach (RendererData data in _Renderers)
            {
                if (!rendererDescriptions.TryGetValue(data.Renderer, out ManualDrawDescription description))
                    continue;

                SetShaderConstantData(description.ConstantData);
                description.ConstantData.UpdateConstantBuffer(description.ConstantBufferData);
                data.Settings.Draw(_DeviceContext);
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
            ClearViews(_DepthView, _RenderView);
            SetOutput(_DepthView, _RenderView);
        }

        private void ClearViews(DepthStencilView depthView, RenderTargetView renderView)
        {
            _DeviceContext.ClearDepthStencilView(depthView, DepthStencilClearFlags.Depth, _DefaultDepth, 0);
            _DeviceContext.ClearRenderTargetView(renderView, _Camera.SkyColor);
        }

        private void SetOutput(DepthStencilView depthView, RenderTargetView renderView)
        {
            switch (OutputMode)
            {
                case OutputMode.RenderTargetDepthBuffer:
                    _DeviceContext.OutputMerger.SetTargets(_DepthView, _RenderView);
                    break;
                case OutputMode.RenderTarget:
                    DepthStencilView depthViewNull = null;
                    _DeviceContext.OutputMerger.SetTargets(depthViewNull, renderView);
                    break;
                case OutputMode.DepthBuffer:
                    RenderTargetView renderViewNull = null;
                    _DeviceContext.OutputMerger.SetTargets(depthView, renderViewNull);
                    break;
            }

            _DeviceContext.OutputMerger.SetDepthStencilState(_DepthState);
            _DeviceContext.OutputMerger.SetBlendState(_BlendState);
            _DeviceContext.Rasterizer.SetViewport(_Viewport);
            _DeviceContext.Rasterizer.State = _RasterizerState;
        }

        private void SetShaderConstantData(ShaderConstantData data)
        {
            _InputAssembler.InputLayout = data.Layout;
            _InputAssembler.PrimitiveTopology = data.Topology;
            _DeviceContext.VertexShader.Set(data.VertexShader);
            _DeviceContext.GeometryShader.Set(data.GeometryShader);
            _DeviceContext.PixelShader.Set(data.PixelShader);
            Buffer constantBuffer = data.ConstantBuffer;
            _DeviceContext.VertexShader.SetConstantBuffer(0, constantBuffer);
            _DeviceContext.PixelShader.SetConstantBuffer(0, constantBuffer);
        }

        private void Draw(RendererData rendererData)
        {
            Renderer renderer = rendererData.Renderer;

            //if (!renderer.NeedToDrawInternal(_Camera))
            //    return;

            ShaderConstantData constantData = rendererData.ConstantData;
            SetShaderConstantData(constantData);

            ShaderDynamicResources shaderResources = renderer.GetResourcesInternal(_Camera);
            constantData.UpdateConstantBuffer(shaderResources.ConstantBufferData);

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
            int width = _Resolution.Width;
            int height = _Resolution.Height;

            _Viewport = new Viewport(0, 0, width, height, 0.0f, 1.0f);
            Aspect = width / (float)height;
        }

        private void UpdateViews()
        {
            Utilities.Dispose(ref _RenderTarget);
            Utilities.Dispose(ref _DepthBuffer);
            Utilities.Dispose(ref _RenderView);
            Utilities.Dispose(ref _DepthView);

            _DepthBuffer = UpdateDepthBuffer(_Viewport);
            _RenderTarget = UpdateRenderTarget(_Viewport);

            _DepthView = new DepthStencilView(_Device, _DepthBuffer);
            _RenderView = new RenderTargetView(_Device, _RenderTarget);
        }

        protected virtual Texture2D UpdateDepthBuffer(Viewport viewport) => new Texture2D(_Device, new Texture2DDescription()
        {
            Format = Format.D32_Float,
            ArraySize = 1,
            MipLevels = 1,
            Width = viewport.Width,
            Height = viewport.Height,
            SampleDescription = new SampleDescription(1, 0),
            Usage = ResourceUsage.Default,
            BindFlags = BindFlags.DepthStencil,
            CpuAccessFlags = CpuAccessFlags.None,
            OptionFlags = ResourceOptionFlags.None
        });

        protected virtual Texture2D UpdateRenderTarget(Viewport viewport) => new Texture2D(_Device, new Texture2DDescription
        {
            Format = Format.R8G8B8A8_UNorm,
            ArraySize = 1,
            MipLevels = 1,
            Width = viewport.Width,
            Height = viewport.Height,
            SampleDescription = new SampleDescription(1, 0),
            Usage = ResourceUsage.Default,
            BindFlags = BindFlags.RenderTarget,
            CpuAccessFlags = CpuAccessFlags.None,
            OptionFlags = ResourceOptionFlags.None
        });

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

        private void UpdateBlendState()
        {
            Utilities.Dispose(ref _BlendState);
            _BlendState = new BlendState(_Device, _BlendStateDescription);
        }
    }
}
