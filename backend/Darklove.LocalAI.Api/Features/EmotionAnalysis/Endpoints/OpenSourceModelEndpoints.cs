using Darklove.LocalAI.Api.Features.EmotionAnalysis.Models;
using Darklove.LocalAI.Api.Features.EmotionAnalysis.Services;
using Microsoft.Extensions.Options;

namespace Darklove.LocalAI.Api.Features.EmotionAnalysis.Endpoints;

public static class OpenSourceModelEndpoints
{
    public static IEndpointRouteBuilder MapOpenSourceModelEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/model/status", GetStatusAsync)
            .WithName("GetLocalModelStatus")
            .WithTags("Local Model")
            .WithSummary("Yerel açık model çalışma zamanının durumunu döndürür.")
            .Produces<OpenSourceModelStatus>(StatusCodes.Status200OK);

        return endpoints;
    }

    private static async Task<OpenSourceModelStatus> GetStatusAsync(
        IOptions<LocalModelOptions> options,
        IOpenSourceModelClient modelClient,
        CancellationToken cancellationToken)
    {
        if (!options.Value.Enabled)
        {
            return new OpenSourceModelStatus(
                options.Value.Provider,
                options.Value.Model,
                RuntimeAvailable: false,
                ModelAvailable: false,
                Status: "disabled");
        }

        return await modelClient.GetStatusAsync(cancellationToken);
    }
}
