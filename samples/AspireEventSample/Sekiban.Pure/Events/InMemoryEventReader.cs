using ResultBoxes;
using Sekiban.Pure.Repositories;
namespace Sekiban.Pure.Events;

public class InMemoryEventReader(Repository repository) : IEventReader
{

    public async Task<ResultBox<IReadOnlyList<IEvent>>> GetEvents(EventRetrievalInfo eventRetrievalInfo)
    {
        var query = repository.Events.AsEnumerable();

        if (eventRetrievalInfo.GetIsPartition())
        {
            var partitionKeyResult = eventRetrievalInfo.GetPartitionKey();
            if (!partitionKeyResult.IsSuccess)
            {
                return ResultBox<IReadOnlyList<IEvent>>.Error(partitionKeyResult.Exception);
            }
            var partitionKey = partitionKeyResult.GetValue();
            query = query.Where(e => e.PartitionKeys.ToPrimaryKeysString() == partitionKey);
        }

        // Sort events by sortable unique ID
        var orderedEvents = query.OrderBy(e => e.SortableUniqueId);

        // Apply max count if specified
        var finalEvents = eventRetrievalInfo.MaxCount.HasValue
            ? orderedEvents.Take(eventRetrievalInfo.MaxCount.GetValue())
            : orderedEvents;
        await Task.CompletedTask;
        return ResultBox<IReadOnlyList<IEvent>>.Ok(finalEvents.ToList());
    }
}