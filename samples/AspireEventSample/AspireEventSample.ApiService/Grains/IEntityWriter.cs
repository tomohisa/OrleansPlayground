namespace AspireEventSample.ApiService.Grains;

using Sekiban.Pure.Documents;

public interface IEntityWriter<TEntity> where TEntity : IReadModelEntity
{
    Task<TEntity> GetEntityByIdAsync(Guid targetId);
    Task<TEntity> GetHistoryEntityByIdAsync(Guid targetId, SortableUniqueIdValue beforeUniqueId);
    Task<TEntity> AddOrUpdateEntityAsync(TEntity entity);
}
