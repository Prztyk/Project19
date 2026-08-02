namespace CodeOcr.Api.Persistence.Entities;

public sealed class ImageOcrRecord
{
    public Guid Id { get; set; }

    public string OriginalFileName { get; set; } = string.Empty;

    public string StoredFileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public string DetectedFormat { get; set; } = string.Empty;

    public DateTime StoredAtUtc { get; set; }

    public string FullText { get; set; } = string.Empty;

    public long ProcessingTimeMs { get; set; }

    public List<OcrLineRecord> Lines { get; set; } = [];
}