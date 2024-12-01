using SharpDX;
using System.Windows.Forms;

namespace DirectXEngine
{
    internal class CameraMovement : Updatable
    {
        public CameraMovement(GameObject attachedGameObject) : base(attachedGameObject)
        {

        }

        public Keys Forward = Keys.W;
        public Keys Left = Keys.A;
        public Keys Backward = Keys.S;
        public Keys Right = Keys.D;
        public Keys Up = Keys.Up;
        public Keys Down = Keys.Down;
        public bool Rotate = true;
        private float _MovementSpeed = 5;
        private float _RotationSpeed = 100;
        private float _SpeedMultiplier = 1;
        private Vector3 _FullRotation;

        protected override void OnUpdate()
        {
            Vector3 forward = Transform.Forward;
            Vector3 right = Transform.Right;
            Vector3 up = Vector3.Cross(forward, right).Normalized();

            Vector3 movement = Vector3.Zero;
            Vector3 rotation = Vector3.Zero;

            if (InputSystem.IsKeyPressed(Forward))
                movement += forward;
            if (InputSystem.IsKeyPressed(Left))
                movement -= right;
            if (InputSystem.IsKeyPressed(Backward))
                movement -= forward;
            if (InputSystem.IsKeyPressed(Right))
                movement += right;
            if (InputSystem.IsKeyPressed(Up))
                movement += up;
            if (InputSystem.IsKeyPressed(Down))
                movement -= up;

            if (InputSystem.IsKeyPressed(Keys.Left))
                rotation += Vector3.Down;
            if (InputSystem.IsKeyPressed(Keys.Right))
                rotation += Vector3.Up;

            if (InputSystem.IsKeyDown(Keys.ShiftKey))
                _SpeedMultiplier = 3;

            if (InputSystem.IsKeyDown(Keys.ControlKey))
                _SpeedMultiplier = 0.3f;

            if (InputSystem.IsKeyUp(Keys.ShiftKey) || InputSystem.IsKeyUp(Keys.ControlKey))
                _SpeedMultiplier = 1;
            
            Transform.WorldPosition += movement * (_MovementSpeed * _SpeedMultiplier * Time.FrameTime);

            if (!Rotate)
                return;

            _FullRotation += rotation * (_RotationSpeed * Time.FrameTime);
            Transform.WorldEulerAngles = _FullRotation;
        }
    }
}
