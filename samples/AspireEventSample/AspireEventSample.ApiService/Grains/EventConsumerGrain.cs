using Orleans;
using Orleans.Runtime;
using Orleans.Streams;
using AspireEventSample.ApiService.Aggregates.Branches;
using AspireEventSample.ApiService.Aggregates.ReadModel;
using Sekiban.Pure.Documents;
using System.Collections.Generic;
using Sekiban.Pure.OrleansEventSourcing;

namespace AspireEventSample.ApiService.Grains;

[ImplicitStreamSubscription("AllEvents")]
public class EventConsumerGrain : Grain, IEventConsumerGrain
{
    private readonly IBranchEntityWriter _branchEntityWriter;
    private IAsyncStream<OrleansEvent> _stream;
    private StreamSubscriptionHandle<OrleansEvent> _subscriptionHandle;

    public EventConsumerGrain(IBranchEntityWriter branchEntityWriter)
    {
        _branchEntityWriter = branchEntityWriter;
    }
    public Task OnErrorAsync(Exception ex)
    {
        Console.WriteLine($"[MyGrain] Stream error: {ex.Message}");
        return Task.CompletedTask;
    }

    // Stream completion handler
    public async Task OnNextAsync(OrleansEvent item, StreamSequenceToken? token)
    {
        Console.WriteLine($"[MyGrain] Received event: {item}");
        
        var targetId = item.Id;
        var existing = await _branchEntityWriter.GetEntityByIdAsync(
            item.PartitionKeys.RootPartitionKey,
            item.PartitionKeys.Group,
            targetId);

        // Create or update branch entity based on event type
        if (item.Payload is BranchCreated created)
        {
            var entity = new BranchEntity
            {
                Id = Guid.NewGuid(),
                TargetId = targetId,
                RootPartitionKey = item.PartitionKeys.RootPartitionKey,
                AggregateGroup = item.PartitionKeys.Group,
                LastSortableUniqueId = item.SortableUniqueId,
                TimeStamp = DateTime.UtcNow,
                Name = created.Name
            };
            await _branchEntityWriter.AddOrUpdateEntityAsync(entity);
        }
        else if (item.Payload is BranchNameChanged nameChanged && existing != null)
        {
            var updated = existing with
            {
                LastSortableUniqueId = item.SortableUniqueId,
                TimeStamp = DateTime.UtcNow,
                Name = nameChanged.Name
            };
            await _branchEntityWriter.AddOrUpdateEntityAsync(updated);
        }
    }

    public Task OnCompletedAsync()
    {
        Console.WriteLine("[MyGrain] Stream completed.");
        return Task.CompletedTask;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        // _logger.LogInformation("OnActivateAsync");
        
        var streamProvider = this.GetStreamProvider("EventStreamProvider");
        
        _stream = streamProvider.GetStream<OrleansEvent>(StreamId.Create("AllEvents", Guid.Empty));
        
        // Subscribe to the stream when this grain is activated
        _subscriptionHandle = await _stream.SubscribeAsync(
            (evt, token) => OnNextAsync(evt, token),    // When an event is received
            OnErrorAsync,                               // When an error occurs
            OnCompletedAsync                            // When the stream completes
        );

         await base.OnActivateAsync(cancellationToken);
    }
}
