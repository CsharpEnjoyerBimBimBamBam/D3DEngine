using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Reflection;
using System.Windows.Forms;
using System.Reflection.Emit;
using System.IO;

namespace DirectXEngine
{
    internal class GameObjectConverter : JsonConverter<GameObject>
    {
        private const string _Components = "_Components";
        private readonly JsonSerializerOptions _Options = new JsonSerializerOptions
        {
            Converters = { new ComponentConverter() }
        };
        private static Type _Type { get; } = typeof(GameObject);
        private static List<MemberInfo> _Members { get; } = GetMembers();

        public override GameObject Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            GameObject gameObject = new GameObject(false, false);

            JsonDocument document = JsonDocument.ParseValue(ref reader);
            JsonElement element = document.RootElement;

            gameObject.Name = element.GetProperty(nameof(GameObject.Name)).GetString();
            gameObject.Enabled = element.GetProperty(nameof(GameObject.Enabled)).GetBoolean();
            
            JsonElement components = element.GetProperty(_Components);

            foreach (JsonElement componentElement in components.EnumerateArray())
            {
                ComponentConverter converter = new ComponentConverter(gameObject);
                
                componentElement.Deserialize<Component>(new JsonSerializerOptions
                {
                    Converters = { converter }
                });
            }

            return gameObject;
        }

        private static List<MemberInfo> GetMembers() =>
            _Type.GetMembers(BindingFlags.Public | BindingFlags.Instance).
                Where(x => (x is PropertyInfo || x is FieldInfo) && x.GetCustomAttribute<JsonIgnoreAttribute>() == null).ToList();

        public override void Write(Utf8JsonWriter writer, GameObject value, JsonSerializerOptions options)
        {
            Dictionary<string, object> members = _Members.
                ToDictionary(x => x.Name, x => x is PropertyInfo property ? property.GetValue(value) : (x as FieldInfo).GetValue(value));
            
            members[_Components] = GetComponents(value);
            writer.WriteRawValue(JsonSerializer.Serialize(members, _Options), true);
        }

        private List<Component> GetComponents(GameObject value) =>
            (List<Component>)_Type.GetField(_Components, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(value);
    }
}
