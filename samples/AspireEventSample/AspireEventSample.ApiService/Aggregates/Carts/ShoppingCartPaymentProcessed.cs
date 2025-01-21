using Sekiban.Pure.Events;

namespace AspireEventSample.ApiService.Aggregates.Carts;

public record ShoppingCartPaymentProcessed(string PaymentMethod) : IEventPayload;