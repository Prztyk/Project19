namespace CodeOcr.Api.Contracts;

public sealed record DiagnosticResponse(
    string ApplicationName,
    string Status,
    DateTimeOffset TimestampUtc,
    string Environment);