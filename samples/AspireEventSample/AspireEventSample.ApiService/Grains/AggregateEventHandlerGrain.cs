using Sekiban.Pure.Events;

namespace AspireEventSample.ApiService.Grains;

public class AggregateEventHandlerGrain : Grain, IAggregateEventHandlerGrain
{
    public Task<string> AppendEventsAsync(
        string expectedLastSortableUniqueId,
        IReadOnlyList<IEvent> newEvents
    )
    {
        throw new System.NotImplementedException();
    }

    public Task<IReadOnlyList<IEvent>> GetDeltaEventsAsync(
        string fromSortableUniqueId,
        int? limit = null
    )
    {
        throw new System.NotImplementedException();
    }

    public Task<IReadOnlyList<IEvent>> GetAllEventsAsync()
    {
        throw new System.NotImplementedException();
    }

    public Task<string> GetLastSortableUniqueIdAsync()
    {
        throw new System.NotImplementedException();
    }

    public Task RegisterProjectorAsync(string projectorKey)
    {
        throw new System.NotImplementedException();
    }
}