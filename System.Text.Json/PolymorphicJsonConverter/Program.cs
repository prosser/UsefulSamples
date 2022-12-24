using Rosser.Json;
using Rosser.Json.Discriminators;
//using System.Linq;
//using System.Reflection;
using System.Text.Json;

JsonSerializerOptions jsonSerializerOptions = new()
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
};

// you can create a factory with inline code like this
PolymorphicJsonConverterFactory factory = new()
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

// alternatively, you could create a factory with reflection code like this
//factory = new()
//{
//    new(
//        typeof(Animal),
//        Assembly.GetExecutingAssembly().GetExportedTypes()
//            .Where(t => !t.IsAbstract && typeof(Animal).IsAssignableFrom(t))
//            .Select(t => new PolymorphicJsonConverterConcreteTypeOptions(t, new PropertyHasValueDiscriminator(nameof(Animal.Species), t.Name)))
//    )
//};

// or, you could create a factory by reading configuration from somewhere (left as an exercise to the reader).

jsonSerializerOptions.Converters.Add(factory);

string json = File.ReadAllText("animals.json");
AnimalsDocument? animals = JsonSerializer.Deserialize<AnimalsDocument>(json, jsonSerializerOptions)
    ?? throw new JsonException("Failed to deserialize animals.json: produced a null result");

Console.WriteLine($"Found {animals.Animals.Count} animals:");

foreach (Animal animal in animals.Animals)
{
    Console.WriteLine(animal switch
    {
        Bear bear => $"Bear \"{bear.Name}\" is {(bear.IsHibernating ? "" : "not ")}hibernating",
        Lion lion => $"Lion \"{lion.Name}\" has a {lion.ManeImpressiveness} mane impressiveness",
        Gazelle gazelle => $"Gazelle \"{gazelle.Name}\" has {gazelle.HornLengths.Length} horns with an average length of {gazelle.HornLengths.Average()}cm",
        _ => $"Unknown animal \"{animal.Name}\" is of species {animal.Species}"
    });
}
