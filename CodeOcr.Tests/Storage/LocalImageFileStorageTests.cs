using CodeOcr.Api.Configuration;
using CodeOcr.Api.Storage;
using CodeOcr.Api.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CodeOcr.Tests.Storage;

public sealed class LocalImageFileStorageTests
{
    [Fact]
    public async Task SaveAsync_WhenCopyFails_RemovesPartialFile()
    {
        // Arrange
        string temporaryDirectory = Path.Combine(
            Path.GetTempPath(),
            "Project19.Tests",
            Guid.NewGuid().ToString("N"));

        try
        {
            IOptions<ImageStorageOptions> options =
                Options.Create(
                    new ImageStorageOptions
                    {
                        DirectoryPath = temporaryDirectory
                    });

            var hostEnvironment =
                new TestHostEnvironment
                {
                    ContentRootPath = temporaryDirectory
                };

            var storage = new LocalImageFileStorage(
                options,
                hostEnvironment,
                NullLogger<
                    LocalImageFileStorage>.Instance);

            IFormFile failingFile =
                new FailingFormFile();

            // Act
            await Assert.ThrowsAsync<IOException>(
                () => storage.SaveAsync(
                    failingFile,
                    ImageFileFormat.Png,
                    CancellationToken.None));

            // Assert
            Assert.True(
                Directory.Exists(temporaryDirectory));

            Assert.Empty(
                Directory.EnumerateFiles(
                    temporaryDirectory));
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

    private sealed class FailingFormFile : IFormFile
    {
        public string ContentType => "image/png";

        public string ContentDisposition =>
            "form-data; name=\"file\"; " +
            "filename=\"sample.png\"";

        public IHeaderDictionary Headers { get; } =
            new HeaderDictionary();

        public long Length => 12;

        public string Name => "file";

        public string FileName => "sample.png";

        public void CopyTo(Stream target)
        {
            throw new IOException(
                "Simulated storage failure.");
        }

        public async Task CopyToAsync(
            Stream target,
            CancellationToken cancellationToken = default)
        {
            byte[] partialContent =
            [
                0x89,
                0x50,
                0x4E,
                0x47
            ];

            await target.WriteAsync(
                partialContent,
                cancellationToken);

            throw new IOException(
                "Simulated storage failure.");
        }

        public Stream OpenReadStream()
        {
            throw new NotSupportedException();
        }
    }

    private sealed class TestHostEnvironment
        : IHostEnvironment
    {
        public string EnvironmentName { get; set; } =
            "Test";

        public string ApplicationName { get; set; } =
            "CodeOcr.Tests";

        public string ContentRootPath { get; set; } =
            string.Empty;

        public IFileProvider ContentRootFileProvider
        {
            get;
            set;
        } = new NullFileProvider();
    }
}