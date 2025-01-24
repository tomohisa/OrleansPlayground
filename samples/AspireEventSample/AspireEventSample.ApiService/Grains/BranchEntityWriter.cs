namespace AspireEventSample.ApiService.Grains;

using System.Collections.Concurrent;
using AspireEventSample.ApiService.Aggregates.ReadModel;
using Orleans;
using Orleans.Runtime;
using Sekiban.Pure.Documents;

[GrainType("BranchEntityWriter")]
public class BranchEntityWriter : Grain, IBranchEntityWriter
{
    private readonly ConcurrentDictionary<Guid, BranchEntity> _entities = new();

    public Task<BranchEntity> GetEntityByIdAsync(Guid targetId)
    {
        return Task.FromResult(_entities.TryGetValue(targetId, out var entity) ? entity : null);
    }

    public Task<BranchEntity> GetHistoryEntityByIdAsync(Guid targetId, SortableUniqueIdValue beforeUniqueId)
    {
        // In a real implementation, this would query historical versions
        // For now, just return the current entity if it exists
        return Task.FromResult(_entities.TryGetValue(targetId, out var entity) ? entity : null);
    }

    public Task<BranchEntity> AddOrUpdateEntityAsync(BranchEntity entity)
    {
        _entities.AddOrUpdate(entity.TargetId, entity, (_, _) => entity);
        return Task.FromResult(entity);
    }
}
