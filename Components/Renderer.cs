using SharpDX;
using SharpDX.Direct3D11;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;

namespace DirectXEngine
{
    public abstract class Renderer : Updatable
    {
        public Renderer(GameObject attachedGameObject) : base(attachedGameObject)
        {
            Material = GameObject.AddComponent<Material>();
        }

        public Material Material { get; }
        internal ShaderDynamicResources GetResourcesInternal(Camera camera) => GetResources(camera);
        private const string _NotInstantiatedException = "Game object must be instantiated on scene before call UpdateGraphicsSettings";
        protected abstract ShaderDynamicResources GetResources(Camera camera);

        protected void UpdateGraphicsSettings(RendererGraphicsSettings update)
        {
            ExceptionHelper.ThrowByCondition(!GameObject.IsInstantiated, _NotInstantiatedException);
            Graphics.UpdateRenderer(this, update);
        }
    }
}
