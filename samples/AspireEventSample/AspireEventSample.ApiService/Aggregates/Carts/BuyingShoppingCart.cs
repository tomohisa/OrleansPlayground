using Sekiban.Pure.Aggregates;

namespace AspireEventSample.ApiService.Aggregates.Carts;

public record BuyingShoppingCart(Guid UserId, List<ShoppingCartItems> Items) : IAggregatePayload;