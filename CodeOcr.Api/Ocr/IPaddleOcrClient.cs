using CodeOcr.Api.Ocr.Contracts;

namespace CodeOcr.Api.Ocr;

public interface IPaddleOcrClient
{
    Task<PaddleOcrResponse> RecognizeAsync(
        byte[] imageContent,
        string fileName,
        string contentType,
        CancellationToken cancellationToken);
}