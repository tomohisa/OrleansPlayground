using ResultBoxes;
using Sekiban.Pure.Events;
using Sekiban.Pure.Exceptions;

namespace Sekiban.Pure.Projectors;

public record MultiProjectionState<TMultiProjector>(
    TMultiProjector Payload,
    Guid LastEventId,
    string LastSortableUniqueId,
    int AppliedSnapshotVersion,
    int Version,
    string RootPartitionKey) : IMultiProjectorStateCommon where TMultiProjector : IMultiProjector<TMultiProjector>
{
    public MultiProjectionState() : this(
        TMultiProjector.GenerateInitialPayload(),
        Guid.Empty,
        string.Empty,
        0,
        0,
        string.Empty)
    {
    }

    public static ResultBox<MultiProjectionState<TMultiProjector>> FromCommon(IMultiProjectorStateCommon common)
    {
        return common switch
        {
            MultiProjectionState<TMultiProjector> state => state,
            _ => new SekibanQueryTypeException("Unexpected common type")
        };
    }

    public string GetPayloadVersionIdentifier()
    {
        return Payload.GetVersion();
    }

    public ResultBox<MultiProjectionState<TMultiProjector>> ApplyEvent(IEvent ev)
    {
        return Payload
            .Project(Payload, ev)
            .Remap(
                p => this with
                {
                    Payload = p,
                    LastEventId = ev.Id,
                    LastSortableUniqueId = ev.SortableUniqueId,
                    Version = Version + 1
                });
    }
}