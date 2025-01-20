using Sekiban.Pure.Events;

namespace AspireEventSample.ApiService.Grains;

public record ShoppingCartItemAdded(string Name, int Quantity, Guid ItemId, int Price) : IEventPayload;