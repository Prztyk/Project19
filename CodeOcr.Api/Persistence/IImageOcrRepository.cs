using CodeOcr.Api.Persistence.Entities;

namespace CodeOcr.Api.Persistence;

public interface IImageOcrRepository
{
    Task AddAsync(ImageOcrRecord record, CancellationToken cancellationToken);
}