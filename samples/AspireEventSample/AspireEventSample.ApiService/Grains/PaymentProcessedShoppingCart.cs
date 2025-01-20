using Sekiban.Pure.Events;

namespace AspireEventSample.ApiService.Grains;

public record PaymentProcessedShoppingCart(string PaymentMethod) : IEventPayload;