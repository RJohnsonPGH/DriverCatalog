using System.Text.Json;
using System.Text.Json.Serialization;

namespace DriverCatalog.Services;

/// <summary>
/// Shared JSON serializer options for the emitted driver catalog file and custom package files.
/// </summary>
public static class CatalogJson
{
    /// <summary>
    /// Serializer options: camelCase property names, enums serialized as strings, indented output.
    /// Reading is lenient (case-insensitive property names, comments and trailing commas allowed)
    /// to make hand-authored custom package files easy to write.
    /// </summary>
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() },
    };
}
