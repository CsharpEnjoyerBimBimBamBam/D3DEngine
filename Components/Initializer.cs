using SharpDX;
using System;
using System.Windows.Forms;

namespace DirectXEngine
{
    internal class Initializer : Startable
    {
        public Initializer(GameObject attachedGameObject) : base(attachedGameObject)
        {
            
        }

        protected override void OnStart()
        {
            Camera.Main.AddComponent<CameraMovement>();
            Camera.Main.Transform.WorldPosition = new Vector3(0, 5, 0);
            //GameObject terrain1 = Scene.Current.Instantiate(terrain);
            //terrain1.Transform.WorldPosition = new Vector3(0, 10, 0);
            //terrain1.Transform.WorldRotation = new Vector3(0, 0, 180);

            //GameObject spotlight = GameObject.Create(GameObjectType.Spotlight);
            //spotlight.Transform.WorldRotation = new Vector3(90, 0, 0);
            //spotlight.Transform.WorldPosition = new Vector3(0, 10, 5);
            //
            //CameraMovement lightMovement = spotlight.AddComponent<CameraMovement>();
            //lightMovement.Forward = Keys.I;
            //lightMovement.Left = Keys.J;
            //lightMovement.Backward = Keys.K;
            //lightMovement.Right = Keys.L;
            //lightMovement.Up = Keys.PageUp;
            //lightMovement.Down = Keys.PageDown;
            //lightMovement.Rotate = false;

            GameObject directionalLight = GameObject.Create(GameObjectType.DirectionalLight);
            directionalLight.Transform.WorldRotation = new Vector3(20, 0, 0);

            GameObject cube = GameObject.Create(GameObjectType.Cube);
            cube.Transform.WorldPosition = new Vector3(0, 5, 5);
            cube.Transform.Size = new Vector3(1);

            CubeMovement movement = cube.AddComponent<CubeMovement>();
            movement.Rotation = new Vector3(60);

            GameObject terrain = CreateTerrain();
            terrain.Transform.WorldPosition = new Vector3(0, -1, 0);

            //GameObject cube1 = GameObject.Create(GameObjectType.Cube);
            //cube1.Transform.WorldPosition = new Vector3(0, 0, 5);

            //GameObject light1 = GameObject.Create(GameObjectType.DirectionalLight);
            //light1.Transform.WorldRotation = new Vector3(50, 180, 0);

            //PointLight pointLight = Scene.Current.Instantiate<PointLight>();
            //pointLight.Transform.WorldPosition = new Vector3(0, 5, 0);
            //pointLight.Diffusion = 0.5f;
            //pointLight.Intensity = 1f;
            //pointLight.Range = 500;
        }

        private GameObject CreateTerrain()
        {
            int columnCount = 50;
            int rowCount = 50;

            float width = 300;
            float height = 300;

            int verticesCount = columnCount * rowCount;

            Vector3[] vertices = new Vector3[verticesCount];
            int[] triangles = new int[(verticesCount + 2) * 6];

            Mesh mesh = new Mesh();
            Random random = new Random();

            int vertexIndex = 0;
            int trianglesIndex = 0;

            int lastColumnIndex = columnCount - 1;
            int lastRowIndex = rowCount - 1;

            float z = height / 2f;

            float xStep = width / columnCount;
            float zStep = height / rowCount;

            for (int row = 0; row < rowCount; row++)
            {
                float x = -width / 2f;

                for (int column = 0; column < columnCount; column++)
                {
                    vertices[vertexIndex] = new Vector3(x, random.NextFloat(0, 5), z);

                    if (column == lastColumnIndex || row == lastRowIndex)
                    {
                        vertexIndex++;
                        x += xStep;
                        continue;
                    }

                    triangles[trianglesIndex] = vertexIndex;
                    triangles[trianglesIndex + 1] = vertexIndex + 1;
                    triangles[trianglesIndex + 2] = vertexIndex + columnCount;

                    triangles[trianglesIndex + 3] = vertexIndex + 1;
                    triangles[trianglesIndex + 4] = vertexIndex + columnCount + 1;
                    triangles[trianglesIndex + 5] = vertexIndex + columnCount;

                    trianglesIndex += 6;
                    vertexIndex++;
                    x += xStep;
                }

                z -= zStep;
            }

            mesh.Vertices = vertices;
            mesh.Triangles = triangles;
            mesh.RecalculateNormals();

            GameObject terrain = Scene.Current.Instantiate();
            MeshRenderer meshRenderer = terrain.AddComponent<MeshRenderer>();
            //meshRenderer.UseFlatShading = true;
            meshRenderer.Mesh = mesh;

            return terrain;
        }
    }
}
