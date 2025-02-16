using Orleans;
using Sekiban.Pure.Documents;
using Sekiban.Pure.Events;

namespace Sekiban.Pure.Orleans.Surrogates;

[GenerateSerializer]
public record struct OrleansEventMetadata(
    [property: Id(0)] string CausationId,
    [property: Id(1)] string CorrelationId,
    [property: Id(2)] string ExecutedUser);

[GenerateSerializer]
public record struct OrleansEvent<TEventPayload>(
    [property: Id(0)] Guid Id,
    [property: Id(1)] TEventPayload Payload,
    [property: Id(2)] OrleansPartitionKeys PartitionKeys,
    [property: Id(3)] string SortableUniqueId,
    [property: Id(4)] int Version,
    [property: Id(5)] OrleansEventMetadata Metadata) where TEventPayload : IEventPayload;

[RegisterConverter]
public sealed class OrleansEventMetadataConverter : IConverter<EventMetadata, OrleansEventMetadata>
{
    public EventMetadata ConvertFromSurrogate(in OrleansEventMetadata surrogate) =>
        new(surrogate.CausationId, surrogate.CorrelationId, surrogate.ExecutedUser);

    public OrleansEventMetadata ConvertToSurrogate(in EventMetadata value) =>
        new(value.CausationId, value.CorrelationId, value.ExecutedUser);
}

[RegisterConverter]
public sealed class OrleansEventConverter<TEventPayload> : IConverter<Event<TEventPayload>, OrleansEvent<TEventPayload>>
    where TEventPayload : IEventPayload
{
    private readonly OrleansPartitionKeysConverter _partitionKeysConverter = new();
    private readonly OrleansEventMetadataConverter _metadataConverter = new();

    public Event<TEventPayload> ConvertFromSurrogate(in OrleansEvent<TEventPayload> surrogate) =>
        new(surrogate.Id,
            surrogate.Payload,
            _partitionKeysConverter.ConvertFromSurrogate(surrogate.PartitionKeys),
            surrogate.SortableUniqueId,
            surrogate.Version,
            _metadataConverter.ConvertFromSurrogate(surrogate.Metadata));

    public OrleansEvent<TEventPayload> ConvertToSurrogate(in Event<TEventPayload> value) =>
        new(value.Id,
            value.Payload,
            _partitionKeysConverter.ConvertToSurrogate(value.PartitionKeys),
            value.SortableUniqueId,
            value.Version,
            _metadataConverter.ConvertToSurrogate(value.Metadata));
}
