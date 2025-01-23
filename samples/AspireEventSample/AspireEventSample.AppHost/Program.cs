var builder = DistributedApplication.CreateBuilder(args);

var storage = builder.AddAzureStorage("azurestorage").RunAsEmulator(r => r.WithImage("azure-storage/azurite", "3.33.0" ) );
var clusteringTable = storage.AddTables("clustering");
var grainStorage = storage.AddBlobs("grain-state");

var orleans = builder.AddOrleans("default")
    .WithClustering(clusteringTable)
    .WithGrainStorage("Default", grainStorage);


var apiService = builder.AddProject<Projects.AspireEventSample_ApiService>("apiservice")
    .WithEndpoint("https", annotation => annotation.IsProxied = false)
    .WithReference(orleans);

builder.AddProject<Projects.AspireEventSample_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
