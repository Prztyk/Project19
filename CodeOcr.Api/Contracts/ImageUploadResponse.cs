namespace CodeOcr.Api.Contracts;

public sealed record ImageUploadResponse(
    string FileName,
    string Extension,
    string ContentType,
    long SizeBytes);