var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.AspireEventSample_ApiService>("apiservice");

builder.AddProject<Projects.AspireEventSample_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
