namespace AspireEventSample.ApiService.Aggregates.Carts;

public record ShoppingCartItems(string Name, int Quantity, Guid ItemId, int Price);