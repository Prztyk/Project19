using System.Net;
using System.Net.Http.Json;
using CodeOcr.Api.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CodeOcr.Tests.Diagnostics;

public sealed class DiagnosticEndpointTests
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _httpClient;

    public DiagnosticEndpointTests(
        WebApplicationFactory<Program> applicationFactory)
    {
        _httpClient = applicationFactory.CreateClient();
    }

    [Fact]
    public async Task GetDiagnostics_ReturnsHealthyApplicationStatus()
    {
        // Act
        HttpResponseMessage response = await _httpClient.GetAsync(
            "/api/diagnostics");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        DiagnosticResponse? diagnostic =
            await response.Content.ReadFromJsonAsync<DiagnosticResponse>();

        Assert.NotNull(diagnostic);
        Assert.Equal("Code OCR", diagnostic.ApplicationName);
        Assert.Equal("Healthy", diagnostic.Status);
        Assert.False(string.IsNullOrWhiteSpace(diagnostic.Environment));
        Assert.NotEqual(default, diagnostic.TimestampUtc);
    }
}