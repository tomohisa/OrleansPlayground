using AspireEventSample.ApiService.Generated;
using ResultBoxes;
using Sekiban.Pure.Aggregates;
using Sekiban.Pure.Command.Executor;
using Sekiban.Pure.Command.Handlers;
using Sekiban.Pure.OrleansEventSourcing;
using Sekiban.Pure.Projectors;
using Sekiban.Pure.Repositories;
using Sekiban.Pure.Types;

namespace AspireEventSample.ApiService.Grains;

public class AggregateProjectorGrain(
    [PersistentState(stateName: "aggregate", storageName: "Default")] IPersistentState<Aggregate> state, SekibanTypeConverters typeConverters) : Grain, IAggregateProjectorGrain
{
    public async Task<OrleansAggregate> GetStateAsync()
    {
        var partitionKeysAndProjector = PartitionKeysAndProjector.FromGrainKey(this.GetPrimaryKeyString()).UnwrapBox();
        await state.ReadStateAsync();
        var read = state.State;
        if (read == null || partitionKeysAndProjector.Projector.GetVersion() != read.ProjectorVersion)
        {
            return await RebuildStateAsync();
        }
        return read.ToOrleansAggregate();
    }

    public Task<IAggregateProjector> GetProjectorAsync()
    {
        return Task.FromResult(PartitionKeysAndProjector.FromGrainKey(this.GetPrimaryKeyString()).UnwrapBox()
            .Projector);
    }

    public async Task<OrleansCommandResponse> ExecuteCommandAsync(ICommandWithHandlerSerializable orleansCommand)
    {
        var partitionKeysAndProjector = PartitionKeysAndProjector.FromGrainKey(this.GetPrimaryKeyString()).UnwrapBox();
        this.GetPrimaryKeyString();
        var commandExecutor = new CommandExecutor() {EventTypes = typeConverters.EventTypes };
        var result = await commandExecutor.ExecuteGeneralNonGeneric(orleansCommand, partitionKeysAndProjector.Projector, partitionKeysAndProjector.PartitionKeys, NoInjection.Empty, orleansCommand.GetHandler(), orleansCommand.GetAggregatePayloadType());
        var aggregate = Repository.Load(partitionKeysAndProjector.PartitionKeys, partitionKeysAndProjector.Projector).UnwrapBox();
        state.State = aggregate;
        await state.WriteStateAsync();
        return result.UnwrapBox().ToOrleansCommandResponse();
    }

    public async Task<OrleansAggregate> RebuildStateAsync()
    {
        var partitionKeysAndProjector = PartitionKeysAndProjector.FromGrainKey(this.GetPrimaryKeyString()).UnwrapBox();
        var state = Repository.Load(partitionKeysAndProjector.PartitionKeys, partitionKeysAndProjector.Projector).UnwrapBox();
        return await Task.FromResult(state.ToOrleansAggregate());
    }
}