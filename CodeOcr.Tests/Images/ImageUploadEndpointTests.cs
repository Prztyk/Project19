using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CodeOcr.Api.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CodeOcr.Tests.Images;

public sealed class ImageUploadEndpointTests
    : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly byte[] PngHeader =
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

    private static readonly byte[] JpegHeader =
    [
        0xFF,
        0xD8,
        0xFF,
        0xE0,
        0x00,
        0x10,
        0x4A,
        0x46,
        0x49,
        0x46,
        0x00,
        0x01
    ];

    private static readonly byte[] WebPHeader =
    [
        0x52,
        0x49,
        0x46,
        0x46,
        0x04,
        0x00,
        0x00,
        0x00,
        0x57,
        0x45,
        0x42,
        0x50
    ];

    private readonly HttpClient _httpClient;

    public ImageUploadEndpointTests(
        WebApplicationFactory<Program> applicationFactory)
    {
        _httpClient = applicationFactory.CreateClient();
    }

    public static TheoryData<byte[], string, string, string>
        SupportedImageFormats =>
        new()
        {
            {
                PngHeader,
                "sample.png",
                "image/png",
                "png"
            },
            {
                JpegHeader,
                "sample.jpg",
                "image/jpeg",
                "jpeg"
            },
            {
                WebPHeader,
                "sample.webp",
                "image/webp",
                "webp"
            }
        };

    [Theory]
    [MemberData(nameof(SupportedImageFormats))]
    public async Task ValidateImage_WithSupportedSignature_ReturnsDetectedFormat(
        byte[] fileContent,
        string fileName,
        string contentType,
        string expectedFormat)
    {
        // Arrange
        using var requestContent = CreateMultipartContent(
            fileContent,
            fileName,
            contentType);

        // Act
        HttpResponseMessage response = await _httpClient.PostAsync(
            "/api/images/validate",
            requestContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ImageUploadResponse? uploadResponse =
            await response.Content
                .ReadFromJsonAsync<ImageUploadResponse>();

        Assert.NotNull(uploadResponse);
        Assert.Equal(fileName, uploadResponse.FileName);
        Assert.Equal(contentType, uploadResponse.ContentType);
        Assert.Equal(fileContent.Length, uploadResponse.SizeBytes);
        Assert.Equal(expectedFormat, uploadResponse.DetectedFormat);
    }

    [Fact]
    public async Task ValidateImage_WithEmptyFile_ReturnsBadRequest()
    {
        // Arrange
        using var requestContent = CreateMultipartContent(
            fileContent: [],
            fileName: "empty.png",
            contentType: "image/png");

        // Act
        HttpResponseMessage response = await _httpClient.PostAsync(
            "/api/images/validate",
            requestContent);

        // Assert
        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        string responseBody =
            await response.Content.ReadAsStringAsync();

        Assert.Contains("empty_file", responseBody);
    }

    [Fact]
    public async Task ValidateImage_WithUnsupportedExtension_ReturnsBadRequest()
    {
        // Arrange
        using var requestContent = CreateMultipartContent(
            fileContent: PngHeader,
            fileName: "sample.txt",
            contentType: "image/png");

        // Act
        HttpResponseMessage response = await _httpClient.PostAsync(
            "/api/images/validate",
            requestContent);

        // Assert
        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        string responseBody =
            await response.Content.ReadAsStringAsync();

        Assert.Contains(
            "unsupported_file_extension",
            responseBody);
    }

    [Fact]
    public async Task ValidateImage_WithUnsupportedContentType_ReturnsBadRequest()
    {
        // Arrange
        using var requestContent = CreateMultipartContent(
            fileContent: PngHeader,
            fileName: "sample.png",
            contentType: "text/plain");

        // Act
        HttpResponseMessage response = await _httpClient.PostAsync(
            "/api/images/validate",
            requestContent);

        // Assert
        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        string responseBody =
            await response.Content.ReadAsStringAsync();

        Assert.Contains(
            "unsupported_content_type",
            responseBody);
    }

    [Fact]
    public async Task ValidateImage_WithUnrecognizedSignature_ReturnsBadRequest()
    {
        // Arrange
        byte[] textContent =
            "This is not an image."u8.ToArray();

        using var requestContent = CreateMultipartContent(
            fileContent: textContent,
            fileName: "fake.png",
            contentType: "image/png");

        // Act
        HttpResponseMessage response = await _httpClient.PostAsync(
            "/api/images/validate",
            requestContent);

        // Assert
        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        string responseBody =
            await response.Content.ReadAsStringAsync();

        Assert.Contains(
            "unrecognized_file_signature",
            responseBody);
    }

    [Fact]
    public async Task ValidateImage_WithSignatureMismatch_ReturnsBadRequest()
    {
        // Arrange
        using var requestContent = CreateMultipartContent(
            fileContent: JpegHeader,
            fileName: "fake.png",
            contentType: "image/png");

        // Act
        HttpResponseMessage response = await _httpClient.PostAsync(
            "/api/images/validate",
            requestContent);

        // Assert
        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        string responseBody =
            await response.Content.ReadAsStringAsync();

        Assert.Contains(
            "file_signature_mismatch",
            responseBody);
    }

    [Fact]
    public async Task ValidateImage_WithMetadataMismatch_ReturnsBadRequest()
    {
        // Arrange
        using var requestContent = CreateMultipartContent(
            fileContent: PngHeader,
            fileName: "sample.png",
            contentType: "image/jpeg");

        // Act
        HttpResponseMessage response = await _httpClient.PostAsync(
            "/api/images/validate",
            requestContent);

        // Assert
        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        string responseBody =
            await response.Content.ReadAsStringAsync();

        Assert.Contains(
            "file_metadata_mismatch",
            responseBody);
    }

    [Fact]
    public async Task ValidateImage_RemovesPathFromReturnedFileName()
    {
        // Arrange
        using var requestContent = CreateMultipartContent(
            fileContent: PngHeader,
            fileName: @"C:\untrusted\sample.png",
            contentType: "image/png");

        // Act
        HttpResponseMessage response = await _httpClient.PostAsync(
            "/api/images/validate",
            requestContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ImageUploadResponse? uploadResponse =
            await response.Content
                .ReadFromJsonAsync<ImageUploadResponse>();

        Assert.NotNull(uploadResponse);
        Assert.Equal("sample.png", uploadResponse.FileName);
    }

    private static MultipartFormDataContent CreateMultipartContent(
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