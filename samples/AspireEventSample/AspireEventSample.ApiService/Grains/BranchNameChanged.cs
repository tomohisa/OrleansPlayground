using Sekiban.Pure.Events;

namespace AspireEventSample.ApiService.Grains;

public record BranchNameChanged(string Name) : IEventPayload;