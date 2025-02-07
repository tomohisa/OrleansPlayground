using Sekiban.Pure.Query;
namespace Sekiban.Pure.OrleansEventSourcing;

public interface IMultiProjectorGrain : IGrainWithStringKey
{
    Task RebuildStateAsync();
    Task BuildStateAsync();
    Task<OrleansMultiProjectorState> GetStateAsync();
    Task<IOrleansQueryResult> QueryAsync(IQueryCommon query);
}