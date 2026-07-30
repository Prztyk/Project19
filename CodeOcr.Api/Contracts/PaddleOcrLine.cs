namespace CodeOcr.Api.Ocr.Contracts;

public sealed record PaddleOcrLine(
    string Text,
    double? Confidence);