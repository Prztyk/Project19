using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CodeOcr.Api.Configuration;
using CodeOcr.Api.Ocr.Contracts;
using Microsoft.Extensions.Options;

namespace CodeOcr.Api.Ocr;

public sealed class PaddleOcrClient : IPaddleOcrClient
{
    private const string FileFormFieldName = "file";

    private readonly HttpClient _httpClient;
    private readonly string _recognizePath;
    private readonly ILogger<PaddleOcrClient> _logger;

    public PaddleOcrClient(
        HttpClient httpClient,
        IOptions<PaddleOcrOptions> options,
        ILogger<PaddleOcrClient> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        PaddleOcrOptions settings = options.Value;

        _httpClient = httpClient;
        _httpClient.BaseAddress = CreateBaseAddress(settings.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);

        _recognizePath = settings.RecognizePath;
        _logger = logger;
    }

    public async Task<PaddleOcrResponse> RecognizeAsync(
        byte[] imageContent,
        string fileName,
        string contentType,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(imageContent);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        if (imageContent.Length == 0)
        {
            throw new ArgumentException(
                "The OCR image content cannot be empty.",
                nameof(imageContent));
        }

        if (!MediaTypeHeaderValue.TryParse(
                contentType,
                out MediaTypeHeaderValue? mediaType))
        {
            throw new ArgumentException(
                "The OCR image content type is invalid.",
                nameof(contentType));
        }

        string safeFileName = GetSafeFileName(fileName);

        using var multipartContent = new MultipartFormDataContent();

        using var imageHttpContent = new ByteArrayContent(imageContent);

        imageHttpContent.Headers.ContentType = mediaType;

        multipartContent.Add(
            imageHttpContent,
            FileFormFieldName,
            safeFileName);

        _logger.LogDebug(
            "Sending an image to PaddleOCR. Size: {ImageSizeBytes} bytes. Content type: {ContentType}.",
            imageContent.Length,
            contentType);

        try
        {
            using HttpResponseMessage response =
                await _httpClient.PostAsync(
                    _recognizePath,
                    multipartContent,
                    cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "PaddleOCR returned HTTP status {StatusCode}.",
                    (int)response.StatusCode);

                throw PaddleOcrClientException.ServiceError(response.StatusCode);
            }

            PaddleOcrResponse? ocrResponse;

            try
            {
                ocrResponse =
                    await response.Content
                        .ReadFromJsonAsync<PaddleOcrResponse>(cancellationToken: cancellationToken);
            }
            catch (JsonException exception)
            {
                throw PaddleOcrClientException
                    .InvalidResponse(
                        "The local PaddleOCR service returned invalid JSON.",
                        exception);
            }
            catch (NotSupportedException exception)
            {
                throw PaddleOcrClientException
                    .InvalidResponse(
                        "The local PaddleOCR service returned an unsupported response.",
                        exception);
            }

            ValidateResponse(ocrResponse);

            _logger.LogInformation(
                "PaddleOCR completed recognition in {ProcessingTimeMs} ms and returned{LineCount} lines.",
                ocrResponse.ProcessingTimeMs,
                ocrResponse.Lines.Count);

            return ocrResponse;
        }
        catch (PaddleOcrClientException)
        {
            throw;
        }
        catch (OperationCanceledException exception)
            when (!cancellationToken.IsCancellationRequested)
        {
            throw PaddleOcrClientException.Timeout(exception);
        }
        catch (HttpRequestException exception)
        {
            throw PaddleOcrClientException.Unavailable(exception);
        }
    }

    private static void ValidateResponse(PaddleOcrResponse? response)
    {
        if (response is null)
        {
            throw PaddleOcrClientException
                .InvalidResponse("The local PaddleOCR servicereturned an empty response.");
        }

        if (response.Lines is null)
        {
            throw PaddleOcrClientException
                .InvalidResponse("The local PaddleOCR response does not contain a lines collection.");
        }

        if (response.FullText is null)
        {
            throw PaddleOcrClientException
                .InvalidResponse("The local PaddleOCR response does not contain full text.");
        }

        if (response.ProcessingTimeMs < 0)
        {
            throw PaddleOcrClientException
                .InvalidResponse("The local PaddleOCR response contains a negative processing time.");
        }

        foreach (PaddleOcrLine line in response.Lines)
        {
            if (line is null || line.Text is null)
            {
                throw PaddleOcrClientException
                    .InvalidResponse("The local PaddleOCR response contains an invalid line.");
            }

            if (line.Confidence is double confidence && (confidence < 0 || confidence > 1))
            {
                throw PaddleOcrClientException
                    .InvalidResponse("The local PaddleOCR response contains confidence outside the range from 0 to 1.");
            }
        }
    }

    private static Uri CreateBaseAddress(string baseUrl)
    {
        string normalizedBaseUrl =
            baseUrl.EndsWith(
                "/",
                StringComparison.Ordinal)
                ? baseUrl
                : $"{baseUrl}/";

        return new Uri(
            normalizedBaseUrl,
            UriKind.Absolute);
    }

    private static string GetSafeFileName(string fileName)
    {
        string normalizedFileName = fileName.Replace('\\', '/');

        string safeFileName = Path.GetFileName(normalizedFileName);

        if (string.IsNullOrWhiteSpace(safeFileName))
        {
            throw new ArgumentException(
                "The OCR image filename is invalid.",
                nameof(fileName));
        }

        return safeFileName;
    }
}