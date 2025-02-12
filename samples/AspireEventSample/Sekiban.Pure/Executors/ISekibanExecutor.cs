using ResultBoxes;
using Sekiban.Pure.Command.Executor;
using Sekiban.Pure.Command.Handlers;
using Sekiban.Pure.Events;
using Sekiban.Pure.Projectors;
using Sekiban.Pure.Query;
using Sekiban.Pure.Repositories;
namespace Sekiban.Pure.Executors;

public interface ISekibanExecutor
{
    public Task<ResultBox<CommandResponse>> ExecuteCommandAsync(
        ICommandWithHandlerSerializable command,
        IEvent? relatedEvent = null);
    public Task<ResultBox<TResult>> ExecuteQueryAsync<TResult>(IQueryCommon<TResult> queryCommon)
        where TResult : notnull;
    public Task<ResultBox<ListQueryResult<TResult>>> ExecuteQueryAsync<TResult>(IListQueryCommon<TResult> queryCommon)
        where TResult : notnull;
}
public class InMemorySekibanExecutor(
    IEventTypes eventTypes,
    ICommandMetadataProvider metadataProvider,
    IQueryTypes queryTypes,
    IMultiProjectorsType multiProjectorsType) : ISekibanExecutor
{
    private readonly CommandExecutor _commandExecutor = new()
        { EventTypes = eventTypes };

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
        var projectorResult = queryTypes.GetMultiProjector(queryCommon);
        if (projectorResult.IsSuccess)
        {
            var projector = projectorResult.GetValue();
            var events = Repository.Events;
            var projectionResult = events
                .ToResultBox()
                .ReduceEach(projector, (ev, proj) => multiProjectorsType.Project(proj, ev));
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
                var typedMultiProjectionState = multiProjectorsType.ToTypedState(multiProjectionState);
                var queryExecutor = new QueryExecutor();
                var queryResult = await queryTypes.ExecuteAsQueryResult(
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
    public Task<ResultBox<ListQueryResult<TResult>>> ExecuteQueryAsync<TResult>(IListQueryCommon<TResult> queryCommon)
        where TResult : notnull => throw new NotImplementedException();
}