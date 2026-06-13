namespace Darklove.LocalAI.Api.Features.EmotionAnalysis.Models;

public sealed class LocalModelManagementException(
    string reasonCode,
    string message,
    int statusCode = StatusCodes.Status400BadRequest,
    Exception? innerException = null) : Exception(message, innerException)
{
    public string ReasonCode { get; } = reasonCode;

    public int StatusCode { get; } = statusCode;
}
