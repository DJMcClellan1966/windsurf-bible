using Microsoft.Extensions.Logging;
using System.IO.Compression;
using System.Text.Json;

namespace AI_Bible_App.Infrastructure.Services;

/// <summary>
/// Optimizes Bible data loading for faster startup and lower memory usage.
/// Supports compressed storage and indexing for quick verse lookup.
/// </summary>
public class BibleDataOptimizer
{
    private readonly ILogger<BibleDataOptimizer> _logger;
    private readonly string _dataDirectory;
    private readonly string _cacheDirectory;

    public BibleDataOptimizer(ILogger<BibleDataOptimizer> logger, string? dataDirectory = null)
    {
        _logger = logger;
        _dataDirectory = dataDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AI-Bible-App",
            "data",
            "bible");
        _cacheDirectory = Path.Combine(_dataDirectory, ".cache");
        
        Directory.CreateDirectory(_cacheDirectory);
    }

    /// <summary>
    /// Compress JSON Bible files to reduce storage and improve load times.
    /// </summary>
    /// <param name="sourcePath">Path to original JSON file</param>
    /// <returns>Path to compressed file</returns>
    public async Task<string> CompressJsonFileAsync(string sourcePath)
    {
        var compressedPath = Path.Combine(_cacheDirectory, 
            Path.GetFileNameWithoutExtension(sourcePath) + ".json.gz");

        if (File.Exists(compressedPath))
        {
            var sourceInfo = new FileInfo(sourcePath);
            var cacheInfo = new FileInfo(compressedPath);
            
            if (cacheInfo.LastWriteTimeUtc >= sourceInfo.LastWriteTimeUtc)
            {
                _logger.LogDebug("Using cached compressed file: {Path}", compressedPath);
                return compressedPath;
            }
        }

        try
        {
            using var sourceStream = File.OpenRead(sourcePath);
            using var destStream = File.Create(compressedPath);
            using var gzipStream = new GZipStream(destStream, CompressionLevel.Optimal);
            
            await sourceStream.CopyToAsync(gzipStream);
            
            var originalSize = new FileInfo(sourcePath).Length;
            var compressedSize = new FileInfo(compressedPath).Length;
            var ratio = (1 - (double)compressedSize / originalSize) * 100;
            
            _logger.LogInformation(
                "Compressed {File}: {OriginalKB}KB → {CompressedKB}KB ({Ratio:F1}% reduction)",
                Path.GetFileName(sourcePath),
                originalSize / 1024,
                compressedSize / 1024,
                ratio);

            return compressedPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compress {Path}", sourcePath);
            throw;
        }
    }

    /// <summary>
    /// Decompress and load a compressed JSON file.
    /// </summary>
    public async Task<T?> LoadCompressedJsonAsync<T>(string compressedPath)
    {
        try
        {
            using var fileStream = File.OpenRead(compressedPath);
            using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
            using var reader = new StreamReader(gzipStream);
            
            var json = await reader.ReadToEndAsync();
            return JsonSerializer.Deserialize<T>(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load compressed JSON from {Path}", compressedPath);
            throw;
        }
    }

    /// <summary>
    /// Create an index for fast book/chapter lookup.
    /// </summary>
    public async Task<BibleIndex> CreateIndexAsync(string jsonFilePath)
    {
        var indexPath = Path.Combine(_cacheDirectory, 
            Path.GetFileNameWithoutExtension(jsonFilePath) + ".index.json");

        // Check if index already exists and is up to date
        if (File.Exists(indexPath))
        {
            var sourceInfo = new FileInfo(jsonFilePath);
            var indexInfo = new FileInfo(indexPath);
            
            if (indexInfo.LastWriteTimeUtc >= sourceInfo.LastWriteTimeUtc)
            {
                _logger.LogDebug("Loading existing index: {Path}", indexPath);
                var existingJson = await File.ReadAllTextAsync(indexPath);
                return JsonSerializer.Deserialize<BibleIndex>(existingJson) ?? new BibleIndex();
            }
        }

        _logger.LogInformation("Creating Bible index for {File}...", Path.GetFileName(jsonFilePath));
        
        var index = new BibleIndex();
        var json = await File.ReadAllTextAsync(jsonFilePath);
        
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        long position = 0;
        
        // Index structure depends on JSON format - this handles common formats
        if (root.ValueKind == JsonValueKind.Object)
        {
            foreach (var book in root.EnumerateObject())
            {
                var bookName = book.Name;
                var bookEntry = new BookIndexEntry { Name = bookName, StartPosition = position };
                
                if (book.Value.ValueKind == JsonValueKind.Object)
                {
                    foreach (var chapter in book.Value.EnumerateObject())
                    {
                        if (int.TryParse(chapter.Name, out var chapterNum))
                        {
                            bookEntry.ChapterOffsets[chapterNum] = position;
                            
                            // Count verses
                            if (chapter.Value.ValueKind == JsonValueKind.Array)
                            {
                                bookEntry.VerseCount += chapter.Value.GetArrayLength();
                            }
                        }
                        position += chapter.Value.GetRawText().Length;
                    }
                }
                
                index.Books[bookName] = bookEntry;
                position += book.Value.GetRawText().Length;
            }
        }

        index.TotalVerses = index.Books.Values.Sum(b => b.VerseCount);
        index.CreatedAt = DateTime.UtcNow;

        // Save index
        var indexJson = JsonSerializer.Serialize(index, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(indexPath, indexJson);
        
        _logger.LogInformation(
            "Index created: {Books} books, {Verses} total verses",
            index.Books.Count,
            index.TotalVerses);

        return index;
    }

    /// <summary>
    /// Optimize all Bible JSON files in the data directory.
    /// </summary>
    public async Task OptimizeAllAsync()
    {
        var jsonFiles = Directory.GetFiles(_dataDirectory, "*.json", SearchOption.TopDirectoryOnly);
        
        _logger.LogInformation("Optimizing {Count} Bible data files...", jsonFiles.Length);

        foreach (var file in jsonFiles)
        {
            try
            {
                // Create compressed version
                await CompressJsonFileAsync(file);
                
                // Create index
                await CreateIndexAsync(file);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to optimize {File}", file);
            }
        }

        _logger.LogInformation("Bible data optimization complete");
    }

    /// <summary>
    /// Get cache statistics.
    /// </summary>
    public CacheStatistics GetCacheStatistics()
    {
        var stats = new CacheStatistics();

        if (Directory.Exists(_cacheDirectory))
        {
            var gzFiles = Directory.GetFiles(_cacheDirectory, "*.gz");
            var indexFiles = Directory.GetFiles(_cacheDirectory, "*.index.json");

            stats.CompressedFiles = gzFiles.Length;
            stats.IndexFiles = indexFiles.Length;
            stats.TotalCacheSizeBytes = gzFiles.Sum(f => new FileInfo(f).Length) +
                                        indexFiles.Sum(f => new FileInfo(f).Length);
        }

        if (Directory.Exists(_dataDirectory))
        {
            var jsonFiles = Directory.GetFiles(_dataDirectory, "*.json", SearchOption.TopDirectoryOnly);
            stats.OriginalFiles = jsonFiles.Length;
            stats.TotalOriginalSizeBytes = jsonFiles.Sum(f => new FileInfo(f).Length);
        }

        stats.CompressionRatio = stats.TotalOriginalSizeBytes > 0
            ? 1 - ((double)stats.TotalCacheSizeBytes / stats.TotalOriginalSizeBytes)
            : 0;

        return stats;
    }

    /// <summary>
    /// Clear the cache directory.
    /// </summary>
    public void ClearCache()
    {
        if (Directory.Exists(_cacheDirectory))
        {
            Directory.Delete(_cacheDirectory, true);
            Directory.CreateDirectory(_cacheDirectory);
            _logger.LogInformation("Bible data cache cleared");
        }
    }
}

/// <summary>
/// Index structure for fast Bible verse lookup.
/// </summary>
public class BibleIndex
{
    public Dictionary<string, BookIndexEntry> Books { get; set; } = new();
    public int TotalVerses { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Index entry for a single Bible book.
/// </summary>
public class BookIndexEntry
{
    public string Name { get; set; } = "";
    public long StartPosition { get; set; }
    public Dictionary<int, long> ChapterOffsets { get; set; } = new();
    public int VerseCount { get; set; }
}

/// <summary>
/// Cache statistics for monitoring.
/// </summary>
public class CacheStatistics
{
    public int OriginalFiles { get; set; }
    public int CompressedFiles { get; set; }
    public int IndexFiles { get; set; }
    public long TotalOriginalSizeBytes { get; set; }
    public long TotalCacheSizeBytes { get; set; }
    public double CompressionRatio { get; set; }
    
    public string TotalOriginalSizeFormatted => FormatBytes(TotalOriginalSizeBytes);
    public string TotalCacheSizeFormatted => FormatBytes(TotalCacheSizeBytes);
    public string CompressionRatioFormatted => $"{CompressionRatio * 100:F1}%";

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        return $"{size:F1} {sizes[order]}";
    }
}
