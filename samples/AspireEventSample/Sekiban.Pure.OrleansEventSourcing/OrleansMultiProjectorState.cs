using Sekiban.Pure.Projectors;

namespace Sekiban.Pure.OrleansEventSourcing;

public record OrleansMultiProjectorState(IMultiProjectorCommon ProjectorCommon, Guid LastEventId, string LastSortableUniqueId, int Version, string RootPartitionKey)
{
}