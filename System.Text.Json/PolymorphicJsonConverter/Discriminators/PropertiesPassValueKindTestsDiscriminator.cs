namespace PRosser.Json.Discriminators;

using System;
using System.Text.Json;

public record PropertiesPassValueKindTestsDiscriminator : IDiscriminatorOptions
{
    public PropertiesPassValueKindTestsDiscriminator(params (string name, Predicate<JsonValueKind> test)[] propertyNamesAndKindTests)
    {
        if (propertyNamesAndKindTests.Length == 0)
        {
            throw new ArgumentException("Must define at least one property name and predicate", nameof(propertyNamesAndKindTests));
        }

        this.PropertyNamesAndKindTests = propertyNamesAndKindTests;
    }

    public (string name, Predicate<JsonValueKind> test)[] PropertyNamesAndKindTests { get; }

    public Predicate<JsonElement> CreatePredicate(JsonNamingPolicy? propertyNamingPolicy)
    {
        return new(element =>
        {
            foreach ((string name, Predicate<JsonValueKind> test) in this.PropertyNamesAndKindTests)
            {
                string propertyName = propertyNamingPolicy?.ConvertName(name) ?? name;
                if (!element.TryGetProperty(propertyName, out JsonElement value) && test(value.ValueKind))
                {
                    return false;
                }
            }

            return true;
        });
    }
}