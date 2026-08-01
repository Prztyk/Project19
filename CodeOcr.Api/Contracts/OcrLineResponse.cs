namespace CodeOcr.Api.Contracts;

public sealed record OcrLineResponse(
    string Text,
    double? Confidence);