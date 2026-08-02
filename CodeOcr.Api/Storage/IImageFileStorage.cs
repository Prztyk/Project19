using CodeOcr.Api.Validation;

namespace CodeOcr.Api.Storage;

public interface IImageFileStorage
{
    Task<StoredImageFile> SaveAsync(
        IFormFile file,
        ImageFileFormat detectedFormat,
        CancellationToken cancellationToken);

    Task DeleteAsync(string storedFileName, CancellationToken cancellationToken);
}