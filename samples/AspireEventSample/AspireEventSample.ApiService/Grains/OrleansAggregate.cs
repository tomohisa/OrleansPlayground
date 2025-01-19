using Sekiban.Pure.Aggregates;
using Sekiban.Pure.Documents;

namespace AspireEventSample.ApiService.Grains;

[GenerateSerializer]
record OrleansAggregate<TAggregatePayload>(
    TAggregatePayload Payload,
    PartitionKeys PartitionKeys,
    int Version,
    string LastSortableUniqueId) : IAggregate where TAggregatePayload : IAggregatePayload
{
    public IAggregatePayload GetPayload()
    {
        return Payload;
    }
}