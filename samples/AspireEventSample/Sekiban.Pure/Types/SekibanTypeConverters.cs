using Sekiban.Pure.Aggregates;
using Sekiban.Pure.Events;

namespace Sekiban.Pure.Types;

public record SekibanTypeConverters(IAggregateTypes AggregateTypes, IEventTypes EventTypes);