using Sekiban.Pure.Aggregates;
using Sekiban.Pure.Events;

namespace Sekiban.Pure.Types;

public interface ISekibanTypeConverters
{
    public IAggregateTypes AggregateTypes { get; }
    public IEventTypes EventTypes { get; }
}
public record SekibanTypeConverters(IAggregateTypes AggregateTypes, IEventTypes EventTypes);