using Orleans;
using Sekiban.Pure.Command.Executor;
using Sekiban.Pure.Documents;
using Sekiban.Pure.Events;

namespace Sekiban.Pure.Orleans.Surrogates;

[GenerateSerializer]
public record struct OrleansCommandResponse(
    [property: Id(0)] OrleansPartitionKeys PartitionKeys,
    [property: Id(1)] List<IEvent> Events,
    [property: Id(2)] int Version);

[RegisterConverter]
public sealed class OrleansCommandResponseConverter : IConverter<CommandResponse, OrleansCommandResponse>
{
    private readonly OrleansPartitionKeysConverter _partitionKeysConverter = new();

    public CommandResponse ConvertFromSurrogate(in OrleansCommandResponse surrogate) =>
        new(_partitionKeysConverter.ConvertFromSurrogate(surrogate.PartitionKeys), surrogate.Events, surrogate.Version);

    public OrleansCommandResponse ConvertToSurrogate(in CommandResponse value) =>
        new(_partitionKeysConverter.ConvertToSurrogate(value.PartitionKeys), value.Events, value.Version);
}
