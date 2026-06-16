using Darklove.LocalAI.Api.Features.EmotionAnalysis.Contracts;

namespace Darklove.LocalAI.Api.Features.EmotionAnalysis.Services;

public interface IChatService
{
    Task<ChatResponse> ReplyAsync(
        ChatRequest request,
        CancellationToken cancellationToken = default);
}
