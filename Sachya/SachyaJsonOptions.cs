using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sachya;

public static class SachyaJsonOptions
{
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}
