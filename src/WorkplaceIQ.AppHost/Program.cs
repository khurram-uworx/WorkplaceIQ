using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.WorkplaceIQ_Web>("workplaceiq-web");

builder.Build().Run();
