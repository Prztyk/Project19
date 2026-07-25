using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CodeOcr.Api.Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace CodeOcr.Tests.Images;

public sealed class ImageStorageEndpointTests
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
    public async Task StoreImage_WithValidPng_SavesGeneratedFile()
    {
        // Arrange
        string temporaryDirectory = Path.Combine(
            Path.GetTempPath(),
            "Project19.Tests",
            Guid.NewGuid().ToString("N"));

        try
        {
            using WebApplicationFactory<Program> factory =
                new WebApplicationFactory<Program>()
                    .WithWebHostBuilder(builder =>
                    {
                        builder.ConfigureAppConfiguration(
                            (_, configurationBuilder) =>
                            {
                                var testConfiguration =
                                    new Dictionary<string, string?>
                                    {
                                        [
                                            "ImageStorage:DirectoryPath"
                                        ] = temporaryDirectory
                                    };

                                configurationBuilder
                                    .AddInMemoryCollection(
                                        testConfiguration);
                            });
                    });

            using HttpClient httpClient = factory.CreateClient();

            using var requestContent =
                CreateMultipartContent(
                    PngContent,
                    fileName:
                        @"C:\untrusted\original.png",
                    contentType: "image/png");

            // Act
            HttpResponseMessage response =
                await httpClient.PostAsync("/api/images", requestContent);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            StoredImageResponse? storedImage =
                await response.Content
                    .ReadFromJsonAsync<
                        StoredImageResponse>();

            Assert.NotNull(storedImage);

            Assert.Equal("original.png", storedImage.OriginalFileName);

            Assert.Equal($"{storedImage.ImageId:N}.png", storedImage.StoredFileName);

            Assert.NotEqual(storedImage.OriginalFileName, storedImage.StoredFileName);

            string storedFilePath = Path.Combine(
                temporaryDirectory,
                storedImage.StoredFileName);

            Assert.True(File.Exists(storedFilePath));

            byte[] storedBytes = await File.ReadAllBytesAsync(storedFilePath);

            Assert.Equal(PngContent, storedBytes);
        }
        finally
        {
            if (Directory.Exists(temporaryDirectory))
            {
                Directory.Delete(
                    temporaryDirectory,
                    recursive: true);
            }
        }
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
}