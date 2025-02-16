namespace Sekiban.Pure.Orleans.Surrogates;

[GenerateSerializer]
public record OrleansCommand([property:Id(0)]string payload);