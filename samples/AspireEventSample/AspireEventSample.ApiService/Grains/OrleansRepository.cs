using System.Collections.Generic;
using System.Threading.Tasks;
using ResultBoxes;
using Sekiban.Pure.Aggregates;
using Sekiban.Pure.Events;
using Sekiban.Pure.Projectors;
using Sekiban.Pure.Documents;

namespace AspireEventSample.ApiService.Grains;

public class OrleansRepository(IAggregateEventHandlerGrain eventHandlerGrain, PartitionKeys partitionKeys, IAggregateProjector projector)
{
    public Task<ResultBox<Aggregate>> Load()
        => ResultBox.WrapTry(() => eventHandlerGrain.GetAllEventsAsync())
            .Conveyor(events => Aggregate.EmptyFromPartitionKeys(partitionKeys).Project(events.ToList(), projector));

    public Task<ResultBox<UnitValue>> Save(string lastSortableUniqueId, List<IEvent> events)
        => ResultBox.WrapTry(() => eventHandlerGrain.AppendEventsAsync(lastSortableUniqueId, events))
            .Remap(_ => UnitValue.Unit);
}