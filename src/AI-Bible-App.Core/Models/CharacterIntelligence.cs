using System;
using System.Collections.Generic;

namespace AI_Bible_App.Core.Models;

/// <summary>
/// Represents the evolving "intelligence" of a biblical character.
/// This grows and changes with each interaction while the base LLM stays static.
/// </summary>
public class CharacterIntelligence
{
    public string CharacterId { get; set; } = string.Empty;
    public string CharacterName { get; set; } = string.Empty;
    
    /// <summary>
    /// The evolved profile that grows from interactions
    /// </summary>
    public CharacterProfile Profile { get; set; } = new();
    
    /// <summary>
    /// All memories from interactions (chats, prayers, roundtables)
    /// </summary>
    public List<CharacterMemory> Memories { get; set; } = new();
    
    /// <summary>
    /// Learned traits discovered from interactions
    /// </summary>
    public List<LearnedTrait> LearnedTraits { get; set; } = new();
    
    /// <summary>
    /// Topics this character has discussed and their stance
    /// </summary>
    public Dictionary<string, TopicStance> TopicStances { get; set; } = new();
    
    /// <summary>
    /// Relationships with other characters discovered through debates
    /// </summary>
    public Dictionary<string, CharacterRelationship> Relationships { get; set; } = new();
    
    /// <summary>
    /// Statistics about this character's usage
    /// </summary>
    public CharacterStats Stats { get; set; } = new();
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
    public int Version { get; set; } = 1;
    public DateTime? LastProfileRebuildAt { get; set; }
}

/// <summary>
/// The synthesized profile built from all interactions
/// </summary>
public class CharacterProfile
{
    /// <summary>
    /// Core personality traits (base + learned)
    /// </summary>
    public List<string> PersonalityTraits { get; set; } = new();
    
    /// <summary>
    /// Communication style patterns discovered
    /// </summary>
    public CommunicationStyle CommunicationStyle { get; set; } = new();
    
    /// <summary>
    /// Theological positions discovered through discussions
    /// </summary>
    public List<TheologicalPosition> TheologicalPositions { get; set; } = new();
    
    /// <summary>
    /// Favorite scripture references this character uses
    /// </summary>
    public List<ScripturePreference> FavoriteScriptures { get; set; } = new();
    
    /// <summary>
    /// Common phrases or expressions this character uses
    /// </summary>
    public List<string> SignaturePhrases { get; set; } = new();
    
    /// <summary>
    /// Topics this character gravitates toward
    /// </summary>
    public List<string> PreferredTopics { get; set; } = new();
    
    /// <summary>
    /// Generated summary of this character based on all interactions
    /// </summary>
    public string EvolvedDescription { get; set; } = string.Empty;
    
    /// <summary>
    /// Confidence score (0-1) in the profile accuracy
    /// </summary>
    public double ProfileConfidence { get; set; } = 0.0;
}

/// <summary>
/// A single memory from an interaction
/// </summary>
public class CharacterMemory
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public MemoryType Type { get; set; }
    public string Context { get; set; } = string.Empty; // The question/topic
    public string Response { get; set; } = string.Empty; // What the character said
    public string UserInput { get; set; } = string.Empty; // What triggered this
    public List<string> OtherParticipants { get; set; } = new(); // In roundtables
    public List<string> ExtractedClaims { get; set; } = new(); // Key points made
    public List<string> ScripturesUsed { get; set; } = new();
    public double EmotionalTone { get; set; } = 0.5; // 0=negative, 1=positive
    public double Importance { get; set; } = 0.5; // How significant this memory is
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Embedding vector for semantic search (if using embeddings)
    /// </summary>
    public float[]? Embedding { get; set; }
}

public enum MemoryType
{
    Chat,
    Prayer,
    Roundtable,
    WisdomCouncil,
    Debate,
    Teaching
}

/// <summary>
/// A trait learned from interactions
/// </summary>
public class LearnedTrait
{
    public string Trait { get; set; } = string.Empty;
    public string Evidence { get; set; } = string.Empty; // Example that showed this trait
    public double Confidence { get; set; } = 0.5;
    public int OccurrenceCount { get; set; } = 1;
    public DateTime FirstObserved { get; set; } = DateTime.UtcNow;
    public DateTime LastObserved { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Character's stance on a particular topic
/// </summary>
public class TopicStance
{
    public string Topic { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty; // Their view
    public List<string> SupportingArguments { get; set; } = new();
    public List<string> ScriptureReferences { get; set; } = new();
    public double Certainty { get; set; } = 0.5; // How strongly they hold this
    public int TimesDiscussed { get; set; } = 1;
}

/// <summary>
/// Relationship with another character
/// </summary>
public class CharacterRelationship
{
    public string OtherCharacterId { get; set; } = string.Empty;
    public string OtherCharacterName { get; set; } = string.Empty;
    public RelationshipType Type { get; set; } = RelationshipType.Neutral;
    public double Affinity { get; set; } = 0.5; // -1 to 1
    public List<string> SharedTopics { get; set; } = new();
    public List<string> DisagreedTopics { get; set; } = new();
    public int InteractionCount { get; set; } = 0;
}

public enum RelationshipType
{
    Ally,      // Often agrees
    Challenger, // Often debates
    Mentor,    // Teaches them
    Student,   // Learns from them
    Neutral    // No strong relationship yet
}

/// <summary>
/// Communication patterns
/// </summary>
public class CommunicationStyle
{
    public double Formality { get; set; } = 0.5; // 0=casual, 1=formal
    public double Verbosity { get; set; } = 0.5; // 0=terse, 1=elaborate
    public double DirectionFocus { get; set; } = 0.5; // 0=indirect, 1=direct
    public double EmotionalExpression { get; set; } = 0.5; // 0=stoic, 1=emotional
    public double QuestionAsking { get; set; } = 0.5; // How often they ask questions
    public double StorytellingTendency { get; set; } = 0.5; // Use of stories/parables
    public List<string> CommonOpenings { get; set; } = new(); // How they start responses
    public List<string> CommonClosings { get; set; } = new(); // How they end responses
}

/// <summary>
/// A theological position held by the character
/// </summary>
public class TheologicalPosition
{
    public string Topic { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string Reasoning { get; set; } = string.Empty;
    public List<string> SupportingScriptures { get; set; } = new();
    public double Confidence { get; set; } = 0.5;
}

/// <summary>
/// Scripture preference
/// </summary>
public class ScripturePreference
{
    public string Reference { get; set; } = string.Empty;
    public int TimesUsed { get; set; } = 1;
    public List<string> ContextsUsed { get; set; } = new();
}

/// <summary>
/// Statistics about character usage
/// </summary>
public class CharacterStats
{
    public int TotalInteractions { get; set; } = 0;
    public int ChatCount { get; set; } = 0;
    public int PrayerCount { get; set; } = 0;
    public int RoundtableCount { get; set; } = 0;
    public int WisdomCouncilCount { get; set; } = 0;
    public int TotalWordsGenerated { get; set; } = 0;
    public int UniqueTopicsDiscussed { get; set; } = 0;
    public Dictionary<string, int> TopicFrequency { get; set; } = new();
    public DateTime FirstInteraction { get; set; } = DateTime.UtcNow;
    public DateTime LastInteraction { get; set; } = DateTime.UtcNow;
}
