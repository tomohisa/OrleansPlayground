using Orleans;
using Sekiban.Pure.Documents;
using Sekiban.Pure.Events;
namespace Sekiban.Pure.Command.Executor;
[GenerateSerializer]
public record CommandResponse(PartitionKeys PartitionKeys, List<IEvent> Events, int Version);
