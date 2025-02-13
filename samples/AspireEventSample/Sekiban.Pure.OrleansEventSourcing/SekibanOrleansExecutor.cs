using ResultBoxes;
using Sekiban.Pure.Aggregates;
using Sekiban.Pure.Command.Executor;
using Sekiban.Pure.Command.Handlers;
using Sekiban.Pure.Documents;
using Sekiban.Pure.Events;
using Sekiban.Pure.Executors;
using Sekiban.Pure.Projectors;
using Sekiban.Pure.Query;
namespace Sekiban.Pure.OrleansEventSourcing;

public class SekibanOrleansExecutor(
    IClusterClient clusterClient,
    SekibanDomainTypes sekibanDomainTypes,
    ICommandMetadataProvider metadataProvider) : ISekibanExecutor
{

    public SekibanDomainTypes GetDomainTypes() => sekibanDomainTypes;
    public async Task<ResultBox<CommandResponse>> ExecuteCommandAsync(
        ICommandWithHandlerSerializable command,
        IEvent? relatedEvent = null)
    {
        var partitionKeySpecifier = command.GetPartitionKeysSpecifier();
        var partitionKeys = partitionKeySpecifier.DynamicInvoke(command) as PartitionKeys;
        if (partitionKeys is null)
            return ResultBox<CommandResponse>.Error(new ApplicationException("Partition keys can not be found"));
        var projector = command.GetProjector();
        var partitionKeyAndProjector = new PartitionKeysAndProjector(partitionKeys, projector);
        var aggregateProjectorGrain =
            clusterClient.GetGrain<IAggregateProjectorGrain>(partitionKeyAndProjector.ToProjectorGrainKey());
        var toReturn = await aggregateProjectorGrain.ExecuteCommandAsync(
            command,
            OrleansCommandMetadata.FromCommandMetadata(metadataProvider.GetMetadata()));
        return toReturn.ToCommandResponse(sekibanDomainTypes.EventTypes);
    }
    public Task<ResultBox<TResult>> ExecuteQueryAsync<TResult>(IQueryCommon<TResult> queryCommon)
        where TResult : notnull
    {
        var projectorResult = sekibanDomainTypes.QueryTypes.GetMultiProjector(queryCommon);
        if (!projectorResult.IsSuccess)
            return Task.FromResult(ResultBox<TResult>.Error(new ApplicationException("Projector not found")));
        var nameResult
            = sekibanDomainTypes.MultiProjectorsType
                .GetMultiProjectorNameFromMultiProjector(projectorResult.GetValue());

        var multiProjectorGrain
            = clusterClient.GetGrain<IMultiProjectorGrain>(projectorResult.GetValue());
        var result = await multiProjectorGrain.QueryAsync(new BranchExistsQuery(nameContains));
        return queryTypes.ToTypedQueryResult(result.ToQueryResultGeneral()).;

    }
    public Task<ResultBox<ListQueryResult<TResult>>> ExecuteQueryAsync<TResult>(IListQueryCommon<TResult> queryCommon)
        where TResult : notnull => throw new NotImplementedException();
    public async Task<ResultBox<Aggregate>> LoadAggregateAsync<TAggregateProjector>(PartitionKeys partitionKeys)
        where TAggregateProjector : IAggregateProjector, new()
    {
        var partitionKeyAndProjector = new PartitionKeysAndProjector(partitionKeys, new TAggregateProjector());

        var aggregateProjectorGrain =
            clusterClient.GetGrain<IAggregateProjectorGrain>(partitionKeyAndProjector.ToProjectorGrainKey());
        var state = await aggregateProjectorGrain.GetStateAsync();
        return state.ToAggregate();
    }
}