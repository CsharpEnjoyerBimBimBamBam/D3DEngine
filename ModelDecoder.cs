using Assimp;
using System.Collections.Generic;
using Node = Assimp.Node;
using SharpDX;
using System.Windows.Forms;
using DirectXEngine.FBXModelParser;
using SharpDX.Direct2D1;
using System.Text;
using System.Drawing.Imaging;

namespace DirectXEngine
{
    public class ModelDecoder
    {
        public ModelDecoder(Assimp.Scene scene)
        {
            ExceptionHelper.ThrowIfNull(scene);
            _Scene = scene;
        }

        private Assimp.Scene _Scene;

        public static GameObject LoadFromPath(string path)
        {
            Assimp.Scene scene = new AssimpContext().ImportFile(path, PostProcessSteps.Triangulate | PostProcessSteps.FixInFacingNormals);
            ModelDecoder decoder = new ModelDecoder(scene);
            return decoder.Decode();
        }

        public GameObject Decode()
        {
            Node node = _Scene.RootNode;
            GameObject gameObject = NodeToGameObject(node);
            TransformData data = TransformData.FromTransform(node.Transform);
            data.ApplyOnTransform(gameObject.Transform);
            CreateChildrens(node, gameObject.Transform);
            return gameObject;
        }

        private GameObject NodeToGameObject(Node node)
        {
            List<Assimp.Mesh> meshes = new List<Assimp.Mesh>(node.MeshIndices.Count);
            node.MeshIndices.ForEach(x => meshes.Add(_Scene.Meshes[x]));
            GameObject gameObject = new GameObject();

            if (meshes.Count == 0)
                return gameObject;
            
            Mesh mesh = Mesh.FromAssimpMesh(meshes);
            MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.Mesh = mesh;
            return gameObject;
        }

        private void CreateChildrens(Node parentNode, Transform parent)
        {
            parentNode.Children.SafetyForEach(childrenNode =>
            {
                GameObject children = NodeToGameObject(childrenNode);
                Transform transform = children.Transform;
                transform.Parent = parent;
                TransformData data = TransformData.FromTransform(childrenNode.Transform);
                data.ApplyOnLocalTransform(transform);
                CreateChildrens(childrenNode, transform);
            });
        }

        private struct TransformData
        {
            public Vector3 Position;
            public SharpDX.Quaternion Rotation;
            public Vector3 Scale;
            
            public static TransformData FromTransform(Matrix4x4 transform)
            {
                transform.Decompose(out Vector3D scaling, out Assimp.Quaternion rotation, out Vector3D translation);
                return new TransformData
                {
                    Position = ToVector3(translation),
                    Rotation = new SharpDX.Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W),
                    Scale = ToVector3(scaling)
                };
            }

            public void ApplyOnTransform(Transform transform)
            {
                transform.WorldPosition = Position;
                transform.WorldRotation = Rotation;
                transform.WorldScale = Scale;
            }

            public void ApplyOnLocalTransform(Transform transform)
            {
                transform.LocalPosition = Position;
                transform.LocalRotation = Rotation;
                transform.LocalScale = Scale;
            }

            private static Vector3 ToVector3(Vector3D vector) => new Vector3(vector.X, vector.Y, vector.Z);
        }
    }
}
