using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CodeOcr.Api.Contracts;
using CodeOcr.Api.Ocr;
using CodeOcr.Api.Ocr.Contracts;
using CodeOcr.Api.Persistence;
using CodeOcr.Api.Persistence.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CodeOcr.Tests.Persistence;

public sealed class ImageOcrPersistenceTests
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
    public async Task RecognizeImage_WithValidImage_PersistsImageAndOcrLines()
    {
        string temporaryDirectory = CreateTemporaryDirectoryPath();
        string databasePath = Path.Combine(temporaryDirectory, "codeocr-tests.db");
        string imageDirectory = Path.Combine(temporaryDirectory, "images");

        try
        {
            using WebApplicationFactory<Program> factory = CreateFactory(
                databasePath,
                imageDirectory);

            await CreateDatabaseAsync(factory);

            using HttpClient httpClient = factory.CreateClient();
            using MultipartFormDataContent request = CreateMultipartContent();

            HttpResponseMessage response = await httpClient.PostAsync(
                "/api/ocr/recognize",
                request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            ImageOcrResponse? apiResponse =
                await response.Content.ReadFromJsonAsync<ImageOcrResponse>();

            Assert.NotNull(apiResponse);

            using IServiceScope scope = factory.Services.CreateScope();

            CodeOcrDbContext dbContext =
                scope.ServiceProvider.GetRequiredService<CodeOcrDbContext>();

            ImageOcrRecord persistedRecord = await dbContext.ImageOcrRecords
                .AsNoTracking()
                .Include(record => record.Lines)
                .SingleAsync(record => record.Id == apiResponse.ImageId);

            Assert.Equal("sample.png", persistedRecord.OriginalFileName);
            Assert.Equal(apiResponse.StoredFileName, persistedRecord.StoredFileName);
            Assert.Equal("image/png", persistedRecord.ContentType);
            Assert.Equal(PngContent.Length, persistedRecord.SizeBytes);
            Assert.Equal("png", persistedRecord.DetectedFormat);
            Assert.Equal("public class Customer", persistedRecord.FullText);
            Assert.Equal(142, persistedRecord.ProcessingTimeMs);

            OcrLineRecord line = Assert.Single(persistedRecord.Lines);

            Assert.Equal(0, line.SequenceNumber);
            Assert.Equal("public class Customer", line.Text);
            Assert.Equal(0.97, line.Confidence);

            string storedFilePath = Path.Combine(imageDirectory, apiResponse.StoredFileName);

            Assert.True(File.Exists(storedFilePath));
        }
        finally
        {
            DeleteTemporaryDirectory(temporaryDirectory);
        }
    }

    private static WebApplicationFactory<Program> CreateFactory(
        string databasePath,
        string imageDirectory)
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configurationBuilder) =>
                {
                    var configuration = new Dictionary<string, string?>
                    {
                        ["Database:FilePath"] = databasePath,
                        ["ImageStorage:DirectoryPath"] = imageDirectory
                    };

                    configurationBuilder.AddInMemoryCollection(configuration);
                });

                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<IPaddleOcrClient>();
                    services.AddSingleton<IPaddleOcrClient, FakePaddleOcrClient>();
                });
            });
    }

    private static async Task CreateDatabaseAsync(WebApplicationFactory<Program> factory)
    {
        using IServiceScope scope = factory.Services.CreateScope();

        CodeOcrDbContext dbContext =
            scope.ServiceProvider.GetRequiredService<CodeOcrDbContext>();

        await dbContext.Database.EnsureCreatedAsync();
    }

    private static MultipartFormDataContent CreateMultipartContent()
    {
        var multipartContent = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(PngContent);

        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");

        multipartContent.Add(fileContent, "file", "sample.png");

        return multipartContent;
    }

    private static string CreateTemporaryDirectoryPath()
    {
        return Path.Combine(
            Path.GetTempPath(),
            "Project19.Tests",
            Guid.NewGuid().ToString("N"));
    }

    private static void DeleteTemporaryDirectory(string temporaryDirectory)
    {
        if (Directory.Exists(temporaryDirectory))
        {
            Directory.Delete(temporaryDirectory, recursive: true);
        }
    }

    private sealed class FakePaddleOcrClient : IPaddleOcrClient
    {
        public Task<PaddleOcrResponse> RecognizeAsync(
            byte[] imageContent,
            string fileName,
            string contentType,
            CancellationToken cancellationToken)
        {
            var response = new PaddleOcrResponse(
                Lines:
                [
                    new PaddleOcrLine(
                        Text: "public class Customer",
                        Confidence: 0.97)
                ],
                FullText: "public class Customer",
                ProcessingTimeMs: 142);

            return Task.FromResult(response);
        }
    }
}