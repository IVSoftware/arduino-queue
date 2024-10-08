﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace arduino_queue
{
    internal class CommandConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(AwaitableCommand).IsAssignableFrom(objectType);
        }
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value is AwaitableCommand command)
            {
                var type = command.GetType();
                writer.WriteStartObject();
                writer.WritePropertyName("Type");
                writer.WriteValue(type.Name); 

                foreach (var property in type.GetProperties())
                {
                    var jsonProperty = property.GetCustomAttributes(typeof(JsonPropertyAttribute), false);
                    if (jsonProperty.Length > 0)
                    {
                        var propertyValue = property.GetValue(command);
                        writer.WritePropertyName(property.Name);
                        serializer.Serialize(writer, propertyValue);
                    }
                }
                writer.WriteEndObject();
            }
        }
        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            if (JObject.Load(reader) is JObject jsonObject && 
                jsonObject["Type"] is object o && 
                o.ToString() is string typeName &&
                Type.GetType($"arduino_queue.{typeName}") is Type type &&
                Activator.CreateInstance(type) is AwaitableCommand command)
            {
                serializer.Populate(jsonObject.CreateReader(), command);
                return command;
            }
            else throw new InvalidOperationException();
        }
    }
}
