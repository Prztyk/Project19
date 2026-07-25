namespace CodeOcr.Api.Validation;

public sealed record ImageFileValidationResult(
    bool IsValid,
    ImageFileFormat? DetectedFormat,
    string? ErrorCode,
    string? ErrorMessage)
{
    public static ImageFileValidationResult Success(
        ImageFileFormat detectedFormat)
    {
        return new ImageFileValidationResult(
            IsValid: true,
            DetectedFormat: detectedFormat,
            ErrorCode: null,
            ErrorMessage: null);
    }

    public static ImageFileValidationResult Failure(
        string errorCode,
        string errorMessage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

        return new ImageFileValidationResult(
            IsValid: false,
            DetectedFormat: null,
            ErrorCode: errorCode,
            ErrorMessage: errorMessage);
    }
}