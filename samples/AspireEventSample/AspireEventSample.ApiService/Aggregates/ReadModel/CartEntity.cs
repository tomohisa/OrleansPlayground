namespace AspireEventSample.ApiService.Aggregates.ReadModel;

using AspireEventSample.ApiService.Aggregates.Carts;
using Orleans.Serialization;
using Sekiban.Pure.Documents;
using System.Collections.Generic;

[GenerateSerializer]
public record CartEntity : IReadModelEntity
{
    [Id(0)]
    public required Guid Id { get; init; }
    [Id(1)]
    public required Guid TargetId { get; init; }
    [Id(2)]
    public required string RootPartitionKey { get; init; }
    [Id(3)]
    public required string AggregateGroup { get; init; }
    [Id(4)]
    public required string LastSortableUniqueId { get; init; }
    [Id(5)]
    public required DateTime TimeStamp { get; init; }
    
    // Cart specific properties
    [Id(6)]
    public required Guid UserId { get; init; }
    [Id(7)]
    public required List<ShoppingCartItems> Items { get; init; }
    [Id(8)]
    public required string Status { get; init; }
    [Id(9)]
    public required int TotalAmount { get; init; }
}
