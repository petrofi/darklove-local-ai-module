using Darklove.LocalAI.Api.Features.EmotionAnalysis.Models;

namespace Darklove.LocalAI.Api.Features.EmotionAnalysis.Services;

public interface IOpenSourceModelClient
{
    Task<OpenSourceModelClassification> ClassifyAsync(
        string userText,
        CancellationToken cancellationToken = default);

    Task<OpenSourceModelStatus> GetStatusAsync(
        CancellationToken cancellationToken = default);
}
