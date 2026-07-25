namespace CodeOcr.Api.Contracts;

public sealed record StoredImageResponse(
    Guid ImageId,
    string OriginalFileName,
    string StoredFileName,
    string ContentType,
    long SizeBytes,
    string DetectedFormat,
    DateTimeOffset StoredAtUtc);