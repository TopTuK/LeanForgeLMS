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
    //.WithDataVolume("quill_postgres") // for debug do not save datavolume
    .AddDatabase("leanforge");

var identityService = builder
    .AddProject<Projects.LF_IdentityService>("lf-identityservice")
    .WithReference(postgres)
    .WaitFor(postgres);

var webApi = builder
    .AddProject<Projects.LF_WebApi>("lf-webapi")
    .WithReference(identityService)
    .WaitFor(identityService);

builder
    .AddViteApp("lf-webapp", "../lf.webapp")
    .WithNpm()
    .WithHttpEndpoint(port: 5173, env: "PORT")
    .WaitFor(webApi);

builder
    .Build()
    .Run();
