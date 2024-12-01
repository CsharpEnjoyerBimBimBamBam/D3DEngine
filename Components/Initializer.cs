using SharpDX;
using System;
using System.Windows.Forms;
using SharpDX.IO;
using Assimp;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using System.Linq;

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
            //Camera.Main.Transform.WorldPosition = new Vector3(0, 5000, 0);
            Camera.Main.FarClipPlane = 10000;
            
            GameObject airplane = CreateAirplane();
            airplane.Transform.WorldPosition = new Vector3(0, 10, 10);
            airplane.Transform.WorldEulerAngles = new Vector3(-90, 0, 0);

            GameObject pointLight1 = GameObject.Create(GameObjectType.PointLight);
            pointLight1.Transform.WorldPosition = new Vector3(20, 20, 10);
            //pointLight1.GetComponent<Light>().CastShadows = false;
            pointLight1.GetComponent<Light>().Color = Color.Red;
            pointLight1.GetComponent<PointLight>().Range = 50;
            
            GameObject pointLight = GameObject.Create(GameObjectType.PointLight);
            pointLight.Transform.WorldPosition = new Vector3(0, 20, 10);
            pointLight.GetComponent<Light>().CastShadows = false;
            pointLight.GetComponent<Light>().Color = Color.Blue;
            pointLight.GetComponent<PointLight>().Range = 50;

            CameraMovement lightMovement = airplane.AddComponent<CameraMovement>();
            lightMovement.Forward = Keys.I;
            lightMovement.Left = Keys.J;
            lightMovement.Backward = Keys.K;
            lightMovement.Right = Keys.L;
            lightMovement.Up = Keys.PageUp;
            lightMovement.Down = Keys.PageDown;
            lightMovement.Rotate = false;

            //GameObject directionalLight = GameObject.Create(GameObjectType.DirectionalLight);
            //directionalLight.Transform.WorldEulerAngles = new Vector3(45, 0, 0);
            //directionalLight.GetComponent<Light>().Color = Color.Red;
            //directionalLight.GetComponent<Light>().CastShadows = false;

            GameObject cube = GameObject.Create(GameObjectType.Cube);
            cube.Transform.WorldPosition = new Vector3(0, 5, 5);
            cube.Transform.WorldScale = new Vector3(1);
            cube.AddComponent<CubeMovement>().Rotation = new Vector3(60);
            cube.Name = "cube1";
            
            GameObject cube1 = GameObject.Create(GameObjectType.Cube);
            cube1.Transform.LocalPosition = new Vector3(0, 2, 0);
            //cube1.Transform.WorldPosition = new Vector3(0, 7, 5);
            cube1.Transform.LocalEulerAngles = new Vector3(45, 0, 0);
            cube1.Transform.Parent = cube.Transform;
            //cube1.AddComponent<CubeMovement>().Rotation = new Vector3(40);
            
            GameObject cube2 = GameObject.Create(GameObjectType.Cube);
            cube2.Transform.LocalPosition = new Vector3(2, 0, 0);
            cube2.Transform.LocalEulerAngles = new Vector3(0, 45, 0);
            cube2.Transform.Parent = cube1.Transform;
            
            GameObject cube3 = Scene.Current.Instantiate(cube);
            cube3.Transform.WorldPosition = new Vector3(10, 5, 5);
            //cube3.Transform.WorldScale = new Vector3(2);
            cube3.Name = "cube3";

            for (int i = 1; i <= 5; i++)
            {
                GameObject cube5 = Scene.Current.Instantiate(cube);
                cube5.Transform.WorldPosition += new Vector3(0, 0, i * 5);
            }

            //Texture texture = Texture.FromImagePath("C:\\sisharp\\DirectXEngine\\DirectXEngine\\bin\\Debug\\cRw-X8Hl1Hw.jpg");
            Texture texture = Texture.FromImagePath("C:\\sisharp\\DirectXEngine\\DirectXEngine\\bin\\Debug\\KMO_162543_42903_1_t214_223945.jpg");
            GameObject terrain = CreateTerrain();
            terrain.Transform.WorldPosition = new Vector3(0, -1, 0);
            terrain.GetComponent<Renderer>().Material.Texture = texture;

            GameObject sphere = Scene.Current.Instantiate(ModelDecoder.LoadFromPath("C:\\Models\\sphere.fbx"));
            sphere.Transform.WorldScale = new Vector3(0.05f);
            sphere.Transform.WorldPosition = new Vector3(0, 10, -10);
            sphere.AddComponent<SphereMovement>();
            
            //string s = JsonSerializer.Serialize(cube);
            //MessageBox.Show(s);
            //JsonSerializer.Deserialize<GameObject>(s);
            //File.WriteAllText("C:\\sisharp\\DirectXEngine\\DirectXEngine\\bin\\Debug\\hueta.txt", s);
            //MessageBox.Show(JsonSerializer.Serialize(terrain));

            ////airplane.Transform.WorldScale = new Vector3(0.01f);
            //airplane.AddComponent<CubeMovement>().Rotation = new Vector3(10);

            GameObject plane = GameObject.Create(GameObjectType.Plane);
            plane.Transform.WorldScale = new Vector3(100);
            plane.Transform.WorldPosition = new Vector3(0, 50, 50);
            plane.Transform.WorldEulerAngles = new Vector3(0, 0, 0);

            //GameObject airplane1 = Scene.Current.Instantiate(airplane);
            //airplane1.Transform.WorldPosition += new Vector3(0, 0, 10);

            //Vector3 position = airplane.Transform.WorldPosition;
            //for (int i = 0; i < 3; i++)
            //{
            //    position += new Vector3(0, 0, 15);
            //    GameObject plane = Scene.Current.Instantiate(airplane);
            //    //plane.RemoveComponent<CubeMovement>();
            //    //plane.Transform.WorldPosition = position;
            //    plane.Transform.Parent = airplane.Transform;
            //    //plane.Transform.WorldPosition = position;
            //    plane.Transform.LocalPosition = new Vector3(0, 10, 10);
            //    //MessageBox.Show(plane.Transform.Childrens[0].Childrens.Count.ToString());
            //}

            //MessageBox.Show(airplane.Transform.Childrens.Count.ToString());
            //PointLight pointLight = Scene.Current.Instantiate<PointLight>();
            //pointLight.Transform.WorldPosition = new Vector3(0, 5, 0);
            //pointLight.Diffusion = 0.5f;
            //pointLight.Intensity = 1f;
            //pointLight.Range = 500;
        }

        private GameObject CreateAirplane()
        {
            GameObject airplane = Scene.Current.Instantiate(ModelDecoder.LoadFromPath("C:\\Models\\f_a 18c hornet.fbx"));
            //airplane.AddComponent<CubeMovement>();
            return airplane;
        }

        private GameObject CreateTerrain()
        {
            int columnCount = 50;
            int rowCount = 50;

            float width = 100;
            float height = 100;

            int verticesCount = columnCount * rowCount;

            Vector3[] vertices = new Vector3[verticesCount];
            int[] triangles = new int[(verticesCount + 2) * 6];
            Vector2[] uvs = new Vector2[verticesCount];

            Mesh mesh = new Mesh();
            Random random = new Random();

            int vertexIndex = 0;
            int trianglesIndex = 0;

            int lastColumnIndex = columnCount - 1;
            int lastRowIndex = rowCount - 1;

            float halfWidth = width / 2;
            float halfHeight = height / 2;

            float xStep = width / columnCount;
            float zStep = height / rowCount;

            float z = halfHeight;

            for (int row = 0; row < rowCount; row++)
            {
                float x = -width / 2f;

                for (int column = 0; column < columnCount; column++)
                {
                    vertices[vertexIndex] = new Vector3(x, random.NextFloat(0, 1), z);

                    float normalizedX = x + halfWidth;
                    float normalizedZ = halfHeight - z;

                    uvs[vertexIndex] = new Vector2(normalizedX / width, normalizedZ / height);

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
            mesh.UVs = uvs;
            mesh.RecalculateNormals();

            GameObject terrain = Scene.Current.Instantiate();
            MeshRenderer meshRenderer = terrain.AddComponent<MeshRenderer>();
            //meshRenderer.UseFlatShading = true;
            meshRenderer.Mesh = mesh;

            return terrain;
        }
    }
}
