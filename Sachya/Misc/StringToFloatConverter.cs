using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sachya.Misc;

public class StringToFloatConverter : JsonConverter<float>
{
    public override float Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // If the value is a string, attempt to parse it as a float.
        if (reader.TokenType == JsonTokenType.String)
        {
            string strValue = reader.GetString();
            if (float.TryParse(strValue, out float value))
            {
                return value;
            }
            throw new JsonException($"Unable to convert \"{strValue}\" to float.");
        }
        // Otherwise, assume it's already a number.
        else if (reader.TokenType == JsonTokenType.Number)
        {
            return reader.GetSingle();
        }
        throw new JsonException($"Unexpected token type: {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, float value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }
}
