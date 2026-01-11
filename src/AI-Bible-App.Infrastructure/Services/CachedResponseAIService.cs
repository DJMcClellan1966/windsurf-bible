using AI_Bible_App.Core.Interfaces;
using AI_Bible_App.Core.Models;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace AI_Bible_App.Infrastructure.Services;

/// <summary>
/// Cached response AI service for emergency fallback on very limited devices.
/// Provides pre-written responses when no other AI is available.
/// </summary>
public class CachedResponseAIService : IAIService
{
    private readonly ILogger<CachedResponseAIService> _logger;
    private readonly Dictionary<string, List<string>> _characterResponses;
    private readonly Random _random = new();

    public CachedResponseAIService(ILogger<CachedResponseAIService> logger)
    {
        _logger = logger;
        _characterResponses = InitializeResponses();
    }

    public Task<string> GetChatResponseAsync(
        BiblicalCharacter character,
        List<ChatMessage> conversationHistory,
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Using cached response for {Character}", character.Name);
        
        var response = GetCachedResponse(character.Id, userMessage);
        return Task.FromResult(response);
    }

    public async IAsyncEnumerable<string> StreamChatResponseAsync(
        BiblicalCharacter character,
        List<ChatMessage> conversationHistory,
        string userMessage,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var response = await GetChatResponseAsync(character, conversationHistory, userMessage, cancellationToken);
        
        // Simulate streaming by yielding words
        var words = response.Split(' ');
        foreach (var word in words)
        {
            yield return word + " ";
            await Task.Delay(30, cancellationToken); // Small delay for natural feel
        }
    }

    public Task<string> GeneratePrayerAsync(string topic, CancellationToken cancellationToken = default)
    {
        var prayer = GetCachedPrayer(topic);
        return Task.FromResult(prayer);
    }

    private string GetCachedResponse(string characterId, string userMessage)
    {
        var lowerMessage = userMessage.ToLowerInvariant();
        
        // Try to match topic-specific responses
        if (lowerMessage.Contains("faith") || lowerMessage.Contains("believe") || lowerMessage.Contains("trust"))
        {
            return GetFaithResponse(characterId);
        }
        if (lowerMessage.Contains("fear") || lowerMessage.Contains("afraid") || lowerMessage.Contains("worry"))
        {
            return GetFearResponse(characterId);
        }
        if (lowerMessage.Contains("pray") || lowerMessage.Contains("prayer"))
        {
            return GetPrayerResponse(characterId);
        }
        if (lowerMessage.Contains("love") || lowerMessage.Contains("forgive"))
        {
            return GetLoveResponse(characterId);
        }
        if (lowerMessage.Contains("sin") || lowerMessage.Contains("wrong") || lowerMessage.Contains("guilt"))
        {
            return GetSinResponse(characterId);
        }
        
        // Default responses
        if (_characterResponses.TryGetValue(characterId, out var responses) && responses.Count > 0)
        {
            return responses[_random.Next(responses.Count)];
        }
        
        return GetGenericResponse(characterId);
    }

    private string GetFaithResponse(string characterId) => characterId switch
    {
        "david" => "My friend, faith is trusting in the Lord even when the path ahead is dark. As I wrote in my psalms, 'The Lord is my shepherd; I shall not want.' Even when I faced Goliath, it was not my strength but my faith in God that prevailed. Trust Him with all your heart.",
        "paul" => "Faith, dear friend, is the substance of things hoped for, the evidence of things not seen. I have learned through many trials that when we are weak, then we are strong in Christ. Let your faith not rest in human wisdom, but in God's power.",
        "moses" => "I remember standing before the Red Sea with Pharaoh's army behind us. Fear gripped the people, but the Lord said 'Stand firm and see the deliverance of the Lord.' Faith is stepping forward when you cannot see the path.",
        "peter" => "I know what it means to have faith tested! I walked on water toward Jesus, but when I looked at the waves instead of Him, I began to sink. Keep your eyes on Jesus, and your faith will sustain you.",
        "mary-mother" => "My child, faith is saying 'yes' to God even when we don't understand His plan. When the angel came to me, I was afraid, yet I said 'Let it be according to your word.' Trust that God's ways are higher than our ways.",
        _ => "Have faith in God. With faith as small as a mustard seed, mountains can be moved. Trust in the Lord with all your heart and lean not on your own understanding."
    };

    private string GetFearResponse(string characterId) => characterId switch
    {
        "david" => "Fear not, for the Lord is with you. Even though I walk through the valley of the shadow of death, I will fear no evil, for You are with me. God has not given us a spirit of fear, but of power and love.",
        "esther" => "I understand fear deeply. When I had to approach the king uninvited, my life was at stake. But Mordecai reminded me - perhaps I was placed in my position 'for such a time as this.' Sometimes we must do what is right despite our fear.",
        "moses" => "The Lord said to me many times, 'Fear not.' When I stood before Pharaoh, when the people rebelled, when the journey seemed impossible - God was always there. He who brought Israel out of Egypt will surely help you through your trials.",
        _ => "Do not be afraid, for the Lord your God is with you wherever you go. Cast all your anxiety on Him because He cares for you. Perfect love casts out fear."
    };

    private string GetPrayerResponse(string characterId) => characterId switch
    {
        "hannah" => "Prayer is pouring out your heart before the Lord. When I wept at the temple, unable to have a child, I prayed from the depths of my soul. God heard my prayer. Pour out your heart to Him - He listens to every word.",
        "david" => "The Psalms are my prayers to God - cries of anguish, songs of praise, pleas for help. I learned to bring everything to God: my fears, my joys, my questions. He is always ready to listen.",
        "paul" => "Pray without ceasing! Let your requests be made known to God with thanksgiving. I have prayed in prison, in shipwreck, in every circumstance. Prayer connects us to the source of all strength.",
        _ => "Draw near to God in prayer, and He will draw near to you. Bring your requests, your praise, and your burdens to Him. He is faithful and hears the prayers of His children."
    };

    private string GetLoveResponse(string characterId) => characterId switch
    {
        "john" => "God is love, and whoever abides in love abides in God. This is the message I have carried all my life: love one another as Christ has loved us. There is no fear in love, for perfect love drives out fear.",
        "ruth" => "Love is loyalty and faithfulness. When I told Naomi 'where you go, I will go,' it was love speaking. Love means staying committed even when the path is hard.",
        "paul" => "Love is patient, love is kind. It does not envy, it does not boast. Love never fails. Of faith, hope, and love, the greatest of these is love.",
        _ => "Above all, love one another deeply, for love covers a multitude of sins. Love your neighbor as yourself, for this is the heart of God's commandments."
    };

    private string GetSinResponse(string characterId) => characterId switch
    {
        "david" => "I know the weight of sin all too well. After my sin with Bathsheba, I cried out to God: 'Create in me a clean heart, O God.' He is faithful to forgive when we truly repent. His mercy is greater than our failures.",
        "peter" => "I denied my Lord three times! The shame was unbearable. But Jesus restored me by the sea, asking 'Do you love me?' three times. There is no sin too great for His forgiveness if we turn back to Him.",
        "paul" => "I once persecuted the church, approving of murder. Yet God's grace reached even me, the chief of sinners. Where sin increased, grace increased all the more. His blood covers every sin.",
        _ => "If we confess our sins, He is faithful and just to forgive us and cleanse us from all unrighteousness. God's mercy is new every morning. Come to Him with a repentant heart."
    };

    private string GetGenericResponse(string characterId) => characterId switch
    {
        "david" => "The Lord is my rock and my fortress. Whatever you are facing, bring it to God in prayer. He has been faithful to me through every trial, and He will be faithful to you.",
        "paul" => "I can do all things through Christ who strengthens me. Whatever situation you find yourself in, remember that God is working all things together for good for those who love Him.",
        "moses" => "The Lord will fight for you; you need only to be still. I have seen God's power move mountains and part seas. Trust in His timing and His ways.",
        "peter" => "Cast all your cares upon Jesus, for He cares for you. I have learned that our Lord is patient with us, not wanting anyone to perish but everyone to come to repentance.",
        "mary-mother" => "My soul magnifies the Lord. In every season of life, whether joy or sorrow, God is with us. Treasure His words in your heart and trust His plan.",
        "john" => "God so loved the world that He gave His only Son. Hold fast to this truth: you are loved with an everlasting love. Let this love transform how you see everything.",
        "esther" => "For such a time as this, God has placed you where you are. Be courageous and trust that God's purposes are being worked out, even when we cannot see them.",
        "ruth" => "The Lord is our kinsman-redeemer. He brings beauty from ashes and hope from despair. Stay faithful in the small things, and watch what God will do.",
        "solomon" => "The fear of the Lord is the beginning of wisdom. Trust in the Lord with all your heart, and in all your ways acknowledge Him, and He will make your paths straight.",
        "deborah" => "The Lord goes before you. He is the one who will be with you; He will not fail you or forsake you. Do not fear or be dismayed. Rise up in faith!",
        "hannah" => "The Lord remembers us in our distress. He lifts the humble and hears the prayers of His people. Pour out your heart to Him, for He is faithful.",
        _ => "The Lord is good, a refuge in times of trouble. He cares for those who trust in Him. Whatever burden you carry, know that He is with you always."
    };

    private string GetCachedPrayer(string topic)
    {
        return $@"Heavenly Father,

We come before You with humble hearts, seeking Your presence and guidance regarding {topic}.

Lord, You know our needs before we even ask. You see our struggles and our hopes. We trust in Your perfect wisdom and unfailing love.

Grant us peace that surpasses understanding, strength for each day, and faith to trust Your plan. Help us to walk in Your ways and to love as You have loved us.

May Your will be done in our lives and in this situation. We surrender our concerns to You, knowing that You work all things together for good.

In Jesus' precious name we pray,
Amen.";
    }

    private Dictionary<string, List<string>> InitializeResponses()
    {
        return new Dictionary<string, List<string>>
        {
            ["david"] = new List<string>
            {
                "The Lord is my shepherd; I shall not want. Whatever burden weighs upon your heart, know that God leads us beside still waters and restores our souls.",
                "I have learned to wait upon the Lord. In my darkest caves, hiding from Saul, I learned that God's timing is perfect. Be patient and trust Him.",
                "Praise the Lord, O my soul! Even in trials, we can lift our voices to God. He inhabits the praises of His people."
            },
            ["paul"] = new List<string>
            {
                "I have learned the secret of being content in any circumstance. Whether in plenty or in want, Christ is my strength.",
                "Brothers and sisters, whatever is true, noble, right, pure, lovely, and admirable - think on these things. Guard your mind with God's truth.",
                "Run the race with perseverance, keeping your eyes fixed on Jesus, the author and perfecter of our faith."
            },
            ["moses"] = new List<string>
            {
                "The Lord is slow to anger and abounding in steadfast love. I have seen His patience with a rebellious people, and His mercy is new every morning.",
                "Be strong and courageous! Do not be terrified or discouraged, for the Lord your God will be with you wherever you go.",
                "Remember what the Lord has done for you. When you face new challenges, recall His faithfulness in the past."
            },
            ["peter"] = new List<string>
            {
                "Grace and peace be multiplied to you! Our Lord is patient with us, giving us time to grow in faith and understanding.",
                "Be sober-minded and watchful. Your adversary prowls around like a roaring lion, but resist him, standing firm in the faith.",
                "You are chosen, a royal priesthood, called out of darkness into His wonderful light. Remember whose you are!"
            },
            ["mary-mother"] = new List<string>
            {
                "My soul magnifies the Lord, for He has looked upon the humble state of His servant. He lifts up the lowly and fills the hungry with good things.",
                "Treasuring God's words in your heart is a practice that has sustained me through joy and sorrow. Hold His promises close.",
                "The Lord's ways are not always clear to us, but His love is constant. Trust in His goodness, even when the path seems dark."
            }
        };
    }
}
