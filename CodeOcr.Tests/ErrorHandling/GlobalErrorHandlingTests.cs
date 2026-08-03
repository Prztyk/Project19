using CodeOcr.Api.Storage;
using CodeOcr.Api.Validation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace CodeOcr.Tests.ErrorHandling;

public sealed class GlobalErrorHandlingTests
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
    public async Task UnknownEndpoint_ReturnsProblemDetails()
    {
        // Arrange
        using var factory =
            new WebApplicationFactory<Program>();

        using HttpClient httpClient =
            factory.CreateClient();

        // Act
        HttpResponseMessage response =
            await httpClient.GetAsync(
                "/api/does-not-exist");

        // Assert
        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);

        string responseBody =
            await response.Content.ReadAsStringAsync();

        using JsonDocument document =
            JsonDocument.Parse(responseBody);

        JsonElement root =
            document.RootElement;

        Assert.Equal(
            404,
            root.GetProperty("status").GetInt32());

        Assert.Equal(
            "resource_not_found",
            root.GetProperty("errorCode").GetString());

        Assert.False(
            string.IsNullOrWhiteSpace(
                root.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task StoreImage_WhenStorageFails_ReturnsSafeProblemDetails()
    {
        // Arrange
        using WebApplicationFactory<Program> factory =
            new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureTestServices(
                        services =>
                        {
                            services.RemoveAll<
                                IImageFileStorage>();

                            services.AddSingleton<
                                IImageFileStorage,
                                FailingImageFileStorage>();
                        });
                });

        using HttpClient httpClient =
            factory.CreateClient();

        using MultipartFormDataContent requestContent =
            CreateMultipartContent(
                PngContent,
                fileName: "sample.png",
                contentType: "image/png");

        // Act
        HttpResponseMessage response =
            await httpClient.PostAsync(
                "/api/images",
                requestContent);

        // Assert
        Assert.Equal(
            HttpStatusCode.InternalServerError,
            response.StatusCode);

        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);

        string responseBody =
            await response.Content.ReadAsStringAsync();

        using JsonDocument document =
            JsonDocument.Parse(responseBody);

        JsonElement root =
            document.RootElement;

        Assert.Equal(
            "Image storage failed.",
            root.GetProperty("title").GetString());

        Assert.Equal(
            500,
            root.GetProperty("status").GetInt32());

        Assert.Equal(
            "The uploaded image could not be stored.",
            root.GetProperty("detail").GetString());

        Assert.Equal(
            "image_storage_failed",
            root.GetProperty("errorCode").GetString());

        Assert.False(
            string.IsNullOrWhiteSpace(
                root.GetProperty("traceId").GetString()));

        Assert.DoesNotContain(
            "C:\\Sensitive",
            responseBody,
            StringComparison.OrdinalIgnoreCase);

        Assert.DoesNotContain(
            "Simulated disk failure",
            responseBody,
            StringComparison.OrdinalIgnoreCase);

        Assert.DoesNotContain(
            "stack",
            responseBody,
            StringComparison.OrdinalIgnoreCase);
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
            new ByteArrayContent(fileContent);

        byteContent.Headers.ContentType =
            new MediaTypeHeaderValue(contentType);

        multipartContent.Add(
            byteContent,
            name: "file",
            fileName: fileName);

        return multipartContent;
    }

    private sealed class FailingImageFileStorage
        : IImageFileStorage
    {
        public Task<StoredImageFile> SaveAsync(
            IFormFile file,
            ImageFileFormat detectedFormat,
            CancellationToken cancellationToken)
        {
            throw new ImageStorageException(
                "Sensitive path: C:\\Sensitive\\Images.",
                new IOException(
                    "Simulated disk failure."));
        }

        public Task DeleteAsync(string storedFileName, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}