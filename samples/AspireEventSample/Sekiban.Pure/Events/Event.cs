using Sekiban.Pure.Command.Handlers;
using Sekiban.Pure.Documents;

namespace Sekiban.Pure.Events;

public record Event<TEventPayload>(
    Guid Id,
    TEventPayload Payload,
    PartitionKeys PartitionKeys,
    string SortableUniqueId,
    int Version,
    EventMetadata Metadata) : IEvent where TEventPayload : IEventPayload
{
    public IEventPayload GetPayload()
    {
        return Payload;
    }
}

public record EventMetadata(string CausationId, string CorrelationId, string ExecutedUser)
{
    public static EventMetadata FromCommandMetadata(CommandMetadata metadata)
    {
        return new EventMetadata(
            string.IsNullOrWhiteSpace(metadata.CausationId) ? metadata.CommandId.ToString() : metadata.CausationId,
            metadata.CorrelationId, metadata.ExecutedUser);
    }
}

public record EventDocument<TEventPayload>(
    Guid Id,
    TEventPayload Payload,
    string SortableUniqueId,
    int Version,
    Guid AggregateId,
    string AggregateGroup,
    string RootPartitionKey,
    string PayloadTypeName,
    DateTime TimeStamp,
    string PartitionKey,
    EventMetadata Metadata) where TEventPayload : IEventPayload
{
    public static EventDocument<TEventPayload> FromEvent(Event<TEventPayload> @event, string payloadTypeName)
    {
        return new EventDocument<TEventPayload>(@event.Id, @event.Payload, @event.SortableUniqueId, @event.Version,
            @event.PartitionKeys.AggregateId, @event.PartitionKeys.Group,
            @event.PartitionKeys.RootPartitionKey, payloadTypeName, DateTime.UtcNow,
            @event.PartitionKeys.ToPrimaryKeysString(), @event.Metadata);
    }
}