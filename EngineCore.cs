using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Windows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Device = SharpDX.Direct3D11.Device;
using Resource = SharpDX.Direct3D11.Resource;

namespace DirectXEngine
{
    internal class EngineCore
    {
        private EngineCore()
        {

        }

        public event Action FrameRenderStart;
        public event Action<Size> FormResized;
        public static EngineCore Current { get; } = new EngineCore();
        public float FrameTime { get; private set; }
        public IReadOnlyDictionary<Keys, KeyState> KeysState => _KeysStates;
        public Size RenderFormResolution
        {
            get => _RenderForm.ClientSize;
            set => _RenderForm.ClientSize = value;
        }
        public Device Device { get; private set; }
        public DeviceContext DeviceContext { get; private set; }
        public RenderTargetView RenderView => _RenderView;
        public DepthStencilView DepthView => _DepthView;
        private RenderTargetView _RenderView;
        private Texture2D _BackBuffer;
        private Texture2D _DepthBuffer;
        private DepthStencilView _DepthView;
        private Dictionary<Keys, KeyState> _KeysStates;
        private Dictionary<Keys, KeyState> _PreviousFrameKeysState;
        private SwapChain _SwapChain;
        private SwapChainDescription _Description;
        private RenderForm _RenderForm = new RenderForm("Engine");
        private Stopwatch _FrameTimer = new Stopwatch();
        private bool _IsUserResized = false;

        public void Run() => RenderLoop.Run(_RenderForm, OnRenderFrame);

        public void Initialize()
        {
            UpdateSwapChainDescription();

            Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.None, _Description, out Device device, out _SwapChain);
            Device = device;
            DeviceContext = Device.ImmediateContext;
            Factory factory = _SwapChain.GetParent<Factory>();
            factory.MakeWindowAssociation(_RenderForm.Handle, WindowAssociationFlags.IgnoreAll);
            factory.Dispose();

            _RenderForm.KeyPreview = true;
            _RenderForm.KeyDown += (e, args) => ChangeKeyStateToDown(args.KeyCode, KeyState.Down);
            _RenderForm.KeyUp += (e, args) => _KeysStates[args.KeyCode] = KeyState.Up;
            
            _RenderForm.UserResized += (e, args) => _IsUserResized = true;

            UpdateViews();
            _KeysStates = FillKeyStates();
            _PreviousFrameKeysState = new Dictionary<Keys, KeyState>(_KeysStates);
        }

        private void ChangeKeyStateToDown(Keys key, KeyState state)
        {
            KeyState currentState = _KeysStates[key];

            if (currentState == KeyState.Pressed && state == KeyState.Down)
                return;

            _KeysStates[key] = state;
        }
        
        private void OnRenderFrame()
        {
            if (_IsUserResized)
            {
                UpdateViews();
                FormResized?.Invoke(_RenderForm.ClientSize);
                _IsUserResized = false;
            }

            UpdateKeysStates();
            FrameTime = (float)_FrameTimer.Elapsed.TotalSeconds;
            _FrameTimer.Restart();
            FrameRenderStart?.Invoke();
            Camera.Main.Graphics.DrawAll();
            _SwapChain.Present(0, PresentFlags.None);
            _RenderForm.Text = (1 / FrameTime).ToString();
            _PreviousFrameKeysState = new Dictionary<Keys, KeyState>(_KeysStates);
        }

        private Dictionary<Keys, KeyState> FillKeyStates()
        {
            Keys[] keyValues = (Keys[])Enum.GetValues(typeof(Keys));

            Dictionary<Keys, KeyState> keysStates = new Dictionary<Keys, KeyState>();

            foreach (Keys keyValue in keyValues)
                keysStates[keyValue] = KeyState.Released;

            return keysStates;
        }

        private void UpdateKeysStates()
        {
            Dictionary<Keys, KeyState> newKeysStates = new Dictionary<Keys, KeyState>();

            foreach (KeyValuePair<Keys, KeyState> keyState in _KeysStates)
            {
                Keys currentKey = keyState.Key;
                KeyState currentState = keyState.Value;
                KeyState previousFrameState = _PreviousFrameKeysState[currentKey];

                if (currentState == KeyState.Up && previousFrameState == KeyState.Up)
                {
                    newKeysStates[currentKey] = KeyState.Released;
                    continue;
                }

                if (currentState == KeyState.Down && previousFrameState == KeyState.Down)
                {
                    newKeysStates[currentKey] = KeyState.Pressed;
                    continue;
                }

                newKeysStates[currentKey] = currentState;
            }
            
            _KeysStates = newKeysStates;
        }

        private void UpdateSwapChainDescription() => _Description = new SwapChainDescription()
        {
            BufferCount = 1,
            ModeDescription =
                new ModeDescription(_RenderForm.ClientSize.Width, _RenderForm.ClientSize.Height,
                                        new Rational(60, 1), Format.R8G8B8A8_UNorm),
            IsWindowed = true,
            OutputHandle = _RenderForm.Handle,
            SampleDescription = new SampleDescription(1, 0),
            SwapEffect = SwapEffect.Discard,
            Usage = Usage.RenderTargetOutput
        };

        private void UpdateViews()
        {
            Utilities.Dispose(ref _BackBuffer);
            Utilities.Dispose(ref _DepthBuffer);
            Utilities.Dispose(ref _RenderView);
            Utilities.Dispose(ref _DepthView);
            
            int width = _RenderForm.ClientSize.Width;
            int height = _RenderForm.ClientSize.Height;

            _SwapChain.ResizeBuffers(_Description.BufferCount, width, height, Format.Unknown, SwapChainFlags.None);
            
            _BackBuffer = Resource.FromSwapChain<Texture2D>(_SwapChain, 0);
            
            _RenderView = new RenderTargetView(Device, _BackBuffer);

            _DepthBuffer = new Texture2D(Device, new Texture2DDescription()
            {
                Format = Format.D32_Float,
                ArraySize = 1,
                MipLevels = 1,
                Width = _RenderForm.ClientSize.Width,
                Height = _RenderForm.ClientSize.Height,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.DepthStencil,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            });

            _DepthView = new DepthStencilView(Device, _DepthBuffer);
        }
    }
}
