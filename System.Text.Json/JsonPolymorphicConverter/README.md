# JsonPolymorphicConverter

A JsonConverter to deserialize abstract/interface types using System.Text.Json using a type discriminator pattern.

## Quickstart

1. Copy the .cs files from this repo to your codebase (except for [Program.cs](Program.cs))
2. If you're not using C# 10.0 or later, either set `<LangVersion>10.0</LangVersion>` in your .csproj, or adjust the syntax in those .cs files to the older, rustier version of C# you're forcing yourself to use.
3. Using [Program.cs](Program.cs) as a guide, add an instance of `JsonPolymorphicConverterFactory` to
your `JsonSerializerOptions.Converters`.
4. ???
5. Profit.

## Why make this?

There is no built-in, general purpose polymorphic deserialization capability in System.Text.Json.
There are good reasons for this, but here in the real world, I needed to get my job done and I'm
too lazy to write a converter for every base type in every codebase I touch.

Full disclosure: this code is opinionated and not as fast as a well-written custom `JsonConverter<T>`
could be. I doubt it's **much** slower, but it's not no-allocation, and due to it's general-purpose
nature, you will pay several microseconds extra for the benefit of not maintaining a bunch more code.

### The problem
You have a class like this that you need to deserialize from a JSON representation:

```csharp
public class Zoo
{
  public IReadOnlyList<Animal> Animals { get; init; } = Array.Empty<Animal>();
}
```

Where `Animal` is an abstract class (or an interface) with more than one possible concrete classes:

```csharp
public abstract class Animal
{
  public string Name { get; init; } = default!;
  public double Weight { get; init; }
  public abstract string Type { get; }
}

public class Lion : Animal
{
  public override string Type => nameof(Lion);
  public int TailLengthCentimeters { get; init; }
  public double ManeImpressiveness { get; init; }
}

public class Gazelle : Animal
{
  public override string Type => nameof(Tiger);
  public double[] HornLengths { get; init; }
}

public class Bear : Animal
{
  public override string Type => nameof(Bear);
  public bool IsHibernating { get; set; }
}
```

When deserializing, you want to instantiate the right class without having to write a new JsonConverter<T> each time.

### How a discriminator approach solves the problem

A discriminator is one or more properties that can
be used to test the JSON structure to see if it "fits" a concrete class
definition.

In its simplest form, it's a specific property like `type` that has a unique
value for each concrete type. Alternatively, it could be any unique
combination of property names and/or value types that could indicate the
concrete type. Either way is fine, so long as the discriminator is unique.

There are tradeoffs in  performance, maintainability, and usability and/or
friendliness of the JSON, depending on which way you go.

Given the classes above, the options are:

1. Use the `Type` property to select the concrete class.
2. Use a unique property presence discriminator for each concrete class:
   * Lion: `ManeImpressiveness` 
   * Gazelle: `HornLengths`
   * Bear: `IsHibernating`

In this case, the choice should be `Type`'s value, since it is available and simplest. If there could be collisions, approach 2 would be better.


## JsonPolymorphicConverterFactory

The `JsonPolymorphicConverterFactory` class is where you configure the discriminators that map each
base type to the proper concrete (i.e., most-derived) types. It understands the property naming policy
passed to it by `JsonSerializer`, so property names should be specified using the real names, ideally
using the `nameof(MyPropertyName)` syntax:

```csharp
JsonPolymorphicConverterFactory factory = new()
{
    // the base type is Animal
    new(typeof(Animal))
    {
        // the concrete type is Lion, and we select it when Species == "Lion"
        new(typeof(Lion), new PropertyHasValueDiscriminator(nameof(Animal.Species), nameof(Lion))),

        // the concrete type is Bear, and we select it when Species == "Bear"
        new(typeof(Bear), new PropertyHasValueDiscriminator(nameof(Animal.Species), nameof(Bear))),

        // the concrete type is Gazelle, and we select it when Species == "Gazelle"
        new(typeof(Gazelle), new PropertyHasValueDiscriminator(nameof(Animal.Species), nameof(Gazelle))),
    },
};
```

In the above example, I'm making use of C# 10.0 concise `new()` syntax.

### Built-in discriminators

There are several discriminator patterns I employ and included [here](Discriminators/). You can implement your own too,
of course--just implement the `IDiscriminatorOptions` interface with proper care.

|Class name|Matches a JSON object when...|
|-|-|
|`PropertyExistsDiscriminator`|A specific property exists|
|`PropertyHasValueDiscriminator`|A specific property exists and has [a specific value](#allowed-specific-values).|
|`PropertiesHaveValuesDiscriminator`|All specified properties exist and have [specific values](#allowed-specific-values).|
|`PropertyNamesExistDiscriminator`|All of the property names in the set exist|
|`PropertiesHaveValueKindsDiscriminator`|All of the properties exist and have specific value types.|

#### Allowed specific values

The following types are permitted when testing for a specific value:
* `string?` (can be `null`)
* `int`
* `double`
* `float`
* `bool`
* `byte`
* `Array` (which can be any of the allowed types, or an `object[]` whose items are all of the allowed types)

For `Array` values, the entire array must match. Yes, you have nested arrays.