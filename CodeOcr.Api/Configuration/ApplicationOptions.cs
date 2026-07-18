namespace CodeOcr.Api.Configuration;

public sealed class ApplicationOptions
{
    public const string SectionName = "Application";

    public string Name { get; init; } = string.Empty;
}