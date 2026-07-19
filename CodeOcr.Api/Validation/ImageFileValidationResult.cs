namespace CodeOcr.Api.Validation;

public sealed record ImageFileValidationResult(
    bool IsValid,
    string? ErrorCode,
    string? ErrorMessage)
{
    public static ImageFileValidationResult Success()
    {
        return new ImageFileValidationResult(
            IsValid: true,
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
            ErrorCode: errorCode,
            ErrorMessage: errorMessage);
    }
}