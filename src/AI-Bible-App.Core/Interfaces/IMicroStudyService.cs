using AI_Bible_App.Core.Models;

namespace AI_Bible_App.Core.Interfaces;

public interface IMicroStudyService
{
    Task<MicroStudySession> BuildSessionAsync(string planId, int dayNumber, bool multiVoiceEnabled, CancellationToken cancellationToken = default);

    Task<SocraticCritique> CritiqueAnswerAsync(string planId, int dayNumber, string question, string userAnswer, bool multiVoiceEnabled, CancellationToken cancellationToken = default);
}
