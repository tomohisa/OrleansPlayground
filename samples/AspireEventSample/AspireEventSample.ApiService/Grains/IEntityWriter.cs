namespace AspireEventSample.ApiService.Grains;

using Sekiban.Pure.Documents;

public interface IEntityWriter<TEntity> where TEntity : IReadModelEntity
{
    Task<TEntity?> GetEntityByIdAsync(string rootPartitionKey, string aggregateGroup, Guid targetId);
    Task<List<TEntity>> GetHistoryEntityByIdAsync(string rootPartitionKey, string aggregateGroup, Guid targetId, SortableUniqueIdValue beforeUniqueId);
    Task<TEntity> AddOrUpdateEntityAsync(TEntity entity);
}
