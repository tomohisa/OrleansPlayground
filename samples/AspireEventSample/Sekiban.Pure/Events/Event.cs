using System.Text.Json.Serialization;
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

public interface IEventDocument
{
    Guid Id { get; }
    string SortableUniqueId { get; }
    int Version { get; }
    Guid AggregateId { get; }
    string AggregateGroup { get; }
    string RootPartitionKey { get; }
    string PayloadTypeName { get; }
    DateTime TimeStamp { get; }
    string PartitionKey { get; }
    EventMetadata Metadata { get; }
}

public record EventDocument<TEventPayload>(
    // [property:JsonPropertyName("id")]
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
    EventMetadata Metadata) : IEventDocument where TEventPayload : IEventPayload
{
    public static EventDocument<TEventPayload> FromEvent(Event<TEventPayload> ev)
    {
        var sortableUniqueIdValue = new SortableUniqueIdValue(ev.SortableUniqueId);
        return new EventDocument<TEventPayload>(ev.Id, ev.Payload, ev.SortableUniqueId, ev.Version,
            ev.PartitionKeys.AggregateId, ev.PartitionKeys.Group,
            ev.PartitionKeys.RootPartitionKey, ev.Payload.GetType().Name, sortableUniqueIdValue.GetTicks() ,
            ev.PartitionKeys.ToPrimaryKeysString(), ev.Metadata);
    }
}