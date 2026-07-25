namespace CodeOcr.Api.Storage;

public sealed record StoredImageFile(
    Guid Id,
    string StoredFileName,
    long SizeBytes,
    DateTimeOffset StoredAtUtc);