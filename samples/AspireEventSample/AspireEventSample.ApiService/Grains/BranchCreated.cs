using Sekiban.Pure.Events;

namespace AspireEventSample.ApiService.Grains;

public record BranchCreated(string Name) : IEventPayload;