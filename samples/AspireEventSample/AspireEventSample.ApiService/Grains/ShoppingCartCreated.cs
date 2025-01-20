using Sekiban.Pure.Events;

namespace AspireEventSample.ApiService.Grains;

public record ShoppingCartCreated(Guid UserId) : IEventPayload;