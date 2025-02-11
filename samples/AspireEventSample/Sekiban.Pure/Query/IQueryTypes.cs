using ResultBoxes;
using Sekiban.Pure.Projectors;

namespace Sekiban.Pure.Query;

public interface IQueryTypes
{
    public Task<ResultBox<IQueryResult>> ExecuteAsQueryResult(
        IQueryCommon query,
        Func<IMultiProjectionEventSelector,
            Task<ResultBox<IMultiProjectorStateCommon>>> repositoryLoader);

    public Task<ResultBox<IListQueryResult>> ExecuteAsQueryResult(
        IListQueryCommon query,
        Func<IMultiProjectionEventSelector,
            Task<ResultBox<IMultiProjectorStateCommon>>> repositoryLoader);

    public ResultBox<IQueryResult> ToTypedQueryResult(QueryResultGeneral general);
    public ResultBox<IListQueryResult> ToTypedListQueryResult(ListQueryResultGeneral general);
}