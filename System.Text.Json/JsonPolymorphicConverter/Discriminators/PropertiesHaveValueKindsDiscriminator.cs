namespace PRosser.Json.Discriminators;

using System;
using System.Collections.Generic;
using System.Text.Json;

public record PropertiesHaveValueKindsDiscriminator : IDiscriminatorOptions
{
    public PropertiesHaveValueKindsDiscriminator(params (string name, JsonValueKind kind)[] propertyNamesAndKinds)
    {
        this.PropertyNamesAndKinds = propertyNamesAndKinds;
    }

    public (string name, JsonValueKind kind)[] PropertyNamesAndKinds { get; }

    public Predicate<JsonElement> CreatePredicate(JsonNamingPolicy? propertyNamingPolicy)
    {
        return new(element => this.PropertyNamesAndKinds
            .All(kv =>
            {
                string propertyName = propertyNamingPolicy?.ConvertName(kv.name) ?? kv.name;
                return element.TryGetProperty(propertyName, out JsonElement value) &&
                        value.ValueKind == kv.kind;
            }));
    }
}