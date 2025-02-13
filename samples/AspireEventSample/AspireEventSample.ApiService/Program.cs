using AspireEventSample.ApiService;
using AspireEventSample.ApiService.Aggregates.Branches;
using AspireEventSample.ApiService.Generated;
using AspireEventSample.ApiService.Projections;
using Microsoft.AspNetCore.Mvc;
using ResultBoxes;
using Scalar.AspNetCore;
using Sekiban.Pure;
using Sekiban.Pure.Command.Handlers;
using Sekiban.Pure.CosmosDb;
using Sekiban.Pure.Documents;
using Sekiban.Pure.OrleansEventSourcing;
using Sekiban.Pure.Postgres;
using Sekiban.Pure.Projectors;
using Sekiban.Pure.Query;
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
builder.UseOrleans(
    config =>
    {
        config.UseDashboard(options => { });
        config.AddMemoryStreams("EventStreamProvider").AddMemoryGrainStorage("EventStreamProvider");
    });

builder.Services.AddHttpContextAccessor();

var domainTypes
    = AspireEventSampleApiServiceDomainTypes.Generate(AspireEventSampleApiServiceEventsJsonContext.Default.Options);
builder.Services.AddSingleton(domainTypes);
// var domainTypes = AspireEventSampleApiServiceDomainTypes.Generate();

if (builder.Configuration.GetSection("Sekiban").GetValue<string>("Database")?.ToLower() == "cosmos")
{
    // Cosmos settings
    builder.Services.AddTransient<IEventWriter, CosmosDbEventWriter>();
    builder.Services.AddTransient<CosmosDbFactory>();
    builder.Services.AddTransient<IEventReader, CosmosDbEventReader>();
    builder.Services.AddTransient<ICosmosMemoryCacheAccessor, CosmosMemoryCacheAccessor>();
    var dbOption =
        SekibanAzureCosmosDbOption.FromConfiguration(
            builder.Configuration.GetSection("Sekiban"),
            builder.Configuration);
    builder.Services.AddSingleton(dbOption);
    builder.Services.AddMemoryCache();

    builder.Services.AddSingleton(SekibanCosmosClientOptions.WithSerializer(domainTypes.JsonSerializerOptions));
} else
{
    // Postgres settings
    builder.Services.AddTransient<IEventWriter, PostgresDbEventWriter>();
    builder.Services.AddTransient<PostgresDbFactory>();
    builder.Services.AddTransient<IPostgresMemoryCacheAccessor, PostgresMemoryCacheAccessor>();
    builder.Services.AddTransient<IEventReader, PostgresDbEventReader>();
    var dbOption =
        SekibanPostgresDbOption.FromConfiguration(builder.Configuration.GetSection("Sekiban"), builder.Configuration);
    builder.Services.AddSingleton(dbOption);
    builder.Services.AddMemoryCache();
}
var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();


var apiRoute = app
    .MapGroup("/api")
    .AddEndpointFilter<ExceptionEndpointFilter>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

string[] summaries =
    ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

app
    .MapGet(
        "/weatherforecast",
        () =>
        {
            var forecast = Enumerable
                .Range(1, 5)
                .Select(
                    index =>
                        new WeatherForecast(
                            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                            Random.Shared.Next(-20, 55),
                            summaries[Random.Shared.Next(summaries.Length)]
                        ))
                .ToArray();
            return forecast;
        })
    .WithName("GetWeatherForecast");

apiRoute
    .MapGet(
        "/getMultiProjection",
        async ([FromServices] IClusterClient clusterClient, [FromServices] IMultiProjectorTypes multiProjectorsType) =>
        {
            var multiProjectorGrain
                = clusterClient.GetGrain<IMultiProjectorGrain>(BranchMultiProjector.GetMultiProjectorName());
            var state = await multiProjectorGrain.GetStateAsync();
            return multiProjectorsType.ToTypedState(state.ToMultiProjectorState());
        })
    .WithName("GetMultiProjection")
    .WithOpenApi();

apiRoute
    .MapGet(
        "/branchProjectionWithAggregate",
        async ([FromServices] IClusterClient clusterClient, [FromServices] IMultiProjectorTypes multiProjectorsType) =>
        {
            var multiProjectorGrain
                = clusterClient.GetGrain<IMultiProjectorGrain>(
                    AggregateListProjector<BranchProjector>.GetMultiProjectorName());
            var state = await multiProjectorGrain.GetStateAsync();
            return multiProjectorsType.ToTypedState(state.ToMultiProjectorState());
        })
    .WithName("branchProjectionWithAggregate")
    .WithDescription(
        "This is failing due to no serializer of ListQueryResult AggregateListProjector<BranchProjector>. Can Still use query")
    .WithOpenApi();

app.MapDefaultEndpoints();

// Add new app.MapPost() method here
apiRoute
    .MapPost(
        "/registerbranch",
        async (
            [FromBody] RegisterBranch command,
            [FromServices] IClusterClient clusterClient,
            [FromServices] IHttpContextAccessor contextAccessor) =>
        {
            var partitionKeyAndProjector =
                new PartitionKeysAndProjector(command.SpecifyPartitionKeys(command), new BranchProjector());
            var aggregateProjectorGrain =
                clusterClient.GetGrain<IAggregateProjectorGrain>(partitionKeyAndProjector.ToProjectorGrainKey());
            var metadataProvider = new FunctionCommandMetadataProvider(
                () =>
                {
                    // return executing user from http context + ip address
                    return (contextAccessor.HttpContext?.User?.Identity?.Name ?? "unknown") +
                        "|" +
                        (contextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "ip unknown");
                });
            return await aggregateProjectorGrain.ExecuteCommandAsync(
                command,
                OrleansCommandMetadata.FromCommandMetadata(metadataProvider.GetMetadata()));
        })
    .WithName("RegisterBranch")
    .WithOpenApi();
apiRoute
    .MapPost(
        "/changebranchname",
        async (
            [FromBody] ChangeBranchName command,
            [FromServices] IClusterClient clusterClient,
            [FromServices] IHttpContextAccessor contextAccessor) =>
        {
            var partitionKeyAndProjector =
                new PartitionKeysAndProjector(command.SpecifyPartitionKeys(command), new BranchProjector());
            var aggregateProjectorGrain =
                clusterClient.GetGrain<IAggregateProjectorGrain>(partitionKeyAndProjector.ToProjectorGrainKey());
            var metadataProvider = new FunctionCommandMetadataProvider(
                () =>
                {
                    // return executing user from http context + ip address
                    return (contextAccessor.HttpContext?.User?.Identity?.Name ?? "unknown") +
                        "|" +
                        (contextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "ip unknown");
                });
            return await aggregateProjectorGrain.ExecuteCommandAsync(
                command,
                OrleansCommandMetadata.FromCommandMetadata(metadataProvider.GetMetadata()));
        })
    .WithName("ChangeBranchName")
    .WithOpenApi();

apiRoute
    .MapGet(
        "/branch/{branchId}",
        async (
            [FromRoute] Guid branchId,
            [FromServices] IClusterClient clusterClient,
            [FromServices] DomainTypes sekibanTypes) =>
        {
            var partitionKeyAndProjector =
                new PartitionKeysAndProjector(PartitionKeys<BranchProjector>.Existing(branchId), new BranchProjector());
            var aggregateProjectorGrain =
                clusterClient.GetGrain<IAggregateProjectorGrain>(partitionKeyAndProjector.ToProjectorGrainKey());
            var state = await aggregateProjectorGrain.GetStateAsync();
            return sekibanTypes.AggregateTypes.ToTypedPayload(state.ToAggregate()).UnwrapBox();
        })
    .WithName("GetBranch")
    .WithOpenApi();

apiRoute
    .MapGet(
        "/branch/{branchId}/reload",
        async (
            [FromRoute] Guid branchId,
            [FromServices] IClusterClient clusterClient,
            [FromServices] DomainTypes sekibanTypes) =>
        {
            var partitionKeyAndProjector =
                new PartitionKeysAndProjector(PartitionKeys<BranchProjector>.Existing(branchId), new BranchProjector());
            var aggregateProjectorGrain =
                clusterClient.GetGrain<IAggregateProjectorGrain>(partitionKeyAndProjector.ToProjectorGrainKey());
            var state = await aggregateProjectorGrain.RebuildStateAsync();
            return sekibanTypes.AggregateTypes.ToTypedPayload(state.ToAggregate()).UnwrapBox();
        })
    .WithName("GetBranchReload")
    .WithOpenApi();

apiRoute
    .MapGet(
        "/branchExists/{nameContains}",
        async (
            [FromRoute] string nameContains,
            [FromServices] IClusterClient clusterClient,
            [FromServices] IQueryTypes queryTypes) =>
        {
            var multiProjectorGrain
                = clusterClient.GetGrain<IMultiProjectorGrain>(BranchMultiProjector.GetMultiProjectorName());
            var result = await multiProjectorGrain.QueryAsync(new BranchExistsQuery(nameContains));
            return queryTypes.ToTypedQueryResult(result.ToQueryResultGeneral()).UnwrapBox();
        })
    .WithName("BranchExists")
    .WithOpenApi();

apiRoute
    .MapGet(
        "/searchBranches",
        async (
            [FromQuery] string nameContains,
            [FromServices] IClusterClient clusterClient,
            [FromServices] IQueryTypes queryTypes) =>
        {
            var multiProjectorGrain
                = clusterClient.GetGrain<IMultiProjectorGrain>(BranchMultiProjector.GetMultiProjectorName());
            var result = await multiProjectorGrain.QueryAsync(new SimpleBranchListQuery(nameContains));
            return queryTypes.ToTypedListQueryResult(result.ToListQueryResultGeneral()).UnwrapBox();
        })
    .WithName("SearchBranches")
    .WithOpenApi();
apiRoute
    .MapGet(
        "/searchBranches2",
        async (
            [FromQuery] string nameContains,
            [FromServices] IClusterClient clusterClient,
            [FromServices] IQueryTypes queryTypes) =>
        {
            var multiProjectorGrain
                = clusterClient.GetGrain<IMultiProjectorGrain>(
                    AggregateListProjector<BranchProjector>.GetMultiProjectorName());
            var result = await multiProjectorGrain.QueryAsync(new BranchQueryFromAggregateList(nameContains));
            return queryTypes.ToTypedListQueryResult(result.ToListQueryResultGeneral()).UnwrapBox();
        })
    .WithName("SearchBranches2")
    .WithOpenApi();

app.Run();