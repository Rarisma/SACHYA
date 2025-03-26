using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
namespace Sachya;

public class RaDateTimeConverter : JsonConverter<DateTime>
{
    private const string Format = "yyyy-MM-dd HH:mm:ss";

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Unexpected token type {reader.TokenType}. Expected String.");
        }

        string? dateString = reader.GetString();
        if (string.IsNullOrWhiteSpace(dateString))
        {
            // Decide how to handle null/empty strings. Throw or return default?
            // Returning default might hide issues. Let's throw for clarity.
            throw new JsonException("DateTime string was null or empty.");
        }

        // Use ParseExact with the specific format and InvariantCulture
        // Use DateTimeStyles.None as the string doesn't have timezone info.
        // The resulting DateTime will have Kind = Unspecified.
        if (DateTime.TryParseExact(dateString, Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
        {
            return result;
        }
        else
        {
             // Could also try parsing standard ISO formats as a fallback if needed,
             // but for this specific converter, let's stick to the expected format.
            throw new JsonException($"Could not parse DateTime string '{dateString}' using format '{Format}'.");
        }
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        // Write back out in the same format the API uses, or use ISO "o" format for standards compliance
         // writer.WriteStringValue(value.ToString("o", CultureInfo.InvariantCulture)); // ISO Standard
         writer.WriteStringValue(value.ToString(Format, CultureInfo.InvariantCulture)); // Match API format
    }
}