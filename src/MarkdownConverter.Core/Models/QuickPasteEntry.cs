using System;

namespace MarkdownConverter.Models
{
    public class QuickPasteEntry
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Title { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastExportedAt { get; set; }
        public string? LastExportFormat { get; set; }
        public long FileSizeBytes { get; set; }
    }
}
