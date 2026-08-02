using CodeOcr.Api.Contracts;
using CodeOcr.Api.Ocr;
using CodeOcr.Api.Ocr.Contracts;
using CodeOcr.Api.Persistence;
using CodeOcr.Api.Persistence.Entities;
using CodeOcr.Api.Storage;
using CodeOcr.Api.Validation;

namespace CodeOcr.Api.Workflows;

public sealed class ImageOcrWorkflow(
    IPaddleOcrClient paddleOcrClient,
    IImageFileStorage imageFileStorage,
    IImageOcrRepository repository,
    ILogger<ImageOcrWorkflow> logger) : IImageOcrWorkflow
{
    public async Task<ImageOcrResponse> RecognizeAsync(
        IFormFile file,
        ImageFileFormat detectedFormat,
        string safeFileName,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentException.ThrowIfNullOrWhiteSpace(safeFileName);

        byte[] imageContent = await ReadFileBytesAsync(file, cancellationToken);

        PaddleOcrResponse paddleOcrResponse = await paddleOcrClient.RecognizeAsync(
            imageContent,
            safeFileName,
            file.ContentType,
            cancellationToken);

        StoredImageFile storedImage = await imageFileStorage.SaveAsync(
            file,
            detectedFormat,
            cancellationToken);

        ImageOcrRecord record = CreateRecord(
            storedImage,
            file,
            detectedFormat,
            safeFileName,
            paddleOcrResponse);

        try
        {
            await repository.AddAsync(record, cancellationToken);
        }
        catch
        {
            await TryDeleteStoredImageAsync(storedImage.StoredFileName);
            throw;
        }

        return CreateResponse(storedImage, file, detectedFormat, safeFileName, paddleOcrResponse);
    }

    private async Task TryDeleteStoredImageAsync(string storedFileName)
    {
        try
        {
            await imageFileStorage.DeleteAsync(storedFileName, CancellationToken.None);
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Could not delete stored image {StoredFileName} after persistence failed.",
                storedFileName);
        }
    }

    private static async Task<byte[]> ReadFileBytesAsync(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file.Length > int.MaxValue)
        {
            throw new InvalidOperationException(
                "The uploaded file is too large to load into memory.");
        }

        using var memoryStream = new MemoryStream(capacity: (int)file.Length);

        await file.CopyToAsync(memoryStream, cancellationToken);

        return memoryStream.ToArray();
    }

    private static ImageOcrRecord CreateRecord(
        StoredImageFile storedImage,
        IFormFile file,
        ImageFileFormat detectedFormat,
        string safeFileName,
        PaddleOcrResponse paddleOcrResponse)
    {
        List<OcrLineRecord> lines = paddleOcrResponse.Lines
            .Select((line, index) => new OcrLineRecord
            {
                SequenceNumber = index,
                Text = line.Text,
                Confidence = line.Confidence
            })
            .ToList();

        return new ImageOcrRecord
        {
            Id = storedImage.Id,
            OriginalFileName = safeFileName,
            StoredFileName = storedImage.StoredFileName,
            ContentType = file.ContentType,
            SizeBytes = storedImage.SizeBytes,
            DetectedFormat = ToApiFormat(detectedFormat),
            StoredAtUtc = storedImage.StoredAtUtc.UtcDateTime,
            FullText = paddleOcrResponse.FullText,
            ProcessingTimeMs = paddleOcrResponse.ProcessingTimeMs,
            Lines = lines
        };
    }

    private static ImageOcrResponse CreateResponse(
        StoredImageFile storedImage,
        IFormFile file,
        ImageFileFormat detectedFormat,
        string safeFileName,
        PaddleOcrResponse paddleOcrResponse)
    {
        OcrLineResponse[] lines = paddleOcrResponse.Lines
            .Select(line => new OcrLineResponse(
                Text: line.Text,
                Confidence: line.Confidence))
            .ToArray();

        var rawOcr = new RawOcrResultResponse(
            Lines: lines,
            FullText: paddleOcrResponse.FullText,
            ProcessingTimeMs: paddleOcrResponse.ProcessingTimeMs);

        return new ImageOcrResponse(
            ImageId: storedImage.Id,
            OriginalFileName: safeFileName,
            StoredFileName: storedImage.StoredFileName,
            ContentType: file.ContentType,
            SizeBytes: storedImage.SizeBytes,
            DetectedFormat: ToApiFormat(detectedFormat),
            StoredAtUtc: storedImage.StoredAtUtc,
            RawOcr: rawOcr);
    }

    private static string ToApiFormat(ImageFileFormat detectedFormat)
    {
        return detectedFormat.ToString().ToLowerInvariant();
    }
}