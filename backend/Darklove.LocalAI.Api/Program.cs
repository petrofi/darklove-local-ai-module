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

app.Run();
