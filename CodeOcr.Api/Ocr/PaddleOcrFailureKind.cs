namespace CodeOcr.Api.Ocr;

public enum PaddleOcrFailureKind
{
    Unavailable,
    Timeout,
    ServiceError,
    InvalidResponse
}