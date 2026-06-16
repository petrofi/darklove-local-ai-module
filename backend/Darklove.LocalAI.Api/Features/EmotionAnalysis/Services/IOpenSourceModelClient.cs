using Darklove.LocalAI.Api.Features.EmotionAnalysis.Contracts;
using Darklove.LocalAI.Api.Features.EmotionAnalysis.Models;

namespace Darklove.LocalAI.Api.Features.EmotionAnalysis.Services;

public interface IOpenSourceModelClient
{
    Task<string> ChatAsync(
        string userText,
        IReadOnlyList<ChatMessage> history,
        CancellationToken cancellationToken = default);

    Task<OpenSourceModelClassification> ClassifyAsync(
        string userText,
        CancellationToken cancellationToken = default);

    Task<OpenSourceModelStatus> GetStatusAsync(
        CancellationToken cancellationToken = default);
}
