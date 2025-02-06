using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using AspireEventSample.ApiService.Aggregates.Branches;
using AspireEventSample.ApiService.Aggregates.Carts;
using ResultBoxes;
using Sekiban.Pure.Documents;
using Sekiban.Pure.Events;
using Sekiban.Pure.Exception;
using Sekiban.Pure.Extensions;
using Sekiban.Pure.Projectors;

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(EventDocumentCommon))]
[JsonSerializable(typeof(EventDocumentCommon[]))]
[JsonSerializable(typeof(EventDocument<AspireEventSample.ApiService.Aggregates.Branches.BranchCreated>))]
[JsonSerializable(typeof(AspireEventSample.ApiService.Aggregates.Branches.BranchCreated))]
[JsonSerializable(typeof(EventDocument<AspireEventSample.ApiService.Aggregates.Branches.BranchNameChanged>))]
[JsonSerializable(typeof(AspireEventSample.ApiService.Aggregates.Branches.BranchNameChanged))]
[JsonSerializable(typeof(EventDocument<AspireEventSample.ApiService.Aggregates.Carts.ShoppingCartCreated>))]
[JsonSerializable(typeof(AspireEventSample.ApiService.Aggregates.Carts.ShoppingCartCreated))]
[JsonSerializable(typeof(EventDocument<AspireEventSample.ApiService.Aggregates.Carts.ShoppingCartItemAdded>))]
[JsonSerializable(typeof(AspireEventSample.ApiService.Aggregates.Carts.ShoppingCartItemAdded))]
[JsonSerializable(typeof(EventDocument<AspireEventSample.ApiService.Aggregates.Carts.ShoppingCartPaymentProcessed>))]
[JsonSerializable(typeof(AspireEventSample.ApiService.Aggregates.Carts.ShoppingCartPaymentProcessed))]
public partial class AspireEventSampleApiServiceEventsJsonContext : JsonSerializerContext
{
}

public record BranchMultiProjector(ImmutableDictionary<Guid, BranchMultiProjector.BranchRecord> Branches) : IMultiProjector<BranchMultiProjector>
{
    public record BranchRecord(Guid BranchId, string BranchName);

    public ResultBox<BranchMultiProjector> Project(BranchMultiProjector payload, IEvent ev)
        => ev.GetPayload() switch
        {
            BranchCreated branchCreated => payload with
            {
                Branches = payload.Branches.Add(
                    ev.PartitionKeys.AggregateId,
                    new BranchRecord(ev.PartitionKeys.AggregateId, branchCreated.Name))
            },
            BranchNameChanged branchNameChanged => payload.Branches.TryGetValue(ev.PartitionKeys.AggregateId, out var existingBranch)
                ? payload with
                {
                    Branches = payload.Branches.SetItem(
                        ev.PartitionKeys.AggregateId,
                        existingBranch with { BranchName = branchNameChanged.Name })
                }
                : payload,
            _ => payload
        };

    public static BranchMultiProjector GenerateInitialPayload() => new(ImmutableDictionary<Guid, BranchRecord>.Empty);
} 

public class AspireApiServiceMultiProjectorType : IMultiProjectorsType
{
    public ResultBox<IMultiProjectorCommon> Project(IMultiProjectorCommon multiProjector, IEvent ev)
        => multiProjector switch
        {
            BranchMultiProjector branchMultiProjector => branchMultiProjector.Project(branchMultiProjector, ev)
                .Remap(mp => (IMultiProjectorCommon)mp),
            _ => new ApplicationException(multiProjector.GetType().Name)
        };
}