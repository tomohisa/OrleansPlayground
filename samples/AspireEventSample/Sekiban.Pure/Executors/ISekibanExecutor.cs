using ResultBoxes;
using Sekiban.Pure.Command.Executor;
using Sekiban.Pure.Command.Handlers;
using Sekiban.Pure.Events;
using Sekiban.Pure.Projectors;
using Sekiban.Pure.Query;
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
    public Task<ResultBox<TResult>> ExecuteQueryAsync<TResult>(IQueryCommon<TResult> queryCommon)
        where TResult : notnull =>
        // if (queryCommon is IMultiprojector)
        // queryTypes.execut
        // var queryExecutor = new QueryExecutor();
        throw new NotImplementedException();
    public Task<ResultBox<ListQueryResult<TResult>>> ExecuteQueryAsync<TResult>(IListQueryCommon<TResult> queryCommon)
        where TResult : notnull => throw new NotImplementedException();
}