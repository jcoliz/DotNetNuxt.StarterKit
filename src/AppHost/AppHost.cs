using Aspire.Hosting;

DotNetEnv.Env.Load();
var builder = DistributedApplication.CreateBuilder(args);
var appInsightsConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];

var postgresDb = builder.AddPostgres("postgres", port: 5501)
    .AddDatabase("starterkit");

var backend = builder.AddProject<Projects.DotNetNuxt_StarterKit_BackEnd>("backend")
    .WithReference(postgresDb, connectionName: "DotNetNuxt.StarterKit.Data.Postgres")
    .WithEnvironment("APPLICATIONINSIGHTS_CONNECTION_STRING", appInsightsConnectionString ?? "")
    .WaitFor(postgresDb);

builder.AddJavaScriptApp("frontend-nuxt", "../FrontEnd.Nuxt")
    .WithPnpm()
    .WithReference(backend)
    .WithEnvironment("NUXT_PUBLIC_API_BASE_URL", backend.GetEndpoint("http"))
    .WithEnvironment("NUXT_PUBLIC_SOLUTION_VERSION", "development")
    .WithHttpEndpoint(port: 5284, env: "PORT")
    .WithEnvironment("NUXT_PUBLIC_APPLICATION_INSIGHTS_CONNECTION_STRING", appInsightsConnectionString ?? "")
    .WithExternalHttpEndpoints();
//    .WithHttpHealthCheck(path: "/status", statusCode: 200);

builder.Build().Run();
