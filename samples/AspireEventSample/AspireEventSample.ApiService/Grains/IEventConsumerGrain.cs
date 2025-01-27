using Sekiban.Pure.OrleansEventSourcing;

namespace AspireEventSample.ApiService.Grains;

public interface IEventConsumerGrain : IGrainWithGuidKey
{
    Task ConsumeEventsAsync(IReadOnlyList<OrleansEvent> events);
}