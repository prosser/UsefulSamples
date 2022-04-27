namespace PRosser.Json.Discriminators;

using System;
using System.Collections.Generic;
using System.Text.Json;

public record PropertyExistsDiscriminator : IDiscriminatorOptions
{
    public PropertyExistsDiscriminator(string propertyName)
    {
        this.PropertyName = propertyName;
    }

    public string PropertyName { get; }

    public Predicate<JsonElement> CreatePredicate(JsonNamingPolicy? propertyNamingPolicy)
    {
        string propertyName = propertyNamingPolicy?.ConvertName(this.PropertyName) ?? this.PropertyName;
        return new(element => element.TryGetProperty(propertyName, out _));
    }
}