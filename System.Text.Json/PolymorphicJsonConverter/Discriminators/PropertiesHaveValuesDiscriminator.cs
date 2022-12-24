namespace Rosser.Json.Discriminators;

using System;
using System.Text.Json;

public record PropertiesHaveValuesDiscriminator : IDiscriminatorOptions
{
    private readonly PropertyHasValueDiscriminator[] discriminators;

    public PropertiesHaveValuesDiscriminator(params (string name, object? value)[] propertyNamesAndValues)
    {
        if (propertyNamesAndValues.Length == 0)
        {
            throw new ArgumentException("Must define at least one property name and value", nameof(propertyNamesAndValues));
        }

        this.discriminators = propertyNamesAndValues
            .Select(kv => new PropertyHasValueDiscriminator(kv.name, kv.value))
            .ToArray();
    }

    public Predicate<JsonElement> CreatePredicate(JsonNamingPolicy? propertyNamingPolicy)
    {
        Predicate<JsonElement>[] predicates = this.discriminators
            .Select(x => x.CreatePredicate(propertyNamingPolicy))
            .ToArray();

        return new(element =>
        {
            for (int i = 0; i < predicates.Length; i++)
            {
                if (!predicates[i](element))
                {
                    return false;
                }
            }

            return true;
        });
    }
}