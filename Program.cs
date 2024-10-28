using SharpDX.DXGI;
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DirectXEngine
{
    internal static class Program
    {
        private static void Main()
        {
            EngineCore core = EngineCore.Current;
            core.Initialize();
            Camera.Main.AddComponent<Initializer>();
            core.FrameRenderStart += Camera.Main.InvokeOnUpdate;
            core.Run();
        }
    }
}
