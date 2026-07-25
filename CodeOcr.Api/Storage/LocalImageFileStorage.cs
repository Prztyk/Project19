using CodeOcr.Api.Configuration;
using CodeOcr.Api.Validation;
using Microsoft.Extensions.Options;

namespace CodeOcr.Api.Storage;

public sealed class LocalImageFileStorage : IImageFileStorage
{
    private const int FileBufferSize = 64 * 1024;

    private readonly string _storageDirectoryPath;
    private readonly ILogger<LocalImageFileStorage> _logger;

    public LocalImageFileStorage(
        IOptions<ImageStorageOptions> options,
        IHostEnvironment hostEnvironment,
        ILogger<LocalImageFileStorage> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(hostEnvironment);
        ArgumentNullException.ThrowIfNull(logger);

        _storageDirectoryPath = ResolveStorageDirectoryPath(
            hostEnvironment.ContentRootPath,
            options.Value.DirectoryPath);

        _logger = logger;
    }

    public async Task<StoredImageFile> SaveAsync(
        IFormFile file,
        ImageFileFormat detectedFormat,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(file);

        Directory.CreateDirectory(_storageDirectoryPath);

        Guid imageId = Guid.NewGuid();
        string extension = GetCanonicalExtension(detectedFormat);
        string storedFileName = $"{imageId:N}{extension}";

        string destinationPath = Path.Combine(
            _storageDirectoryPath,
            storedFileName);

        bool destinationFileCreated = false;

        try
        {
            var streamOptions = new FileStreamOptions
            {
                Mode = FileMode.CreateNew,
                Access = FileAccess.Write,
                Share = FileShare.None,
                BufferSize = FileBufferSize,
                Options = FileOptions.Asynchronous
            };

            await using (var destinationStream =
                         new FileStream(destinationPath, streamOptions))
            {
                destinationFileCreated = true;

                await file.CopyToAsync(
                    destinationStream,
                    cancellationToken);
            }
        }
        catch
        {
            if (destinationFileCreated)
            {
                TryDeletePartialFile(destinationPath);
            }

            throw;
        }

        DateTimeOffset storedAtUtc = DateTimeOffset.UtcNow;

        _logger.LogInformation(
            "Stored image {ImageId} as {StoredFileName}. " +
            "File size: {FileSizeBytes} bytes.",
            imageId,
            storedFileName,
            file.Length);

        return new StoredImageFile(
            Id: imageId,
            StoredFileName: storedFileName,
            SizeBytes: file.Length,
            StoredAtUtc: storedAtUtc);
    }

    private void TryDeletePartialFile(string destinationPath)
    {
        try
        {
            if (!File.Exists(destinationPath))
            {
                return;
            }

            File.Delete(destinationPath);

            _logger.LogInformation(
                "Removed partially written image file {StoredFileName}.",
                Path.GetFileName(destinationPath));
        }
        catch (Exception cleanupException)
        {
            _logger.LogWarning(
                cleanupException,
                "Could not remove partially written image file " +
                "{StoredFileName}.",
                Path.GetFileName(destinationPath));
        }
    }

    private static string ResolveStorageDirectoryPath(
        string contentRootPath,
        string configuredDirectoryPath)
    {
        if (string.IsNullOrWhiteSpace(configuredDirectoryPath))
        {
            throw new InvalidOperationException(
                "The image storage directory is not configured.");
        }

        if (Path.IsPathRooted(configuredDirectoryPath))
        {
            return Path.GetFullPath(configuredDirectoryPath);
        }

        return Path.GetFullPath(
            Path.Combine(
                contentRootPath,
                configuredDirectoryPath));
    }

    private static string GetCanonicalExtension(
        ImageFileFormat detectedFormat)
    {
        return detectedFormat switch
        {
            ImageFileFormat.Jpeg => ".jpg",
            ImageFileFormat.Png => ".png",
            ImageFileFormat.WebP => ".webp",
            _ => throw new ArgumentOutOfRangeException(
                nameof(detectedFormat),
                detectedFormat,
                "The image format is not supported by local storage.")
        };
    }
}