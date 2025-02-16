using Orleans.Streams;
using AspireEventSample.ApiService.Aggregates.Branches;
using AspireEventSample.ApiService.Aggregates.Carts;
using AspireEventSample.ApiService.Aggregates.ReadModel;
using Sekiban.Pure.Orleans;

namespace AspireEventSample.ApiService.Grains;

[ImplicitStreamSubscription("AllEvents")]
public class EventConsumerGrain : Grain, IEventConsumerGrain
{
    private IAsyncStream<OrleansEvent>? _stream = null;
    private StreamSubscriptionHandle<OrleansEvent>? _subscriptionHandle = null;

    public Task OnErrorAsync(Exception ex)
    {
        Console.WriteLine($"[MyGrain] Stream error: {ex.Message}");
        return Task.CompletedTask;
    }

    // Stream completion handler
    public async Task OnNextAsync(OrleansEvent item, StreamSequenceToken? token)
    {
        Console.WriteLine($"[MyGrain] Received event: {item}");
        
        var targetId = item.PartitionKeys.AggregateId;

        // Handle Branch events
        if (item.Payload is BranchCreated || item.Payload is BranchNameChanged)
        {
            var branchEntityWriter = GrainFactory.GetGrain<IBranchEntityWriter>(item.PartitionKeys.RootPartitionKey);
            var existing = await branchEntityWriter.GetEntityByIdAsync(
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
                await branchEntityWriter.AddOrUpdateEntityAsync(entity);
            }
            else if (item.Payload is BranchNameChanged nameChanged && existing != null)
            {
                var updated = existing with
                {
                    LastSortableUniqueId = item.SortableUniqueId,
                    TimeStamp = DateTime.UtcNow,
                    Name = nameChanged.Name
                };
                await branchEntityWriter.AddOrUpdateEntityAsync(updated);
            }
        }
        // Handle Cart events
        else if (item.Payload is ShoppingCartCreated || item.Payload is ShoppingCartItemAdded || item.Payload is ShoppingCartPaymentProcessed)
        {
            var cartEntityWriter = GrainFactory.GetGrain<ICartEntityWriter>(item.PartitionKeys.RootPartitionKey);
            var existing = await cartEntityWriter.GetEntityByIdAsync(
                item.PartitionKeys.RootPartitionKey,
                item.PartitionKeys.Group,
                targetId);

            if (item.Payload is ShoppingCartCreated created)
            {
                var entity = new CartEntity
                {
                    Id = Guid.NewGuid(),
                    TargetId = targetId,
                    RootPartitionKey = item.PartitionKeys.RootPartitionKey,
                    AggregateGroup = item.PartitionKeys.Group,
                    LastSortableUniqueId = item.SortableUniqueId,
                    TimeStamp = DateTime.UtcNow,
                    UserId = created.UserId,
                    Items = new List<ShoppingCartItems>(),
                    Status = "Created",
                    TotalAmount = 0
                };
                await cartEntityWriter.AddOrUpdateEntityAsync(entity);
            }
            else if (item.Payload is ShoppingCartItemAdded itemAdded && existing != null)
            {
                var updatedItems = new List<ShoppingCartItems>(existing.Items)
                {
                    new ShoppingCartItems(itemAdded.Name, itemAdded.Quantity, itemAdded.ItemId, itemAdded.Price)
                };
                var totalAmount = updatedItems.Sum(item => item.Price * item.Quantity);

                var updated = existing with
                {
                    LastSortableUniqueId = item.SortableUniqueId,
                    TimeStamp = DateTime.UtcNow,
                    Items = updatedItems,
                    TotalAmount = totalAmount
                };
                await cartEntityWriter.AddOrUpdateEntityAsync(updated);
            }
            else if (item.Payload is ShoppingCartPaymentProcessed && existing != null)
            {
                var updated = existing with
                {
                    LastSortableUniqueId = item.SortableUniqueId,
                    TimeStamp = DateTime.UtcNow,
                    Status = "Paid"
                };
                await cartEntityWriter.AddOrUpdateEntityAsync(updated);
            }
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
