using Sekiban.Pure.Aggregates;

namespace AspireEventSample.ApiService.Aggregates.Branches;

[GenerateSerializer]
public record Branch(string Name) : IAggregatePayload;