using Orleans;
using Sekiban.Pure.Aggregates;

namespace Sekiban.Pure.Orleans.Surrogates;

[GenerateSerializer]
public record struct OrleansEmptyAggregatePayload;

[RegisterConverter]
public sealed class OrleansEmptyAggregatePayloadConverter : IConverter<EmptyAggregatePayload, OrleansEmptyAggregatePayload>
{
    public EmptyAggregatePayload ConvertFromSurrogate(in OrleansEmptyAggregatePayload surrogate) =>
        new();

    public OrleansEmptyAggregatePayload ConvertToSurrogate(in EmptyAggregatePayload value) =>
        new();
}
