using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CherryFramework.SaveGameManager
{
    public class TypeAwareConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => true;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            if (value is IEnumerable enumerable && !(value is string))
            {
                writer.WriteStartArray();
                foreach (var item in enumerable)
                {
                    WriteWithType(writer, item, serializer);
                }

                writer.WriteEndArray();
            }
            else
            {
                WriteWithType(writer, value, serializer);
            }
        }

        private void WriteWithType(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var jo = new JObject
            {
                ["$type"] = value.GetType().AssemblyQualifiedName,
                ["$value"] = JToken.FromObject(value, serializer)
            };
            jo.WriteTo(writer);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            if (reader.TokenType == JsonToken.StartArray)
            {
                var jArray = JArray.Load(reader);
                var elementType = objectType.IsArray
                    ? objectType.GetElementType()
                    : objectType.GetGenericArguments()[0];

                var listType = typeof(List<>).MakeGenericType(elementType);
                var list = (IList) Activator.CreateInstance(listType);

                foreach (var token in jArray)
                {
                    list.Add(ReadWithType((JObject) token, serializer));
                }

                if (objectType.IsArray)
                {
                    var array = Array.CreateInstance(elementType, list.Count);
                    list.CopyTo(array, 0);
                    return list;
                }

                return list;
            }

            var jo = JObject.Load(reader);
            return ReadWithType(jo, serializer);
        }

        private object ReadWithType(JObject jo, JsonSerializer serializer)
        {
            var typeName = jo["$type"]?.ToString();
            if (typeName == null)
                throw new JsonSerializationException("Field not found $type");

            var type = Type.GetType(typeName);
            if (type == null)
                throw new JsonSerializationException($"Failed to load type : {typeName}");

            return jo["$value"].ToObject(type, serializer);
        }
    }
}