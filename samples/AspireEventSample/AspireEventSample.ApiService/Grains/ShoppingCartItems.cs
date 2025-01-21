namespace AspireEventSample.ApiService.Grains;

public record ShoppingCartItems(string Name, int Quantity, Guid ItemId, int Price);