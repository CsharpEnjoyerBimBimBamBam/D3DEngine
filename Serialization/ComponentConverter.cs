using SharpDX.WIC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Forms;
using System.Xml.Linq;

namespace DirectXEngine
{
    internal class ComponentConverter : JsonConverter<Component>
    {
        public ComponentConverter()
        {

        }

        public ComponentConverter(GameObject gameObject)
        {
            _GameObject = gameObject;
        }

        private static readonly JsonSerializerOptions _Options = new JsonSerializerOptions
        {
            Converters = 
            { 
                new Vector2Converter(),
                new Vector3Converter(),
                new QuaternionConverter(),
            },
        };
        public const string TypeName = "$TypeName";
        private GameObject _GameObject;
        
        public override Component Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            JsonDocument document = JsonDocument.ParseValue(ref reader);
            JsonElement element = document.RootElement;

            string typeName = element.GetProperty(TypeName).GetString();
            Type componentType = Type.GetType(typeName);
            Component component = _GameObject.AddComponent(componentType);
            List<MemberInfo> members = GetMembers(componentType);

            foreach (JsonProperty property in element.EnumerateObject())
            {
                string name = property.Name;
                MemberInfo member = members.Find(x => x.Name == name);

                if (member == null)
                    continue;

                string propertyString = property.Value.GetRawText();
                Type type = member is PropertyInfo propertyInfo ? propertyInfo.PropertyType : (member as FieldInfo).FieldType;
                object deserializedProperty = JsonSerializer.Deserialize(property.Value.GetRawText(), type, _Options);

                SetValue(member, component, deserializedProperty);
            }

            return component;
        }

        public override void Write(Utf8JsonWriter writer, Component component, JsonSerializerOptions options)
        {
            Dictionary<string, object> members = new Dictionary<string, object>
            {
                { TypeName, component.GetType().FullName }
            };

            GetMembers(component.GetType()).
            ForEach(x => members[x.Name] = x is PropertyInfo property ? property.GetValue(component) : (x as FieldInfo).GetValue(component));
            
            writer.WriteRawValue(JsonSerializer.Serialize(members, _Options), true);
        }

        private void SetValue(MemberInfo member, object instance, object value)
        {
            if (member is PropertyInfo property && property.GetSetMethod() != null)
            {
                property.SetValue(instance, value);
                return;
            }

            if (member is FieldInfo fieldInfo)
                fieldInfo.SetValue(instance, value);
        }

        private List<MemberInfo> GetMembers(Type componentType)
        {
            List<MemberInfo> members = componentType.
            GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).
            Where(member =>
            {
                if (member is not PropertyInfo && member is not FieldInfo)
                    return false;

                if (member is PropertyInfo property && !IsPropertySerializable(property))
                    return false;

                if (member is FieldInfo field && !IsFieldSerializable(field))
                    return false;

                return true;
            }).
            ToList();
            
            Type baseType = componentType.BaseType;
            if (baseType == null || baseType == typeof(object) || baseType == typeof(Component))
                return members;

            members.AddRange(GetMembers(baseType));

            return members;
        }

        private bool IsHaveAttribute<T>(MemberInfo member) where T : Attribute =>
            member.GetCustomAttribute<T>() != null;

        private bool IsPropertySerializable(PropertyInfo property)
        {
            MethodInfo getMethod = property.GetGetMethod(true);
            MethodInfo setMethod = property.GetSetMethod(true);
            
            if (getMethod == null || setMethod == null)
                return false;         

            if (IsHaveAttribute<JsonIgnoreAttribute>(property))
                return false;

            bool isGetPrivate = !getMethod.IsPublic;
            bool isSetPrivate = setMethod != null && !setMethod.IsPublic;
            
            if (isGetPrivate && isSetPrivate && !IsHaveAttribute<SerializeMemberAttribute>(property))
                return false;

            return true;
        }

        private bool IsFieldSerializable(FieldInfo field)
        {
            if (IsHaveAttribute<JsonIgnoreAttribute>(field))
                return false;

            if (!field.IsPublic && !IsHaveAttribute<SerializeMemberAttribute>(field))
                return false;

            return true;
        }
    }
}
