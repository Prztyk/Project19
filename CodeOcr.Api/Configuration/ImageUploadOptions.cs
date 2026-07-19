namespace CodeOcr.Api.Configuration;

public sealed class ImageUploadOptions
{
    public const string SectionName = "ImageUpload";

    public long MaximumFileSizeBytes { get; init; }

    public string[] AllowedExtensions { get; init; } = [];

    public string[] AllowedContentTypes { get; init; } = [];
}