namespace PRosser.Json;

public abstract record Animal
{
  public string Name { get; init; } = default!;
  public double Weight { get; init; }
  public abstract string Species { get; }
}

public record Lion : Animal
{
  public override string Species => nameof(Lion);
  public int TailLengthCentimeters { get; init; }
  public double ManeImpressiveness { get; init; }
}

public record Gazelle : Animal
{
  public override string Species => nameof(Gazelle);
  public double[] HornLengths { get; init; } = Array.Empty<double>();
}

public record Bear : Animal
{
  public override string Species => nameof(Bear);
  public bool IsHibernating { get; set; }
}

public record AnimalsDocument
{
    public IReadOnlyList<Animal> Animals { get; init; } = Array.Empty<Animal>();
}