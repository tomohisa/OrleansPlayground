using System.Collections.Generic;
using System.Threading.Tasks;
using ResultBoxes;
using Sekiban.Pure.Aggregates;
using Sekiban.Pure.Events;
using Sekiban.Pure.Projectors;
using Sekiban.Pure.Documents;
using Sekiban.Pure.OrleansEventSourcing;

namespace AspireEventSample.ApiService.Grains;

public class OrleansRepository(IAggregateEventHandlerGrain eventHandlerGrain, PartitionKeys partitionKeys, IAggregateProjector projector, IEventTypes eventTypes)
{
    public Task<ResultBox<Aggregate>> Load()
        => ResultBox.WrapTry(() => eventHandlerGrain.GetAllEventsAsync())
            .Conveyor(events => Aggregate.EmptyFromPartitionKeys(partitionKeys).Project(events.ToList().ToEvents(eventTypes), projector));

    public Task<ResultBox<UnitValue>> Save(string lastSortableUniqueId, List<IEvent> events)
        => ResultBox.WrapTry(() => eventHandlerGrain.AppendEventsAsync(lastSortableUniqueId, events.ToOrleansEvents()))
            .Remap(_ => UnitValue.Unit);
}
