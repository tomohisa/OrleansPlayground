using ResultBoxes;
using Sekiban.Pure.Aggregates;
using Sekiban.Pure.Command.Executor;
using Sekiban.Pure.Command.Handlers;
using Sekiban.Pure.Documents;
using Sekiban.Pure.Events;
using Sekiban.Pure.Projectors;
using Sekiban.Pure.Query;
using Sekiban.Pure.Repositories;
namespace Sekiban.Pure.Executors;

public class InMemorySekibanExecutor(
    DomainTypes domainTypes,
    ICommandMetadataProvider metadataProvider) : ISekibanExecutor
{
    private readonly CommandExecutor _commandExecutor = new()
        { EventTypes = domainTypes.EventTypes };

    public async Task<ResultBox<CommandResponse>> ExecuteCommandAsync(
        ICommandWithHandlerSerializable command,
        IEvent? relatedEvent = null)
    {
        var result = await _commandExecutor.ExecuteGeneralNonGeneric(
            command,
            command.GetProjector(),
            command.GetPartitionKeysSpecifier(),
            null,
            command.GetHandler(),
            command.GetAggregatePayloadType(),
            relatedEvent is null
                ? metadataProvider.GetMetadata()
                : metadataProvider.GetMetadataWithSubscribedEvent(relatedEvent));
        return result;
    }
    public async Task<ResultBox<TResult>> ExecuteQueryAsync<TResult>(IQueryCommon<TResult> queryCommon)
        where TResult : notnull
    {
        var projectorResult = domainTypes.QueryTypes.GetMultiProjector(queryCommon);
        if (projectorResult.IsSuccess)
        {
            var projector = projectorResult.GetValue();
            var events = Repository.Events;
            var projectionResult = events
                .ToResultBox()
                .ReduceEach(projector, (ev, proj) => domainTypes.MultiProjectorsType.Project(proj, ev));
            if (projectionResult.IsSuccess)
            {
                var projection = projectionResult.GetValue();
                var lastEvent = events.LastOrDefault();
                var multiProjectionState = new MultiProjectionState(
                    projection,
                    lastEvent?.Id ?? Guid.Empty,
                    lastEvent?.SortableUniqueId ?? "",
                    events.Count,
                    0,
                    lastEvent?.PartitionKeys.RootPartitionKey ?? "default");
                var typedMultiProjectionState = domainTypes.MultiProjectorsType.ToTypedState(multiProjectionState);
                var queryExecutor = new QueryExecutor();
                var queryResult = await domainTypes.QueryTypes.ExecuteAsQueryResult(
                    queryCommon,
                    selector => typedMultiProjectionState
                        .ToResultBox()
                        .ConveyorWrapTry(state => state)
                        .ToTask());
                return queryResult.ConveyorWrapTry(val => (TResult)val.GetValue());
            }
            return ResultBox<TResult>.Error(new ApplicationException("Projection failed"));
        }
        return ResultBox<TResult>.Error(new ApplicationException("Projector not found"));
    }
    public async Task<ResultBox<ListQueryResult<TResult>>> ExecuteQueryAsync<TResult>(
        IListQueryCommon<TResult> queryCommon)
        where TResult : notnull
    {
        var projectorResult = domainTypes.QueryTypes.GetMultiProjector(queryCommon);
        if (projectorResult.IsSuccess)
        {
            var projector = projectorResult.GetValue();
            var events = Repository.Events;
            var projectionResult = events
                .ToResultBox()
                .ReduceEach(projector, (ev, proj) => domainTypes.MultiProjectorsType.Project(proj, ev));
            if (projectionResult.IsSuccess)
            {
                var projection = projectionResult.GetValue();
                var lastEvent = events.LastOrDefault();
                var multiProjectionState = new MultiProjectionState(
                    projection,
                    lastEvent?.Id ?? Guid.Empty,
                    lastEvent?.SortableUniqueId ?? "",
                    events.Count,
                    0,
                    lastEvent?.PartitionKeys.RootPartitionKey ?? "default");
                var typedMultiProjectionState = domainTypes.MultiProjectorsType.ToTypedState(multiProjectionState);
                var queryExecutor = new QueryExecutor();
                var queryResult = await domainTypes.QueryTypes.ExecuteAsQueryResult(
                    queryCommon,
                    selector => typedMultiProjectionState
                        .ToResultBox()
                        .ConveyorWrapTry(state => state)
                        .ToTask());
                return queryResult.ConveyorWrapTry(val => (ListQueryResult<TResult>)val);
            }
            return ResultBox<ListQueryResult<TResult>>.Error(new ApplicationException("Projection failed"));
        }
        return ResultBox<ListQueryResult<TResult>>.Error(new ApplicationException("Projector not found"));
    }
    public Task<ResultBox<Aggregate>> LoadAggregateAsync<TAggregateProjector>(PartitionKeys partitionKeys)
        where TAggregateProjector : IAggregateProjector, new()
    {
        var events = Repository.Events.Where(x => x.PartitionKeys == partitionKeys).ToList();
        return Aggregate.EmptyFromPartitionKeys(partitionKeys).Project(events, new TAggregateProjector()).ToTask();
    }
}