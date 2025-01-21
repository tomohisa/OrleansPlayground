using Sekiban.Pure.Events;

namespace AspireEventSample.ApiService.Aggregates.Carts;

public record ShoppingCartItemAdded(string Name, int Quantity, Guid ItemId, int Price) : IEventPayload;