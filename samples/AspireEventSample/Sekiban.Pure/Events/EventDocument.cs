using System.Text.Json;
using System.Text.Json.Nodes;
using ResultBoxes;
using Sekiban.Pure.Documents;
using Sekiban.Pure.Exception;

namespace Sekiban.Pure.Events;

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


public record EventDocumentCommon(
    Guid Id,
    JsonNode Payload,
    string SortableUniqueId,
    int Version,
    Guid AggregateId,
    string AggregateGroup,
    string RootPartitionKey,
    string PayloadTypeName,
    DateTime TimeStamp,
    string PartitionKey,
    EventMetadata Metadata) : IEventPayload
{
    public ResultBox<IEvent> ToEvent<TEventPayload>(JsonSerializerOptions options) where TEventPayload : IEventPayload
    {
        var p = Payload.Deserialize<TEventPayload>(options);
        if (p == null)
        {
            return ResultBox<IEvent>.FromException(new SekibanEventTypeNotFoundException("Failed to deserialize payload"));
        }
        return new Event<TEventPayload>(Id, p, new PartitionKeys(AggregateId, AggregateGroup, RootPartitionKey), SortableUniqueId, Version, Metadata);
    }
}