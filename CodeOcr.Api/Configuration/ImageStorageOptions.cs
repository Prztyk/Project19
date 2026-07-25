namespace CodeOcr.Api.Configuration;

public sealed class ImageStorageOptions
{
    public const string SectionName = "ImageStorage";

    public string DirectoryPath { get; init; } = string.Empty;
}