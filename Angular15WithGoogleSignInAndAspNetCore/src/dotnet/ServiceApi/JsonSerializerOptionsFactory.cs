namespace ServiceApi;

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

public static class JsonSerializerOptionsFactory
{
    public static JsonSerializerOptions CreateSerializerOptions()
    {
        JsonSerializerOptions options = new()
        {
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            IgnoreReadOnlyFields = true,
            IgnoreReadOnlyProperties = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

        return options;
    }

    public static void PopulateOptions(this JsonSerializerOptions target, JsonSerializerOptions source)
    {
        target.AllowTrailingCommas = source.AllowTrailingCommas;
        foreach (JsonConverter converter in source.Converters)
        {
            target.Converters.Add(converter);
        }

        target.DefaultBufferSize = source.DefaultBufferSize;
        target.DefaultIgnoreCondition = source.DefaultIgnoreCondition;
        target.DictionaryKeyPolicy = source.DictionaryKeyPolicy;
        target.Encoder = source.Encoder;
        target.IgnoreReadOnlyFields = source.IgnoreReadOnlyFields;
        target.IgnoreReadOnlyProperties = source.IgnoreReadOnlyProperties;
        target.IncludeFields = source.IncludeFields;
        target.MaxDepth = source.MaxDepth;
        target.NumberHandling = source.NumberHandling;
        target.PropertyNameCaseInsensitive = source.PropertyNameCaseInsensitive;
        target.PropertyNamingPolicy = source.PropertyNamingPolicy;
        target.ReadCommentHandling = source.ReadCommentHandling;
        target.ReferenceHandler = source.ReferenceHandler;
        target.TypeInfoResolver = source.TypeInfoResolver;
        target.UnknownTypeHandling = source.UnknownTypeHandling;
        target.WriteIndented = source.WriteIndented;
    }
}
