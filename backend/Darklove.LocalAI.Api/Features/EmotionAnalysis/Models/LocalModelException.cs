namespace Darklove.LocalAI.Api.Features.EmotionAnalysis.Models;

public sealed class LocalModelException : Exception
{
    public LocalModelException(string reasonCode, string message)
        : base(message)
    {
        ReasonCode = reasonCode;
    }

    public LocalModelException(string reasonCode, string message, Exception innerException)
        : base(message, innerException)
    {
        ReasonCode = reasonCode;
    }

    public string ReasonCode { get; }
}
