using Sekiban.Pure.Aggregates;

namespace AspireEventSample.ApiService.Grains;

[GenerateSerializer]
public record Branch(string Name) : IAggregatePayload;