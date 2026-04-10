using System;
using GTA.Math;
using Newtonsoft.Json;

namespace ModdedCamera
{
    /// <summary>
    /// FIXED: Custom JSON converter for GTA.Math.Vector3.
    /// Prevents "Self referencing loop detected for property 'Normalized'" error.
    /// SHVDN Vector3 has a Normalized property that returns another Vector3,
    /// causing infinite recursion during JSON serialization.
    /// </summary>
    public class Vector3JsonConverter : JsonConverter<Vector3>
    {
        public override void WriteJson(JsonWriter writer, Vector3 value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("X");
            writer.WriteValue(value.X);
            writer.WritePropertyName("Y");
            writer.WriteValue(value.Y);
            writer.WritePropertyName("Z");
            writer.WriteValue(value.Z);
            writer.WriteEndObject();
        }

        public override Vector3 ReadJson(JsonReader reader, Type objectType, Vector3 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            float x = 0, y = 0, z = 0;

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject)
                {
                    break;
                }

                if (reader.TokenType == JsonToken.PropertyName)
                {
                    string propertyName = (string)reader.Value;
                    reader.Read();

                    switch (propertyName)
                    {
                        case "X":
                            x = reader.Value != null ? Convert.ToSingle(reader.Value) : 0f;
                            break;
                        case "Y":
                            y = reader.Value != null ? Convert.ToSingle(reader.Value) : 0f;
                            break;
                        case "Z":
                            z = reader.Value != null ? Convert.ToSingle(reader.Value) : 0f;
                            break;
                    }
                }
            }

            return new Vector3(x, y, z);
        }
    }
}
