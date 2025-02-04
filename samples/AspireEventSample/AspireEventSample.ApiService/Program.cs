using AspireEventSample.ApiService.Aggregates.Branches;
using AspireEventSample.ApiService.Aggregates.ReadModel;
using AspireEventSample.ApiService.Generated;
using AspireEventSample.ApiService.Grains;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using ResultBoxes;
using Scalar.AspNetCore;
using Sekiban.Pure.Command.Handlers;
using Sekiban.Pure.CosmosDb;
using Sekiban.Pure.Documents;
using Sekiban.Pure.Events;
using Sekiban.Pure.OrleansEventSourcing;
using Sekiban.Pure.Types;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// orleans related integrations
builder.AddKeyedAzureTableClient("clustering");
builder.AddKeyedAzureBlobClient("grain-state");
builder.UseOrleans(config =>
{
    config.UseDashboard(options => { });
    config.AddMemoryStreams("EventStreamProvider").AddMemoryGrainStorage("EventStreamProvider");
});

builder.Services.AddSingleton(new SekibanTypeConverters(new AspireEventSampleApiServiceAggregateTypes(),
    new AspireEventSampleApiServiceEventTypes(), new AspireEventSampleApiServiceAggregateProjectorSpecifier()));

builder.Services.AddHttpContextAccessor();

builder.Services.AddSingleton(new SekibanCosmosClientOptions());
// builder.Services.AddSingleton(new SekibanCosmosClientOptions()
// {
//     ClientOptions = new CosmosClientOptions()
//     {
//         Serializer = new SourceGenCosmosSerializer(AspireEventSampleApiServiceEventsJsonContext.Default.Options),
//         AllowBulkExecution = true,
//         MaxRetryAttemptsOnRateLimitedRequests = 200,
//         ConnectionMode = ConnectionMode.Gateway,
//         GatewayModeMaxConnectionLimit = 200
//     }
// });
builder.Services.AddTransient<IEventWriter, CosmosDbEventWriter>();
builder.Services.AddTransient<CosmosDbFactory>();
builder.Services.AddTransient<ICosmosMemoryCacheAccessor, CosmosMemoryCacheAccessor>();
builder.Services.AddTransient<IEventTypes, AspireEventSampleApiServiceEventTypes>();
var dbOption = SekibanAzureCosmosDbOption.FromConfiguration(builder.Configuration.GetSection("Sekiban"), builder.Configuration);
builder.Services.AddSingleton(dbOption);
builder.Services.AddTransient<IEventReader, CosmosDbEventReader>();
builder.Services.AddMemoryCache();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

string[] summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.MapDefaultEndpoints();

// Add new app.MapPost() method here
app.MapPost("/registerbranch", async ([FromBody]RegisterBranch command, [FromServices]IClusterClient clusterClient, [FromServices] IHttpContextAccessor contextAccessor) =>
    {
    var partitionKeyAndProjector = new PartitionKeysAndProjector(command.SpecifyPartitionKeys(command), new BranchProjector());
    var aggregateProjectorGrain = clusterClient.GetGrain<IAggregateProjectorGrain>(partitionKeyAndProjector.ToProjectorGrainKey());
    CommandMetadataProvider metadataProvider = new CommandMetadataProvider(() =>
    {
        // return executing user from http context + ip address
        return (contextAccessor.HttpContext?.User?.Identity?.Name ?? "unknown") + "|" + (contextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "ip unknown");
    });
    return await aggregateProjectorGrain.ExecuteCommandAsync(command, OrleansCommandMetadata.FromCommandMetadata(metadataProvider.GetMetadata()));
    
}).WithName("RegisterBranch")
    .WithOpenApi();
app.MapPost("/changebranchname", async ([FromBody]ChangeBranchName command, [FromServices]IClusterClient clusterClient, [FromServices] IHttpContextAccessor contextAccessor) =>
    {
    var partitionKeyAndProjector = new PartitionKeysAndProjector(command.SpecifyPartitionKeys(command), new BranchProjector());
    var aggregateProjectorGrain = clusterClient.GetGrain<IAggregateProjectorGrain>(partitionKeyAndProjector.ToProjectorGrainKey());
    CommandMetadataProvider metadataProvider = new CommandMetadataProvider(() =>
    {
        // return executing user from http context + ip address
        return (contextAccessor.HttpContext?.User?.Identity?.Name ?? "unknown") + "|" + (contextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "ip unknown");
    });
    return await aggregateProjectorGrain.ExecuteCommandAsync(command, OrleansCommandMetadata.FromCommandMetadata(metadataProvider.GetMetadata()));
}).WithName("ChangeBranchName")
    .WithOpenApi();

app.MapGet("/branch/{branchId}", async ([FromRoute]Guid branchId, [FromServices]IClusterClient clusterClient, [FromServices] SekibanTypeConverters typeConverters) =>
    {
        var partitionKeyAndProjector = new PartitionKeysAndProjector(PartitionKeys<BranchProjector>.Existing(branchId), new BranchProjector());
        var aggregateProjectorGrain = clusterClient.GetGrain<IAggregateProjectorGrain>(partitionKeyAndProjector.ToProjectorGrainKey());
        var state = await aggregateProjectorGrain.GetStateAsync();
        return typeConverters.AggregateTypes.ToTypedPayload(state.ToAggregate()).UnwrapBox();
    }).WithName("GetBranch")
    .WithOpenApi();

app.MapGet("/branch/{branchId}/reload", async ([FromRoute]Guid branchId, [FromServices]IClusterClient clusterClient, [FromServices] SekibanTypeConverters typeConverters) =>
    {
        var partitionKeyAndProjector = new PartitionKeysAndProjector(PartitionKeys<BranchProjector>.Existing(branchId), new BranchProjector());
        var aggregateProjectorGrain = clusterClient.GetGrain<IAggregateProjectorGrain>(partitionKeyAndProjector.ToProjectorGrainKey());
        var state = await aggregateProjectorGrain.RebuildStateAsync();
        return typeConverters.AggregateTypes.ToTypedPayload(state.ToAggregate()).UnwrapBox();
    }).WithName("GetBranchReload")
    .WithOpenApi();


app.Run();