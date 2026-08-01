namespace CodeOcr.Api.Contracts;

public sealed record ImageOcrResponse(
    Guid ImageId,
    string OriginalFileName,
    string StoredFileName,
    string ContentType,
    long SizeBytes,
    string DetectedFormat,
    DateTimeOffset StoredAtUtc,
    RawOcrResultResponse RawOcr);