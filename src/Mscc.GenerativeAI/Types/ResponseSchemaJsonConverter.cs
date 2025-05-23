#if NET472_OR_GREATER || NETSTANDARD2_0
using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
#endif
using Json.Schema;
using Json.Schema.Generation;
using System.Reflection;
using System.Text.Json.Nodes;

namespace Mscc.GenerativeAI
{
    /// <summary>
    /// Custom JSON converter to serialize and deserialize JSON schema.
    /// </summary>
    public sealed class ResponseSchemaJsonConverter : JsonConverter<object>
    {
        /// <inheritdoc cref="JsonConverter"/>
        public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return JsonSerializer.Deserialize<dynamic>(reader.GetString()!, options);
        }

        /// <inheritdoc cref="JsonConverter"/>
        public override void Write(
            Utf8JsonWriter writer,
            object value,
            JsonSerializerOptions options)
        {
            var type = value.GetType();
            
            Type typeValue = null;
            try
            {
                typeValue = (Type)value;
            }
            catch
            {
                // ignored
            }

            if (type == typeof(String))
            {
                value = JsonSerializer.Deserialize<JsonElement>(value.ToString());
                type = value.GetType();
            }

            // How to figure out: type vs anonymous vs dynamic?
            if (type == typeof(JsonDocument) ||
                type == typeof(JsonElement) ||
                type == typeof(JsonNode) ||
                type.Name.Substring(0, Math.Min(type.Name.Length, 20)).Contains("AnonymousType"))
            {
                var newOptions = new JsonSerializerOptions(options);
                newOptions.Converters.Remove(this);
                JsonSerializer.Serialize(writer, value, type, newOptions);
            }
            else if (typeValue != null && typeValue.IsEnum)
            {
                var schemaBuilder = new JsonSchemaBuilder();
                var elements = typeValue.GetEnumValues()
                    .OfType<object>()
                    .Select(o => o.ToString())
                    .ToArray();
                var schema = schemaBuilder
                    .Type(SchemaValueType.String)
                    .Enum(elements)
                    .Build();
                JsonSerializer.Serialize(writer, schema, schema.GetType(), options);
            }
            else
            {
                var schemaBuilder = new JsonSchemaBuilder();
                var config = new SchemaGeneratorConfiguration()
                {
                    PropertyNameResolver = PropertyNameResolvers.CamelCase
                };
                if (IsRecord(type))
                {
                    // Problem with `readOnly` keys...
                    var schema = schemaBuilder.FromType(type, config).Build();
                    JsonSerializer.Serialize(writer, schema, schema.GetType(), options);
                }
                else
                {
                    var schema = schemaBuilder.FromType(type, config).Build();
                    JsonSerializer.Serialize(writer, schema, schema.GetType(), options);
                }
            }
        }
        
        private static bool IsRecord(Type type)
        {
            // Check for the IsRecord property (C# 10 and later)
            var isRecordProperty = type.GetProperty("IsRecord", BindingFlags.Public | BindingFlags.Static);
            if (isRecordProperty != null)
            {
                return (bool)isRecordProperty.GetValue(null);
            }
            // Check for the EqualityContract property (C# 9 and later)
            var equalityContractProperty = type.GetProperty("EqualityContract", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (equalityContractProperty != null)
            {
                return true;
            }
            // Check for the <Clone>$ method (C# 9)
            var cloneMethod = type.GetMethod("<Clone>$", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (cloneMethod != null)
            {
                return true;
            }
            return false;
        }
    }
}