namespace Sekiban.Pure.Projectors;

public record MultiProjectorState(IMultiProjectorCommon ProjectorCommon, Guid LastEventId, string LastSortableUniqueId, int Version, int AppliedSnapshotVersion, string RootPartitionKey)
{
}