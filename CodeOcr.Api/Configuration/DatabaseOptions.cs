namespace CodeOcr.Api.Configuration;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public string FilePath { get; init; } = string.Empty;
}