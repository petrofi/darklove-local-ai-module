var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/api/health", () => new
{
    status = "running",
    project = "Darklove Local AI Module",
    version = "0.1.0",
    module = "backend-api"
});

app.MapPost("/api/emotion/analyze", (EmotionAnalysisRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.UserText))
    {
        return Results.BadRequest(new
        {
            error = "UserText cannot be empty."
        });
    }

    var text = request.UserText.ToLower();

    var sadnessKeywords = new[] { "üzgün", "yalnız", "yorgun", "kırgın", "boşluk", "ağlamak", "mutsuz" };
    var anxietyKeywords = new[] { "kaygı", "stres", "panik", "korku", "endişe", "gergin" };
    var hopeKeywords = new[] { "umut", "iyi", "başaracağım", "güçlü", "devam", "toparlanmak" };
    var angerKeywords = new[] { "sinir", "öfke", "bıktım", "kızgın", "dayanamıyorum" };

    var sadnessScore = sadnessKeywords.Count(word => text.Contains(word));
    var anxietyScore = anxietyKeywords.Count(word => text.Contains(word));
    var hopeScore = hopeKeywords.Count(word => text.Contains(word));
    var angerScore = angerKeywords.Count(word => text.Contains(word));

    var scores = new Dictionary<string, int>
    {
        { "sadness", sadnessScore },
        { "anxiety", anxietyScore },
        { "hope", hopeScore },
        { "anger", angerScore }
    };

    var detectedEmotion = scores.OrderByDescending(x => x.Value).First().Key;
    var highestScore = scores[detectedEmotion];

    if (highestScore == 0)
    {
        detectedEmotion = "neutral";
    }

    var confidence = highestScore == 0
        ? 0.40
        : Math.Min(0.95, 0.50 + highestScore * 0.15);

    var motivationMessage = detectedEmotion switch
    {
        "sadness" => "Bugün zor geçiyor olabilir ama yalnız değilsin. Küçük bir nefes molası verip kendine nazik davranabilirsin.",
        "anxiety" => "Şu an kaygılı hissetmen anlaşılır. Derin bir nefes al, her şeyi aynı anda çözmek zorunda değilsin.",
        "hope" => "İçindeki umut çok değerli. Küçük adımlarla ilerlemeye devam edersen güçlendiğini göreceksin.",
        "anger" => "Öfkenin altında yorgunluk veya kırgınlık olabilir. Biraz durup kendine alan açman iyi gelebilir.",
        _ => "Duygularını fark etmen bile önemli bir adım. Bugün kendine karşı biraz daha yumuşak olmayı deneyebilirsin."
    };

    var needsSupportWarning =
        text.Contains("kendime zarar") ||
        text.Contains("yaşamak istemiyorum") ||
        text.Contains("intihar") ||
        text.Contains("ölmek istiyorum");

    var response = new EmotionAnalysisResponse
    {
        DetectedEmotion = detectedEmotion,
        Confidence = confidence,
        MotivationMessage = motivationMessage,
        NeedsSupportWarning = needsSupportWarning
    };

    return Results.Ok(response);
});

app.Run();

public class EmotionAnalysisRequest
{
    public string UserText { get; set; } = string.Empty;
}

public class EmotionAnalysisResponse
{
    public string DetectedEmotion { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public string MotivationMessage { get; set; } = string.Empty;
    public bool NeedsSupportWarning { get; set; }
}

