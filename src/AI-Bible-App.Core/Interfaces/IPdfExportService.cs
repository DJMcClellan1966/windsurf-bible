using AI_Bible_App.Core.Models;

namespace AI_Bible_App.Core.Interfaces;

/// <summary>
/// Service for exporting content to various formats including PDF
/// </summary>
public interface IPdfExportService
{
    /// <summary>
    /// Export a chat session to PDF
    /// </summary>
    /// <param name="session">The chat session to export</param>
    /// <param name="characterName">Name of the biblical character</param>
    /// <returns>Path to the generated PDF file</returns>
    Task<string> ExportChatToPdfAsync(ChatSession session, string characterName);

    /// <summary>
    /// Export a devotional to PDF
    /// </summary>
    /// <param name="devotional">The devotional to export</param>
    /// <returns>Path to the generated PDF file</returns>
    Task<string> ExportDevotionalToPdfAsync(Devotional devotional);

    /// <summary>
    /// Export a prayer to PDF
    /// </summary>
    /// <param name="prayer">The prayer to export</param>
    /// <param name="characterName">Name of the biblical character</param>
    /// <returns>Path to the generated PDF file</returns>
    Task<string> ExportPrayerToPdfAsync(Prayer prayer, string characterName);

    /// <summary>
    /// Export multiple items to a combined PDF
    /// </summary>
    /// <param name="title">Title for the PDF document</param>
    /// <param name="sessions">Optional chat sessions to include</param>
    /// <param name="devotionals">Optional devotionals to include</param>
    /// <param name="prayers">Optional prayers to include</param>
    /// <returns>Path to the generated PDF file</returns>
    Task<string> ExportCombinedToPdfAsync(
        string title,
        IEnumerable<ChatSession>? sessions = null,
        IEnumerable<Devotional>? devotionals = null,
        IEnumerable<Prayer>? prayers = null);

    /// <summary>
    /// Get the default export directory
    /// </summary>
    string GetExportDirectory();

    /// <summary>
    /// Open the export directory in the file explorer
    /// </summary>
    Task OpenExportDirectoryAsync();
}
