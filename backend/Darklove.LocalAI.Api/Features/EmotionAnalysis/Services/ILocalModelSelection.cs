namespace Darklove.LocalAI.Api.Features.EmotionAnalysis.Services;

public interface ILocalModelSelection
{
    string Provider { get; }

    string Model { get; }

    void Select(string model);
}
