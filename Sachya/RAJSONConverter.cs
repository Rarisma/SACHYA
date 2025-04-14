using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
namespace Sachya;

public class RaDateTimeConverter : JsonConverter<DateTime>
{
    private const string Format = "yyyy-MM-dd HH:mm:ss";

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Handle null values in the JSON
        if (reader.TokenType == JsonTokenType.Null)
        {
            // Return a default value for null dates
            return DateTime.MinValue;
        }

        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Unexpected token type {reader.TokenType}. Expected String or Null.");
        }

        string? dateString = reader.GetString();
        if (string.IsNullOrWhiteSpace(dateString))
        {
            // Handle empty strings as defaults too
            return DateTime.MinValue;
        }

        // Use ParseExact with the specific format and InvariantCulture
        if (DateTime.TryParseExact(dateString, Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
        {
            return result;
        }
        
        // Try parsing with more flexible DateTime.Parse as fallback
        if (DateTime.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
        {
            return result;
        }

        throw new JsonException($"Could not parse DateTime string '{dateString}' using format '{Format}'.");
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        // Special handling for the MinValue sentinel (which represents null)
        if (value == DateTime.MinValue)
        {
            writer.WriteNullValue();
            return;
        }
        
        writer.WriteStringValue(value.ToString(Format, CultureInfo.InvariantCulture));
    }
}