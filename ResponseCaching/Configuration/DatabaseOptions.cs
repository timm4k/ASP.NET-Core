namespace ResponseCaching.Configuration;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public required string FileName { get; init; }

    public bool IsValid() =>
        !string.IsNullOrWhiteSpace(FileName)
        && Path.GetFileName(FileName) == FileName
        && FileName.EndsWith(".db", StringComparison.OrdinalIgnoreCase);
}
