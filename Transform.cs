using System;
using System.Collections.Generic;
using System.Windows.Forms;
using SharpDX;

namespace DirectXEngine
{
    public class Transform
    {
        public Vector3 WorldPosition
        {
            get => _WorldPosition;
            set => _WorldPosition = value;
        }
        public Vector3 WorldRotation { get; set; }
        public Vector3 LocalPosition { get; set; }
        public Vector3 Size
        {
            get => _Size;
            set
            {
                ExceptionHelper.ThrowByCondition(value.X < 0 || value.Y < 0 || value.Z < 0);
                _Size = value;
            }
        }
        public Vector3 Forward => GetForward(RotationMatrix);
        public Vector3 Right => GetRight(RotationMatrix);
        public Vector3 Up => GetUp(RotationMatrix);
        public Transform Parent
        {
            get => _Parent;
            set
            {
                if (_Parent == value)
                    return;

                if (value == null)
                {
                    _Parent?._Childrens.Remove(this);
                    _Parent = null;
                    return;
                }

                ExceptionHelper.ThrowByCondition(value == this);

                _Parent._Childrens.Remove(this);
                _Parent = value;
                _Parent._Childrens.Add(this);
            }
        }
        public IReadOnlyList<Transform> Childrens => _Childrens;
        public Matrix LocalToWorldMatrix => Matrix.Scaling(_Size) * RotationMatrix * Matrix.Translation(_WorldPosition);
        public Matrix WorldToLocalMatrix => Matrix.Scaling(1 / _Size) * ViewMatrix;
        public Matrix RotationMatrix
        {
            get
            {
                Vector3 rotationInRadians = _WorldRotationInRadians;
                return Matrix.RotationX(rotationInRadians.X) * Matrix.RotationY(rotationInRadians.Y) * Matrix.RotationZ(rotationInRadians.Z);
            }
        }
        public Matrix ViewMatrix
        {
            get
            {
                Matrix rotationMatrix = RotationMatrix;
                Vector3 forward = Vector3.Transform(Vector3.ForwardLH, rotationMatrix).ToVector3();
                Vector3 up = Vector3.Transform(Vector3.Up, rotationMatrix).ToVector3();
                return Matrix.LookAtLH(_WorldPosition, _WorldPosition + forward, up);
            }
        }
        private Vector3 _WorldPosition;
        private Vector3 _WorldRotationInRadians => WorldRotation * MathUtil.DegreesToRadians(1);
        private Transform _Parent;
        private List<Transform> _Childrens = new List<Transform>();
        private Vector3 _Size = new Vector3(1, 1, 1);

        public Vector3 GetForward(Matrix rotationMatrix) => Vector3.Transform(Vector3.ForwardLH, rotationMatrix).ToVector3(false);

        public Vector3 GetRight(Matrix rotationMatrix) => Vector3.Transform(Vector3.Right, rotationMatrix).ToVector3(false);

        public Vector3 GetUp(Matrix rotationMatrix) => Vector3.Transform(Vector3.Up, rotationMatrix).ToVector3(false);

        public Vector3 ToWorldPosition(Vector3 localPosition) => Vector3.Transform(localPosition, LocalToWorldMatrix).ToVector3();

        public Vector3 ToLocalPosition(Vector3 worldPosition) => Vector3.Transform(worldPosition, WorldToLocalMatrix).ToVector3();

        internal Transform Copy()
        {
            return new Transform
            {
                WorldPosition = _WorldPosition,
                WorldRotation = WorldRotation,
                LocalPosition = LocalPosition,
                Size = _Size,
            };
        }
    }
}
