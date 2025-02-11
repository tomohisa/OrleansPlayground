using System.Text.Json.Serialization;
using AspireEventSample.ApiService.Aggregates.Branches;
using AspireEventSample.ApiService.Aggregates.Carts;
using Sekiban.Pure.Events;

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(EventDocumentCommon))]
[JsonSerializable(typeof(EventDocumentCommon[]))]
[JsonSerializable(typeof(EventDocument<BranchCreated>))]
[JsonSerializable(typeof(BranchCreated))]
[JsonSerializable(typeof(EventDocument<BranchNameChanged>))]
[JsonSerializable(typeof(BranchNameChanged))]
[JsonSerializable(typeof(EventDocument<ShoppingCartCreated>))]
[JsonSerializable(typeof(ShoppingCartCreated))]
[JsonSerializable(typeof(EventDocument<ShoppingCartItemAdded>))]
[JsonSerializable(typeof(ShoppingCartItemAdded))]
[JsonSerializable(typeof(EventDocument<ShoppingCartPaymentProcessed>))]
[JsonSerializable(typeof(ShoppingCartPaymentProcessed))]
public partial class AspireEventSampleApiServiceEventsJsonContext : JsonSerializerContext
{
}
//
// public class AspireEventSampleApiServiceQueryTypes : IQueryTypes
// {
//     public Task<ResultBox<IQueryResult>> ExecuteAsQueryResult(
//         IQueryCommon query,
//         Func<IMultiProjectionEventSelector, Task<ResultBox<IMultiProjectorStateCommon>>> repositoryLoader)
//     {
//         return (query, repositoryLoader) switch
//         {
//             (BranchExistsQuery q,
//                 Func<IMultiProjectionEventSelector, Task<ResultBox<IMultiProjectorStateCommon>>> loader) =>
//                 new QueryExecutor().ExecuteAsQueryResult(q, selector => loader(selector)
//                     .Conveyor(MultiProjectionState<BranchMultiProjector>
//                         .FromCommon)),
//             _ => Task.FromResult(ResultBox<IQueryResult>.FromException(
//                 new SekibanQueryTypeException($"Unknown query type {query.GetType().Name}")))
//         };
//     }
//
//     public Task<ResultBox<IListQueryResult>> ExecuteAsQueryResult(
//         IListQueryCommon query,
//         Func<IMultiProjectionEventSelector, Task<ResultBox<IMultiProjectorStateCommon>>> repositoryLoader)
//     {
//         return (query, repositoryLoader) switch
//         {
//             (SimpleBranchListQuery q,
//                 Func<IMultiProjectionEventSelector, Task<ResultBox<IMultiProjectorStateCommon>>> loader) =>
//                 new QueryExecutor().ExecuteAsQueryResult(q, selector => loader(selector)
//                     .Conveyor(MultiProjectionState<BranchMultiProjector>
//                         .FromCommon)),
//             _ => Task.FromResult(ResultBox<IListQueryResult>.FromException(
//                 new SekibanQueryTypeException($"Unknown query type {query.GetType().Name} ")))
//         };
//     }
//
//     public ResultBox<IQueryResult> ToTypedQueryResult(QueryResultGeneral general)
//     {
//         return general.Query switch
//         {
//             BranchExistsQuery => new QueryResult<bool>((bool)general.Value),
//             _ => throw new SekibanQueryTypeException($"Unknown query type {general.Query.GetType().Name}")
//         };
//     }
// }