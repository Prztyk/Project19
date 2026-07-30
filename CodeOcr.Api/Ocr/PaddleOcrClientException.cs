using System.Net;

namespace CodeOcr.Api.Ocr;

public sealed class PaddleOcrClientException : Exception
{
    private PaddleOcrClientException(
        string message,
        PaddleOcrFailureKind failureKind,
        HttpStatusCode? statusCode,
        Exception? innerException)
        : base(message, innerException)
    {
        FailureKind = failureKind;
        StatusCode = statusCode;
    }

    public PaddleOcrFailureKind FailureKind { get; }

    public HttpStatusCode? StatusCode { get; }

    public static PaddleOcrClientException Unavailable(Exception innerException)
    {
        ArgumentNullException.ThrowIfNull(innerException);

        return new PaddleOcrClientException(
            message: "The local PaddleOCR service is unavailable.",
            failureKind: PaddleOcrFailureKind.Unavailable,
            statusCode: null,
            innerException: innerException);
    }

    public static PaddleOcrClientException Timeout(Exception innerException)
    {
        ArgumentNullException.ThrowIfNull(innerException);

        return new PaddleOcrClientException(
            message: "The local PaddleOCR request timed out.",
            failureKind: PaddleOcrFailureKind.Timeout,
            statusCode: null,
            innerException: innerException);
    }

    public static PaddleOcrClientException ServiceError(HttpStatusCode statusCode)
    {
        return new PaddleOcrClientException(
            message: $"The local PaddleOCR service returned HTTP status {(int)statusCode}.",
            failureKind: PaddleOcrFailureKind.ServiceError,
            statusCode: statusCode,
            innerException: null);
    }

    public static PaddleOcrClientException InvalidResponse(
        string message,
        Exception? innerException = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        return new PaddleOcrClientException(
            message: message,
            failureKind: PaddleOcrFailureKind.InvalidResponse,
            statusCode: null,
            innerException: innerException);
    }
}