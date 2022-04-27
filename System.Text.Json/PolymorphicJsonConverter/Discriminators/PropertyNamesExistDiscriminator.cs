namespace PRosser.Json.Discriminators;

using System;
using System.Collections.Generic;
using System.Text.Json;

public record PropertiesExistDiscriminator : IDiscriminatorOptions
{
    public PropertiesExistDiscriminator(ISet<string> propertyNames)
    {
        this.PropertyNames = propertyNames;
    }

    public ISet<string> PropertyNames { get; }

    public Predicate<JsonElement> CreatePredicate(JsonNamingPolicy? propertyNamingPolicy)
    {
        return new(element => this.PropertyNames
            .All(k => element.TryGetProperty(propertyNamingPolicy?.ConvertName(k) ?? k, out _)));
    }
}