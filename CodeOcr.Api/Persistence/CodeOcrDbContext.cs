using CodeOcr.Api.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace CodeOcr.Api.Persistence;

public sealed class CodeOcrDbContext(DbContextOptions<CodeOcrDbContext> options)
    : DbContext(options)
{
    public DbSet<ImageOcrRecord> ImageOcrRecords => Set<ImageOcrRecord>();

    public DbSet<OcrLineRecord> OcrLineRecords => Set<OcrLineRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureImageOcrRecord(modelBuilder);
        ConfigureOcrLineRecord(modelBuilder);
    }

    private static void ConfigureImageOcrRecord(ModelBuilder modelBuilder)
    {
        var image = modelBuilder.Entity<ImageOcrRecord>();

        image.ToTable("Images");

        image.HasKey(record => record.Id);

        image.Property(record => record.Id).ValueGeneratedNever();

        image.Property(record => record.OriginalFileName)
            .HasMaxLength(255)
            .IsRequired();

        image.Property(record => record.StoredFileName)
            .HasMaxLength(100)
            .IsRequired();

        image.Property(record => record.ContentType)
            .HasMaxLength(100)
            .IsRequired();

        image.Property(record => record.DetectedFormat)
            .HasMaxLength(20)
            .IsRequired();

        image.Property(record => record.StoredAtUtc).IsRequired();

        image.Property(record => record.FullText).IsRequired();

        image.Property(record => record.ProcessingTimeMs).IsRequired();

        image.HasIndex(record => record.StoredFileName).IsUnique();

        image.HasMany(record => record.Lines)
            .WithOne(line => line.ImageOcrRecord)
            .HasForeignKey(line => line.ImageOcrRecordId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureOcrLineRecord(ModelBuilder modelBuilder)
    {
        var line = modelBuilder.Entity<OcrLineRecord>();

        line.ToTable("OcrLines");

        line.HasKey(record => record.Id);

        line.Property(record => record.Text).IsRequired();

        line.Property(record => record.SequenceNumber).IsRequired();

        line.HasIndex(record => new
        {
            record.ImageOcrRecordId,
            record.SequenceNumber
        }).IsUnique();
    }
}