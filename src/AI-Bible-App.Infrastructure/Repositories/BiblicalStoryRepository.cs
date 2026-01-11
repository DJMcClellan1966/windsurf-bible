using AI_Bible_App.Core.Models;

namespace AI_Bible_App.Infrastructure.Repositories;

/// <summary>
/// Repository for biblical stories - currently using in-memory data
/// In future, could load from JSON file or database
/// </summary>
public class BiblicalStoryRepository
{
    private readonly List<BiblicalStory> _stories;

    public BiblicalStoryRepository()
    {
        _stories = InitializeStories();
    }

    public Task<IEnumerable<BiblicalStory>> GetAllAsync()
    {
        return Task.FromResult<IEnumerable<BiblicalStory>>(_stories);
    }

    public Task<BiblicalStory?> GetByIdAsync(string id)
    {
        return Task.FromResult(_stories.FirstOrDefault(s => s.Id == id));
    }

    public Task<IEnumerable<BiblicalStory>> GetByCharacterIdAsync(string characterId)
    {
        return Task.FromResult<IEnumerable<BiblicalStory>>(
            _stories.Where(s => s.CharacterId == characterId)
        );
    }

    public Task<IEnumerable<BiblicalStory>> GetByThemeAsync(string theme)
    {
        return Task.FromResult<IEnumerable<BiblicalStory>>(
            _stories.Where(s => s.Themes.Contains(theme, StringComparer.OrdinalIgnoreCase))
        );
    }

    private List<BiblicalStory> InitializeStories()
    {
        return new List<BiblicalStory>
        {
            new BiblicalStory
            {
                Id = "peter-storm",
                Title = "Walking on Water with Peter",
                Description = "Experience the storm on the Sea of Galilee and witness Jesus walking on water through Peter's eyes",
                Reference = "Matthew 14:22-33",
                CharacterId = "peter",
                DifficultyLevel = "beginner",
                EstimatedMinutes = 15,
                Themes = new List<string> { "Faith", "Fear", "Trust", "Miracles" },
                Scenes = new List<StoryScene>
                {
                    new StoryScene
                    {
                        Order = 1,
                        Title = "The Evening Crossing",
                        Setting = "Sea of Galilee, evening",
                        Narrative = "Jesus sent us ahead across the lake while He went up the mountain to pray. The wind started picking up as we pushed off shore...",
                        SuggestedQuestions = new List<string>
                        {
                            "What was it like sailing without Jesus?",
                            "Were you worried about the weather?",
                            "Did you expect what happened next?"
                        },
                        KeyMoment = "The storm begins"
                    },
                    new StoryScene
                    {
                        Order = 2,
                        Title = "The Fourth Watch",
                        Setting = "Sea of Galilee, deep night, stormy",
                        Narrative = "We'd been fighting the wind for hours. Then, in the darkness, we saw something... someone... walking toward us on the water.",
                        SuggestedQuestions = new List<string>
                        {
                            "What did you think when you first saw Him?",
                            "Why did you think it was a ghost?",
                            "When did you recognize it was Jesus?"
                        },
                        KeyMoment = "Jesus walks on water"
                    },
                    new StoryScene
                    {
                        Order = 3,
                        Title = "Come",
                        Setting = "On the water",
                        Narrative = "I heard myself say 'Lord, if it's you, tell me to come to you on the water.' And He said one word: 'Come.'",
                        SuggestedQuestions = new List<string>
                        {
                            "Why did you ask to walk on water?",
                            "What gave you the courage?",
                            "What did it feel like taking that first step?"
                        },
                        KeyMoment = "Peter's moment of faith"
                    },
                    new StoryScene
                    {
                        Order = 4,
                        Title = "Sinking",
                        Setting = "On the water, sinking",
                        Narrative = "For a few glorious moments, I was doing it! Then I saw the waves... felt the wind... and started to sink.",
                        SuggestedQuestions = new List<string>
                        {
                            "What made you doubt?",
                            "What went through your mind as you sank?",
                            "What did you learn from this?"
                        },
                        KeyMoment = "The lesson about faith and doubt"
                    }
                }
            },
            new BiblicalStory
            {
                Id = "david-goliath",
                Title = "Facing Goliath with David",
                Description = "Stand with young David as he faces the giant Goliath in the Valley of Elah",
                Reference = "1 Samuel 17",
                CharacterId = "david",
                DifficultyLevel = "beginner",
                EstimatedMinutes = 20,
                Themes = new List<string> { "Courage", "Faith", "God's Power", "Overcoming Fear" },
                Scenes = new List<StoryScene>
                {
                    new StoryScene
                    {
                        Order = 1,
                        Title = "The Valley of Elah",
                        Setting = "Israelite camp, valley battlefield",
                        Narrative = "I brought food to my brothers at the battle lines. That's when I first heard him - Goliath's voice booming across the valley...",
                        SuggestedQuestions = new List<string>
                        {
                            "What was Goliath like up close?",
                            "Why was everyone so afraid?",
                            "What made you think you could face him?"
                        },
                        KeyMoment = "Hearing Goliath's challenge"
                    },
                    new StoryScene
                    {
                        Order = 2,
                        Title = "The King's Armor",
                        Setting = "Saul's tent",
                        Narrative = "King Saul tried to fit me with his armor. The bronze helmet was too big, the coat of mail too heavy. I couldn't even move!",
                        SuggestedQuestions = new List<string>
                        {
                            "Why didn't you wear Saul's armor?",
                            "What gave you confidence with just a sling?",
                            "Did Saul believe you could win?"
                        },
                        KeyMoment = "Choosing faith over human protection"
                    },
                    new StoryScene
                    {
                        Order = 3,
                        Title = "Five Smooth Stones",
                        Setting = "The stream, choosing stones",
                        Narrative = "I went to the stream and carefully chose five smooth stones. I only needed one, but I was prepared.",
                        SuggestedQuestions = new List<string>
                        {
                            "How did you know which stones to pick?",
                            "Were you nervous?",
                            "What were you thinking about?"
                        },
                        KeyMoment = "Preparation meets faith"
                    },
                    new StoryScene
                    {
                        Order = 4,
                        Title = "The Battle",
                        Setting = "Valley floor, facing Goliath",
                        Narrative = "Goliath mocked me, cursed me by his gods. But I knew the battle was the Lord's. I ran toward him...",
                        SuggestedQuestions = new List<string>
                        {
                            "What was going through your mind?",
                            "Why did you run toward him?",
                            "How did God help you?"
                        },
                        KeyMoment = "Victory through God's power"
                    }
                }
            },
            new BiblicalStory
            {
                Id = "paul-damascus",
                Title = "The Road to Damascus with Paul",
                Description = "Experience Saul's dramatic conversion to Paul on the road to Damascus",
                Reference = "Acts 9:1-19",
                CharacterId = "paul",
                DifficultyLevel = "intermediate",
                EstimatedMinutes = 18,
                Themes = new List<string> { "Conversion", "Grace", "Transformation", "God's Calling" },
                Scenes = new List<StoryScene>
                {
                    new StoryScene
                    {
                        Order = 1,
                        Title = "Breathing Threats",
                        Setting = "Jerusalem, on the road",
                        Narrative = "I was so sure I was doing God's work, hunting down these followers of 'The Way.' I had letters from the high priest...",
                        SuggestedQuestions = new List<string>
                        {
                            "Why did you persecute Christians?",
                            "Did you really think you were serving God?",
                            "What did you think about Jesus?"
                        },
                        KeyMoment = "Saul's misguided zeal"
                    },
                    new StoryScene
                    {
                        Order = 2,
                        Title = "The Blinding Light",
                        Setting = "Road to Damascus, noon",
                        Narrative = "Suddenly, a light from heaven - brighter than the midday sun - flashed around me. I fell to the ground...",
                        SuggestedQuestions = new List<string>
                        {
                            "What was the light like?",
                            "What did you feel when you fell?",
                            "Did you know immediately it was Jesus?"
                        },
                        KeyMoment = "Encounter with the risen Christ"
                    },
                    new StoryScene
                    {
                        Order = 3,
                        Title = "Who Are You, Lord?",
                        Setting = "Still on the ground",
                        Narrative = "I heard a voice: 'Saul, Saul, why do you persecute me?' I asked who He was. The answer changed everything: 'I am Jesus, whom you are persecuting.'",
                        SuggestedQuestions = new List<string>
                        {
                            "What went through your mind?",
                            "How did you feel about persecuting Jesus?",
                            "Did your whole worldview change instantly?"
                        },
                        KeyMoment = "The moment of truth"
                    },
                    new StoryScene
                    {
                        Order = 4,
                        Title = "Three Days of Darkness",
                        Setting = "Damascus, Ananias's house",
                        Narrative = "I was blind for three days. Couldn't eat, couldn't drink. Just praying, thinking about everything I'd been so wrong about...",
                        SuggestedQuestions = new List<string>
                        {
                            "What did you think about during those three days?",
                            "Were you afraid?",
                            "How did Ananias heal you?"
                        },
                        KeyMoment = "Receiving sight and the Holy Spirit"
                    }
                }
            }
        };
    }
}
