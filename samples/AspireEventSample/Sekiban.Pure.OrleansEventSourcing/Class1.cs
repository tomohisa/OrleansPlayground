using Sekiban.Pure.Aggregates;
using Sekiban.Pure.Documents;
using Sekiban.Pure.Events;

namespace Sekiban.Pure.OrleansEventSourcing;

public class Class1
{
}

[GenerateSerializer]
public record OrleansEvent(
    [property:Id(0)]Guid Id,
    [property:Id(1)]IEventPayload Payload,
    [property:Id(2)]OrleansPartitionKeys PartitionKeys,
    [property:Id(3)]string SortableUniqueId,
    [property:Id(4)]int Version,
    [property:Id(5)]string EventPayloadTypeName)
{
    public static OrleansEvent FromEvent(IEvent ev)
    {
        var payload = ev.GetPayload();
        return new(
            ev.Id,
            payload,
            ev.PartitionKeys.ToOrleansPartitionKeys(),
            ev.SortableUniqueId,
            ev.Version,
            payload.GetType().Name); 
    }

}
[GenerateSerializer]
public record OrleansEmptyAggregatePayload() : IAggregatePayload;
public static class OrleansEventExtensions
{
    public static List<OrleansEvent> ToOrleansEvents(this List<IEvent> events) =>
        events.Select(OrleansEvent.FromEvent).ToList();
    public static List<IEvent> ToEvents(this List<OrleansEvent> events, IEventTypes eventTypes) =>
        events.Select(e => eventTypes.GenerateTypedEvent(e.Payload,e.PartitionKeys.ToPartitionKeys(),e.SortableUniqueId,e.Version))
            .Where(result => result.IsSuccess)
            .Select(result => result.GetValue()).ToList();
    public static List<IEvent> ToEventsAndReplaceTime(this List<OrleansEvent> events, IEventTypes eventTypes) =>
        events.Select(e => eventTypes.GenerateTypedEvent(e.Payload,e.PartitionKeys.ToPartitionKeys(),SortableUniqueIdValue.Generate(DateTime.UtcNow, e.Id),e.Version))
            .Where(result => result.IsSuccess)
            .Select(result => result.GetValue()).ToList();
}
