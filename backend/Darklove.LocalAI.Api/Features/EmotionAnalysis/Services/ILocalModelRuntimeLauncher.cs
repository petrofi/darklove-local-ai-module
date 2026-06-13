namespace Darklove.LocalAI.Api.Features.EmotionAnalysis.Services;

public interface ILocalModelRuntimeLauncher
{
    Task<bool> EnsureRunningAsync(CancellationToken cancellationToken = default);
}
