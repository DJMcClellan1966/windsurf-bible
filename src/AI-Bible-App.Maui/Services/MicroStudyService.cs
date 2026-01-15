using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AI_Bible_App.Core.Interfaces;
using AI_Bible_App.Core.Models;

namespace AI_Bible_App.Maui.Services;

public class MicroStudyService : IMicroStudyService
{
    private readonly IReadingPlanRepository _readingPlanRepository;
    private readonly ICharacterRepository _characterRepository;
    private readonly IBibleLookupService _bibleLookupService;
    private readonly IAIService _aiService;

    public MicroStudyService(
        IReadingPlanRepository readingPlanRepository,
        ICharacterRepository characterRepository,
        IBibleLookupService bibleLookupService,
        IAIService aiService)
    {
        _readingPlanRepository = readingPlanRepository;
        _characterRepository = characterRepository;
        _bibleLookupService = bibleLookupService;
        _aiService = aiService;
    }

    public async Task<MicroStudySession> BuildSessionAsync(
        string planId,
        int dayNumber,
        bool multiVoiceEnabled,
        CancellationToken cancellationToken = default)
    {
        var plan = await _readingPlanRepository.GetPlanByIdAsync(planId, cancellationToken);
        if (plan == null)
            throw new ArgumentException($"Plan '{planId}' not found");

        var day = plan.Days.FirstOrDefault(d => d.DayNumber == dayNumber);
        if (day == null)
            throw new ArgumentException($"Day {dayNumber} not found for plan '{planId}'");

        var primaryGuideId = plan.GuideCharacterId ?? "scholar";
        var guide = await GetGuideCharacterAsync(primaryGuideId);

        var excerptReference = day.Passages.FirstOrDefault() ?? string.Empty;
        var excerptText = await BuildExcerptTextAsync(excerptReference, cancellationToken);

        var prompt = BuildMicroStudyPrompt(day, excerptReference, excerptText);
        var response = await _aiService.GetChatResponseAsync(guide, new List<ChatMessage>(), prompt, cancellationToken);

        var parsed = TryParseMicroStudyJson(response) ?? new MicroStudyJson
        {
            Claim = "State the main claim of the excerpt in one sentence.",
            Questions = new List<string>
            {
                "Which word or phrase is doing the most work here, and what does it likely mean in context?",
                "Which verse in the excerpt most strongly supports your reading?",
                "What alternative reading is plausible, and what would it change?"
            }
        };

        return new MicroStudySession
        {
            PlanId = plan.Id,
            DayNumber = day.DayNumber,
            DayTitle = day.Title,
            Passages = day.Passages.ToList(),
            ExcerptReference = excerptReference,
            ExcerptText = excerptText,
            Claim = parsed.Claim ?? string.Empty,
            Questions = (parsed.Questions ?? new List<string>()).Where(q => !string.IsNullOrWhiteSpace(q))
                .Take(3)
                .Select(q => new MicroStudyQuestion { Question = q.Trim() })
                .ToList(),
            MultiVoiceEnabled = multiVoiceEnabled,
            PrimaryGuideCharacterId = guide.Id
        };
    }

    public async Task<SocraticCritique> CritiqueAnswerAsync(
        string planId,
        int dayNumber,
        string question,
        string userAnswer,
        bool multiVoiceEnabled,
        CancellationToken cancellationToken = default)
    {
        var plan = await _readingPlanRepository.GetPlanByIdAsync(planId, cancellationToken);
        if (plan == null)
            throw new ArgumentException($"Plan '{planId}' not found");

        var day = plan.Days.FirstOrDefault(d => d.DayNumber == dayNumber);
        if (day == null)
            throw new ArgumentException($"Day {dayNumber} not found for plan '{planId}'");

        var primaryGuideId = plan.GuideCharacterId ?? "scholar";
        var guide = await GetGuideCharacterAsync(primaryGuideId);

        var reference = day.Passages.FirstOrDefault() ?? string.Empty;
        var excerptText = await BuildExcerptTextAsync(reference, cancellationToken);

        var prompt = BuildCritiquePrompt(day, reference, excerptText, question, userAnswer);
        var response = await _aiService.GetChatResponseAsync(guide, new List<ChatMessage>(), prompt, cancellationToken);

        var critique = TryParseCritiqueJson(response);
        if (critique != null)
            return critique;

        return new SocraticCritique
        {
            Feedback = response?.Trim() ?? string.Empty,
            VerseReferences = new List<string>()
        };
    }

    private async Task<BiblicalCharacter> GetGuideCharacterAsync(string guideId)
    {
        if (string.Equals(guideId, "scholar", StringComparison.OrdinalIgnoreCase))
        {
            return new BiblicalCharacter
            {
                Id = "scholar",
                Name = "Bible Scholar",
                Title = "Guide",
                Description = "A helpful guide who explains background and meaning",
                SystemPrompt = "You are a helpful Bible scholar. Be accurate, cite the text, and keep responses structured."
            };
        }

        return await _characterRepository.GetCharacterAsync(guideId)
               ?? new BiblicalCharacter
               {
                   Id = guideId,
                   Name = guideId,
                   Title = "Guide",
                   Description = "A guided Bible study voice",
                   SystemPrompt = "You are a helpful guide through Scripture. Keep responses structured and concise."
               };
    }

    private async Task<string> BuildExcerptTextAsync(string reference, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reference))
            return string.Empty;

        var trimmed = reference.Trim();

        // Verse range (single chapter)
        var verseMatch = Regex.Match(trimmed, @"^(?<book>.+?)\s+(?<chapter>\d+):(?<v1>\d+)(?:-(?<v2>\d+))?$", RegexOptions.IgnoreCase);
        if (verseMatch.Success)
        {
            var book = verseMatch.Groups["book"].Value.Trim();
            var chapter = int.Parse(verseMatch.Groups["chapter"].Value);
            var v1 = int.Parse(verseMatch.Groups["v1"].Value);
            var v2 = verseMatch.Groups["v2"].Success ? int.Parse(verseMatch.Groups["v2"].Value) : (int?)null;

            var res = await _bibleLookupService.LookupPassageAsync(book, chapter, v1, v2);
            return FormatVerses(res);
        }

        // Chapter or chapter range -> excerpt first 12 verses of first chapter
        var chapterMatch = Regex.Match(trimmed, @"^(?<book>.+?)\s+(?<c1>\d+)(?:-(?<c2>\d+))?$", RegexOptions.IgnoreCase);
        if (chapterMatch.Success)
        {
            var book = chapterMatch.Groups["book"].Value.Trim();
            var chapter = int.Parse(chapterMatch.Groups["c1"].Value);
            var res = await _bibleLookupService.LookupPassageAsync(book, chapter, 1, 12);
            return FormatVerses(res);
        }

        return string.Empty;
    }

    private static string FormatVerses(BibleLookupResult res)
    {
        if (!res.Found)
            return "(Passage text not found locally.)";

        if (res.Verses != null && res.Verses.Count > 0)
        {
            var sb = new StringBuilder();
            foreach (var v in res.Verses.OrderBy(v => v.Verse))
            {
                if (!string.IsNullOrWhiteSpace(v.Text))
                    sb.AppendLine($"{v.Verse} {v.Text}");
            }
            return sb.ToString().Trim();
        }

        return res.Text?.Trim() ?? string.Empty;
    }

    private static string BuildMicroStudyPrompt(ReadingPlanDay day, string reference, string excerptText)
    {
        return $@"Create a 3-minute micro-study for this reading.

Day title: {day.Title}
Passage: {reference}

Excerpt:
{excerptText}

Return ONLY valid JSON like:
{{"claim":"...","questions":["...","...","..."]}}

Rules:
- claim: one sentence, academically cautious, tied to the excerpt.
- questions: 1-3 Socratic questions that force clarity and citing the excerpt.
- Avoid preaching tone; focus on text and meaning.
";
    }

    private static string BuildCritiquePrompt(ReadingPlanDay day, string reference, string excerptText, string question, string userAnswer)
    {
        return $@"You are helping a user study Scripture rigorously.

Day title: {day.Title}
Passage: {reference}

Excerpt:
{excerptText}

Question:
{question}

User answer:
{userAnswer}

Return ONLY valid JSON like:
{{"feedback":"...","verse_references":["Romans 3:21","Romans 3:22"]}}

Rules:
- feedback: 3-6 sentences. Be constructive, point out what is supported, what is overreach, and what is missing.
- verse_references: cite ONLY verses from the excerpt when possible.
- If the answer is not supported by the excerpt, say so.
";
    }

    private static MicroStudyJson? TryParseMicroStudyJson(string response)
    {
        try
        {
            var json = ExtractJsonObject(response);
            if (json == null)
                return null;

            return JsonSerializer.Deserialize<MicroStudyJson>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return null;
        }
    }

    private static SocraticCritique? TryParseCritiqueJson(string response)
    {
        try
        {
            var json = ExtractJsonObject(response);
            if (json == null)
                return null;

            var dto = JsonSerializer.Deserialize<CritiqueJson>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (dto == null)
                return null;

            return new SocraticCritique
            {
                Feedback = dto.Feedback ?? string.Empty,
                VerseReferences = dto.VerseReferences?.Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v.Trim()).ToList() ?? new List<string>()
            };
        }
        catch
        {
            return null;
        }
    }

    private static string? ExtractJsonObject(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start < 0 || end <= start)
            return null;

        return text.Substring(start, end - start + 1);
    }

    private sealed class MicroStudyJson
    {
        public string? Claim { get; set; }
        public List<string>? Questions { get; set; }
    }

    private sealed class CritiqueJson
    {
        public string? Feedback { get; set; }
        public List<string>? VerseReferences { get; set; }
    }
}
