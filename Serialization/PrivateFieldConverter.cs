using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Forms;

namespace DirectXEngine
{
    internal abstract class PrivateFieldConverter<T> : JsonConverter<T>
    {
        private Type _Type { get; } = typeof(T);

        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            MemberInfo[] publicMembers = _Type.GetMembers(BindingFlags.Public | BindingFlags.Instance);
            MemberInfo[] privateMembers = _Type.GetMembers(BindingFlags.NonPublic | BindingFlags.Instance);
            Dictionary<string, object> propertiesInstances = new Dictionary<string, object>();

            foreach (MemberInfo member in publicMembers)
            {
                WritePublicProperty(member, value, propertiesInstances);
                WritePublicField(member, value, propertiesInstances);
            }

            foreach (MemberInfo member in privateMembers)
            {
                WritePrivateProperty(member, value, propertiesInstances);
                WritePrivateField(member, value, propertiesInstances);
            }

            foreach (KeyValuePair<string, object> element in propertiesInstances)
            {
                string serializedValue = JsonSerializer.Serialize(element.Value);
                //MessageBox.Show(element.Value.ToString());
            }

            propertiesInstances.SafetyForEach(x => writer.WriteStringValue(JsonSerializer.Serialize(x.Value)));
            
            //string serializedInstances = JsonSerializer.Serialize(propertiesInstances, options);
            //MessageBox.Show(serializedInstances);
            //writer.WriteStringValue(serializedInstances);
        }

        protected void WritePublicProperty(MemberInfo member, object value, Dictionary<string, object> propertiesInstances)
        {
            if (!CheckMemberType(member, out PropertyInfo property))
                return;

            if (IsPropertySerializable(property))
                propertiesInstances[property.Name] = property.GetValue(value);
        }

        protected void WritePublicField(MemberInfo member, object value, Dictionary<string, object> propertiesInstances)
        {
            if (!CheckMemberType(member, out FieldInfo field))
                return;

            if (!IsHaveAttribute<JsonIgnoreAttribute>(field))
                propertiesInstances[field.Name] = field.GetValue(value);
        }

        protected void WritePrivateProperty(MemberInfo member, object value, Dictionary<string, object> propertiesInstances)
        {
            if (!CheckMemberType(member, out PropertyInfo property))
                return;

            if (IsPropertySerializable(property) && IsHaveAttribute<SerializeMemberAttribute>(property))
                propertiesInstances[property.Name] = property.GetValue(value);
        }

        protected void WritePrivateField(MemberInfo member, object value, Dictionary<string, object> propertiesInstances)
        {
            if (!CheckMemberType(member, out FieldInfo field))
                return;

            if (!IsHaveAttribute<JsonIgnoreAttribute>(field) && IsHaveAttribute<SerializeMemberAttribute>(field))
                propertiesInstances[field.Name] = field.GetValue(value);
        }

        private bool CheckMemberType<MemberType>(MemberInfo member, out MemberType castedMember) where MemberType : MemberInfo
        {
            castedMember = member as MemberType;
            return castedMember != null;
        }

        private bool IsHaveAttribute<AttributeType>(MemberInfo member) where AttributeType : Attribute => 
            member.GetCustomAttribute<AttributeType>() != null;

        private bool IsPropertySerializable(PropertyInfo property) => property.CanRead && property.CanWrite && !IsHaveAttribute<JsonIgnoreAttribute>(property);
    }
}
