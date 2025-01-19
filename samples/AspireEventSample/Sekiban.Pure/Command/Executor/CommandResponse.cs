using Orleans;
using Sekiban.Pure.Documents;
using Sekiban.Pure.Events;
namespace Sekiban.Pure.Command.Executor;
public record CommandResponse(PartitionKeys PartitionKeys, List<IEvent> Events, int Version);

[GenerateSerializer]
public record OrleansCommandResponse([property:Id(0)]OrleansPartitionKeys PartitionKeys, [property:Id(1)]List<string> Events, [property:Id(2)]int Version)
{
}

[GenerateSerializer]
public record OrleansPartitionKeys(
    [property: Id(0)] Guid AggregateId,
    [property: Id(1)] string Group,
    [property: Id(2)] string RootPartitionKey);

public static class PartitionKeysExtensions
{
    public static OrleansPartitionKeys ToOrleansPartitionKeys(this PartitionKeys partitionKeys) =>
        new(partitionKeys.AggregateId, partitionKeys.Group, partitionKeys.RootPartitionKey);
}

public static class CommandResponseExtensions
{
    public static OrleansCommandResponse ToOrleansCommandResponse(this CommandResponse response) =>
        new(response.PartitionKeys.ToOrleansPartitionKeys(), response.Events.Select(e => e.ToString() ?? String.Empty).ToList(), response.Version);
}
