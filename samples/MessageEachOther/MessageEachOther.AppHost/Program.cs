using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var storage = builder.AddAzureStorage("azurestorage")
    .RunAsEmulator(r => r.WithImage("azure-storage/azurite", "3.33.0"));
var clusteringTable = storage.AddTables("orleans-sekiban-clustering");
var grainStorage = storage.AddBlobs("orleans-sekiban-grain-state");

var postgresPassword = builder.AddParameter("postgres-password", true);
var postgres = builder
    .AddPostgres("orleansSekibanPostgres", password: postgresPassword)
    .WithDataVolume("orleansSekibanPostgresData")
    .WithPgAdmin()
    .AddDatabase("SekibanPostgres");

var orleans = builder.AddOrleans("default")
    .WithClustering(clusteringTable)
    .WithGrainStorage("Default", grainStorage);


var apiService = builder.AddProject<MessageEachOther_ApiService>("apiservice")
    // .WithEndpoint("https", annotation => annotation.IsProxied = false)
    .WithReference(postgres)
    .WithReference(orleans)
    .WithReplicas(2);

builder.AddProject<Projects.MessageEachOther_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
