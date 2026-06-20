using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using WorkplaceIQ.AspNet;
using WorkplaceIQ.AspNet.Data;
using WorkplaceIQ.AspNet.Files;
using WorkplaceIQ.Web;
using WorkplaceIQ.Web.Hubs;
using WorkplaceIQ.Web.SignalFlow.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
builder.Services.Configure<FileStorageOptions>(
    builder.Configuration.GetSection("WorkplaceIQ:Storage"));
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(
        Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtectionKeys")));

var connectionString = builder.Configuration.GetConnectionString("WorkplaceIQ");
if (!string.IsNullOrEmpty(connectionString) && connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddWorkplaceIqAspNet(options =>
        options.UseNpgsql(connectionString));
}
else
{
    builder.Services.AddWorkplaceIqAspNet(options =>
        options.UseSqlite(connectionString ?? "Data Source=workplaceiq.db;Pooling=False"));
}

builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics.AddMeter("WorkplaceIQ"));

// SignalFlow services
var sigConfig = new WorkplaceIQ.Web.SignalFlow.Models.PipelineConfig();
builder.Configuration.GetSection("SignalFlow").Bind(sigConfig);
builder.Services.AddSingleton(sigConfig);
builder.Services.AddSingleton<CategoryCentroidTracker>();
builder.Services.AddSingleton<IVectorStore, InMemoryVectorStore>();
builder.Services.AddSingleton<ConfigLoader>();
builder.Services.AddScoped<IFeedbackService, FeedbackService>();
builder.Services.AddScoped<PipelineOrchestrator>();
builder.Services.AddSingleton<PipelineBackgroundService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<PipelineBackgroundService>());

var endpoint = Environment.GetEnvironmentVariable("AI_ENDPOINT") ?? sigConfig.Endpoint;
var modelName = Environment.GetEnvironmentVariable("AI_MODEL") ?? sigConfig.LlmModel;
var embeddingModel = Environment.GetEnvironmentVariable("AI_EMBEDDING_MODEL") ?? sigConfig.EmbeddingModel;
var apiKey = Environment.GetEnvironmentVariable("AI_API_KEY") ?? sigConfig.ApiKey ?? "no-auth";

var openAIClient = new OpenAIClient(
    new ApiKeyCredential(apiKey),
    new OpenAIClientOptions { Endpoint = new Uri(endpoint) });

builder.Services.AddChatClient(openAIClient.GetChatClient(modelName).AsIChatClient());
builder.Services.AddEmbeddingGenerator<string, Embedding<float>>(
    _ => openAIClient.GetEmbeddingClient(embeddingModel).AsIEmbeddingGenerator());
builder.Services.AddTransient<EmbeddingService>();

var app = builder.Build();

app.MapDefaultEndpoints();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<WorkplaceIqDbContext>();
    dbContext.Database.EnsureCreated();

    if (app.Environment.IsDevelopment())
    {
        await DemoDataSeeder.SeedAsync(scope.ServiceProvider);
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapHub<PipelineHub>("/hubs/pipeline");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
