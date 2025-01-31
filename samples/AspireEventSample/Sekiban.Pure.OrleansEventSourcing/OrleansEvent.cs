using Sekiban.Pure.Command.Handlers;
using Sekiban.Pure.Events;

namespace Sekiban.Pure.OrleansEventSourcing;

[GenerateSerializer]
public record OrleansEvent(
    [property:Id(0)]Guid Id,
    [property:Id(1)]IEventPayload Payload,
    [property:Id(2)]OrleansPartitionKeys PartitionKeys,
    [property:Id(3)]string SortableUniqueId,
    [property:Id(4)]int Version,
    [property:Id(5)]OrleansEventMetadata Metadata)
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
            OrleansEventMetadata.FromEventMetadata(ev.Metadata)); 
    }

}
[GenerateSerializer]
public record OrleansEventMetadata([property:Id(0)]string CausationId,
    [property:Id(1)]string CorrelationId, [property:Id(2)]string ExecutedUser)
{
    public static OrleansEventMetadata FromEventMetadata(EventMetadata metadata) => new(metadata.CausationId, metadata.CorrelationId, metadata.ExecutedUser);
    public EventMetadata ToEventMetadata() => new(CausationId, CorrelationId, ExecutedUser);
}
[GenerateSerializer]
public record OrleansCommandMetadata([property:Id(0)]Guid CommandId, [property:Id(1)]string CausationId,
    [property:Id(2)]string CorrelationId, [property:Id(3)]string ExecutedUser)
{
    public static OrleansCommandMetadata FromCommandMetadata(CommandMetadata metadata) => new(metadata.CommandId, metadata.CausationId, metadata.CorrelationId, metadata.ExecutedUser);
    public CommandMetadata ToCommandMetadata() => new(CommandId, CausationId, CorrelationId, ExecutedUser);
}
