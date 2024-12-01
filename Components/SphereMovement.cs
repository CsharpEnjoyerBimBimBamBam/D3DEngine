using SharpDX;
using System;
using System.Collections;

namespace DirectXEngine
{
    internal class SphereMovement : Updatable
    {
        public SphereMovement(GameObject attachedGameObject) : base(attachedGameObject)
        {

        }

        protected override void OnStart()
        {
            StartUpdateCoroutine(UpdatePosition());
        }

        private IEnumerator UpdatePosition()
        {
            Transform transform = Transform;
            float speed = 5;
            float minSpeed = 1;
            float maxSpeed = 10;
            float minY = 0;
            float maxY = 20;

            while (true)
            {
                while (transform.WorldPosition.Y < maxY)
                {
                    float currentSpeed = maxY / transform.WorldPosition.Y;
                    currentSpeed = speed * MathUtil.Clamp(currentSpeed, minSpeed, maxSpeed);

                    transform.WorldPosition += new Vector3(0, currentSpeed, 0) * Time.FrameTime;
                    yield return null;
                }

                yield return new WaitForCoroutine(TimeSpan.FromSeconds(0.5));

                while (transform.WorldPosition.Y > minY)
                {
                    float currentSpeed = transform.WorldPosition.Y / maxY;
                    currentSpeed = speed * MathUtil.Clamp(currentSpeed, minSpeed, maxSpeed);

                    transform.WorldPosition -= new Vector3(0, currentSpeed, 0) * Time.FrameTime;
                    yield return null;
                }

                yield return new WaitForCoroutine(TimeSpan.FromSeconds(0.5));
            }
        }
    }
}
