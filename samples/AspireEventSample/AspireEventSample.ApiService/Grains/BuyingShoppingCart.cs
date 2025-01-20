using Sekiban.Pure.Aggregates;

namespace AspireEventSample.ApiService.Grains;

public record BuyingShoppingCart(Guid UserId, List<ShoppingCartItems> Items) : IAggregatePayload;