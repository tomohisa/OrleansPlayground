using Sekiban.Pure.Events;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Sekiban.Pure.OrleansEventSourcing;

namespace AspireEventSample.ApiService.Grains;

public class AggregateEventHandlerGrain : Grain, IAggregateEventHandlerGrain
{
    private List<OrleansEvent> _events = new();

    public Task<string> AppendEventsAsync(
        string expectedLastSortableUniqueId,
        IReadOnlyList<OrleansEvent> newEvents
    )
    {
        var sortedEvents = newEvents.OrderBy(m => m.SortableUniqueId).ToList();
        if (string.IsNullOrWhiteSpace(expectedLastSortableUniqueId) && 
            _events.Count > 0 &&
            _events.Last().SortableUniqueId != expectedLastSortableUniqueId)
        {
            throw new InvalidCastException("Expected last event ID does not match");
        }
        // if last sortable unique id is not empty and it is later than newEvents, throw exception
        if (_events.Any() &&
            sortedEvents.Any() &&
            String.Compare(_events.Last().SortableUniqueId, sortedEvents.First().SortableUniqueId, StringComparison.Ordinal) > 0)
        {
            throw new InvalidCastException("Expected last event ID is later than new events");
        }
        _events.AddRange(sortedEvents);
        return Task.FromResult(_events.Last().SortableUniqueId);
    }

    public Task<IReadOnlyList<OrleansEvent>> GetDeltaEventsAsync(
        string fromSortableUniqueId,
        int? limit = null
    )
    {
        var index = _events.FindIndex(e => e.SortableUniqueId == fromSortableUniqueId);
        
        if (index < 0)
            return Task.FromResult((IReadOnlyList<OrleansEvent>)new IEvent[0]);

        var events = _events.Skip(index + 1)
                            .Take(limit ?? int.MaxValue)
                            .ToList();

        return Task.FromResult((IReadOnlyList<OrleansEvent>)events);
    }

    public Task<IReadOnlyList<OrleansEvent>> GetAllEventsAsync()
    {
        return Task.FromResult((IReadOnlyList<OrleansEvent>)_events.ToList());
    }

    public Task<string> GetLastSortableUniqueIdAsync()
    {
        if (!_events.Any())
        {
            return Task.FromResult(String.Empty);
        }

        return Task.FromResult(_events.Last().SortableUniqueId);
    }

    public Task RegisterProjectorAsync(string projectorKey)
    {
        // No-op for in-memory implementation
        return Task.CompletedTask;
    }
}
