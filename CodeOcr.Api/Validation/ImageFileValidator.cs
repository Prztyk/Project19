using CodeOcr.Api.Configuration;
using Microsoft.Extensions.Options;

namespace CodeOcr.Api.Validation;

public sealed class ImageFileValidator(
    IOptions<ImageUploadOptions> options,
    ILogger<ImageFileValidator> logger)
    : IImageFileValidator
{
    private readonly ImageUploadOptions _options = options.Value;

    public ImageFileValidationResult Validate(IFormFile file)
    {
        ArgumentNullException.ThrowIfNull(file);

        if (file.Length == 0)
        {
            return ImageFileValidationResult.Failure(
                errorCode: "empty_file",
                errorMessage: "The uploaded file is empty.");
        }

        if (file.Length > _options.MaximumFileSizeBytes)
        {
            return ImageFileValidationResult.Failure(
                errorCode: "file_too_large",
                errorMessage:
                    $"The uploaded file exceeds the maximum allowed size of " +
                    $"{_options.MaximumFileSizeBytes} bytes.");
        }

        string safeFileName = Path.GetFileName(file.FileName);
        string extension = Path.GetExtension(safeFileName);

        bool isAllowedExtension = _options.AllowedExtensions.Contains(
            extension,
            StringComparer.OrdinalIgnoreCase);

        if (!isAllowedExtension)
        {
            logger.LogInformation(
                "An upload was rejected because extension {Extension} is not allowed.",
                extension);

            return ImageFileValidationResult.Failure(
                errorCode: "unsupported_file_extension",
                errorMessage:
                    $"The file extension '{extension}' is not supported.");
        }

        bool isAllowedContentType = _options.AllowedContentTypes.Contains(
            file.ContentType,
            StringComparer.OrdinalIgnoreCase);

        if (!isAllowedContentType)
        {
            logger.LogInformation(
                "An upload was rejected because content type {ContentType} is not allowed.",
                file.ContentType);

            return ImageFileValidationResult.Failure(
                errorCode: "unsupported_content_type",
                errorMessage:
                    $"The content type '{file.ContentType}' is not supported.");
        }

        return ImageFileValidationResult.Success();
    }
}