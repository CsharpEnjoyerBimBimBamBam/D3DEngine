using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Windows.Forms;
using SharpDX;

namespace DirectXEngine
{
    [Serializable]
    public class Transform : Component, ICloneableComponent
    {
        public Transform(GameObject attachedGameObject) : base(attachedGameObject)
        {
            
        }

        [JsonIgnore]
        public Vector3 WorldPosition
        {
            get => _Parent == null ? _WorldPosition : _Parent.ToWorldPosition(_LocalPosition);
            set
            {
                _WorldPosition = value;
                if (_Parent != null)
                    _LocalPosition = CalculateLocalPosition();
            }
        }
        [JsonIgnore]
        public Quaternion WorldRotation
        {
            get => _Parent == null ? _WorldRotation : _Parent.WorldRotation * _LocalRotation;
            set
            {
                _WorldRotation = value;
                if (_Parent != null)
                    _LocalRotation = CalculateLocalRotation();
            }
        }
        [JsonIgnore]
        public Vector3 WorldEulerAngles
        {
            get => VectorUtilities.ToEulerAngles(WorldRotation) * MathUtil.RadiansToDegrees(1);
            set
            {
                value *= MathUtil.DegreesToRadians(1);
                WorldRotation = Quaternion.RotationYawPitchRoll(value.Y, value.X, value.Z);
            }
        }
        [JsonIgnore]
        public Vector3 LocalPosition
        {
            get => _LocalPosition;
            set => _LocalPosition = value;
        }
        [JsonIgnore]
        public Quaternion LocalRotation
        {
            get => _LocalRotation;
            set => _LocalRotation = value;
        }
        [JsonIgnore]
        public Vector3 LocalEulerAngles
        {
            get => VectorUtilities.ToEulerAngles(LocalRotation) * MathUtil.RadiansToDegrees(1);
            set
            {
                value *= MathUtil.DegreesToRadians(1);
                LocalRotation = Quaternion.RotationYawPitchRoll(value.Y, value.X, value.Z);
            }
        }
        [JsonIgnore]
        public Vector3 WorldScale
        {
            get => _Parent == null ? _WorldScale : _Parent.WorldScale * _LocalScale;
            set
            {
                if (_Parent == null)
                {
                    _WorldScale = value;
                    return;
                }
                _LocalScale = value / _Parent.WorldScale;
            }
        }
        [JsonIgnore]
        public Vector3 LocalScale
        {
            get => _LocalScale;
            set => _LocalScale = value;
        }
        public Vector3 Forward => GetForward(WorldRotation);
        public Vector3 Right => GetRight(WorldRotation);
        public Vector3 Up => GetUp(WorldRotation);
        [JsonIgnore]
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
                bool isParentValid = (GameObject.IsInstantiated && value.GameObject.IsInstantiated) ||
                                     (!GameObject.IsInstantiated && !value.GameObject.IsInstantiated);
                ExceptionHelper.ThrowByCondition(!isParentValid, "Parent must be the same type as transform (Prefab or GameObject)");

                _Parent?._Childrens.Remove(this);
                _Parent = value;
                _Parent._Childrens.Add(this);
            }
        }
        [JsonIgnore] public IReadOnlyList<Transform> Childrens => _Childrens;
        public Matrix LocalToWorldMatrix => Matrix.Scaling(WorldScale) * RotationMatrix * Matrix.Translation(WorldPosition);
        public Matrix WorldToLocalMatrix => Matrix.Scaling(1 / WorldScale) * ViewMatrix;
        public Matrix RotationMatrix => Matrix.RotationQuaternion(WorldRotation);
        public Matrix ViewMatrix
        {
            get
            {
                Quaternion worldRotation = WorldRotation;
                Vector3 forwardLH = Vector3.ForwardLH;
                Vector3.Transform(ref forwardLH, ref worldRotation, out Vector3 forward);
                Vector3 up = Vector3.Transform(Vector3.Up, worldRotation);
                Vector3 position = WorldPosition;
                return Matrix.LookAtLH(position, position + forward, up);
            }
        }
        [SerializeMember] private Vector3 _WorldPosition;
        [SerializeMember] private Quaternion _WorldRotation = Quaternion.Identity;
        [SerializeMember] private Vector3 _LocalPosition;
        [SerializeMember] private Quaternion _LocalRotation = Quaternion.Identity;
        [SerializeMember] private Vector3 _WorldScale = Vector3.One;
        [SerializeMember] private Vector3 _LocalScale = Vector3.One;
        private Transform _Parent;
        private List<Transform> _Childrens = new List<Transform>();

        public Vector3 GetForward(Quaternion rotation) => Vector3.Transform(Vector3.ForwardLH, rotation);

        public Vector3 GetRight(Quaternion rotation) => Vector3.Transform(Vector3.Right, rotation);

        public Vector3 GetUp(Quaternion rotation) => Vector3.Transform(Vector3.Up, rotation);

        public Vector3 ToWorldPosition(Vector3 localPosition) => Vector3.Transform(localPosition, LocalToWorldMatrix).ToVector3(false);

        public Vector3 ToLocalPosition(Vector3 worldPosition) => Vector3.Transform(worldPosition, WorldToLocalMatrix).ToVector3(false);

        public Vector3 ToWorldDirection(Vector3 localDirection) => Vector3.Transform(localDirection, RotationMatrix).ToVector3(false);

        public Vector3 ToLocalDirection(Vector3 worldDirection)
        {
            Quaternion rotation = Quaternion.Invert(WorldRotation);
            return Vector3.Transform(worldDirection, rotation);
        }

        public Component Clone()
        {
            Transform copy = MemberwiseClone() as Transform;
            copy._Parent = null;
            copy._Childrens = new List<Transform>();
            copy._WorldPosition = WorldPosition;
            copy._WorldRotation = WorldRotation;
            return copy;
        }

        private Vector3 CalculateLocalPosition() => _WorldPosition - _Parent.WorldPosition;

        private Quaternion CalculateLocalRotation() => _WorldRotation * Quaternion.Invert(_Parent.WorldRotation);
    }

    public class ValueGetter<T>
    {
        public ValueGetter()
        {

        }

        public ValueGetter(Func<T, T> recalculateAction, T defaultValue)
        {
            _RecalculateAction = recalculateAction;
            Value = defaultValue;
        }

        public ValueGetter(Func<T, T> recalculateAction)
        {
            _RecalculateAction = recalculateAction;
        }

        public ValueGetter(Func<T, T> recalculateAction, bool isCalculated) : this(isCalculated)
        {
            _RecalculateAction = recalculateAction;
        }

        public ValueGetter(Func<T, T> recalculateAction, T defaultValue, bool isCalculated) : this(isCalculated)
        {
            _RecalculateAction = recalculateAction;
            Value = defaultValue;
        }

        public ValueGetter(bool isCalculated) => IsCalculated = isCalculated;

        public T Value
        {
            get
            {
                //if (!IsCalculated)
                //{
                //    _Value = _RecalculateAction.Invoke(_Value);
                //    IsCalculated = true;
                //}
                return _Value;
            }
            set
            {
                IsCalculated = true;
                _Value = value;
            }
        }
        public bool IsCalculated { get; set; }
        private T _Value;
        private Func<T, T> _RecalculateAction;

        internal ValueGetter<T> Copy() => Copy(IsCalculated);

        internal ValueGetter<T> Copy(bool isCalculated) => new ValueGetter<T>
        { 
            _Value = _Value,
            _RecalculateAction = _RecalculateAction,
            IsCalculated = isCalculated,
        };
    }
}
