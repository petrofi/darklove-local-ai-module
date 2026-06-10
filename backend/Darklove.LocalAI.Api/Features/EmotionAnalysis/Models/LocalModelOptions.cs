namespace Darklove.LocalAI.Api.Features.EmotionAnalysis.Models;

public sealed class LocalModelOptions
{
    public const string SectionName = "LocalModel";

    public bool Enabled { get; set; }

    public string Provider { get; set; } = "ollama";

    public string Endpoint { get; set; } = "http://localhost:11434";

    public string Model { get; set; } = "qwen3:4b";

    public int TimeoutSeconds { get; set; } = 90;
}
