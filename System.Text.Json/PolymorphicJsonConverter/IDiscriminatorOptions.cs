namespace PRosser.Json;

using System;
using System.Text.Json;

public interface IDiscriminatorOptions
{
    /// <summary>
    /// Creates the <see cref="Predicate{JsonElement}"/> that tests if a <see cref="JsonElement"/> should be selected by
    /// this discriminator.
    /// </summary>
    /// <param name="propertyNamingPolicy">
    /// Optional <see cref="JsonNamingPolicy"/> to use when converting property names.
    /// </param>
    /// <returns>The created predicate.</returns>
    Predicate<JsonElement> CreatePredicate(JsonNamingPolicy? propertyNamingPolicy);
}