using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Darklove.LocalAI.Api.Features.EmotionAnalysis.Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Darklove.LocalAI.Api.Tests;

public sealed class ApiIntegrationTests : IClassFixture<DarkloveApiFactory>
{
    private readonly HttpClient _client;

    public ApiIntegrationTests(DarkloveApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsRunningStatus()
    {
        var response = await _client.GetAsync("/api/health");
        var document = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("running", document.RootElement.GetProperty("status").GetString());
        Assert.Equal("backend-api", document.RootElement.GetProperty("module").GetString());
    }

    [Fact]
    public async Task DemoPage_IsAvailableAtRoot()
    {
        var response = await _client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("Darklove Local AI", html);
        Assert.Contains("analysis-form", html);
        Assert.Contains("model-download-form", html);
        Assert.Contains("/app.js?v=3", html);
    }

    [Theory]
    [InlineData("/styles.css", "text/css")]
    [InlineData("/app.js", "text/javascript")]
    public async Task DemoAssets_AreAvailable(string path, string expectedMediaType)
    {
        var response = await _client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(expectedMediaType, response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
    }

    [Fact]
    public async Task AnalyzeEndpoint_ReturnsExplainableResult()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/emotion/analyze",
            new EmotionAnalysisRequest("Bugün kendimi yalnız ve yorgun hissediyorum."));
        var result = await response.Content.ReadFromJsonAsync<EmotionAnalysisResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.Equal("sadness", result.DetectedEmotion);
        Assert.Equal(2, result.Scores["sadness"]);
        Assert.Equal("none", result.RiskLevel);
        Assert.False(result.NeedsSupportWarning);
        Assert.Equal("rule-based", result.AnalysisMethod);
    }

    [Fact]
    public async Task AnalyzeEndpoint_ReturnsSafeCrisisResponse()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/emotion/analyze",
            new EmotionAnalysisRequest("Artık yaşamak istemiyorum."));
        var result = await response.Content.ReadFromJsonAsync<EmotionAnalysisResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.Equal("high", result.RiskLevel);
        Assert.True(result.NeedsSupportWarning);
        Assert.Contains("112", result.MotivationMessage);
    }

    [Fact]
    public async Task ChatEndpoint_ReturnsFriendlyDisabledMessage_WhenModelsAreDisabledForTests()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/chat",
            new ChatRequest("naber nasıl gidiyor"));
        var result = await response.Content.ReadFromJsonAsync<ChatResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.Equal("local-model-disabled", result.AnalysisMethod);
        Assert.Contains("yerel sohbet modeli", result.AssistantMessage);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AnalyzeEndpoint_ReturnsProblemDetails_ForEmptyText(string text)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/emotion/analyze",
            new EmotionAnalysisRequest(text));
        var document = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            "İstek doğrulanamadı.",
            document.RootElement.GetProperty("title").GetString());
        Assert.True(document.RootElement.GetProperty("errors").TryGetProperty(
            "userText",
            out _));
    }

    [Fact]
    public async Task AnalyzeEndpoint_ReturnsProblemDetails_ForLongText()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/emotion/analyze",
            new EmotionAnalysisRequest(new string('a', 2001)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task AnalyzeEndpoint_ReturnsBadRequest_ForMalformedJson()
    {
        using var content = new StringContent(
            """{"userText": "eksik kapanış" """,
            Encoding.UTF8,
            "application/json");

        var response = await _client.PostAsync("/api/emotion/analyze", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task OpenApiDocument_IsAvailableInDevelopment()
    {
        var response = await _client.GetAsync("/openapi/v1.json");
        var document = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync());
        var operation = document.RootElement
            .GetProperty("paths")
            .GetProperty("/api/emotion/analyze")
            .GetProperty("post");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            "application/json",
            response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(
            "Kullanıcı metnindeki duygusal işaretleri analiz eder.",
            operation.GetProperty("summary").GetString());
        Assert.True(operation.GetProperty("responses").TryGetProperty("200", out _));
        Assert.True(operation.GetProperty("responses").TryGetProperty("400", out _));
    }

    [Fact]
    public async Task ModelStatusEndpoint_ReturnsDisabled_WhenModelIsDisabledForTests()
    {
        var response = await _client.GetAsync("/api/model/status");
        var document = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("disabled", document.RootElement.GetProperty("status").GetString());
        Assert.False(document.RootElement.GetProperty("runtimeAvailable").GetBoolean());
    }

    [Fact]
    public async Task ModelsEndpoint_ReturnsEmptyDisabledCatalog_WhenModelsAreDisabled()
    {
        var response = await _client.GetAsync("/api/models/");
        var document = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("disabled", document.RootElement.GetProperty("status").GetString());
        Assert.Empty(document.RootElement.GetProperty("models").EnumerateArray());
    }

    [Fact]
    public async Task SelectModelEndpoint_ReturnsProblemDetails_WhenModelsAreDisabled()
    {
        var response = await _client.PutAsJsonAsync(
            "/api/models/selected",
            new { model = "qwen/qwen3-4b" });
        var document = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync());

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(
            "model-disabled",
            document.RootElement.GetProperty("reasonCode").GetString());
    }

    [Fact]
    public async Task SwaggerUi_IsAvailableInDevelopment()
    {
        var response = await _client.GetAsync("/swagger/index.html");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("swagger-ui", html, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class DarkloveApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("HttpsRedirection:Enabled", "false");
        builder.UseSetting("LocalModel:Enabled", "false");
    }
}
