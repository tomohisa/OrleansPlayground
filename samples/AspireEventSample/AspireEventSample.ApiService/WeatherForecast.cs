using System.Text.Json;
using System.Text.Json.Serialization;
using AspireEventSample.ApiService.Aggregates.Carts;
using AspireEventSample.ApiService.Projections;
using ResultBoxes;
using Sekiban.Pure.Documents;
using Sekiban.Pure.Events;
using Sekiban.Pure.Extensions;
using Sekiban.Pure.Projectors;
using Sekiban.Pure.Query;

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(EventDocumentCommon))]
[JsonSerializable(typeof(EventDocumentCommon[]))]
[JsonSerializable(typeof(EventDocument<AspireEventSample.ApiService.Aggregates.Branches.BranchCreated>))]
[JsonSerializable(typeof(AspireEventSample.ApiService.Aggregates.Branches.BranchCreated))]
[JsonSerializable(typeof(EventDocument<AspireEventSample.ApiService.Aggregates.Branches.BranchNameChanged>))]
[JsonSerializable(typeof(AspireEventSample.ApiService.Aggregates.Branches.BranchNameChanged))]
[JsonSerializable(typeof(EventDocument<AspireEventSample.ApiService.Aggregates.Carts.ShoppingCartCreated>))]
[JsonSerializable(typeof(AspireEventSample.ApiService.Aggregates.Carts.ShoppingCartCreated))]
[JsonSerializable(typeof(EventDocument<AspireEventSample.ApiService.Aggregates.Carts.ShoppingCartItemAdded>))]
[JsonSerializable(typeof(AspireEventSample.ApiService.Aggregates.Carts.ShoppingCartItemAdded))]
[JsonSerializable(typeof(EventDocument<AspireEventSample.ApiService.Aggregates.Carts.ShoppingCartPaymentProcessed>))]
[JsonSerializable(typeof(AspireEventSample.ApiService.Aggregates.Carts.ShoppingCartPaymentProcessed))]
public partial class AspireEventSampleApiServiceEventsJsonContext : JsonSerializerContext
{
}

public static class QueryExecutorExtensions2
{
    public static Task<ResultBox<bool>> Execute(this QueryExecutor queryExecutor, AspireEventSample.ApiService.Projections.BranchExistsQuery query,  Func<IMultiProjectionEventSelector, ResultBox<MultiProjectionState<AspireEventSample.ApiService.Projections.BranchMultiProjector>>> repositoryLoader) =>
        queryExecutor.ExecuteWithMultiProjectionFunction<AspireEventSample.ApiService.Projections.BranchMultiProjector,AspireEventSample.ApiService.Projections.BranchExistsQuery,bool>(
            query,
            AspireEventSample.ApiService.Projections.BranchExistsQuery.HandleQuery, repositoryLoader);

    public static Task<ResultBox<IQueryResult>> ExecuteAsQueryResult(this QueryExecutor queryExecutor, AspireEventSample.ApiService.Projections.BranchExistsQuery query,  Func<IMultiProjectionEventSelector, ResultBox<MultiProjectionState<AspireEventSample.ApiService.Projections.BranchMultiProjector>>> repositoryLoader) =>
        queryExecutor.ExecuteWithMultiProjectionFunction<AspireEventSample.ApiService.Projections.BranchMultiProjector,AspireEventSample.ApiService.Projections.BranchExistsQuery,bool>(
            query,
            AspireEventSample.ApiService.Projections.BranchExistsQuery.HandleQuery, repositoryLoader).Remap(value => new QueryResult<bool>(value)).Remap(valueResult => (IQueryResult)valueResult);

    public static Task<ResultBox<ListQueryResult<AspireEventSample.ApiService.Projections.BranchMultiProjector.BranchRecord>>> Execute(this QueryExecutor queryExecutor, AspireEventSample.ApiService.Projections.SimpleBranchListQuery query, Func<IMultiProjectionEventSelector, ResultBox<MultiProjectionState<AspireEventSample.ApiService.Projections.BranchMultiProjector>>> repositoryLoader) =>
        queryExecutor.ExecuteListWithMultiProjectionFunction<AspireEventSample.ApiService.Projections.BranchMultiProjector,AspireEventSample.ApiService.Projections.SimpleBranchListQuery,AspireEventSample.ApiService.Projections.BranchMultiProjector.BranchRecord>(
            query,
            AspireEventSample.ApiService.Projections.SimpleBranchListQuery.HandleFilter,
            AspireEventSample.ApiService.Projections.SimpleBranchListQuery.HandleSort, repositoryLoader);

    public static Task<ResultBox<IListQueryResult>> ExecuteAsQueryResult(this QueryExecutor queryExecutor, AspireEventSample.ApiService.Projections.SimpleBranchListQuery query, Func<IMultiProjectionEventSelector, ResultBox<MultiProjectionState<AspireEventSample.ApiService.Projections.BranchMultiProjector>>> repositoryLoader) =>
        queryExecutor.ExecuteListWithMultiProjectionFunction<AspireEventSample.ApiService.Projections.BranchMultiProjector,AspireEventSample.ApiService.Projections.SimpleBranchListQuery,AspireEventSample.ApiService.Projections.BranchMultiProjector.BranchRecord>(
            query,
            AspireEventSample.ApiService.Projections.SimpleBranchListQuery.HandleFilter,
            AspireEventSample.ApiService.Projections.SimpleBranchListQuery.HandleSort, repositoryLoader).Remap(IListQueryResult (rs) => rs);
    

}

public class AspireEventSampleApiServiceQueryTypes : IQueryTypes
{
    public Task<ResultBox<IQueryResult>> ExecuteAsQueryResult<TMultiProjector>(IQueryCommon query,
        Func<IMultiProjectionEventSelector, ResultBox<MultiProjectionState<TMultiProjector>>> repositoryLoader)
        where TMultiProjector : IMultiProjector<TMultiProjector>
        => (query, repositoryLoader) switch
        {
            (AspireEventSample.ApiService.Projections.BranchExistsQuery branchExistsQuery, Func<IMultiProjectionEventSelector, ResultBox<MultiProjectionState<BranchMultiProjector>>> branchLoader) => new QueryExecutor()
                .ExecuteAsQueryResult(branchExistsQuery, branchLoader),
            _ => throw new NotImplementedException()
        };

    public Task<ResultBox<IListQueryResult>> ExecuteAsQueryResult<TMultiProjector>(IListQueryCommon query,
        Func<IMultiProjectionEventSelector, ResultBox<MultiProjectionState<TMultiProjector>>> repositoryLoader)
        where TMultiProjector : IMultiProjector<TMultiProjector>
        => (query, repositoryLoader) switch
        {
            (AspireEventSample.ApiService.Projections.SimpleBranchListQuery simpleBranchListQuery,
                Func<IMultiProjectionEventSelector, ResultBox<MultiProjectionState<BranchMultiProjector>>> branchLoader)
                => (new QueryExecutor()).ExecuteAsQueryResult(simpleBranchListQuery, branchLoader),
            _ => throw new NotImplementedException()
        };
}
