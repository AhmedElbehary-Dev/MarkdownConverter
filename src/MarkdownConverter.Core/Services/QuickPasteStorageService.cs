using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using MarkdownConverter.Models;

namespace MarkdownConverter.Services
{
    public class QuickPasteStorageService
    {
        private readonly string _storageDir;
        private readonly string _indexPath;
        private List<QuickPasteEntry> _cache;

        public QuickPasteStorageService()
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _storageDir = Path.Combine(localAppData, "MarkdownConverterPro", "QuickPaste");
            _indexPath = Path.Combine(_storageDir, "index.json");
            _cache = new List<QuickPasteEntry>();
        }

        public async Task<List<QuickPasteEntry>> LoadHistoryAsync()
        {
            if (!Directory.Exists(_storageDir))
            {
                Directory.CreateDirectory(_storageDir);
                return new List<QuickPasteEntry>();
            }

            if (!File.Exists(_indexPath))
            {
                return new List<QuickPasteEntry>();
            }

            try
            {
                var json = await File.ReadAllTextAsync(_indexPath);
                _cache = JsonSerializer.Deserialize<List<QuickPasteEntry>>(json) ?? new List<QuickPasteEntry>();
                return _cache;
            }
            catch
            {
                return new List<QuickPasteEntry>();
            }
        }

        public async Task<QuickPasteEntry> SaveAsync(string title, string markdownContent)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                title = ExtractTitle(markdownContent);
            }

            var safeTitle = string.Join("_", title.Split(Path.GetInvalidFileNameChars()));
            if (string.IsNullOrWhiteSpace(safeTitle)) safeTitle = "Untitled";

            var fileName = $"{safeTitle}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.md";
            var filePath = Path.Join(_storageDir, fileName);

            if (!Directory.Exists(_storageDir)) Directory.CreateDirectory(_storageDir);

            await File.WriteAllTextAsync(filePath, markdownContent);
            var fileInfo = new FileInfo(filePath);

            var entry = new QuickPasteEntry
            {
                Id = Guid.NewGuid().ToString("N"),
                Title = title,
                FileName = fileName,
                CreatedAt = DateTime.UtcNow,
                FileSizeBytes = fileInfo.Length
            };

            await LoadHistoryAsync();
            _cache.Insert(0, entry);
            await SaveIndexAsync();

            return entry;
        }

        public async Task UpdateAsync(string id, string title, string markdownContent)
        {
            var entry = _cache.FirstOrDefault(e => e.Id == id);
            if (entry == null) return;

            entry.Title = title;
            var filePath = Path.Join(_storageDir, entry.FileName);
            
            await File.WriteAllTextAsync(filePath, markdownContent);
            entry.FileSizeBytes = new FileInfo(filePath).Length;
            
            await SaveIndexAsync();
        }

        public async Task MarkExportedAsync(string id, string format)
        {
            var entry = _cache.FirstOrDefault(e => e.Id == id);
            if (entry == null) return;

            entry.LastExportedAt = DateTime.UtcNow;
            entry.LastExportFormat = format;
            
            await SaveIndexAsync();
        }

        public async Task<string?> LoadContentAsync(string id)
        {
            var entry = _cache.FirstOrDefault(e => e.Id == id);
            if (entry == null) return null;

            var filePath = Path.Join(_storageDir, entry.FileName);
            if (File.Exists(filePath))
            {
                return await File.ReadAllTextAsync(filePath);
            }
            return null;
        }

        public async Task DeleteAsync(string id)
        {
            var entry = _cache.FirstOrDefault(e => e.Id == id);
            if (entry == null) return;

            var filePath = Path.Join(_storageDir, entry.FileName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            _cache.Remove(entry);
            await SaveIndexAsync();
        }

        private async Task SaveIndexAsync()
        {
            var json = JsonSerializer.Serialize(_cache, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_indexPath, json);
        }

        public string ExtractTitle(string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return "Untitled";

            var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var title = lines.FirstOrDefault(l => l.StartsWith("# "))?.Substring(2).Trim() 
                      ?? lines.FirstOrDefault(l => !string.IsNullOrWhiteSpace(l))?.Trim() 
                      ?? "Untitled";

            if (title.Length > 50) title = title.Substring(0, 50) + "...";
            return title;
        }
    }
}
