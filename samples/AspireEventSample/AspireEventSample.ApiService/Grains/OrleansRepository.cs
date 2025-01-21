using System.Collections.Generic;
using System.Threading.Tasks;
using ResultBoxes;
using Sekiban.Pure.Aggregates;
using Sekiban.Pure.Events;
using Sekiban.Pure.Projectors;
using Sekiban.Pure.Documents;
using Sekiban.Pure.OrleansEventSourcing;

namespace AspireEventSample.ApiService.Grains;

public class OrleansRepository(IAggregateEventHandlerGrain eventHandlerGrain, PartitionKeys partitionKeys, IAggregateProjector projector, IEventTypes eventTypes, Aggregate aggregate)
{
    public Task<ResultBox<Aggregate>> Load()
        => aggregate.ToResultBox().ToTask();

    public Task<ResultBox<UnitValue>> Save(string lastSortableUniqueId, List<IEvent> events)
        => ResultBox.WrapTry(() => eventHandlerGrain.AppendEventsAsync(lastSortableUniqueId, events.ToOrleansEvents()))
            .Remap(_ => UnitValue.Unit);
    
    public ResultBox<Aggregate> GetProjectedAggregate(List<IEvent> events)
        => aggregate.Project(events, projector);
}
