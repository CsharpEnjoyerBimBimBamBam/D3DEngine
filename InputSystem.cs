using System.Windows.Forms;

namespace DirectXEngine
{
    public class InputSystem
    {
        public static bool IsKeyDown(Keys key) => CheckKey(key, KeyState.Down);

        public static bool IsKeyUp(Keys key) => CheckKey(key, KeyState.Up);

        public static bool IsKeyPressed(Keys key) => CheckKey(key, KeyState.Pressed);

        private static bool CheckKey(Keys key, KeyState state) => EngineCore.Current.KeysState[key] == state;
    }
}
