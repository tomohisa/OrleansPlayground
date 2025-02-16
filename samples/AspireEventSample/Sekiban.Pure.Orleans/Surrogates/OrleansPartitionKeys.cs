using Sekiban.Pure.Documents;
namespace Sekiban.Pure.Orleans.Surrogates;

[GenerateSerializer]
public record struct OrleansPartitionKeys(
    [property: Id(0)] Guid AggregateId,
    [property: Id(1)] string Group,
    [property: Id(2)] string RootPartitionKey);
[RegisterConverter]
public sealed class OrleansPartitionKeysConverter : IConverter<PartitionKeys, OrleansPartitionKeys>
{
    public PartitionKeys ConvertFromSurrogate(in OrleansPartitionKeys surrogate) =>
        new(surrogate.AggregateId, surrogate.Group, surrogate.RootPartitionKey);

    public OrleansPartitionKeys ConvertToSurrogate(in PartitionKeys value) =>
        new(value.AggregateId, value.Group, value.RootPartitionKey);
}