var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .AddDatabase("workplaceiq");

var minio = builder.AddContainer("minio", "minio/minio:latest")
    .WithEnvironment("MINIO_ROOT_USER", "workplaceiq")
    .WithEnvironment("MINIO_ROOT_PASSWORD", "workplaceiq-secret")
    .WithVolume("minio-data", "/data")
    .WithHttpEndpoint(port: 9000, targetPort: 9000, name: "api")
    .WithHttpEndpoint(port: 9001, targetPort: 9001, name: "console")
    .WithArgs("server", "/data", "--console-address", ":9001");

var web = builder.AddProject<Projects.WorkplaceIQ_Web>("workplaceiq-web")
    .WithReference(postgres)
    .WithEnvironment("WorkplaceIQ__Storage__Provider", "MinIO")
    .WithEnvironment("WorkplaceIQ__Storage__Endpoint", minio.GetEndpoint("api"))
    .WithEnvironment("WorkplaceIQ__Storage__BucketName", "workplaceiq-files")
    .WithEnvironment("WorkplaceIQ__Storage__AccessKey", "workplaceiq")
    .WithEnvironment("WorkplaceIQ__Storage__SecretKey", "workplaceiq-secret")
    .WithEnvironment("WorkplaceIQ__Storage__UseSsl", "false")
    .WaitFor(postgres)
    .WaitFor(minio);

builder.Build().Run();
