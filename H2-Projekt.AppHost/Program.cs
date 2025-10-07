var builder = DistributedApplication.CreateBuilder(args);


var apiService = builder.AddProject<Projects.API>("apiservice");

builder
    .AddProject<Projects.Blazor>("frontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
