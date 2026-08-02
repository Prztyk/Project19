namespace CodeOcr.Api.Persistence;

public sealed class ImageOcrPersistenceException : Exception
{
    public ImageOcrPersistenceException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}