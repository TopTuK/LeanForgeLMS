var builder = DistributedApplication.CreateBuilder(args);

var pgUser = builder.AddParameter("postgres-user", "leanforge");
var pgPassword = builder.AddParameter("postgres-password", "leanforge", secret: true);
var postgres = builder
    .AddPostgres("postgres", port: 5432)
    .WithEnvironment("POSTGRES_USER", "leanforge") // for scripts
    .WithEnvironment("POSTGRES_PASSWORD", "leanforge") // for scripts
    .WithEnvironment("POSTGRES_DB", "leanforge") // for scripts
    .WithUserName(pgUser)
    .WithPassword(pgPassword)
    //.WithDataVolume("leanforge-db") // for debug do not save datavolume
    .AddDatabase("leanforge");

var minioUser = builder.AddParameter("minio-user", "minioadmin");
var minioPassword = builder.AddParameter("minio-password", "minioadmin", secret: true);

var minio = builder.AddMinioContainer("minio", minioUser, minioPassword, port: 9000);

var identityService = builder
    .AddProject<Projects.LF_IdentityService>("lf-identityservice")
    .WithReference(postgres)
    .WaitFor(postgres);

var webApp = builder
    .AddViteApp("lf-webapp", "../lf.webapp")
    .WithNpm()
    .WithHttpEndpoint(port: 5173, env: "PORT");

builder
    .AddProject<Projects.LF_WebApi>("lf-webapi")
    .WithReference(identityService)
    .WaitFor(identityService)
    .WithReference(webApp)
    .WaitFor(webApp)
    .WithReference(minio)
    .WaitFor(minio);

builder.AddProject<Projects.LF_CourseService>("lf-courseservice");

builder
    .Build()
    .Run();
