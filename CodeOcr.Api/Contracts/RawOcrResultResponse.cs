namespace CodeOcr.Api.Contracts;

public sealed record RawOcrResultResponse(
    IReadOnlyList<OcrLineResponse> Lines,
    string FullText,
    long ProcessingTimeMs);