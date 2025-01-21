using Sekiban.Pure.Events;

namespace AspireEventSample.ApiService.Grains;

[GenerateSerializer]
public record BranchNameChanged(string Name) : IEventPayload;