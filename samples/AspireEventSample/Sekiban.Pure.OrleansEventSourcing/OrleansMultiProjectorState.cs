using Orleans;
using Sekiban.Pure.Projectors;

namespace Sekiban.Pure.OrleansEventSourcing;

[GenerateSerializer]
public record OrleansMultiProjectorState(
    [property: Id(1)] IMultiProjectorCommon ProjectorCommon,
    [property: Id(2)] Guid LastEventId,
    [property: Id(3)] string LastSortableUniqueId,
    [property: Id(4)] int Version,
    [property: Id(5)] string RootPartitionKey)
{
}
