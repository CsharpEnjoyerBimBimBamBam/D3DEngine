using SharpDX;
using System.Windows.Forms;

namespace DirectXEngine
{
    internal class CubeMovement : Updatable
    {
        public CubeMovement(GameObject attachedGameObject) : base(attachedGameObject)
        {
            
        }

        public Vector3 Rotation
        {
            get => _Rotation;
            set
            {
                _Rotation = value;
            }
        }
        private Vector3 _Rotation = new Vector3(60, 60, 60);
        private Quaternion _RotationQuaternion;
        private Quaternion _FullRotation;
        private Vector3 _FullRotationEuler;

        protected override void OnUpdate()
        {
            _FullRotationEuler += _Rotation * Time.FrameTime;
            Transform.WorldEulerAngles = _FullRotationEuler;
        }
    }
}
