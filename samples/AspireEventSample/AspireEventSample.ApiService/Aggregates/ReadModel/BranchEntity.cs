namespace AspireEventSample.ApiService.Aggregates.ReadModel;

using Sekiban.Pure.Documents;

public record BranchEntity : IReadModelEntity
{
    public required Guid Id { get; init; }
    public required Guid TargetId { get; init; }
    public required string RootPartitionKey { get; init; }
    public required string AggregateGroup { get; init; }
    public required string LastSortableUniqueId { get; init; }
    public required DateTime TimeStamp { get; init; }
    public required string Name { get; init; }
}
