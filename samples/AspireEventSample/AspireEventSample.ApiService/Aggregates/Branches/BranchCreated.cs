using Sekiban.Pure.Events;

namespace AspireEventSample.ApiService.Aggregates.Branches;
[GenerateSerializer]
public record BranchCreated([property: Id(0)]string Name) : IEventPayload;