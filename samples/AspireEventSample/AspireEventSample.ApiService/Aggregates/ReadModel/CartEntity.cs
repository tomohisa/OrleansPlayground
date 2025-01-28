namespace AspireEventSample.ApiService.Aggregates.ReadModel;

using AspireEventSample.ApiService.Aggregates.Carts;
using Sekiban.Pure.Documents;
using System.Collections.Generic;

public record CartEntity : IReadModelEntity
{
    public required Guid Id { get; init; }
    public required Guid TargetId { get; init; }
    public required string RootPartitionKey { get; init; }
    public required string AggregateGroup { get; init; }
    public required string LastSortableUniqueId { get; init; }
    public required DateTime TimeStamp { get; init; }
    
    // Cart specific properties
    public required Guid UserId { get; init; }
    public required List<ShoppingCartItems> Items { get; init; }
    public required string Status { get; init; }
    public required int TotalAmount { get; init; }
}
