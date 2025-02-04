using System.Reflection;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using ResultBoxes;
using Sekiban.Pure.Documents;
using Sekiban.Pure.Events;
using Sekiban.Pure.OrleansEventSourcing;

namespace Sekiban.Pure.CosmosDb;

public class CosmosDbEventWriter(CosmosDbFactory dbFactory, IEventTypes eventTypes) : IEventWriter
{
    
    public Task SaveEvents<TEvent>(IEnumerable<TEvent> events) where TEvent : IEvent=> dbFactory.CosmosActionAsync(
        DocumentType.Event,
        async container =>
        {
            var taskList = events.ToList()
                .Select(ev => eventTypes.ConvertToEventDocument(ev))
                .Select(ev => SaveEventFromEventDocument(ev.UnwrapBox(), container))
                .ToList();
            await Task.WhenAll(taskList);
        });

    public Task SaveEventFromEventDocument(IEventDocument eventDocument, Container container)
    {
        var documentType = eventDocument.GetType();
        var methods = container.GetType().GetMethods().Where(m => m.Name == nameof(Container.UpsertItemAsync));
        var method = methods.FirstOrDefault(m => m.Name == nameof(Container.UpsertItemAsync) && m.GetParameters().Length == 4);
        var genericMethod = method?.MakeGenericMethod(documentType);
        return ((Task?)genericMethod?.Invoke(container, new object?[] { eventDocument, CosmosPartitionGenerator.ForEvent(eventDocument) ,null, default(CancellationToken) })) ?? Task.CompletedTask;
    }
}

public class CosmosDbEventReader(CosmosDbFactory dbFactory, IEventTypes eventTypes) : IEventReader
{
    private const int DefaultOptionsMax = -1;
    private static QueryRequestOptions CreateDefaultOptions() =>
        new()
        {
            MaxConcurrency = DefaultOptionsMax, MaxItemCount = DefaultOptionsMax,
            MaxBufferedItemCount = DefaultOptionsMax
        };

    public async Task<ResultBox<IReadOnlyList<IEvent>>> GetEvents(EventRetrievalInfo eventRetrievalInfo)
    {
         await dbFactory.CosmosActionAsync(
            DocumentType.Event,
            async container =>
            {
                if (eventRetrievalInfo.GetIsPartition())
                {
                    var options = CreateDefaultOptions();
                    options.PartitionKey = CosmosPartitionGenerator.ForAggregate(PartitionKeys.Existing(eventRetrievalInfo.AggregateId.GetValue(), eventRetrievalInfo.RootPartitionKey.GetValue(),
                            eventRetrievalInfo.AggregateStream.GetValue().GetSingleStreamName().UnwrapBox()));
                    var query = container.GetItemLinqQueryable<IEvent>();
                    query = eventRetrievalInfo.SortableIdCondition switch
                    {
                        (SinceSortableIdCondition since) => query
                            .Where(m => m.SortableUniqueId.CompareTo(since.SortableUniqueId.Value) > 0)
                            .OrderBy(m => m.SortableUniqueId),
                        (BetweenSortableIdCondition between) => query
                            .Where(m => m.SortableUniqueId.CompareTo(between.Start.Value) > 0 &&
                                m.SortableUniqueId.CompareTo(between.End.Value) < 0)
                            .OrderBy(m => m.SortableUniqueId),
                        SortableIdConditionNone => query.OrderBy(m => m.SortableUniqueId),
                        _ => throw new ArgumentOutOfRangeException()
                    };
                    var feedIterator = container.GetItemQueryIterator<dynamic>(
                        query.ToQueryDefinition(),
                        null,
                        options);
                    var events = new List<IEvent>();
                    while (feedIterator.HasMoreResults)
                    {
                        var response = await feedIterator.ReadNextAsync();
                        var toAdds = ProcessEvents(response, eventRetrievalInfo.SortableIdCondition);
                        events.AddRange(toAdds);
                        if (eventRetrievalInfo.MaxCount.HasValue &&
                            events.Count > eventRetrievalInfo.MaxCount.GetValue())
                        {
                            events = events.Take(eventRetrievalInfo.MaxCount.GetValue()).ToList();
                            break;
                        }
                    }
                    resultAction(events);
                } else
                {
                    var options = CreateDefaultOptions();

                    var query = container.GetItemLinqQueryable<IEvent>().AsQueryable();
                    if (eventRetrievalInfo.HasAggregateStream())
                    {
                        var aggregates = eventRetrievalInfo.AggregateStream.GetValue().GetStreamNames();
                        query = query.Where(m => aggregates.Contains(m.AggregateType));
                    }
                    if (eventRetrievalInfo.HasRootPartitionKey())
                    {
                        query = query.Where(m => m.RootPartitionKey == eventRetrievalInfo.RootPartitionKey.GetValue());
                    }
                    query = eventRetrievalInfo.SortableIdCondition switch
                    {
                        (SinceSortableIdCondition since) => query
                            .Where(m => m.SortableUniqueId.CompareTo(since.SortableUniqueId.Value) > 0)
                            .OrderBy(m => m.SortableUniqueId),
                        BetweenSortableIdCondition between => query
                            .Where(m => m.SortableUniqueId.CompareTo(between.Start.Value) > 0 &&
                                m.SortableUniqueId.CompareTo(between.End.Value) < 0)
                            .OrderBy(m => m.SortableUniqueId),
                        (SortableIdConditionNone _) => query.OrderBy(m => m.SortableUniqueId),
                        _ => throw new ArgumentOutOfRangeException()
                    };
                    var feedIterator = container.GetItemQueryIterator<dynamic>(
                        query.ToQueryDefinition(),
                        null,
                        options);
                    var events = new List<IEvent>();
                    while (feedIterator.HasMoreResults)
                    {
                        var response = await feedIterator.ReadNextAsync();
                        var toAdds = ProcessEvents(response, eventRetrievalInfo.SortableIdCondition);
                        events.AddRange(toAdds);
                        if (eventRetrievalInfo.MaxCount.HasValue &&
                            events.Count > eventRetrievalInfo.MaxCount.GetValue())
                        {
                            events = events.Take(eventRetrievalInfo.MaxCount.GetValue()).ToList();
                            break;
                        }
                    }
                    resultAction(events);
                }
            });
        return true;
   }
}