using Sekiban.Pure.Aggregates;
using Sekiban.Pure.OrleansEventSourcing;

namespace AspireEventSample.ApiService.Grains;

public static class OrleansAggregateExtensions
{
    public static OrleansAggregate ToOrleansAggregate(this IAggregate aggregate)
    {
        return new OrleansAggregate(aggregate.GetPayload(), aggregate.PartitionKeys.ToOrleansPartitionKeys(), aggregate.Version,
            aggregate.LastSortableUniqueId, aggregate.ProjectorVersion, aggregate.ProjectorTypeName, aggregate.PayloadTypeName);
    }
    public static Aggregate ToAggregate(this OrleansAggregate oAggregate)
        => new Aggregate(oAggregate.Payload, oAggregate.PartitionKeys.ToPartitionKeys(), oAggregate.Version, oAggregate.LastSortableUniqueId, oAggregate.ProjectorVersion, oAggregate.ProjectorTypeName, oAggregate.PayloadTypeName);
}