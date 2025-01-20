using Sekiban.Pure.Events;

namespace AspireEventSample.ApiService.Grains;
[GenerateSerializer]
public record BranchCreated([property: Id(0)]string Name) : IEventPayload;