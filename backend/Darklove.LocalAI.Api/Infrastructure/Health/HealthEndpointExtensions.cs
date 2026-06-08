using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Darklove.LocalAI.Api.Infrastructure.Health;

public static class HealthEndpointExtensions
{
    public static IEndpointRouteBuilder MapHealthEndpoint(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/health", CheckHealthAsync)
        .WithName("HealthCheck")
        .WithTags("System")
        .WithSummary("API servisinin çalışır durumda olduğunu doğrular.")
        .Produces<HealthResponse>(StatusCodes.Status200OK)
        .Produces<HealthResponse>(StatusCodes.Status503ServiceUnavailable);

        return endpoints;
    }

    private static async Task<IResult> CheckHealthAsync(
        HealthCheckService healthCheckService,
        CancellationToken cancellationToken)
    {
        var report = await healthCheckService.CheckHealthAsync(cancellationToken);
        var response = new HealthResponse(
            report.Status == HealthStatus.Healthy ? "running" : "unhealthy",
            "Darklove Local AI Module",
            "1.0.0",
            "backend-api");

        var statusCode = report.Status == HealthStatus.Healthy
            ? StatusCodes.Status200OK
            : StatusCodes.Status503ServiceUnavailable;

        return Results.Json(response, statusCode: statusCode);
    }
}

public sealed record HealthResponse(
    string Status,
    string Project,
    string Version,
    string Module);
