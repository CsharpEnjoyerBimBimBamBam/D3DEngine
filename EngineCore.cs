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
        public Device Device { get; private set; }
        public DeviceContext DeviceContext { get; private set; }
        public RenderForm RenderForm { get; } = new RenderForm("Engine");
        public SwapChain SwapChain { get; private set; }
        private Dictionary<Keys, KeyState> _KeysStates;
        private Dictionary<Keys, KeyState> _PreviousFrameKeysState;
        private SwapChainDescription _Description;
        private Stopwatch _FrameTimer = new Stopwatch();
        private bool _IsUserResized = false;

        public void Run() => RenderLoop.Run(RenderForm, OnRenderFrame);

        public void Initialize()
        {
            UpdateSwapChainDescription();
            
            Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.None, _Description, out Device device, out SwapChain swapChain);
            SwapChain = swapChain;
            Device = device;
            DeviceContext = Device.ImmediateContext;
            Factory factory = SwapChain.GetParent<Factory>();
            factory.MakeWindowAssociation(RenderForm.Handle, WindowAssociationFlags.IgnoreAll);
            factory.Dispose();

            RenderForm.KeyPreview = true;
            RenderForm.KeyDown += (e, args) => ChangeKeyStateToDown(args.KeyCode, KeyState.Down);
            RenderForm.KeyUp += (e, args) => _KeysStates[args.KeyCode] = KeyState.Up;
            RenderForm.UserResized += (e, args) => _IsUserResized = true;

            //UpdateViews();
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
                FormResized?.Invoke(RenderForm.ClientSize);
                _IsUserResized = false;
            }

            UpdateKeysStates();
            FrameTime = (float)_FrameTimer.Elapsed.TotalSeconds;
            _FrameTimer.Restart();
            FrameRenderStart?.Invoke();
            Camera.Main.Graphics.DrawAll();
            SwapChain.Present(0, PresentFlags.None);
            RenderForm.Text = (1 / FrameTime).ToString();
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
                new ModeDescription(RenderForm.ClientSize.Width, RenderForm.ClientSize.Height,
                                        new Rational(60, 1), Format.R8G8B8A8_UNorm),
            IsWindowed = true,
            OutputHandle = RenderForm.Handle,
            SampleDescription = new SampleDescription(1, 0),
            SwapEffect = SwapEffect.Discard,
            Usage = Usage.RenderTargetOutput
        };
    }
}
