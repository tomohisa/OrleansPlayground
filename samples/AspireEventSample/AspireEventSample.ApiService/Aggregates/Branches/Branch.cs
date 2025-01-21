using ResultBoxes;
using Sekiban.Pure.Aggregates;
using Sekiban.Pure.Exception;

namespace AspireEventSample.ApiService.Aggregates.Branches;

[GenerateSerializer]
public record Branch(string Name) : IAggregatePayload;