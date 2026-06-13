using System.Net;
using System.Text;
using System.Text.Json;
using Darklove.LocalAI.Api.Features.EmotionAnalysis.Models;
using Darklove.LocalAI.Api.Features.EmotionAnalysis.Services;
using Microsoft.Extensions.Options;

namespace Darklove.LocalAI.Api.Tests;

public sealed class LmStudioOpenSourceModelClientTests
{
    [Fact]
    public async Task GetCatalogAsync_ListsOnlyLanguageModels()
    {
        var client = CreateClient(_ => JsonResponse(
            """
            {
              "models": [
                {
                  "type": "embedding",
                  "key": "nomic-embed",
                  "display_name": "Nomic Embed",
                  "size_bytes": 100,
                  "params_string": null,
                  "quantization": { "name": "Q4_K_M" },
                  "loaded_instances": [],
                  "capabilities": null
                },
                {
                  "type": "llm",
                  "key": "qwen/qwen3-vl-30b",
                  "display_name": "Qwen3 VL 30B",
                  "size_bytes": 19640366937,
                  "params_string": "30B-A3B",
                  "quantization": { "name": "Q4_K_M" },
                  "loaded_instances": [{ "id": "qwen/qwen3-vl-30b" }],
                  "capabilities": { "vision": true }
                }
              ]
            }
            """));

        var catalog = await client.GetCatalogAsync();

        var model = Assert.Single(catalog.Models);
        Assert.Equal("qwen/qwen3-vl-30b", model.Key);
        Assert.True(model.IsLoaded);
        Assert.True(model.SupportsVision);
        Assert.Equal("ready", catalog.Status);
    }

    [Fact]
    public async Task ClassifyAsync_UsesSelectedModelAndParsesStructuredResponse()
    {
        string? requestBody = null;
        var client = CreateClient(async request =>
        {
            requestBody = await request.Content!.ReadAsStringAsync();

            return JsonResponse(
                """
                {
                  "choices": [
                    {
                      "message": {
                        "content": "{\"detectedEmotion\":\"hope\",\"confidence\":0.88,\"scores\":{\"sadness\":0.01,\"anxiety\":0.02,\"hope\":0.88,\"anger\":0.01,\"neutral\":0.08}}"
                      }
                    }
                  ]
                }
                """);
        });

        var result = await client.ClassifyAsync("Başaracağıma inanıyorum.");

        Assert.Equal("hope", result.DetectedEmotion);
        Assert.Equal(0.88, result.Confidence);

        using var document = JsonDocument.Parse(requestBody!);
        Assert.Equal(
            "qwen/qwen3-vl-30b",
            document.RootElement.GetProperty("model").GetString());
        Assert.True(document.RootElement.TryGetProperty("response_format", out _));
    }

    [Fact]
    public async Task SelectAsync_LoadsModelAndChangesSelection()
    {
        var selection = CreateSelection();
        var requests = new List<string>();
        var client = CreateClient(request =>
        {
            requests.Add(request.RequestUri!.AbsolutePath);

            return Task.FromResult(request.Method == HttpMethod.Get
                ? JsonResponse(
                    """
                    {
                      "models": [
                        {
                          "type": "llm",
                          "key": "qwen/qwen3-4b",
                          "display_name": "Qwen3 4B",
                          "size_bytes": 2500000000,
                          "params_string": "4B",
                          "quantization": { "name": "Q4_K_M" },
                          "loaded_instances": [],
                          "capabilities": { "vision": false }
                        }
                      ]
                    }
                    """)
                : JsonResponse(
                    """
                    {
                      "type": "llm",
                      "instance_id": "qwen/qwen3-4b",
                      "status": "loaded"
                    }
                    """));
        }, selection);

        var result = await client.SelectAsync("qwen/qwen3-4b");

        Assert.True(result.Loaded);
        Assert.Equal("qwen/qwen3-4b", selection.Model);
        Assert.Contains("/api/v1/models/load", requests);
    }

    [Fact]
    public async Task StartDownloadAsync_ParsesDownloadJob()
    {
        var client = CreateClient(_ => JsonResponse(
            """
            {
              "job_id": "job_abc123",
              "status": "downloading",
              "total_size_bytes": 2500000000,
              "downloaded_bytes": 1000
            }
            """));

        var result = await client.StartDownloadAsync("qwen/qwen3-4b", null);

        Assert.Equal("job_abc123", result.JobId);
        Assert.Equal("downloading", result.Status);
        Assert.Equal(2500000000, result.TotalSizeBytes);
    }

    [Fact]
    public async Task StartDownloadAsync_RejectsUntrustedUrl()
    {
        var client = CreateClient(_ => JsonResponse("{}"));

        var exception = await Assert.ThrowsAsync<LocalModelManagementException>(
            () => client.StartDownloadAsync("https://example.com/model.gguf", null));

        Assert.Equal("invalid-model-id", exception.ReasonCode);
    }

    private static LmStudioOpenSourceModelClient CreateClient(
        Func<HttpRequestMessage, HttpResponseMessage> handler,
        ILocalModelSelection? selection = null)
    {
        return CreateClient(
            request => Task.FromResult(handler(request)),
            selection);
    }

    private static LmStudioOpenSourceModelClient CreateClient(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> handler,
        ILocalModelSelection? selection = null)
    {
        return new LmStudioOpenSourceModelClient(
            new HttpClient(new StubHttpMessageHandler(handler))
            {
                BaseAddress = new Uri("http://localhost:1234")
            },
            selection ?? CreateSelection(),
            new FakeRuntimeLauncher());
    }

    private static ILocalModelSelection CreateSelection()
    {
        return new LocalModelSelection(Options.Create(new LocalModelOptions
        {
            Enabled = true,
            Provider = "lmstudio",
            Endpoint = "http://localhost:1234",
            Model = "qwen/qwen3-vl-30b"
        }));
    }

    private static HttpResponseMessage JsonResponse(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private sealed class FakeRuntimeLauncher : ILocalModelRuntimeLauncher
    {
        public Task<bool> EnsureRunningAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return handler(request);
        }
    }
}
