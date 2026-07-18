using CodeOcr.Api.Configuration;
using CodeOcr.Api.Contracts;
using Microsoft.Extensions.Options;

namespace CodeOcr.Api.Services;

public sealed class DiagnosticService(
    IOptions<ApplicationOptions> applicationOptions,
    IHostEnvironment hostEnvironment,
    ILogger<DiagnosticService> logger) : IDiagnosticService
{
    private readonly ApplicationOptions _applicationOptions = applicationOptions.Value;

    public DiagnosticResponse GetStatus()
    {
        logger.LogDebug(
            "Creating a diagnostic response for application {ApplicationName}.",
            _applicationOptions.Name);

        return new DiagnosticResponse(
            ApplicationName: _applicationOptions.Name,
            Status: "Healthy",
            TimestampUtc: DateTimeOffset.UtcNow,
            Environment: hostEnvironment.EnvironmentName);
    }
}