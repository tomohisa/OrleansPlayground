using Sekiban.Pure.Aggregates;

namespace Sekiban.Pure.Orleans;

[GenerateSerializer]
public record OrleansEmptyAggregatePayload() : IAggregatePayload;
