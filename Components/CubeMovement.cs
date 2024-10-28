using SharpDX;

namespace DirectXEngine
{
    internal class CubeMovement : Updatable
    {
        public CubeMovement(GameObject attachedGameObject) : base(attachedGameObject)
        {

        }

        public Vector3 Rotation = new Vector3(60, 60 ,60);

        protected override void OnUpdate()
        {
            Transform.WorldRotation += Rotation * Time.FrameTime;
        }
    }
}
