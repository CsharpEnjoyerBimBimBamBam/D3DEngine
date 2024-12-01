using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using SharpDX;

namespace DirectXEngine
{
    internal class Vector2Converter : JsonConverter<Vector2>
    {
        public override Vector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            reader.Read();
            float x = reader.GetSingle();
            reader.Read();
            float y =  reader.GetSingle();
            reader.Read();
            return new Vector2(x, y);
        }

        public override void Write(Utf8JsonWriter writer, Vector2 value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            writer.WriteNumberValue(value.X);
            writer.WriteNumberValue(value.Y);
            writer.WriteEndArray();
        }
    }
}
