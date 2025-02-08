using System.Collections.Immutable;
using AspireEventSample.ApiService.Aggregates.Branches;
using Orleans;
using ResultBoxes;
using Sekiban.Pure.Events;
using Sekiban.Pure.Projectors;
using Sekiban.Pure.Query;

namespace AspireEventSample.ApiService.Projections;

[GenerateSerializer]
public record BranchMultiProjector([property: Id(1)] ImmutableDictionary<Guid, BranchMultiProjector.BranchRecord> Branches) : IMultiProjector<BranchMultiProjector>
{
    [GenerateSerializer]
    public record BranchRecord([property: Id(1)] Guid BranchId, [property: Id(2)] string BranchName);

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

public record BranchExistsQuery(string NameContains) : IMultiProjectionQuery<BranchMultiProjector, BranchExistsQuery, bool>
{

    public static ResultBox<bool> HandleQuery(MultiProjectionState<BranchMultiProjector> projection, BranchExistsQuery query, IQueryContext context) => projection.Payload.Branches.Values.Any(b => b.BranchName.Contains(query.NameContains));
}

public record SimpleBranchListQuery(string NameContain)
    : IMultiProjectionListQuery<BranchMultiProjector, SimpleBranchListQuery, BranchMultiProjector.BranchRecord>
{
    public static ResultBox<IEnumerable<BranchMultiProjector.BranchRecord>> HandleFilter(MultiProjectionState<BranchMultiProjector> projection, SimpleBranchListQuery query, IQueryContext context)
    {
        return ResultBox.Ok(projection.Payload.Branches.Values.Where(b => b.BranchName.Contains(query.NameContain)));
    }

    public static ResultBox<IEnumerable<BranchMultiProjector.BranchRecord>> HandleSort(IEnumerable<BranchMultiProjector.BranchRecord> filteredList, SimpleBranchListQuery query, IQueryContext context)
    {
        return filteredList.OrderBy(m => m.BranchId).AsEnumerable().ToResultBox();
    }
}