using Microsoft.SqlServer.Server;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json;

namespace DirectXEngine
{
    public class Serializer
    {
        public static string Serialize<T>(T instance)
        {
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                //Converters = { new PrivateFieldConverter<T>() }
            };

            return JsonSerializer.Serialize(instance, options);
        }

        public static T Deserialize<T>(byte[] bytes)
        {
            BinaryFormatter binaryFormatter = new BinaryFormatter();

            using (MemoryStream stream = new MemoryStream(bytes))
                return (T)binaryFormatter.Deserialize(stream);
        }
    }
}
