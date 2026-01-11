using System.Text.RegularExpressions;
using AI_Bible_App.Maui.Services;

namespace AI_Bible_App.Maui.Controls;

/// <summary>
/// A Label that automatically converts Bible references to clickable links
/// that display the passage in-app with optional AI summary
/// </summary>
public class BibleLinkLabel : Label
{
    // Bible reference pattern: matches "Book Chapter:Verse" or "Book Chapter:Verse-Verse"
    // Examples: John 3:16, 1 Corinthians 13:4-7, Psalm 23:1-6, Genesis 1:1
    private static readonly Regex BibleRefRegex = new Regex(
        @"\b((?:1|2|3|I|II|III)?\s*[A-Z][a-z]+(?:\s+[A-Z][a-z]+)?)\s+(\d{1,3}):(\d{1,3})(?:-(\d{1,3}))?\b",
        RegexOptions.Compiled);

    public static readonly BindableProperty LinkedTextProperty =
        BindableProperty.Create(
            nameof(LinkedText),
            typeof(string),
            typeof(BibleLinkLabel),
            string.Empty,
            propertyChanged: OnLinkedTextChanged);

    public string LinkedText
    {
        get => (string)GetValue(LinkedTextProperty);
        set => SetValue(LinkedTextProperty, value);
    }

    private static void OnLinkedTextChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is BibleLinkLabel label && newValue is string text)
        {
            label.UpdateFormattedText(text);
        }
    }

    private void UpdateFormattedText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            FormattedText = null;
            return;
        }

        var formattedString = new FormattedString();
        int lastIndex = 0;

        foreach (Match match in BibleRefRegex.Matches(text))
        {
            // Add text before the match
            if (match.Index > lastIndex)
            {
                formattedString.Spans.Add(new Span
                {
                    Text = text.Substring(lastIndex, match.Index - lastIndex)
                });
            }

            // Add the Bible reference as a clickable link
            var reference = match.Value;
            var span = new Span
            {
                Text = reference,
                TextColor = Application.Current?.RequestedTheme == AppTheme.Dark 
                    ? Colors.LightBlue 
                    : Colors.Blue,
                TextDecorations = TextDecorations.Underline
            };

            // Create tap gesture for in-app display
            var tapGesture = new TapGestureRecognizer();
            var book = match.Groups[1].Value.Trim();
            var chapter = int.Parse(match.Groups[2].Value);
            var verseStart = int.Parse(match.Groups[3].Value);
            var verseEnd = match.Groups[4].Success ? int.Parse(match.Groups[4].Value) : (int?)null;

            tapGesture.Tapped += async (s, e) =>
            {
                await ShowPassagePopupAsync(book, chapter, verseStart, verseEnd, reference);
            };
            span.GestureRecognizers.Add(tapGesture);

            formattedString.Spans.Add(span);
            lastIndex = match.Index + match.Length;
        }

        // Add remaining text after last match
        if (lastIndex < text.Length)
        {
            formattedString.Spans.Add(new Span
            {
                Text = text.Substring(lastIndex)
            });
        }

        FormattedText = formattedString;
    }

    private async Task ShowPassagePopupAsync(string book, int chapter, int verseStart, int? verseEnd, string reference)
    {
        try
        {
            var lookupService = Application.Current?.Handler?.MauiContext?.Services.GetService<IBibleLookupService>();
            
            if (lookupService == null)
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] BibleLinkLabel: lookupService is null, falling back to browser");
                // Fallback to browser if service not available
                await OpenInBrowserAsync(book, chapter, verseStart, verseEnd);
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[DEBUG] BibleLinkLabel: Looking up {book} {chapter}:{verseStart}");
            var result = await lookupService.LookupPassageAsync(book, chapter, verseStart, verseEnd);
            System.Diagnostics.Debug.WriteLine($"[DEBUG] BibleLinkLabel: Lookup result Found={result.Found}, Text length={result.Text?.Length ?? 0}");

            if (result.Found && Shell.Current?.CurrentPage != null)
            {
                // Show verse directly in-app (user preference)
                await Shell.Current.CurrentPage.DisplayAlert(
                    $"📖 {result.Reference} ({result.Translation})",
                    result.Text,
                    "Close");
            }
            else
            {
                // Passage not found locally - offer to open in browser
                System.Diagnostics.Debug.WriteLine($"[DEBUG] BibleLinkLabel: Passage not found locally for '{reference}'");
                if (Shell.Current?.CurrentPage != null)
                {
                    var openBrowser = await Shell.Current.CurrentPage.DisplayAlert(
                        "Passage Not Found",
                        $"'{reference}' wasn't found in local Bible data.\n\nWould you like to view it on Bible.com?",
                        "Open Browser",
                        "Cancel");

                    if (openBrowser)
                    {
                        await OpenInBrowserAsync(book, chapter, verseStart, verseEnd);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] BibleLinkLabel: Error showing passage: {ex.Message}");
            // Fallback to browser
            await OpenInBrowserAsync(book, chapter, verseStart, verseEnd);
        }
    }

    private static async Task OpenInBrowserAsync(string book, int chapter, int verseStart, int? verseEnd)
    {
        try
        {
            var url = GetBibleGatewayUrl(book, chapter, verseStart, verseEnd);
            await Launcher.OpenAsync(new Uri(url));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to open Bible link: {ex.Message}");
        }
    }

    private static string GetBibleGatewayUrl(string book, int chapter, int verseStart, int? verseEnd)
    {
        // Bible.com (YouVersion) URL format - free access, no subscription required
        // Uses the World English Bible (WEB) translation which is public domain
        // Format: https://www.bible.com/bible/206/GEN.1.1.WEB
        
        var bookCode = GetBibleComBookCode(book);
        var verseRef = verseEnd.HasValue && verseEnd != verseStart
            ? $"{chapter}.{verseStart}-{verseEnd}"
            : $"{chapter}.{verseStart}";

        // 206 = World English Bible (WEB) - public domain
        return $"https://www.bible.com/bible/206/{bookCode}.{verseRef}.WEB";
    }

    private static string GetBibleComBookCode(string book)
    {
        // Bible.com uses 3-letter book codes
        var bookCodes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Old Testament
            { "Genesis", "GEN" }, { "Gen", "GEN" },
            { "Exodus", "EXO" }, { "Exod", "EXO" }, { "Ex", "EXO" },
            { "Leviticus", "LEV" }, { "Lev", "LEV" },
            { "Numbers", "NUM" }, { "Num", "NUM" },
            { "Deuteronomy", "DEU" }, { "Deut", "DEU" },
            { "Joshua", "JOS" }, { "Josh", "JOS" },
            { "Judges", "JDG" }, { "Judg", "JDG" },
            { "Ruth", "RUT" },
            { "1 Samuel", "1SA" }, { "1Samuel", "1SA" }, { "1 Sam", "1SA" },
            { "2 Samuel", "2SA" }, { "2Samuel", "2SA" }, { "2 Sam", "2SA" },
            { "1 Kings", "1KI" }, { "1Kings", "1KI" },
            { "2 Kings", "2KI" }, { "2Kings", "2KI" },
            { "1 Chronicles", "1CH" }, { "1Chronicles", "1CH" },
            { "2 Chronicles", "2CH" }, { "2Chronicles", "2CH" },
            { "Ezra", "EZR" },
            { "Nehemiah", "NEH" }, { "Neh", "NEH" },
            { "Esther", "EST" }, { "Est", "EST" },
            { "Job", "JOB" },
            { "Psalms", "PSA" }, { "Psalm", "PSA" }, { "Ps", "PSA" },
            { "Proverbs", "PRO" }, { "Prov", "PRO" },
            { "Ecclesiastes", "ECC" }, { "Eccl", "ECC" },
            { "Song of Solomon", "SNG" }, { "Song", "SNG" },
            { "Isaiah", "ISA" }, { "Isa", "ISA" },
            { "Jeremiah", "JER" }, { "Jer", "JER" },
            { "Lamentations", "LAM" }, { "Lam", "LAM" },
            { "Ezekiel", "EZK" }, { "Ezek", "EZK" },
            { "Daniel", "DAN" }, { "Dan", "DAN" },
            { "Hosea", "HOS" }, { "Hos", "HOS" },
            { "Joel", "JOL" },
            { "Amos", "AMO" },
            { "Obadiah", "OBA" }, { "Obad", "OBA" },
            { "Jonah", "JON" },
            { "Micah", "MIC" }, { "Mic", "MIC" },
            { "Nahum", "NAM" }, { "Nah", "NAM" },
            { "Habakkuk", "HAB" }, { "Hab", "HAB" },
            { "Zephaniah", "ZEP" }, { "Zeph", "ZEP" },
            { "Haggai", "HAG" }, { "Hag", "HAG" },
            { "Zechariah", "ZEC" }, { "Zech", "ZEC" },
            { "Malachi", "MAL" }, { "Mal", "MAL" },
            // New Testament
            { "Matthew", "MAT" }, { "Matt", "MAT" }, { "Mt", "MAT" },
            { "Mark", "MRK" }, { "Mk", "MRK" },
            { "Luke", "LUK" }, { "Lk", "LUK" },
            { "John", "JHN" }, { "Jn", "JHN" },
            { "Acts", "ACT" },
            { "Romans", "ROM" }, { "Rom", "ROM" },
            { "1 Corinthians", "1CO" }, { "1Corinthians", "1CO" }, { "1 Cor", "1CO" },
            { "2 Corinthians", "2CO" }, { "2Corinthians", "2CO" }, { "2 Cor", "2CO" },
            { "Galatians", "GAL" }, { "Gal", "GAL" },
            { "Ephesians", "EPH" }, { "Eph", "EPH" },
            { "Philippians", "PHP" }, { "Phil", "PHP" },
            { "Colossians", "COL" }, { "Col", "COL" },
            { "1 Thessalonians", "1TH" }, { "1Thessalonians", "1TH" },
            { "2 Thessalonians", "2TH" }, { "2Thessalonians", "2TH" },
            { "1 Timothy", "1TI" }, { "1Timothy", "1TI" },
            { "2 Timothy", "2TI" }, { "2Timothy", "2TI" },
            { "Titus", "TIT" },
            { "Philemon", "PHM" }, { "Phlm", "PHM" },
            { "Hebrews", "HEB" }, { "Heb", "HEB" },
            { "James", "JAS" }, { "Jas", "JAS" },
            { "1 Peter", "1PE" }, { "1Peter", "1PE" },
            { "2 Peter", "2PE" }, { "2Peter", "2PE" },
            { "1 John", "1JN" }, { "1John", "1JN" },
            { "2 John", "2JN" }, { "2John", "2JN" },
            { "3 John", "3JN" }, { "3John", "3JN" },
            { "Jude", "JUD" },
            { "Revelation", "REV" }, { "Rev", "REV" }
        };

        return bookCodes.TryGetValue(book, out var code) ? code : book.ToUpperInvariant().Substring(0, Math.Min(3, book.Length));
    }
}
