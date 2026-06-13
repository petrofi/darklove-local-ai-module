using Darklove.LocalAI.Api.Features.EmotionAnalysis.Contracts;
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

        var models = endpoints.MapGroup("/api/models")
            .WithTags("Local Models");

        models.MapGet("/", GetModelsAsync)
            .WithName("GetLocalModels")
            .WithSummary("Bilgisayardaki çalışabilir yerel modelleri listeler.")
            .Produces<LocalModelCatalog>(StatusCodes.Status200OK);

        models.MapPut("/selected", SelectModelAsync)
            .WithName("SelectLocalModel")
            .WithSummary("Bir yerel modeli yükler ve analiz için etkinleştirir.")
            .Accepts<SelectLocalModelRequest>("application/json")
            .Produces<LocalModelSelectionResult>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status502BadGateway);

        models.MapPost("/downloads", StartDownloadAsync)
            .WithName("DownloadLocalModel")
            .WithSummary("LM Studio veya Ollama üzerinden yerel model indirmesi başlatır.")
            .Accepts<DownloadLocalModelRequest>("application/json")
            .Produces<LocalModelDownloadStatus>(StatusCodes.Status200OK)
            .Produces<LocalModelDownloadStatus>(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status502BadGateway);

        models.MapGet("/downloads/{jobId}", GetDownloadStatusAsync)
            .WithName("GetLocalModelDownload")
            .WithSummary("Devam eden yerel model indirmesinin ilerlemesini döndürür.")
            .Produces<LocalModelDownloadStatus>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

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

    private static async Task<IResult> GetModelsAsync(
        IOptions<LocalModelOptions> options,
        ILocalModelManager modelManager,
        ILocalModelSelection selection,
        CancellationToken cancellationToken)
    {
        if (!options.Value.Enabled)
        {
            return Results.Ok(new LocalModelCatalog(
                selection.Provider,
                selection.Model,
                RuntimeAvailable: false,
                Status: "disabled",
                []));
        }

        return Results.Ok(await modelManager.GetCatalogAsync(cancellationToken));
    }

    private static async Task<IResult> SelectModelAsync(
        SelectLocalModelRequest request,
        IOptions<LocalModelOptions> options,
        ILocalModelManager modelManager,
        CancellationToken cancellationToken)
    {
        if (!options.Value.Enabled)
        {
            return Problem(
                "model-disabled",
                "Yerel model kullanımı yapılandırmada kapalıdır.",
                StatusCodes.Status409Conflict);
        }

        if (string.IsNullOrWhiteSpace(request.Model) || request.Model.Length > 200)
        {
            return Problem(
                "invalid-model",
                "Model adı boş olamaz ve 200 karakteri geçemez.",
                StatusCodes.Status400BadRequest);
        }

        try
        {
            return Results.Ok(await modelManager.SelectAsync(
                request.Model.Trim(),
                cancellationToken));
        }
        catch (LocalModelManagementException exception)
        {
            return Problem(exception.ReasonCode, exception.Message, exception.StatusCode);
        }
    }

    private static async Task<IResult> StartDownloadAsync(
        DownloadLocalModelRequest request,
        IOptions<LocalModelOptions> options,
        ILocalModelManager modelManager,
        CancellationToken cancellationToken)
    {
        if (!options.Value.Enabled)
        {
            return Problem(
                "model-disabled",
                "Yerel model kullanımı yapılandırmada kapalıdır.",
                StatusCodes.Status409Conflict);
        }

        if (string.IsNullOrWhiteSpace(request.Model) || request.Model.Length > 500)
        {
            return Problem(
                "invalid-model",
                "İndirilecek model adı boş olamaz ve 500 karakteri geçemez.",
                StatusCodes.Status400BadRequest);
        }

        try
        {
            var status = await modelManager.StartDownloadAsync(
                request.Model.Trim(),
                request.Quantization?.Trim(),
                cancellationToken);

            return status.Status == "downloading"
                ? Results.Accepted(
                    status.JobId is null
                        ? "/api/models"
                        : $"/api/models/downloads/{Uri.EscapeDataString(status.JobId)}",
                    status)
                : Results.Ok(status);
        }
        catch (LocalModelManagementException exception)
        {
            return Problem(exception.ReasonCode, exception.Message, exception.StatusCode);
        }
    }

    private static async Task<IResult> GetDownloadStatusAsync(
        string jobId,
        ILocalModelManager modelManager,
        CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await modelManager.GetDownloadStatusAsync(
                jobId,
                cancellationToken));
        }
        catch (LocalModelManagementException exception)
        {
            return Problem(exception.ReasonCode, exception.Message, exception.StatusCode);
        }
    }

    private static IResult Problem(string reasonCode, string detail, int statusCode)
    {
        return Results.Problem(
            statusCode: statusCode,
            title: "Yerel model işlemi tamamlanamadı.",
            detail: detail,
            extensions: new Dictionary<string, object?>
            {
                ["reasonCode"] = reasonCode
            });
    }
}
