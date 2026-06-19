using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using WorkplaceIQ.AspNet;
using WorkplaceIQ.AspNet.Data;
using WorkplaceIQ.AspNet.Files;
using WorkplaceIQ.Web;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Add services to the container.
builder.Services.AddControllersWithViews();
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
        options.UseSqlite(connectionString ?? "Data Source=workplaceiq.db"));
}

builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics.AddMeter("WorkplaceIQ"));

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
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
