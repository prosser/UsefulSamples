namespace Rosser.Contracts;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

public class JsonSerializerOptionsFactory
{
    // keep a local copy of options so that caches don't have to be recreated.
    private readonly Lazy<JsonSerializerOptions> options = new(CreateNew);

    /// <summary>
    /// Gets the default <see cref="JsonSerializerOptions"/>.
    /// </summary>
    /// <remarks>Callers should not modify this instance! If you need to add converters, etc., do so on a copy.</remarks>
    /// <example>
    /// Just use the defaults:
    /// <code lang="csharp">
    /// this.serializerOptions = jsof.Default;
    /// // ...
    /// MyType value = JsonSerializer.Deserialize&lt;MyType&gt;(json, this.serializerOptions);
    /// </code>
    /// Add your own privately-scoped custom converter:
    /// <code lang="csharp">
    /// public class MyService
    /// {
    ///     private readonly JsonSerializerOptions serializerOptions;
    ///     public MyService(JsonSerializerOptionsFactory jsof)
    ///     {
    ///         this.serializerOptions = new(jsof.Default);
    ///         this.serializerOptions.Converters.Add(new MyCustomConverter());
    ///     }
    /// }
    /// </code>
    /// </example>
    public JsonSerializerOptions Default => options.Value;

    public void Apply(JsonSerializerOptions options)
    {
        options.WriteIndented = this.Default.WriteIndented;
        options.PropertyNamingPolicy = this.Default.PropertyNamingPolicy;

        foreach (JsonConverter converter in this.Default.Converters)
        {
            options.Converters.Add(converter);
        }
    }

    private static JsonSerializerOptions CreateNew()
    {
        JsonSerializerOptions options = new()
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        options.Converters.Add(new JsonStringEnumConverter());

        return options;
    }
}
