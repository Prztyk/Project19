using CodeOcr.Api.Configuration;
using CodeOcr.Api.Contracts;
using CodeOcr.Api.ErrorHandling;
using CodeOcr.Api.Ocr;
using CodeOcr.Api.Persistence;
using CodeOcr.Api.Services;
using CodeOcr.Api.Storage;
using CodeOcr.Api.Validation;
using CodeOcr.Api.Workflows;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        if (!context.ProblemDetails.Extensions.ContainsKey(
                "traceId"))
        {
            context.ProblemDetails.Extensions["traceId"] =
                context.HttpContext.TraceIdentifier;
        }

        if (!context.ProblemDetails.Extensions.ContainsKey(
                "errorCode"))
        {
            int statusCode =
                context.ProblemDetails.Status ??
                context.HttpContext.Response.StatusCode;

            context.ProblemDetails.Extensions["errorCode"] =
                statusCode switch
                {
                    StatusCodes.Status404NotFound =>
                        "resource_not_found",

                    StatusCodes.Status405MethodNotAllowed =>
                        "method_not_allowed",

                    >= StatusCodes
                        .Status500InternalServerError =>
                        "internal_server_error",

                    _ => "request_failed"
                };
        }
    };
});

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

builder.Services
    .AddOptions<ImageStorageOptions>()
    .Bind(builder.Configuration.GetSection(ImageStorageOptions.SectionName))
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.DirectoryPath),
        "The image storage directory must be configured.")
    .ValidateOnStart();

builder.Services
    .AddOptions<DatabaseOptions>()
    .Bind(builder.Configuration.GetSection(DatabaseOptions.SectionName))
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.FilePath),
        "The database file path must be configured.")
    .ValidateOnStart();

builder.Services
    .AddOptions<PaddleOcrOptions>()
    .Bind(builder.Configuration.GetSection(PaddleOcrOptions.SectionName))
    .Validate(
        options => IsValidHttpBaseUrl(options.BaseUrl),
        "The PaddleOCR base URL must be an absolute HTTP or HTTPS URL.")
    .Validate(
        options => IsValidRelativePath(options.RecognizePath),
        "The PaddleOCR recognize path must be a non-empty relative URL without a leading slash.")
    .Validate(
        options => options.TimeoutSeconds is > 0 and <= 300,
        "The PaddleOCR timeout must be between 1 and 300 seconds.")
    .ValidateOnStart();

builder.Services.AddDbContext<CodeOcrDbContext>((serviceProvider, options) =>
{
    DatabaseOptions databaseOptions = serviceProvider
        .GetRequiredService<IOptions<DatabaseOptions>>()
        .Value;

    IHostEnvironment environment = serviceProvider.GetRequiredService<IHostEnvironment>();

    string databasePath = ResolveDatabaseFilePath(
        environment.ContentRootPath,
        databaseOptions.FilePath);

    string? databaseDirectory = Path.GetDirectoryName(databasePath);

    if (string.IsNullOrWhiteSpace(databaseDirectory))
    {
        throw new InvalidOperationException(
            "The configured database file path does not contain a directory.");
    }

    Directory.CreateDirectory(databaseDirectory);

    string connectionString = new SqliteConnectionStringBuilder
    {
        DataSource = databasePath
    }.ToString();

    options.UseSqlite(connectionString);
});

builder.Services.AddScoped<IImageOcrRepository, EfImageOcrRepository>();

builder.Services.AddScoped<IImageOcrWorkflow, ImageOcrWorkflow>();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddSingleton<IDiagnosticService, DiagnosticService>();

builder.Services.AddSingleton<IImageFileValidator, ImageFileValidator>();

builder.Services.AddSingleton<IImageFileStorage, LocalImageFileStorage>();

builder.Services.AddHttpClient<IPaddleOcrClient, PaddleOcrClient>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

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
            HttpContext httpContext,
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
                return CreateValidationProblem(
                    httpContext,
                    validationResult);
            }

            ImageFileFormat detectedFormat =
                GetDetectedFormat(validationResult);

            string safeFileName =
                GetSafeFileName(file.FileName);

            var response = new ImageUploadResponse(
                    FileName: safeFileName,
                    Extension: Path.GetExtension(safeFileName),
                    ContentType: file.ContentType,
                    SizeBytes: file.Length,
                    DetectedFormat: ToApiFormat(detectedFormat));

            return Results.Ok(response);
        })
    .DisableAntiforgery()
    .WithName("ValidateImageUpload");

app.MapPost(
        "/api/images",
        async Task<IResult> (
            HttpContext httpContext,
            IFormFile file,
            IImageFileValidator validator,
            IImageFileStorage imageFileStorage,
            CancellationToken cancellationToken) =>
        {
            ImageFileValidationResult validationResult =
                await validator.ValidateAsync(
                    file,
                    cancellationToken);

            if (!validationResult.IsValid)
            {
                return CreateValidationProblem(
                    httpContext,
                    validationResult);
            }

            ImageFileFormat detectedFormat =
                GetDetectedFormat(validationResult);

            StoredImageFile storedImage =
                await imageFileStorage.SaveAsync(
                    file,
                    detectedFormat,
                    cancellationToken);

            var response = new StoredImageResponse(
                    ImageId: storedImage.Id,
                    OriginalFileName: GetSafeFileName(file.FileName),
                    StoredFileName: storedImage.StoredFileName,
                    ContentType: file.ContentType,
                    SizeBytes: storedImage.SizeBytes,
                    DetectedFormat: ToApiFormat(detectedFormat),
                    StoredAtUtc: storedImage.StoredAtUtc);

            return Results.Ok(response);
        })
    .DisableAntiforgery()
    .WithName("StoreImage");

app.MapPost(
        "/api/ocr/recognize",
        async Task<IResult> (
            HttpContext httpContext,
            IFormFile file,
            IImageFileValidator validator,
            IImageOcrWorkflow workflow,
            CancellationToken cancellationToken) =>
        {
            ImageFileValidationResult validationResult =
                await validator.ValidateAsync(file, cancellationToken);

            if (!validationResult.IsValid)
            {
                return CreateValidationProblem(httpContext, validationResult);
            }

            ImageFileFormat detectedFormat = GetDetectedFormat(validationResult);
            string safeFileName = GetSafeFileName(file.FileName);

            ImageOcrResponse response = await workflow.RecognizeAsync(
                file,
                detectedFormat,
                safeFileName,
                cancellationToken);

            return Results.Ok(response);
        })
    .DisableAntiforgery()
    .WithName("RecognizeImage");

app.Run();

static IResult CreateValidationProblem(
    HttpContext httpContext,
    ImageFileValidationResult validationResult)
{
    return Results.Problem(
        statusCode: StatusCodes.Status400BadRequest,
        title: "Image upload validation failed.",
        detail: validationResult.ErrorMessage,
        extensions: new Dictionary<string, object?>
            {
                ["errorCode"] = validationResult.ErrorCode,
                ["traceId"] = httpContext.TraceIdentifier
            });
}

static ImageFileFormat GetDetectedFormat(
    ImageFileValidationResult validationResult)
{
    if (validationResult.DetectedFormat is
        ImageFileFormat detectedFormat)
    {
        return detectedFormat;
    }

    throw new InvalidOperationException(
        "Successful image validation did not provide a detected format.");
}

static string GetSafeFileName(string fileName)
{
    string normalizedFileName =
        fileName.Replace('\\', '/');

    return Path.GetFileName(normalizedFileName);
}

static string ToApiFormat(
    ImageFileFormat detectedFormat)
{
    return detectedFormat
        .ToString()
        .ToLowerInvariant();
}

static bool IsValidHttpBaseUrl(
    string baseUrl)
{
    return Uri.TryCreate(
               baseUrl,
               UriKind.Absolute,
               out Uri? uri) &&
           (uri.Scheme == Uri.UriSchemeHttp ||
            uri.Scheme == Uri.UriSchemeHttps);
}

static bool IsValidRelativePath(
    string relativePath)
{
    return
        !string.IsNullOrWhiteSpace(relativePath) &&
        !relativePath.StartsWith("/", StringComparison.Ordinal) &&
        !Uri.IsWellFormedUriString(
            relativePath,
            UriKind.Absolute);
}

static string ResolveDatabaseFilePath(string contentRootPath, string configuredFilePath)
{
    if (Path.IsPathRooted(configuredFilePath))
    {
        return Path.GetFullPath(configuredFilePath);
    }

    return Path.GetFullPath(Path.Combine(contentRootPath, configuredFilePath));
}

public partial class Program;