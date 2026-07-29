namespace CodeOcr.Api.Storage;

public sealed class ImageStorageException : Exception
{
    public ImageStorageException(
        string message,
        Exception innerException)
        : base(message, innerException)
    {
    }
}