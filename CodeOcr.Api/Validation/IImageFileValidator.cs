namespace CodeOcr.Api.Validation;

public interface IImageFileValidator
{
    ImageFileValidationResult Validate(IFormFile file);
}