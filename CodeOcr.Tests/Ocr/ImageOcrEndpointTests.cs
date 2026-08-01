using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CodeOcr.Api.Contracts;
using CodeOcr.Api.Ocr;
using CodeOcr.Api.Ocr.Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CodeOcr.Tests.Ocr;

public sealed class ImageOcrEndpointTests
{
    private static readonly byte[] PngContent =
    [
        0x89,
        0x50,
        0x4E,
        0x47,
        0x0D,
        0x0A,
        0x1A,
        0x0A,
        0x00,
        0x00,
        0x00,
        0x0D
    ];

    [Fact]
    public async Task RecognizeImage_WithValidPng_ReturnsRawOcrAndStoresImage()
    {
        // Arrange
        string temporaryDirectory =
            CreateTemporaryDirectoryPath();

        var ocrResponse =
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

        var fakeOcrClient =
            new RecordingPaddleOcrClient(
                ocrResponse);

        try
        {
            using WebApplicationFactory<Program> factory =
                CreateFactory(
                    temporaryDirectory,
                    fakeOcrClient);

            using HttpClient httpClient =
                factory.CreateClient();

            using MultipartFormDataContent requestContent =
                CreateMultipartContent(
                    PngContent,
                    fileName:
                        @"C:\untrusted\sample.png",
                    contentType:
                        "image/png");

            // Act
            HttpResponseMessage response =
                await httpClient.PostAsync(
                    "/api/ocr/recognize",
                    requestContent);

            // Assert
            Assert.Equal(
                HttpStatusCode.OK,
                response.StatusCode);

            ImageOcrResponse? result =
                await response.Content
                    .ReadFromJsonAsync<
                        ImageOcrResponse>();

            Assert.NotNull(result);

            Assert.Equal(
                "sample.png",
                result.OriginalFileName);

            Assert.Equal(
                $"{result.ImageId:N}.png",
                result.StoredFileName);

            Assert.Equal(
                "image/png",
                result.ContentType);

            Assert.Equal(
                PngContent.Length,
                result.SizeBytes);

            Assert.Equal(
                "png",
                result.DetectedFormat);

            Assert.Equal(
                "public class Customer",
                result.RawOcr.FullText);

            Assert.Equal(
                142,
                result.RawOcr.ProcessingTimeMs);

            OcrLineResponse line =
                Assert.Single(
                    result.RawOcr.Lines);

            Assert.Equal(
                "public class Customer",
                line.Text);

            Assert.Equal(
                0.97,
                line.Confidence);

            Assert.Equal(
                PngContent,
                fakeOcrClient
                    .ReceivedImageContent);

            Assert.Equal(
                "sample.png",
                fakeOcrClient
                    .ReceivedFileName);

            Assert.Equal(
                "image/png",
                fakeOcrClient
                    .ReceivedContentType);

            string storedFilePath =
                Path.Combine(
                    temporaryDirectory,
                    result.StoredFileName);

            Assert.True(
                File.Exists(
                    storedFilePath));

            byte[] storedContent =
                await File.ReadAllBytesAsync(
                    storedFilePath);

            Assert.Equal(
                PngContent,
                storedContent);
        }
        finally
        {
            DeleteTemporaryDirectory(
                temporaryDirectory);
        }
    }

    [Theory]
    [InlineData(
        PaddleOcrFailureKind.Unavailable,
        HttpStatusCode.ServiceUnavailable,
        "ocr_service_unavailable")]
    [InlineData(
        PaddleOcrFailureKind.Timeout,
        HttpStatusCode.GatewayTimeout,
        "ocr_timeout")]
    [InlineData(
        PaddleOcrFailureKind.ServiceError,
        HttpStatusCode.BadGateway,
        "ocr_service_error")]
    [InlineData(
        PaddleOcrFailureKind.InvalidResponse,
        HttpStatusCode.BadGateway,
        "ocr_invalid_response")]
    public async Task RecognizeImage_WhenOcrFails_ReturnsProblemDetailsWithoutStoringFile(
        PaddleOcrFailureKind failureKind,
        HttpStatusCode expectedStatusCode,
        string expectedErrorCode)
    {
        // Arrange
        string temporaryDirectory =
            CreateTemporaryDirectoryPath();

        PaddleOcrClientException exception =
            CreateOcrException(
                failureKind);

        var fakeOcrClient =
            new FailingPaddleOcrClient(
                exception);

        try
        {
            using WebApplicationFactory<Program> factory =
                CreateFactory(
                    temporaryDirectory,
                    fakeOcrClient);

            using HttpClient httpClient =
                factory.CreateClient();

            using MultipartFormDataContent requestContent =
                CreateMultipartContent(
                    PngContent,
                    fileName:
                        "sample.png",
                    contentType:
                        "image/png");

            // Act
            HttpResponseMessage response =
                await httpClient.PostAsync(
                    "/api/ocr/recognize",
                    requestContent);

            // Assert
            Assert.Equal(
                expectedStatusCode,
                response.StatusCode);

            Assert.Equal(
                "application/problem+json",
                response.Content.Headers
                    .ContentType?
                    .MediaType);

            string responseBody =
                await response.Content
                    .ReadAsStringAsync();

            using JsonDocument document =
                JsonDocument.Parse(
                    responseBody);

            JsonElement root =
                document.RootElement;

            Assert.Equal(
                expectedErrorCode,
                root.GetProperty(
                        "errorCode")
                    .GetString());

            Assert.False(
                string.IsNullOrWhiteSpace(
                    root.GetProperty(
                            "traceId")
                        .GetString()));

            bool storedFilesExist =
                Directory.Exists(
                    temporaryDirectory) &&
                Directory
                    .EnumerateFiles(
                        temporaryDirectory)
                    .Any();

            Assert.False(
                storedFilesExist);
        }
        finally
        {
            DeleteTemporaryDirectory(
                temporaryDirectory);
        }
    }

    private static WebApplicationFactory<Program>
        CreateFactory(
            string storageDirectory,
            IPaddleOcrClient paddleOcrClient)
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration(
                    (_, configurationBuilder) =>
                    {
                        var configuration =
                            new Dictionary<
                                string,
                                string?>
                            {
                                [
                                    "ImageStorage:" +
                                    "DirectoryPath"
                                ] = storageDirectory
                            };

                        configurationBuilder
                            .AddInMemoryCollection(
                                configuration);
                    });

                builder.ConfigureTestServices(
                    services =>
                    {
                        services.RemoveAll<
                            IPaddleOcrClient>();

                        services.AddSingleton(
                            paddleOcrClient);
                    });
            });
    }

    private static MultipartFormDataContent
        CreateMultipartContent(
            byte[] fileContent,
            string fileName,
            string contentType)
    {
        var multipartContent =
            new MultipartFormDataContent();

        var byteContent =
            new ByteArrayContent(
                fileContent);

        byteContent.Headers.ContentType =
            new MediaTypeHeaderValue(
                contentType);

        multipartContent.Add(
            byteContent,
            name: "file",
            fileName: fileName);

        return multipartContent;
    }

    private static PaddleOcrClientException
        CreateOcrException(
            PaddleOcrFailureKind failureKind)
    {
        return failureKind switch
        {
            PaddleOcrFailureKind.Unavailable =>
                PaddleOcrClientException
                    .Unavailable(
                        new HttpRequestException(
                            "Simulated connection failure.")),

            PaddleOcrFailureKind.Timeout =>
                PaddleOcrClientException
                    .Timeout(
                        new TaskCanceledException(
                            "Simulated timeout.")),

            PaddleOcrFailureKind.ServiceError =>
                PaddleOcrClientException
                    .ServiceError(
                        HttpStatusCode
                            .InternalServerError),

            PaddleOcrFailureKind.InvalidResponse =>
                PaddleOcrClientException
                    .InvalidResponse(
                        "Simulated invalid response."),

            _ =>
                throw new ArgumentOutOfRangeException(
                    nameof(failureKind),
                    failureKind,
                    "Unsupported OCR failure kind.")
        };
    }

    private static string
        CreateTemporaryDirectoryPath()
    {
        return Path.Combine(
            Path.GetTempPath(),
            "Project19.Tests",
            Guid.NewGuid().ToString("N"));
    }

    private static void DeleteTemporaryDirectory(
        string temporaryDirectory)
    {
        if (Directory.Exists(
                temporaryDirectory))
        {
            Directory.Delete(
                temporaryDirectory,
                recursive: true);
        }
    }

    private sealed class RecordingPaddleOcrClient(
        PaddleOcrResponse response)
        : IPaddleOcrClient
    {
        public byte[]? ReceivedImageContent
        {
            get;
            private set;
        }

        public string? ReceivedFileName
        {
            get;
            private set;
        }

        public string? ReceivedContentType
        {
            get;
            private set;
        }

        public Task<PaddleOcrResponse> RecognizeAsync(
            byte[] imageContent,
            string fileName,
            string contentType,
            CancellationToken cancellationToken)
        {
            ReceivedImageContent =
                imageContent.ToArray();

            ReceivedFileName =
                fileName;

            ReceivedContentType =
                contentType;

            return Task.FromResult(
                response);
        }
    }

    private sealed class FailingPaddleOcrClient(
        PaddleOcrClientException exception)
        : IPaddleOcrClient
    {
        public Task<PaddleOcrResponse> RecognizeAsync(
            byte[] imageContent,
            string fileName,
            string contentType,
            CancellationToken cancellationToken)
        {
            return Task.FromException<
                PaddleOcrResponse>(
                    exception);
        }
    }
}