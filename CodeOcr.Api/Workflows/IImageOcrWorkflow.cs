using CodeOcr.Api.Contracts;
using CodeOcr.Api.Validation;

namespace CodeOcr.Api.Workflows;

public interface IImageOcrWorkflow
{
    Task<ImageOcrResponse> RecognizeAsync(
        IFormFile file,
        ImageFileFormat detectedFormat,
        string safeFileName,
        CancellationToken cancellationToken);
}