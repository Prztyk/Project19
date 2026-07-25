namespace CodeOcr.Api.Validation;

public interface IImageFileValidator
{
    Task<ImageFileValidationResult> ValidateAsync(
        IFormFile file, 
        CancellationToken cancellationToken);
}