using CodeOcr.Api.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace CodeOcr.Api.Persistence;

public sealed class EfImageOcrRepository(
    CodeOcrDbContext dbContext,
    ILogger<EfImageOcrRepository> logger)
    : IImageOcrRepository
{
    public async Task AddAsync(ImageOcrRecord record, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(record);

        dbContext.ImageOcrRecords.Add(record);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Persisted OCR result for image {ImageId} with {LineCount} lines.",
                record.Id,
                record.Lines.Count);
        }
        catch (DbUpdateException exception)
        {
            throw new ImageOcrPersistenceException(
                "The OCR result could not be saved to the database.",
                exception);
        }
    }
}