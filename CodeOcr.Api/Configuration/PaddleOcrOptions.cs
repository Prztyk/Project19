namespace CodeOcr.Api.Configuration;

public sealed class PaddleOcrOptions
{
    public const string SectionName = "PaddleOcr";

    public string BaseUrl { get; init; } = string.Empty;

    public string RecognizePath { get; init; } = string.Empty;

    public int TimeoutSeconds { get; init; }
}