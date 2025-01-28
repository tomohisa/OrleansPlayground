using AspireEventSample.ApiService.Aggregates.Branches;
using AspireEventSample.ApiService.Aggregates.ReadModel;
using AspireEventSample.ApiService.Generated;
using AspireEventSample.ApiService.Grains;
using Microsoft.AspNetCore.Mvc;
using ResultBoxes;
using Scalar.AspNetCore;
using Sekiban.Pure.Documents;
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
builder.Services.AddTransient<IEntityWriter<BranchEntity>, BranchEntityWriter>();

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
app.MapPost("/registerbranch", async ([FromBody]RegisterBranch command, [FromServices]IClusterClient clusterClient) =>
    {
    var partitionKeyAndProjector = new PartitionKeysAndProjector(command.SpecifyPartitionKeys(command), new BranchProjector());
    var aggregateProjectorGrain = clusterClient.GetGrain<IAggregateProjectorGrain>(partitionKeyAndProjector.ToProjectorGrainKey());
    OrleansCommand orleansCommand = new OrleansCommand(command.ToString());
    return await aggregateProjectorGrain.ExecuteCommandAsync(command);
    
}).WithName("RegisterBranch")
    .WithOpenApi();
app.MapPost("/changebranchname", async ([FromBody]ChangeBranchName command, [FromServices]IClusterClient clusterClient) =>
    {
    var partitionKeyAndProjector = new PartitionKeysAndProjector(command.SpecifyPartitionKeys(command), new BranchProjector());
    var aggregateProjectorGrain = clusterClient.GetGrain<IAggregateProjectorGrain>(partitionKeyAndProjector.ToProjectorGrainKey());
    OrleansCommand orleansCommand = new OrleansCommand(command.ToString());
    return await aggregateProjectorGrain.ExecuteCommandAsync(command);
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


app.Run();