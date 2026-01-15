using AI_Bible_App.Core.Models;

namespace AI_Bible_App.Core.Interfaces;

public interface IGuidedStudyService
{
    Task<GuidedStudySession> BuildSessionAsync(string planId, int dayNumber, bool multiVoiceEnabled, CancellationToken cancellationToken = default);
}
