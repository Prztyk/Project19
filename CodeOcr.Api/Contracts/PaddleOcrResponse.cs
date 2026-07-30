namespace CodeOcr.Api.Ocr.Contracts;

public sealed record PaddleOcrResponse(
    IReadOnlyList<PaddleOcrLine> Lines,
    string FullText,
    long ProcessingTimeMs);