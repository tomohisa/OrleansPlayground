using Orleans;
using Sekiban.Pure.Aggregates;
using Sekiban.Pure.Documents;

namespace Sekiban.Pure.Orleans.Surrogates;

[GenerateSerializer]
public record struct OrleansAggregate(
    [property: Id(0)] IAggregatePayload Payload,
    [property: Id(1)] OrleansPartitionKeys PartitionKeys,
    [property: Id(2)] int Version,
    [property: Id(3)] string LastSortableUniqueId,
    [property: Id(4)] string ProjectorVersion,
    [property: Id(5)] string ProjectorTypeName,
    [property: Id(6)] string PayloadTypeName);

[GenerateSerializer]
public record struct OrleansAggregate<TAggregatePayload>(
    [property: Id(0)] TAggregatePayload Payload,
    [property: Id(1)] OrleansPartitionKeys PartitionKeys,
    [property: Id(2)] int Version,
    [property: Id(3)] string LastSortableUniqueId,
    [property: Id(4)] string ProjectorVersion,
    [property: Id(5)] string ProjectorTypeName,
    [property: Id(6)] string PayloadTypeName) where TAggregatePayload : IAggregatePayload;

[RegisterConverter]
public sealed class OrleansAggregateConverter : IConverter<Aggregate, OrleansAggregate>
{
    private readonly OrleansPartitionKeysConverter _partitionKeysConverter = new();

    public Aggregate ConvertFromSurrogate(in OrleansAggregate surrogate) =>
        new(surrogate.Payload,
            _partitionKeysConverter.ConvertFromSurrogate(surrogate.PartitionKeys),
            surrogate.Version,
            surrogate.LastSortableUniqueId,
            surrogate.ProjectorVersion,
            surrogate.ProjectorTypeName,
            surrogate.PayloadTypeName);

    public OrleansAggregate ConvertToSurrogate(in Aggregate value) =>
        new(value.Payload,
            _partitionKeysConverter.ConvertToSurrogate(value.PartitionKeys),
            value.Version,
            value.LastSortableUniqueId,
            value.ProjectorVersion,
            value.ProjectorTypeName,
            value.PayloadTypeName);
}

[RegisterConverter]
public sealed class OrleansAggregateConverter<TAggregatePayload> : IConverter<Aggregate<TAggregatePayload>, OrleansAggregate<TAggregatePayload>>
    where TAggregatePayload : IAggregatePayload
{
    private readonly OrleansPartitionKeysConverter _partitionKeysConverter = new();

    public Aggregate<TAggregatePayload> ConvertFromSurrogate(in OrleansAggregate<TAggregatePayload> surrogate) =>
        new(surrogate.Payload,
            _partitionKeysConverter.ConvertFromSurrogate(surrogate.PartitionKeys),
            surrogate.Version,
            surrogate.LastSortableUniqueId,
            surrogate.ProjectorVersion,
            surrogate.ProjectorTypeName,
            surrogate.PayloadTypeName);

    public OrleansAggregate<TAggregatePayload> ConvertToSurrogate(in Aggregate<TAggregatePayload> value) =>
        new(value.Payload,
            _partitionKeysConverter.ConvertToSurrogate(value.PartitionKeys),
            value.Version,
            value.LastSortableUniqueId,
            value.ProjectorVersion,
            value.ProjectorTypeName,
            value.PayloadTypeName);
}
