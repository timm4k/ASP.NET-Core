namespace AstralObservatory.Configuration;

public sealed class ApplicationOptions
{
    public const string SectionName = "Application";

    public required string Name { get; init; }

    public bool IsValid() => !string.IsNullOrWhiteSpace(Name);
}
