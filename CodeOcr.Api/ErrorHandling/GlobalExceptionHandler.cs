using CodeOcr.Api.Storage;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CodeOcr.Api.ErrorHandling;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        if (exception is OperationCanceledException &&
            httpContext.RequestAborted.IsCancellationRequested)
        {
            logger.LogDebug(
                "Request {TraceId} was cancelled by the client.",
                httpContext.TraceIdentifier);

            return true;
        }

        ExceptionMapping mapping =
            MapException(exception);

        logger.LogError(
            exception,
            "Request {Method} {Path} failed with " +
            "error code {ErrorCode}. Trace ID: {TraceId}.",
            httpContext.Request.Method,
            httpContext.Request.Path,
            mapping.ErrorCode,
            httpContext.TraceIdentifier);

        httpContext.Response.StatusCode =
            mapping.StatusCode;

        var problemDetails = new ProblemDetails
        {
            Status = mapping.StatusCode,
            Title = mapping.Title,
            Detail = mapping.Detail
        };

        problemDetails.Extensions["errorCode"] =
            mapping.ErrorCode;

        problemDetails.Extensions["traceId"] =
            httpContext.TraceIdentifier;

        IProblemDetailsService problemDetailsService =
            httpContext.RequestServices
                .GetRequiredService<IProblemDetailsService>();

        bool responseWritten =
            await problemDetailsService.TryWriteAsync(
                new ProblemDetailsContext
                {
                    HttpContext = httpContext,
                    Exception = exception,
                    ProblemDetails = problemDetails
                });

        if (responseWritten)
        {
            return true;
        }

        if (httpContext.Response.HasStarted)
        {
            return false;
        }

        httpContext.Response.ContentType =
            "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(
            problemDetails,
            cancellationToken);

        return true;
    }

    private static ExceptionMapping MapException(
        Exception exception)
    {
        return exception switch
        {
            ImageStorageException =>
                new ExceptionMapping(
                    StatusCode:
                        StatusCodes
                            .Status500InternalServerError,
                    Title: "Image storage failed.",
                    Detail:
                        "The uploaded image could not be stored.",
                    ErrorCode: "image_storage_failed"),

            _ =>
                new ExceptionMapping(
                    StatusCode:
                        StatusCodes
                            .Status500InternalServerError,
                    Title: "Unexpected server error.",
                    Detail:
                        "An unexpected error occurred.",
                    ErrorCode: "internal_server_error")
        };
    }

    private sealed record ExceptionMapping(
        int StatusCode,
        string Title,
        string Detail,
        string ErrorCode);
}