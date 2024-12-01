
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
