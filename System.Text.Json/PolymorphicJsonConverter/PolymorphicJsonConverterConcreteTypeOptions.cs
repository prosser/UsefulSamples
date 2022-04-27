namespace PRosser.Json;

using System;
using System.Collections.Generic;
using System.Text.Json;

/// <summary>Options to configure a concrete type for polymorphic JSON deserialization.</summary>
public class PolymorphicJsonConverterConcreteTypeOptions
{
    private readonly IDiscriminatorOptions options;
    private Predicate<JsonElement>? predicate;

    /// <summary>
    /// Initializes a new instance of <see cref="PolymorphicJsonConverterConcreteTypeOptions"/> using simple property
    /// name existence.
    /// </summary>
    /// <param name="type">Type of the concrete class.</param>
    /// <param name="options">Discriminator configuration options.</param>
    public PolymorphicJsonConverterConcreteTypeOptions(Type type, IDiscriminatorOptions options)
    {
        this.Type = type;
        this.options = options;
    }

    /// <summary>
    /// Gets the predicate whose result indicates that an element represents the serialized form of
    /// the concrete type.
    /// </summary>
    public Predicate<JsonElement> Predicate => this.predicate
        ?? throw new InvalidOperationException("Not initialized");

    /// <summary>Gets the concrete type being configured.</summary>
    public Type Type { get; }

    /// <summary>
    /// Initializes the options for conversion. Called by the factory during its
    /// <see cref="PolymorphicJsonConverterFactory.CreateConverter(Type, JsonSerializerOptions)"/> method.
    /// </summary>
    /// <param name="propertyNamingPolicy">The current property naming policy.</param>
    public void Initialize(JsonNamingPolicy? propertyNamingPolicy)
    {
        this.predicate = this.options.CreatePredicate(propertyNamingPolicy);
    }

    public static PolymorphicJsonConverterConcreteTypeOptions Create<T>(IDiscriminatorOptions options)
    {
        return new(typeof(T), options);
    }
}