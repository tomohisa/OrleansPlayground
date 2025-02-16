using Sekiban.Pure.Projectors;
using Sekiban.Pure.Query;
namespace Sekiban.Pure.Orleans;

public interface IMultiProjectorGrain : IGrainWithStringKey
{
    Task RebuildStateAsync();
    Task BuildStateAsync();
    Task<MultiProjectionState> GetStateAsync();
    Task<OrleansQueryResultGeneral> QueryAsync(IQueryCommon query);
    Task<OrleansListQueryResultGeneral> QueryAsync(IListQueryCommon query);
}