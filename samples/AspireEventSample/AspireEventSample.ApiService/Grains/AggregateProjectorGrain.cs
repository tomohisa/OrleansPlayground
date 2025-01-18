using ResultBoxes;
using Sekiban.Pure.Aggregates;
using Sekiban.Pure.Command.Executor;
using Sekiban.Pure.Command.Handlers;
using Sekiban.Pure.Repositories;

namespace AspireEventSample.ApiService.Grains;

public class AggregateProjectorGrain(
    [PersistentState(stateName: "aggregate", storageName: "projected")] IPersistentState<Aggregate> state) : Grain, IAggregateProjectorGrain
{
    public async Task<IAggregate> GetStateAsync()
    {
        var partitionKeysAndProjector = PartitionKeysAndProjector.FromGrainKey(this.GetPrimaryKeyString()).UnwrapBox();
        return Repository.Load(partitionKeysAndProjector.PartitionKeys, partitionKeysAndProjector.Projector).UnwrapBox();
    }
    public async Task<CommandResponse> ExecuteCommandAsync(ICommandWithHandlerSerializable command)
    {
        var partitionKeysAndProjector = PartitionKeysAndProjector.FromGrainKey(this.GetPrimaryKeyString()).UnwrapBox();
        this.GetPrimaryKeyString();
        var commandExecutor = new CommandExecutor();
        var result = await commandExecutor.ExecuteGeneralNonGeneric(command, partitionKeysAndProjector.Projector, partitionKeysAndProjector.PartitionKeys, NoInjection.Empty, command.GetHandler(), command.GetAggregatePayloadType());
        var aggregate = Repository.Load(partitionKeysAndProjector.PartitionKeys, partitionKeysAndProjector.Projector).UnwrapBox();
        state.State = aggregate;
        await state.WriteStateAsync();
        return result.UnwrapBox();
    }

    public async Task<IAggregatePayload> RebuildStateAsync()
    {
        throw new System.NotImplementedException();
    }
}