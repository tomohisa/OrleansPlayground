using Sekiban.Pure.Events;
namespace AspireEventSample.ApiService.Aggregates.Carts;

public record ShoppingCartCreated(Guid UserId) : IEventPayload;