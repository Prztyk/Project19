using CodeOcr.Api.Configuration;
using CodeOcr.Api.Contracts;
using CodeOcr.Api.Services;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOptions<ApplicationOptions>()
    .Bind(builder.Configuration.GetSection(ApplicationOptions.SectionName))
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.Name),
        "Application name must be configured.")
    .ValidateOnStart();

builder.Services.AddSingleton<IDiagnosticService, DiagnosticService>();

var app = builder.Build();

app.MapGet(
        "/api/diagnostics",
        (IDiagnosticService diagnosticService) =>
        {
            DiagnosticResponse response = diagnosticService.GetStatus();

            return Results.Ok(response);
        })
    .WithName("GetDiagnostics");

app.Run();

public partial class Program;