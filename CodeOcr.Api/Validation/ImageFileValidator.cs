using CodeOcr.Api.Configuration;
using Microsoft.Extensions.Options;

namespace CodeOcr.Api.Validation;

public sealed class ImageFileValidator(
    IOptions<ImageUploadOptions> options,
    ILogger<ImageFileValidator> logger)
    : IImageFileValidator
{
    private const int HeaderLength = 12;

    private static readonly byte[] PngSignature =
    [
        0x89,
        0x50,
        0x4E,
        0x47,
        0x0D,
        0x0A,
        0x1A,
        0x0A
    ];

    private static readonly byte[] JpegSignature =
    [
        0xFF,
        0xD8,
        0xFF
    ];

    private static readonly byte[] RiffSignature =
    [
        0x52,
        0x49,
        0x46,
        0x46
    ];

    private static readonly byte[] WebPContainerSignature =
    [
        0x57,
        0x45,
        0x42,
        0x50
    ];

    private readonly ImageUploadOptions _options = options.Value;

    public async Task<ImageFileValidationResult> ValidateAsync(
        IFormFile file,
        CancellationToken cancellationToken)
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

        string safeFileName = GetSafeFileName(file.FileName);
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

        ImageFileFormat extensionFormat =
            GetFormatFromExtension(extension)
            ?? throw new InvalidOperationException(
                $"The configured extension '{extension}' does not have " +
                "a corresponding signature validator.");

        ImageFileFormat contentTypeFormat =
            GetFormatFromContentType(file.ContentType)
            ?? throw new InvalidOperationException(
                $"The configured content type '{file.ContentType}' does not have " +
                "a corresponding signature validator.");

        if (extensionFormat != contentTypeFormat)
        {
            logger.LogInformation(
                "An upload was rejected because extension {Extension} and " +
                "content type {ContentType} represent different formats.",
                extension,
                file.ContentType);

            return ImageFileValidationResult.Failure(
                errorCode: "file_metadata_mismatch",
                errorMessage:
                    $"The extension '{extension}' does not match the " +
                    $"content type '{file.ContentType}'.");
        }

        byte[] header = new byte[HeaderLength];

        await using Stream stream = file.OpenReadStream();

        int bytesRead = await ReadHeaderAsync(
            stream,
            header,
            cancellationToken);

        ImageFileFormat? detectedFormat = DetectFormat(
            header,
            bytesRead);

        if (detectedFormat is null)
        {
            logger.LogInformation(
                "An upload was rejected because its binary signature " +
                "was not recognized.");

            return ImageFileValidationResult.Failure(
                errorCode: "unrecognized_file_signature",
                errorMessage:
                    "The uploaded file does not have a recognized image signature.");
        }

        if (detectedFormat != extensionFormat)
        {
            logger.LogInformation(
                "An upload was rejected because the detected format " +
                "{DetectedFormat} does not match extension {Extension}.",
                detectedFormat,
                extension);

            return ImageFileValidationResult.Failure(
                errorCode: "file_signature_mismatch",
                errorMessage:
                    $"The uploaded file content does not match " +
                    $"the '{extension}' extension.");
        }

        return ImageFileValidationResult.Success(
            detectedFormat.Value);
    }

    private static async Task<int> ReadHeaderAsync(
        Stream stream,
        byte[] buffer,
        CancellationToken cancellationToken)
    {
        int totalBytesRead = 0;

        while (totalBytesRead < buffer.Length)
        {
            int bytesRead = await stream.ReadAsync(
                buffer.AsMemory(
                    totalBytesRead,
                    buffer.Length - totalBytesRead),
                cancellationToken);

            if (bytesRead == 0)
            {
                break;
            }

            totalBytesRead += bytesRead;
        }

        return totalBytesRead;
    }

    private static ImageFileFormat? DetectFormat(
        byte[] header,
        int bytesRead)
    {
        ReadOnlySpan<byte> headerBytes =
            header.AsSpan(0, bytesRead);

        if (headerBytes.StartsWith(PngSignature))
        {
            return ImageFileFormat.Png;
        }

        if (headerBytes.StartsWith(JpegSignature))
        {
            return ImageFileFormat.Jpeg;
        }

        bool isWebP =
            headerBytes.Length >= HeaderLength &&
            headerBytes[..4].SequenceEqual(RiffSignature) &&
            headerBytes.Slice(8, 4).SequenceEqual(WebPContainerSignature);

        if (isWebP)
        {
            return ImageFileFormat.WebP;
        }

        return null;
    }

    private static ImageFileFormat? GetFormatFromExtension(
        string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".jpg" => ImageFileFormat.Jpeg,
            ".jpeg" => ImageFileFormat.Jpeg,
            ".png" => ImageFileFormat.Png,
            ".webp" => ImageFileFormat.WebP,
            _ => null
        };
    }

    private static ImageFileFormat? GetFormatFromContentType(
        string contentType)
    {
        return contentType.ToLowerInvariant() switch
        {
            "image/jpeg" => ImageFileFormat.Jpeg,
            "image/png" => ImageFileFormat.Png,
            "image/webp" => ImageFileFormat.WebP,
            _ => null
        };
    }

    private static string GetSafeFileName(string fileName)
    {
        string normalizedFileName = fileName.Replace('\\', '/');

        return Path.GetFileName(normalizedFileName);
    }
}