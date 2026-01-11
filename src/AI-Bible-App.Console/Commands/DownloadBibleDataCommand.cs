using AI_Bible_App.Infrastructure.Utilities;
using Microsoft.Extensions.Logging;

namespace AI_Bible_App.Console.Commands;

/// <summary>
/// Console command to download full Bible data
/// </summary>
public class DownloadBibleDataCommand
{
    private readonly ILogger<DownloadBibleDataCommand> _logger;

    public DownloadBibleDataCommand(ILogger<DownloadBibleDataCommand> logger)
    {
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        System.Console.WriteLine("=== Bible Data Downloader ===");
        System.Console.WriteLine("This will download full Bible text from public domain sources.");
        System.Console.WriteLine();

        var downloader = new BibleDataDownloader(LoggerFactory.Create(b => b.AddConsole()).CreateLogger<BibleDataDownloader>());

        // Determine output directory
        var baseDir = AppContext.BaseDirectory;
        var dataDir = Path.Combine(baseDir, "..", "..", "..", "..", "AI-Bible-App.Maui", "Data", "Bible");
        var fullDataDir = Path.GetFullPath(dataDir);

        System.Console.WriteLine($"Output directory: {fullDataDir}");
        System.Console.WriteLine();

        // Download WEB
        System.Console.WriteLine("Downloading World English Bible (WEB)...");
        try
        {
            var webVerses = await downloader.DownloadWebBibleAsync();
            var webPath = Path.Combine(fullDataDir, "web.json");
            await downloader.SaveToFileAsync(webVerses, webPath);
            System.Console.WriteLine($"✓ WEB Bible saved: {webVerses.Count} verses");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download WEB Bible");
            System.Console.WriteLine($"✗ WEB download failed: {ex.Message}");
        }

        System.Console.WriteLine();

        // Download KJV
        System.Console.WriteLine("Downloading King James Version (KJV)...");
        try
        {
            var kjvVerses = await downloader.DownloadKjvBibleAsync();
            var kjvPath = Path.Combine(fullDataDir, "kjv.json");
            await downloader.SaveToFileAsync(kjvVerses, kjvPath);
            System.Console.WriteLine($"✓ KJV Bible saved: {kjvVerses.Count} verses");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download KJV Bible");
            System.Console.WriteLine($"✗ KJV download failed: {ex.Message}");
        }

        System.Console.WriteLine();

        // Download additional resources
        var resourceDownloader = new BibleResourceDownloader(LoggerFactory.Create(b => b.AddConsole()).CreateLogger<BibleResourceDownloader>());
        
        // Download ASV
        System.Console.WriteLine("Downloading American Standard Version (ASV)...");
        try
        {
            var asvVerses = await resourceDownloader.DownloadAsvBibleAsync();
            var asvPath = Path.Combine(fullDataDir, "asv.json");
            await downloader.SaveToFileAsync(asvVerses, asvPath);
            System.Console.WriteLine($"✓ ASV Bible saved: {asvVerses.Count} verses");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download ASV Bible");
            System.Console.WriteLine($"✗ ASV download failed: {ex.Message}");
        }

        System.Console.WriteLine();

        // Download Matthew Henry Commentary excerpts
        System.Console.WriteLine("Downloading Matthew Henry Commentary excerpts...");
        try
        {
            var commentary = await resourceDownloader.GenerateMatthewHenryExcerptsAsync();
            var commentaryPath = Path.Combine(fullDataDir, "matthew_henry.json");
            await resourceDownloader.SaveCommentaryAsync(commentary, commentaryPath);
            System.Console.WriteLine($"✓ Matthew Henry Commentary saved: {commentary.Count} entries");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download commentary");
            System.Console.WriteLine($"✗ Commentary download failed: {ex.Message}");
        }

        System.Console.WriteLine();

        // Download Treasury of Scripture Knowledge cross-references
        System.Console.WriteLine("Downloading Treasury of Scripture Knowledge cross-references...");
        try
        {
            var crossRefs = await resourceDownloader.GenerateTskCrossReferencesAsync();
            var crossRefsPath = Path.Combine(fullDataDir, "tsk_crossrefs.json");
            await resourceDownloader.SaveCrossReferencesAsync(crossRefs, crossRefsPath);
            System.Console.WriteLine($"✓ TSK Cross-references saved: {crossRefs.Count} entries");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download cross-references");
            System.Console.WriteLine($"✗ Cross-references download failed: {ex.Message}");
        }

        System.Console.WriteLine();
        System.Console.WriteLine("Download complete! You can now run the MAUI app.");
        System.Console.WriteLine();
        System.Console.WriteLine("Bible sources available:");
        System.Console.WriteLine("  ✓ KJV - King James Version (Public Domain)");
        System.Console.WriteLine("  ✓ WEB - World English Bible (Public Domain)");
        System.Console.WriteLine("  ✓ ASV - American Standard Version (Public Domain)");
        System.Console.WriteLine("  ✓ Matthew Henry Commentary excerpts");
        System.Console.WriteLine("  ✓ Treasury of Scripture Knowledge cross-references");
    }
}
