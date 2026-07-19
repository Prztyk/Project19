using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CodeOcr.Api.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CodeOcr.Tests.Images;

public sealed class ImageUploadEndpointTests
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _httpClient;

    public ImageUploadEndpointTests(
        WebApplicationFactory<Program> applicationFactory)
    {
        _httpClient = applicationFactory.CreateClient();
    }

    [Fact]
    public async Task ValidateImage_WithValidPng_ReturnsFileMetadata()
    {
        // Arrange
        byte[] fileContent = [137, 80, 78, 71];

        using var requestContent = CreateMultipartContent(
            fileContent,
            fileName: "sample.png",
            contentType: "image/png");

        // Act
        HttpResponseMessage response = await _httpClient.PostAsync(
            "/api/images/validate",
            requestContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ImageUploadResponse? uploadResponse =
            await response.Content.ReadFromJsonAsync<ImageUploadResponse>();

        Assert.NotNull(uploadResponse);
        Assert.Equal("sample.png", uploadResponse.FileName);
        Assert.Equal(".png", uploadResponse.Extension);
        Assert.Equal("image/png", uploadResponse.ContentType);
        Assert.Equal(fileContent.Length, uploadResponse.SizeBytes);
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
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        string responseBody = await response.Content.ReadAsStringAsync();

        Assert.Contains("empty_file", responseBody);
    }

    [Fact]
    public async Task ValidateImage_WithUnsupportedExtension_ReturnsBadRequest()
    {
        // Arrange
        using var requestContent = CreateMultipartContent(
            fileContent: [1, 2, 3, 4],
            fileName: "sample.txt",
            contentType: "image/png");

        // Act
        HttpResponseMessage response = await _httpClient.PostAsync(
            "/api/images/validate",
            requestContent);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        string responseBody = await response.Content.ReadAsStringAsync();

        Assert.Contains("unsupported_file_extension", responseBody);
    }

    [Fact]
    public async Task ValidateImage_WithUnsupportedContentType_ReturnsBadRequest()
    {
        // Arrange
        using var requestContent = CreateMultipartContent(
            fileContent: [1, 2, 3, 4],
            fileName: "sample.png",
            contentType: "text/plain");

        // Act
        HttpResponseMessage response = await _httpClient.PostAsync(
            "/api/images/validate",
            requestContent);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        string responseBody = await response.Content.ReadAsStringAsync();

        Assert.Contains("unsupported_content_type", responseBody);
    }

    [Fact]
    public async Task ValidateImage_RemovesPathFromReturnedFileName()
    {
        // Arrange
        using var requestContent = CreateMultipartContent(
            fileContent: [1, 2, 3, 4],
            fileName: @"C:\untrusted\sample.png",
            contentType: "image/png");

        // Act
        HttpResponseMessage response = await _httpClient.PostAsync(
            "/api/images/validate",
            requestContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ImageUploadResponse? uploadResponse =
            await response.Content.ReadFromJsonAsync<ImageUploadResponse>();

        Assert.NotNull(uploadResponse);
        Assert.Equal("sample.png", uploadResponse.FileName);
    }

    private static MultipartFormDataContent CreateMultipartContent(
        byte[] fileContent,
        string fileName,
        string contentType)
    {
        var multipartContent = new MultipartFormDataContent();

        var streamContent = new ByteArrayContent(fileContent);
        streamContent.Headers.ContentType =
            new MediaTypeHeaderValue(contentType);

        multipartContent.Add(
            streamContent,
            name: "file",
            fileName: fileName);

        return multipartContent;
    }
}