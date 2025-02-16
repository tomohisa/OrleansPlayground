namespace Sekiban.Pure.Orleans;

[GenerateSerializer]
public record OrleansCommand([property:Id(0)]string payload);