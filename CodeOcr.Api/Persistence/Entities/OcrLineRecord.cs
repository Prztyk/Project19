namespace CodeOcr.Api.Persistence.Entities;

public sealed class OcrLineRecord
{
    public long Id { get; set; }

    public Guid ImageOcrRecordId { get; set; }

    public int SequenceNumber { get; set; }

    public string Text { get; set; } = string.Empty;

    public double? Confidence { get; set; }

    public ImageOcrRecord ImageOcrRecord { get; set; } = null!;
}