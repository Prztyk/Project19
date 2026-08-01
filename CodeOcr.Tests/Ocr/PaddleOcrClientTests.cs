using System.Net;
using System.Net.Http.Json;
using System.Text;
using CodeOcr.Api.Configuration;
using CodeOcr.Api.Ocr;
using CodeOcr.Api.Ocr.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CodeOcr.Tests.Ocr;

public sealed class PaddleOcrClientTests
{
    [Fact]
    public async Task RecognizeAsync_WithValidResponse_SendsMultipartRequest()
    {
        // Arrange
        byte[] imageContent =
        [
            0x89,
            0x50,
            0x4E,
            0x47
        ];

        var handler =
            new StubHttpMessageHandler(
                async (
                    request,
                    cancellationToken) =>
                {
                    Assert.Equal(
                        HttpMethod.Post,
                        request.Method);

                    Assert.Equal(
                        new Uri(
                            "http://127.0.0.1:8000/api/ocr"),
                        request.RequestUri);

                    MultipartFormDataContent multipart =
                        Assert.IsType<
                            MultipartFormDataContent>(
                            request.Content);

                    HttpContent filePart =
                        Assert.Single(multipart);

                    Assert.Equal(
                        "file",
                        filePart.Headers
                            .ContentDisposition?
                            .Name?
                            .Trim('"'));

                    Assert.Equal(
                        "sample.png",
                        filePart.Headers
                            .ContentDisposition?
                            .FileName?
                            .Trim('"'));

                    Assert.Equal(
                        "image/png",
                        filePart.Headers
                            .ContentType?
                            .MediaType);

                    byte[] sentImage =
                        await filePart
                            .ReadAsByteArrayAsync(
                                cancellationToken);

                    Assert.Equal(
                        imageContent,
                        sentImage);

                    var serviceResponse =
                        new PaddleOcrResponse(
                            Lines:
                            [
                                new PaddleOcrLine(
                                    Text:
                                        "public class Customer",
                                    Confidence:
                                        0.97)
                            ],
                            FullText:
                                "public class Customer",
                            ProcessingTimeMs:
                                142);

                    return new HttpResponseMessage(
                        HttpStatusCode.OK)
                    {
                        Content =
                            JsonContent.Create(
                                serviceResponse)
                    };
                });

        using var httpClient =
            new HttpClient(handler);

        PaddleOcrClient client =
            CreateClient(httpClient);

        // Act
        PaddleOcrResponse result =
            await client.RecognizeAsync(
                imageContent,
                fileName:
                    @"C:\untrusted\sample.png",
                contentType:
                    "image/png",
                CancellationToken.None);

        // Assert
        Assert.Equal(
            "public class Customer",
            result.FullText);

        Assert.Equal(
            142,
            result.ProcessingTimeMs);

        PaddleOcrLine line =
            Assert.Single(result.Lines);

        Assert.Equal(
            "public class Customer",
            line.Text);

        Assert.Equal(
            0.97,
            line.Confidence);
    }

    [Fact]
    public async Task RecognizeAsync_WhenServiceReturnsError_ThrowsServiceError()
    {
        // Arrange
        var handler =
            new StubHttpMessageHandler(
                (_, _) =>
                    Task.FromResult(
                        new HttpResponseMessage(
                            HttpStatusCode
                                .ServiceUnavailable)));

        using var httpClient =
            new HttpClient(handler);

        PaddleOcrClient client =
            CreateClient(httpClient);

        // Act
        PaddleOcrClientException exception =
            await Assert.ThrowsAsync<
                PaddleOcrClientException>(
                () => client.RecognizeAsync(
                    imageContent: [1, 2, 3],
                    fileName: "sample.png",
                    contentType: "image/png",
                    CancellationToken.None));

        // Assert
        Assert.Equal(
            PaddleOcrFailureKind.ServiceError,
            exception.FailureKind);

        Assert.Equal(
            HttpStatusCode.ServiceUnavailable,
            exception.StatusCode);
    }

    [Fact]
    public async Task RecognizeAsync_WhenConnectionFails_ThrowsUnavailable()
    {
        // Arrange
        var handler =
            new StubHttpMessageHandler(
                (_, _) =>
                    throw new HttpRequestException(
                        "Connection refused."));

        using var httpClient =
            new HttpClient(handler);

        PaddleOcrClient client =
            CreateClient(httpClient);

        // Act
        PaddleOcrClientException exception =
            await Assert.ThrowsAsync<
                PaddleOcrClientException>(
                () => client.RecognizeAsync(
                    imageContent: [1, 2, 3],
                    fileName: "sample.png",
                    contentType: "image/png",
                    CancellationToken.None));

        // Assert
        Assert.Equal(
            PaddleOcrFailureKind.Unavailable,
            exception.FailureKind);

        Assert.IsType<HttpRequestException>(
            exception.InnerException);
    }

    [Fact]
    public async Task RecognizeAsync_WhenRequestTimesOut_ThrowsTimeout()
    {
        // Arrange
        var handler =
            new StubHttpMessageHandler(
                (_, _) =>
                    throw new TaskCanceledException(
                        "Simulated timeout."));

        using var httpClient =
            new HttpClient(handler);

        PaddleOcrClient client =
            CreateClient(httpClient);

        // Act
        PaddleOcrClientException exception =
            await Assert.ThrowsAsync<
                PaddleOcrClientException>(
                () => client.RecognizeAsync(
                    imageContent: [1, 2, 3],
                    fileName: "sample.png",
                    contentType: "image/png",
                    CancellationToken.None));

        // Assert
        Assert.Equal(
            PaddleOcrFailureKind.Timeout,
            exception.FailureKind);

        Assert.IsAssignableFrom<
            OperationCanceledException>(
                exception.InnerException);
    }

    [Fact]
    public async Task RecognizeAsync_WhenJsonIsInvalid_ThrowsInvalidResponse()
    {
        // Arrange
        var handler =
            new StubHttpMessageHandler(
                (_, _) =>
                    Task.FromResult(
                        new HttpResponseMessage(
                            HttpStatusCode.OK)
                        {
                            Content =
                                new StringContent(
                                    "{ invalid-json",
                                    Encoding.UTF8,
                                    "application/json")
                        }));

        using var httpClient =
            new HttpClient(handler);

        PaddleOcrClient client =
            CreateClient(httpClient);

        // Act
        PaddleOcrClientException exception =
            await Assert.ThrowsAsync<
                PaddleOcrClientException>(
                () => client.RecognizeAsync(
                    imageContent: [1, 2, 3],
                    fileName: "sample.png",
                    contentType: "image/png",
                    CancellationToken.None));

        // Assert
        Assert.Equal(
            PaddleOcrFailureKind.InvalidResponse,
            exception.FailureKind);
    }

    [Fact]
    public async Task RecognizeAsync_WhenConfidenceIsInvalid_ThrowsInvalidResponse()
    {
        // Arrange
        var invalidResponse =
            new PaddleOcrResponse(
                Lines:
                [
                    new PaddleOcrLine(
                        Text: "invalid",
                        Confidence: 1.5)
                ],
                FullText: "invalid",
                ProcessingTimeMs: 10);

        var handler =
            new StubHttpMessageHandler(
                (_, _) =>
                    Task.FromResult(
                        new HttpResponseMessage(
                            HttpStatusCode.OK)
                        {
                            Content =
                                JsonContent.Create(
                                    invalidResponse)
                        }));

        using var httpClient =
            new HttpClient(handler);

        PaddleOcrClient client =
            CreateClient(httpClient);

        // Act
        PaddleOcrClientException exception =
            await Assert.ThrowsAsync<
                PaddleOcrClientException>(
                () => client.RecognizeAsync(
                    imageContent: [1, 2, 3],
                    fileName: "sample.png",
                    contentType: "image/png",
                    CancellationToken.None));

        // Assert
        Assert.Equal(
            PaddleOcrFailureKind.InvalidResponse,
            exception.FailureKind);
    }

    [Fact]
    public async Task RecognizeAsync_WhenCallerCancels_PreservesCancellation()
    {
        // Arrange
        using var cancellationTokenSource =
            new CancellationTokenSource();

        cancellationTokenSource.Cancel();

        var handler =
            new StubHttpMessageHandler(
                (_, cancellationToken) =>
                    Task.FromCanceled<
                        HttpResponseMessage>(
                        cancellationToken));

        using var httpClient =
            new HttpClient(handler);

        PaddleOcrClient client =
            CreateClient(httpClient);

        // Act and assert
        await Assert.ThrowsAnyAsync<
            OperationCanceledException>(
            () => client.RecognizeAsync(
                imageContent: [1, 2, 3],
                fileName: "sample.png",
                contentType: "image/png",
                cancellationTokenSource.Token));
    }

    private static PaddleOcrClient CreateClient(
        HttpClient httpClient)
    {
        IOptions<PaddleOcrOptions> options =
            Options.Create(
                new PaddleOcrOptions
                {
                    BaseUrl =
                        "http://127.0.0.1:8000",
                    RecognizePath =
                        "api/ocr",
                    TimeoutSeconds =
                        30
                });

        return new PaddleOcrClient(
            httpClient,
            options,
            NullLogger<
                PaddleOcrClient>.Instance);
    }

    private sealed class StubHttpMessageHandler(
        Func<
            HttpRequestMessage,
            CancellationToken,
            Task<HttpResponseMessage>>
            handler)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage>
            SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
        {
            return handler(
                request,
                cancellationToken);
        }
    }
}