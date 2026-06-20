using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.InMemory;
using WorkplaceIQ.AspNet;
using WorkplaceIQ.AspNet.Data;
using WorkplaceIQ.AspNet.Files;
using WorkplaceIQ.Web;
using WorkplaceIQ.Web.Hubs;
using WorkplaceIQ.Web.SignalFlow.Models;
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

// Storage:Provider selects the EF Core provider.
// Supported: sqlite, pgvector, sqlserver, inmemory.
// Connection strings are read from the matching key under ConnectionStrings.
var provider = builder.Configuration.GetValue<string>("Storage:Provider") ?? "sqlite";

var connectionString = provider.ToLowerInvariant() switch
{
    "sqlite" => builder.Configuration.GetConnectionString("Sqlite")
        ?? "Data Source=App_Data/workplaceiq.db;Pooling=False",
    "pgvector" => builder.Configuration.GetConnectionString("Npgsql")
        ?? builder.Configuration.GetConnectionString("PgVector"),
    "sqlserver" => builder.Configuration.GetConnectionString("SqlServer"),
    "inmemory" => null,
    var p => throw new InvalidOperationException(
        $"Unsupported storage provider '{p}'. Supported: sqlite, pgvector, sqlserver, inmemory")
};

switch (provider.ToLowerInvariant())
{
    case "sqlite":
        builder.Services.AddWorkplaceIqAspNet(options =>
            options.UseSqlite(connectionString));
        break;
    case "pgvector":
        builder.Services.AddWorkplaceIqAspNet(options =>
            options.UseNpgsql(connectionString));
        break;
    case "sqlserver":
        builder.Services.AddWorkplaceIqAspNet(options =>
            options.UseSqlServer(connectionString));
        break;
    case "inmemory":
        builder.Services.AddWorkplaceIqAspNet(options =>
            options.UseInMemoryDatabase("workplaceiq"));
        break;
}

builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics.AddMeter("WorkplaceIQ"));

// SignalFlow services
var sigConfig = new WorkplaceIQ.Web.SignalFlow.Models.PipelineConfig();
builder.Configuration.GetSection("SignalFlow").Bind(sigConfig);
builder.Services.AddSingleton(sigConfig);
builder.Services.AddSingleton<CategoryCentroidTracker>();

// VectorStore provider dispatch — matches Storage:Provider used by EF Core above.
var vectorStore = CreateVectorStore(provider);
builder.Services.AddSingleton(vectorStore);

// Build the typed collection once with the embedding dimension from config.
// The dimension comes from engine.md → PipelineConfig.EmbeddingDimension (default 768).
// If you change the embedding model, update engine.md accordingly.
var collection = vectorStore.GetCollection<string, SignalFlowVectorEntry>(
    SignalFlowVectorEntry.CollectionName,
    SignalFlowVectorSchema.CreateEntryDefinition(sigConfig.EmbeddingDimension));
builder.Services.AddSingleton(collection);

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

static VectorStore CreateVectorStore(string provider) => provider.ToLowerInvariant() switch
{
    "inmemory" => new InMemoryVectorStore(),
    "sqlite" => throw new NotSupportedException(
        "SQLite vector store requires the Microsoft.SemanticKernel.Connectors.SqliteVec package."),
    "pgvector" => throw new NotSupportedException(
        "pgvector vector store requires the Microsoft.SemanticKernel.Connectors.PgVector package."),
    "sqlserver" => throw new NotSupportedException(
        "SQL Server vector store requires the Microsoft.SemanticKernel.Connectors.SqlServer package."),
    var p => throw new InvalidOperationException(
        $"Unsupported vector store provider '{p}'. Supported: inmemory, sqlite, pgvector, sqlserver")
};
