using Darklove.LocalAI.Api.Features.EmotionAnalysis.Models;
using Microsoft.Extensions.Options;

namespace Darklove.LocalAI.Api.Features.EmotionAnalysis.Services;

public sealed class LocalModelSelection(IOptions<LocalModelOptions> options)
    : ILocalModelSelection
{
    private string _model = options.Value.Model;

    public string Provider => options.Value.Provider;

    public string Model => Volatile.Read(ref _model);

    public void Select(string model)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(model);
        Volatile.Write(ref _model, model);
    }
}
