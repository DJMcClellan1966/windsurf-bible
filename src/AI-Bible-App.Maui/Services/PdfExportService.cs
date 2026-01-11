using AI_Bible_App.Core.Interfaces;
using AI_Bible_App.Core.Models;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QContainer = QuestPDF.Infrastructure.IContainer;
using QPDFColors = QuestPDF.Helpers.Colors;

namespace AI_Bible_App.Maui.Services;

/// <summary>
/// Service for exporting content to PDF using QuestPDF
/// </summary>
public class PdfExportService : IPdfExportService
{
    private readonly ILogger<PdfExportService> _logger;
    private readonly string _exportPath;

    // Brand colors
    private static readonly string PrimaryColor = "#4A90D9";
    private static readonly string SecondaryColor = "#6B7280";
    private static readonly string AccentColor = "#10B981";

    public PdfExportService(ILogger<PdfExportService> logger)
    {
        _logger = logger;
        
        // Set up export directory
        var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        _exportPath = Path.Combine(documentsPath, "Voices of Scripture", "Exports");
        
        // Ensure directory exists
        if (!Directory.Exists(_exportPath))
        {
            Directory.CreateDirectory(_exportPath);
        }

        // Configure QuestPDF license (Community license is free)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public string GetExportDirectory() => _exportPath;

    public async Task OpenExportDirectoryAsync()
    {
        try
        {
#if WINDOWS
            System.Diagnostics.Process.Start("explorer.exe", _exportPath);
#else
            await Launcher.OpenAsync(new OpenFileRequest
            {
                File = new ReadOnlyFile(_exportPath)
            });
#endif
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open export directory");
        }
        await Task.CompletedTask;
    }

    public async Task<string> ExportChatToPdfAsync(ChatSession session, string characterName)
    {
        var fileName = $"Chat_{characterName}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
        var filePath = Path.Combine(_exportPath, fileName);

        try
        {
            _logger.LogInformation("Exporting chat to PDF: {Path}", filePath);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.PageColor(QPDFColors.White);
                    page.DefaultTextStyle(x => x.FontSize(11).FontColor(QPDFColors.Black));

                    page.Header().Element(c => CreateHeader(c, $"Conversation with {characterName}"));

                    page.Content().Element(c => CreateChatContent(c, session, characterName));

                    page.Footer().Element(CreateFooter);
                });
            });

            document.GeneratePdf(filePath);
            _logger.LogInformation("Chat exported successfully to: {Path}", filePath);
            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export chat to PDF");
            throw;
        }
    }

    public async Task<string> ExportDevotionalToPdfAsync(Devotional devotional)
    {
        var sanitizedTitle = string.Join("_", devotional.Title.Split(Path.GetInvalidFileNameChars()));
        var fileName = $"Devotional_{sanitizedTitle}_{DateTime.Now:yyyyMMdd}.pdf";
        var filePath = Path.Combine(_exportPath, fileName);

        try
        {
            _logger.LogInformation("Exporting devotional to PDF: {Path}", filePath);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.PageColor(QPDFColors.White);
                    page.DefaultTextStyle(x => x.FontSize(11).FontColor(QPDFColors.Black));

                    page.Header().Element(c => CreateHeader(c, "Daily Devotional"));

                    page.Content().Element(c => CreateDevotionalContent(c, devotional));

                    page.Footer().Element(CreateFooter);
                });
            });

            document.GeneratePdf(filePath);
            _logger.LogInformation("Devotional exported successfully to: {Path}", filePath);
            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export devotional to PDF");
            throw;
        }
    }

    public async Task<string> ExportPrayerToPdfAsync(Prayer prayer, string characterName)
    {
        var fileName = $"Prayer_{characterName}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
        var filePath = Path.Combine(_exportPath, fileName);

        try
        {
            _logger.LogInformation("Exporting prayer to PDF: {Path}", filePath);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.PageColor(QPDFColors.White);
                    page.DefaultTextStyle(x => x.FontSize(11).FontColor(QPDFColors.Black));

                    page.Header().Element(c => CreateHeader(c, "Prayer"));

                    page.Content().Element(c => CreatePrayerContent(c, prayer, characterName));

                    page.Footer().Element(CreateFooter);
                });
            });

            document.GeneratePdf(filePath);
            _logger.LogInformation("Prayer exported successfully to: {Path}", filePath);
            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export prayer to PDF");
            throw;
        }
    }

    public async Task<string> ExportCombinedToPdfAsync(
        string title,
        IEnumerable<ChatSession>? sessions = null,
        IEnumerable<Devotional>? devotionals = null,
        IEnumerable<Prayer>? prayers = null)
    {
        var sanitizedTitle = string.Join("_", title.Split(Path.GetInvalidFileNameChars()));
        var fileName = $"{sanitizedTitle}_{DateTime.Now:yyyyMMdd}.pdf";
        var filePath = Path.Combine(_exportPath, fileName);

        try
        {
            _logger.LogInformation("Exporting combined content to PDF: {Path}", filePath);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.PageColor(QPDFColors.White);
                    page.DefaultTextStyle(x => x.FontSize(11).FontColor(QPDFColors.Black));

                    page.Header().Element(c => CreateHeader(c, title));

                    page.Content().Column(column =>
                    {
                        // Export devotionals
                        if (devotionals?.Any() == true)
                        {
                            column.Item().Element(c => CreateSectionHeader(c, "📖 Devotionals"));
                            foreach (var devotional in devotionals)
                            {
                                column.Item().Element(c => CreateDevotionalContent(c, devotional));
                                column.Item().PageBreak();
                            }
                        }

                        // Export prayers
                        if (prayers?.Any() == true)
                        {
                            column.Item().Element(c => CreateSectionHeader(c, "🙏 Prayers"));
                            foreach (var prayer in prayers)
                            {
                                var characterName = prayer.UserId ?? "Unknown";
                                column.Item().Element(c => CreatePrayerContent(c, prayer, characterName));
                                column.Item().PaddingVertical(20);
                            }
                        }

                        // Export chat sessions
                        if (sessions?.Any() == true)
                        {
                            column.Item().Element(c => CreateSectionHeader(c, "💬 Conversations"));
                            foreach (var session in sessions)
                            {
                                column.Item().Element(c => CreateChatContent(c, session, session.CharacterId ?? "Unknown"));
                                column.Item().PageBreak();
                            }
                        }
                    });

                    page.Footer().Element(CreateFooter);
                });
            });

            document.GeneratePdf(filePath);
            _logger.LogInformation("Combined content exported successfully to: {Path}", filePath);
            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export combined content to PDF");
            throw;
        }
    }

    #region PDF Layout Components

    private void CreateHeader(QContainer container, string title)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item()
                    .Text("✝️ Voices of Scripture")
                    .FontSize(12)
                    .FontColor(QPDFColors.Grey.Medium);

                column.Item()
                    .Text(title)
                    .FontSize(22)
                    .Bold()
                    .FontColor(QuestPDF.Infrastructure.Color.FromHex(PrimaryColor));
            });

            row.ConstantItem(100).AlignRight().Text(text =>
            {
                text.Span(DateTime.Now.ToString("MMMM d, yyyy"))
                    .FontSize(10)
                    .FontColor(QPDFColors.Grey.Medium);
            });
        });

        container.PaddingBottom(15);
    }

    private void CreateSectionHeader(QContainer container, string title)
    {
        container.PaddingVertical(10).Row(row =>
        {
            row.RelativeItem()
                .BorderBottom(2)
                .BorderColor(QuestPDF.Infrastructure.Color.FromHex(PrimaryColor))
                .PaddingBottom(5)
                .Text(title)
                .FontSize(18)
                .Bold()
                .FontColor(QuestPDF.Infrastructure.Color.FromHex(PrimaryColor));
        });
    }

    private void CreateFooter(QContainer container)
    {
        container.AlignCenter().Text(text =>
        {
            text.DefaultTextStyle(x => x.FontSize(9).FontColor(QPDFColors.Grey.Medium));
            text.Span("Generated by Voices of Scripture • ");
            text.CurrentPageNumber();
            text.Span(" of ");
            text.TotalPages();
        });
    }

    private void CreateChatContent(QContainer container, ChatSession session, string characterName)
    {
        container.Column(column =>
        {
            // Session info
            column.Item().PaddingBottom(10).Text($"Conversation started: {session.StartedAt:MMMM d, yyyy h:mm tt}")
                .FontSize(10)
                .Italic()
                .FontColor(QPDFColors.Grey.Darken1);

            // Messages
            foreach (var message in session.Messages)
            {
                column.Item().Element(c => CreateMessageBubble(c, message, characterName));
            }
        });
    }

    private void CreateMessageBubble(QContainer container, ChatMessage message, string characterName)
    {
        var isUser = message.Role?.ToLower() == "user";
        var bubbleColor = isUser ? QPDFColors.Blue.Lighten4 : QPDFColors.Grey.Lighten3;
        var senderName = isUser ? "You" : characterName;

        container.PaddingVertical(5).Row(row =>
        {
            if (isUser) row.ConstantItem(40); // Indent user messages

            row.RelativeItem()
                .Background(bubbleColor)
                .Padding(12)
                .Column(col =>
                {
                    col.Item().Text(senderName)
                        .FontSize(10)
                        .Bold()
                        .FontColor(isUser ? QuestPDF.Infrastructure.Color.FromHex(PrimaryColor) : QuestPDF.Infrastructure.Color.FromHex(AccentColor));

                    col.Item().PaddingTop(5).Text(message.Content ?? "")
                        .FontSize(11)
                        .LineHeight(1.4f);

                    col.Item().AlignRight().Text(message.Timestamp.ToString("h:mm tt"))
                        .FontSize(8)
                        .FontColor(QPDFColors.Grey.Medium);
                });

            if (!isUser) row.ConstantItem(40); // Indent character messages
        });
    }

    private void CreateDevotionalContent(QContainer container, Devotional devotional)
    {
        container.Column(column =>
        {
            // Title
            column.Item().PaddingBottom(10)
                .Text(devotional.Title)
                .FontSize(18)
                .Bold()
                .FontColor(QuestPDF.Infrastructure.Color.FromHex(PrimaryColor));

            // Date
            column.Item().PaddingBottom(15)
                .Text(devotional.Date.ToString("MMMM d, yyyy"))
                .FontSize(10)
                .Italic()
                .FontColor(QPDFColors.Grey.Darken1);

            // Category if present
            if (!string.IsNullOrEmpty(devotional.Category))
            {
                column.Item().PaddingBottom(10)
                    .Text(devotional.Category)
                    .FontSize(10)
                    .Bold()
                    .FontColor(QuestPDF.Infrastructure.Color.FromHex(AccentColor));
            }

            // Scripture reference
            if (!string.IsNullOrEmpty(devotional.ScriptureReference))
            {
                column.Item()
                    .Background(QPDFColors.Blue.Lighten5)
                    .Padding(15)
                    .Column(scriptureCol =>
                    {
                        scriptureCol.Item().Text("📖 Scripture")
                            .FontSize(12)
                            .Bold()
                            .FontColor(QuestPDF.Infrastructure.Color.FromHex(PrimaryColor));

                        scriptureCol.Item().PaddingTop(5).Text(devotional.ScriptureReference)
                            .FontSize(11)
                            .Bold();

                        if (!string.IsNullOrEmpty(devotional.Scripture))
                        {
                            scriptureCol.Item().PaddingTop(8)
                                .Text($"\"{devotional.Scripture}\"")
                                .FontSize(11)
                                .Italic()
                                .LineHeight(1.4f);
                        }
                    });
            }

            // Content (main devotional text)
            if (!string.IsNullOrEmpty(devotional.Content))
            {
                column.Item().PaddingTop(15)
                    .Text("💭 Devotional")
                    .FontSize(12)
                    .Bold()
                    .FontColor(QuestPDF.Infrastructure.Color.FromHex(SecondaryColor));

                column.Item().PaddingTop(5)
                    .Text(devotional.Content)
                    .FontSize(11)
                    .LineHeight(1.5f);
            }

            // Prayer
            if (!string.IsNullOrEmpty(devotional.Prayer))
            {
                column.Item().PaddingTop(15)
                    .Text("🙏 Prayer")
                    .FontSize(12)
                    .Bold()
                    .FontColor(QuestPDF.Infrastructure.Color.FromHex(AccentColor));

                column.Item().PaddingTop(5)
                    .Background(QPDFColors.Green.Lighten5)
                    .Padding(12)
                    .Text(devotional.Prayer)
                    .FontSize(11)
                    .Italic()
                    .LineHeight(1.5f);
            }
        });
    }

    private void CreatePrayerContent(QContainer container, Prayer prayer, string characterName)
    {
        container.Background(QPDFColors.Blue.Lighten5).Padding(20).Column(column =>
        {
            // Character attribution
            column.Item().PaddingBottom(10)
                .Text($"A prayer in the voice of {characterName}")
                .FontSize(12)
                .Italic()
                .FontColor(QuestPDF.Infrastructure.Color.FromHex(SecondaryColor));

            // Date
            column.Item().PaddingBottom(15)
                .Text(prayer.CreatedAt.ToString("MMMM d, yyyy"))
                .FontSize(10)
                .FontColor(QPDFColors.Grey.Darken1);

            // Topic
            if (!string.IsNullOrEmpty(prayer.Topic))
            {
                column.Item().PaddingBottom(10)
                    .Text($"Topic: {prayer.Topic}")
                    .FontSize(11)
                    .Bold();
            }

            // Tags if present
            if (prayer.Tags != null && prayer.Tags.Any())
            {
                column.Item().PaddingBottom(10)
                    .Text($"Tags: {string.Join(", ", prayer.Tags)}")
                    .FontSize(10)
                    .FontColor(QuestPDF.Infrastructure.Color.FromHex(PrimaryColor));
            }

            // Prayer content
            column.Item()
                .Text(prayer.Content ?? "")
                .FontSize(11)
                .Italic()
                .LineHeight(1.6f);
        });
    }

    #endregion
}
