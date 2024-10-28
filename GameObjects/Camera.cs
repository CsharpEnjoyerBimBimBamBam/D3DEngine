using SharpDX;
using SharpDX.Direct3D11;
using System;

namespace DirectXEngine
{
    public class Camera : GameObject
    {
        public Camera() : this(false, true)
        {
            
        }

        internal Camera(bool isInstantiated) : this(isInstantiated, true)
        {
            
        }

        internal Camera(bool isInstantiated, bool initialize) : base(isInstantiated)
        {
            if (!initialize)
                return;

            Graphics = new Graphics(this);
            UpdateFieldOfView(_DefaultFieldOfView);
        }

        public static Camera Main { get; } = new MainCamera(true);
        public Color SkyColor { get; set; } = Color.SkyBlue;
        public bool UsePerspective { get; set; } = true;
        public float FieldOfView
        {
            get => _FieldOfView;
            set
            {
                ExceptionHelper.ThrowIfOutOfRange(value, 0, 180 - double.Epsilon);
                UpdateFieldOfView(value);
            }
        }
        public float FarClipPlane
        {
            get => _FarClipPlane;
            set
            {
                ExceptionHelper.ThrowIfOutOfRange(value, 0, double.PositiveInfinity);
                _FarClipPlane = value;
                UpdateFrustrum();
            }
        }
        public float NearClipPlane
        {
            get => _NearClipPlane;
            set
            {
                ExceptionHelper.ThrowIfOutOfRange(value, 0, double.PositiveInfinity);
                _NearClipPlane = value;
                UpdateFrustrum();
            }
        }
        public bool UseReversedZDepthBuffer
        {
            get => _UseReversedZDepthBuffer;
            set
            {
                if (value == _UseReversedZDepthBuffer)
                    return;

                _UseReversedZDepthBuffer = value;

                DepthStencilStateDescription description = Graphics.DepthStateDescription;

                if (_UseReversedZDepthBuffer)
                {
                    Graphics.DefaultDepth = 0;

                    description.DepthComparison = Comparison.Greater;
                    Graphics.DepthStateDescription = description;
                    return;
                }

                Graphics.DefaultDepth = 1;
                description.DepthComparison = Comparison.Less;
                Graphics.DepthStateDescription = description;
            }
        }
        public Matrix WorldToScreenMatrix => Transform.ViewMatrix * ProjectionMatrix;
        public Matrix ScreenToWorldMatrix => Matrix.Invert(WorldToScreenMatrix);
        public Matrix ProjectionMatrix
        {
            get
            {
                float nearClipPlane = _NearClipPlane;
                float farClipPlane = _FarClipPlane;

                if (_UseReversedZDepthBuffer)
                {
                    nearClipPlane = _FarClipPlane;
                    farClipPlane = _NearClipPlane;
                }

                if (UsePerspective)
                    return Matrix.PerspectiveFovLH(FieldOfViewRadians, Graphics.Aspect, nearClipPlane, farClipPlane);

                return Matrix.OrthoLH(_OrthographicSize.Width, _OrthographicSize.Height, nearClipPlane, farClipPlane);
            }
        }
        public Size2F OrthographicSize
        {
            get => _OrthographicSize;
            set
            {
                ExceptionHelper.ThrowIfOutOfRange(value.Width, 0, double.PositiveInfinity);
                ExceptionHelper.ThrowIfOutOfRange(value.Height, 0, double.PositiveInfinity);
                _OrthographicSize = value;
            }
        }
        public Frustum Frustum => _Frustum;
        public float FieldOfViewRadians { get; private set; }
        internal virtual Graphics Graphics { get; }
        private bool _UseReversedZDepthBuffer = false;
        private float _FieldOfView = _DefaultFieldOfView;
        private float _FarClipPlane = 200;
        private float _NearClipPlane = 0.1f;
        private Size2F _OrthographicSize;
        private const float _DefaultFieldOfView = 60;
        private Frustum _Frustum;

        public Vector3 WorldToScreenPosition(Vector3 position) => Vector3.Transform(position, WorldToScreenMatrix).ToVector3();

        public Vector3 ScreenToWorldPosition(Vector3 position) => Vector3.Transform(position, ScreenToWorldMatrix).ToVector3();

        internal override GameObject Copy(bool isInstantiated)
        {
            Camera copy = (Camera)base.Copy(isInstantiated);
            copy._FieldOfView = _FieldOfView;
            copy.FieldOfViewRadians = FieldOfViewRadians;
            copy._FarClipPlane = _FarClipPlane;
            copy._NearClipPlane = _NearClipPlane;
            copy._OrthographicSize = _OrthographicSize;
            return copy;
        }

        protected void UpdateFieldOfView(float fieldOfViewDegrees)
        {
            _FieldOfView = fieldOfViewDegrees;
            FieldOfViewRadians = MathUtil.DegreesToRadians(fieldOfViewDegrees);
            UpdateFrustrum();
        }

        private void UpdateFrustrum() => _Frustum = Frustum.Calculate(this);
    }
}
