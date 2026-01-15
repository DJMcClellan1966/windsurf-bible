using System.Text;
using System.Text.RegularExpressions;
using AI_Bible_App.Core.Interfaces;
using AI_Bible_App.Core.Models;
using AI_Bible_App.Maui.Services;

namespace AI_Bible_App.Maui.Services;

public class GuidedStudyService : IGuidedStudyService
{
    private readonly IReadingPlanRepository _readingPlanRepository;
    private readonly ICharacterRepository _characterRepository;
    private readonly IBibleLookupService _bibleLookupService;
    private readonly IAIService _aiService;

    public GuidedStudyService(
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

    public async Task<GuidedStudySession> BuildSessionAsync(
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

        var session = new GuidedStudySession
        {
            PlanId = plan.Id,
            DayNumber = day.DayNumber,
            DayTitle = day.Title,
            Passages = day.Passages.ToList(),
            MultiVoiceEnabled = multiVoiceEnabled,
            PrimaryGuideCharacterId = plan.GuideCharacterId ?? string.Empty,
            AdditionalGuideCharacterIds = plan.AdditionalGuideCharacterIds.ToList()
        };

        session.PassageText = await BuildPassageTextAsync(day.Passages, cancellationToken);

        // Always include passage as first step
        session.Steps.Add(new GuidedStudyStep
        {
            Type = GuidedStudyStepType.Passage,
            Title = string.Join(", ", day.Passages),
            Content = session.PassageText
        });

        var guideIds = new List<string>();
        if (!string.IsNullOrWhiteSpace(plan.GuideCharacterId))
            guideIds.Add(plan.GuideCharacterId);

        if (multiVoiceEnabled && plan.AdditionalGuideCharacterIds.Count > 0)
            guideIds.AddRange(plan.AdditionalGuideCharacterIds.Where(id => !string.IsNullOrWhiteSpace(id)));

        if (guideIds.Count == 0)
        {
            // Fallback to a neutral scholar if no guide was set
            guideIds.Add("scholar");
        }

        foreach (var guideId in guideIds.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var guide = await GetGuideCharacterAsync(guideId, cancellationToken);
            var guideSteps = await GenerateGuideStepsAsync(guide, day, session.PassageText, cancellationToken);
            session.Steps.AddRange(guideSteps);
        }

        return session;
    }

    private async Task<BiblicalCharacter> GetGuideCharacterAsync(string guideId, CancellationToken cancellationToken)
    {
        if (string.Equals(guideId, "scholar", StringComparison.OrdinalIgnoreCase))
        {
            return new BiblicalCharacter
            {
                Id = "scholar",
                Name = "Bible Scholar",
                Title = "Guide",
                Description = "A helpful guide who explains background, structure, and key insights",
                SystemPrompt = "You are a helpful Bible scholar. Be accurate, cite the text, and keep the study guide structured and practical."
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

    private async Task<List<GuidedStudyStep>> GenerateGuideStepsAsync(
        BiblicalCharacter guide,
        ReadingPlanDay day,
        string passageText,
        CancellationToken cancellationToken)
    {
        var steps = new List<GuidedStudyStep>();

        var prompt = BuildGuidePrompt(guide, day, passageText);
        var response = await _aiService.GetChatResponseAsync(guide, new List<ChatMessage>(), prompt, cancellationToken);

        if (string.IsNullOrWhiteSpace(response))
            return steps;

        // Very lightweight parsing: split into sections by headings.
        // If the model doesn't comply, fall back to a single Insights block.
        var sections = SplitSections(response);

        if (sections.Count == 0)
        {
            steps.Add(new GuidedStudyStep
            {
                Type = GuidedStudyStepType.Insights,
                Title = $"{guide.Name}: Insights",
                Content = response.Trim(),
                CharacterId = guide.Id,
                CharacterName = guide.Name
            });
            return steps;
        }

        foreach (var (title, content) in sections)
        {
            steps.Add(new GuidedStudyStep
            {
                Type = MapTitleToStepType(title),
                Title = $"{guide.Name}: {title}",
                Content = content.Trim(),
                CharacterId = guide.Id,
                CharacterName = guide.Name
            });
        }

        return steps;
    }

    private static string BuildGuidePrompt(BiblicalCharacter guide, ReadingPlanDay day, string passageText)
    {
        return $@"You are {guide.Name}. Create a guided walkthrough for this Bible reading.

Day title: {day.Title}
Passages: {string.Join(", ", day.Passages)}

Passage text:
{passageText}

Write the walkthrough with EXACT section headings (each heading on its own line):
BACKGROUND:
OUTLINE:
KEY INSIGHTS:
QUESTIONS:
APPLICATION:

Rules:
- Keep each section concise (3-7 bullet points or short paragraphs).
- Use only what can reasonably be inferred from the text and basic historical/literary context.
- If you are unsure of a historical detail, say so.
";
    }

    private async Task<string> BuildPassageTextAsync(IEnumerable<string> references, CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();

        foreach (var reference in references)
        {
            var chunks = await LookupReferenceAsync(reference, cancellationToken);
            if (chunks.Count == 0)
            {
                sb.AppendLine(reference);
                sb.AppendLine("(Passage text not found locally.)");
                sb.AppendLine();
                continue;
            }

            sb.AppendLine(reference);
            foreach (var chunk in chunks)
            {
                if (!chunk.Found)
                    continue;

                if (chunk.Verses != null && chunk.Verses.Count > 0)
                {
                    foreach (var verse in chunk.Verses.OrderBy(v => v.Verse))
                    {
                        if (!string.IsNullOrWhiteSpace(verse.Text))
                            sb.AppendLine($"{verse.Verse} {verse.Text}");
                    }
                    sb.AppendLine();
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(chunk.Text))
                {
                    sb.AppendLine(chunk.Text);
                }
            }
            sb.AppendLine();
        }

        return sb.ToString().Trim();
    }

    private async Task<List<BibleLookupResult>> LookupReferenceAsync(string reference, CancellationToken cancellationToken)
    {
        // Supports:
        // - "Romans 3"
        // - "Genesis 1-4" (chapter ranges)
        // - "John 3:16-18" (single chapter verse range)
        var results = new List<BibleLookupResult>();
        var trimmed = reference.Trim();

        // Verse range
        var verseMatch = Regex.Match(trimmed, @"^(?<book>.+?)\s+(?<chapter>\d+):(?<v1>\d+)(?:-(?<v2>\d+))?$", RegexOptions.IgnoreCase);
        if (verseMatch.Success)
        {
            var book = verseMatch.Groups["book"].Value.Trim();
            var chapter = int.Parse(verseMatch.Groups["chapter"].Value);
            var v1 = int.Parse(verseMatch.Groups["v1"].Value);
            var v2 = verseMatch.Groups["v2"].Success ? int.Parse(verseMatch.Groups["v2"].Value) : (int?)null;
            results.Add(await _bibleLookupService.LookupPassageAsync(book, chapter, v1, v2));
            return results;
        }

        // Chapter or chapter range
        var chapterMatch = Regex.Match(trimmed, @"^(?<book>.+?)\s+(?<c1>\d+)(?:-(?<c2>\d+))?$", RegexOptions.IgnoreCase);
        if (chapterMatch.Success)
        {
            var book = chapterMatch.Groups["book"].Value.Trim();
            var c1 = int.Parse(chapterMatch.Groups["c1"].Value);
            var c2 = chapterMatch.Groups["c2"].Success ? int.Parse(chapterMatch.Groups["c2"].Value) : c1;

            if (c2 < c1)
                (c1, c2) = (c2, c1);

            for (int chapter = c1; chapter <= c2; chapter++)
            {
                // Use generous max verse number; Lookup will return what exists.
                results.Add(await _bibleLookupService.LookupPassageAsync(book, chapter, 1, 200));
            }
        }

        return results;
    }

    private static List<(string Title, string Content)> SplitSections(string response)
    {
        var titles = new[]
        {
            "BACKGROUND:",
            "OUTLINE:",
            "KEY INSIGHTS:",
            "QUESTIONS:",
            "APPLICATION:"
        };

        var index = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var t in titles)
        {
            var i = response.IndexOf(t, StringComparison.OrdinalIgnoreCase);
            if (i >= 0)
                index[t] = i;
        }

        if (index.Count == 0)
            return new List<(string, string)>();

        var ordered = index.OrderBy(kv => kv.Value).ToList();
        var sections = new List<(string, string)>();

        for (int i = 0; i < ordered.Count; i++)
        {
            var title = ordered[i].Key.TrimEnd(':');
            var start = ordered[i].Value + ordered[i].Key.Length;
            var end = i + 1 < ordered.Count ? ordered[i + 1].Value : response.Length;
            var content = response.Substring(start, end - start);
            sections.Add((title, content));
        }

        return sections;
    }

    private static GuidedStudyStepType MapTitleToStepType(string title)
    {
        return title.Trim().ToUpperInvariant() switch
        {
            "BACKGROUND" => GuidedStudyStepType.Background,
            "OUTLINE" => GuidedStudyStepType.Outline,
            "KEY INSIGHTS" => GuidedStudyStepType.Insights,
            "QUESTIONS" => GuidedStudyStepType.Questions,
            "APPLICATION" => GuidedStudyStepType.Application,
            _ => GuidedStudyStepType.Insights
        };
    }
}
