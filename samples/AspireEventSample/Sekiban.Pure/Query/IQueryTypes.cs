using ResultBoxes;
using Sekiban.Pure.Projectors;

namespace Sekiban.Pure.Query;

public interface IQueryTypes
{

    public Task<ResultBox<IQueryResult>> ExecuteAsQueryResult<TMultiProjector>(
        IQueryCommon query,
        Func<IMultiProjectionEventSelector,
            ResultBox<MultiProjectionState<TMultiProjector>>> repositoryLoader) where TMultiProjector : IMultiProjector<TMultiProjector>;
            
    public Task<ResultBox<IListQueryResult>> ExecuteAsQueryResult<TMultiProjector>(
        IListQueryCommon query,
        Func<IMultiProjectionEventSelector,
            ResultBox<MultiProjectionState<TMultiProjector>>> repositoryLoader) where TMultiProjector : IMultiProjector<TMultiProjector>;

}