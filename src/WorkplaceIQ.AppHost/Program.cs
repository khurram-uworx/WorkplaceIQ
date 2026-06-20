var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithImage("pgvector/pgvector")
    .WithImageTag("pg16");

var postgresDb = postgres.AddDatabase(name: "Npgsql", databaseName: "workplaceiq");

var minio = builder.AddContainer("minio", "minio/minio:latest")
    .WithEnvironment("MINIO_ROOT_USER", "workplaceiq")
    .WithEnvironment("MINIO_ROOT_PASSWORD", "workplaceiq-secret")
    .WithVolume("minio-data", "/data")
    .WithHttpEndpoint(port: 9000, targetPort: 9000, name: "api")
    .WithHttpEndpoint(port: 9001, targetPort: 9001, name: "console")
    .WithArgs("server", "/data", "--console-address", ":9001");

var web = builder.AddProject<Projects.WorkplaceIQ_Web>("workplaceiq-web")
    .WithReference(postgresDb)
    .WithEnvironment("ConnectionStrings__PgVector",
        postgresDb.Resource.ConnectionStringExpression)
    .WithEnvironment("Storage__Provider", "pgvector")
    .WithEnvironment("WorkplaceIQ__Storage__Provider", "MinIO")
    .WithEnvironment("WorkplaceIQ__Storage__Endpoint", minio.GetEndpoint("api"))
    .WithEnvironment("WorkplaceIQ__Storage__BucketName", "workplaceiq-files")
    .WithEnvironment("WorkplaceIQ__Storage__AccessKey", "workplaceiq")
    .WithEnvironment("WorkplaceIQ__Storage__SecretKey", "workplaceiq-secret")
    .WithEnvironment("WorkplaceIQ__Storage__UseSsl", "false")
    .WaitFor(postgresDb)
    .WaitFor(minio);

builder.Build().Run();
