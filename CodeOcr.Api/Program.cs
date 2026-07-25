using CodeOcr.Api.Configuration;
using CodeOcr.Api.Contracts;
using CodeOcr.Api.Services;
using CodeOcr.Api.Validation;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOptions<ApplicationOptions>()
    .Bind(builder.Configuration.GetSection(ApplicationOptions.SectionName))
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.Name),
        "Application name must be configured.")
    .ValidateOnStart();

builder.Services
    .AddOptions<ImageUploadOptions>()
    .Bind(builder.Configuration.GetSection(ImageUploadOptions.SectionName))
    .Validate(
        options => options.MaximumFileSizeBytes > 0,
        "Maximum image file size must be greater than zero.")
    .Validate(
        options => options.AllowedExtensions.Length > 0,
        "At least one image extension must be configured.")
    .Validate(
        options => options.AllowedContentTypes.Length > 0,
        "At least one image content type must be configured.")
    .ValidateOnStart();

builder.Services.AddSingleton<IDiagnosticService, DiagnosticService>();
builder.Services.AddSingleton<IImageFileValidator, ImageFileValidator>();

var app = builder.Build();

app.MapGet(
        "/api/diagnostics",
        (IDiagnosticService diagnosticService) =>
        {
            DiagnosticResponse response = diagnosticService.GetStatus();

            return Results.Ok(response);
        })
    .WithName("GetDiagnostics");

app.MapPost(
        "/api/images/validate",
        async Task<IResult> (
            IFormFile file,
            IImageFileValidator validator,
            CancellationToken cancellationToken) =>
        {
            ImageFileValidationResult validationResult =
                await validator.ValidateAsync(
                    file,
                    cancellationToken);

            if (!validationResult.IsValid)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Image upload validation failed.",
                    detail: validationResult.ErrorMessage,
                    extensions: new Dictionary<string, object?>
                    {
                        ["errorCode"] = validationResult.ErrorCode
                    });
            }

            if (validationResult.DetectedFormat is not
                ImageFileFormat detectedFormat)
            {
                throw new InvalidOperationException(
                    "Successful image validation did not provide " +
                    "a detected format.");
            }

            string normalizedFileName =
                file.FileName.Replace('\\', '/');

            string safeFileName =
                Path.GetFileName(normalizedFileName);

            string extension =
                Path.GetExtension(safeFileName);

            var response = new ImageUploadResponse(
                FileName: safeFileName,
                Extension: extension,
                ContentType: file.ContentType,
                SizeBytes: file.Length,
                DetectedFormat: detectedFormat
                    .ToString()
                    .ToLowerInvariant());

            return Results.Ok(response);
        })
    .DisableAntiforgery()
    .WithName("ValidateImageUpload");

app.Run();

public partial class Program;